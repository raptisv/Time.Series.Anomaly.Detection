using Graylog2Grafana.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Graylog2Grafana.Web.Controllers
{
    [Authorize]
    public class MonitorSeriesController : Controller
    {
        private readonly IMonitorSeriesService _monitorSeriesService;
        private readonly IMonitorSourcesService _monitorSourcesService;
        private readonly IMonitorGroupsService _monitorGroupsService; 
        private readonly IMonitorSeriesDataService _monitorSeriesDataService;
        private readonly IMonitorSeriesDataAnomalyDetectionService _monitorSeriesDataAnomalyDetectionService;
        private readonly IServiceScopeFactory _scopeFactory;

        public MonitorSeriesController(
            IMonitorSeriesService monitorSeriesService,
            IMonitorSourcesService monitorSourcesService,
            IMonitorSeriesDataService monitorSeriesDataService,
            IMonitorSeriesDataAnomalyDetectionService monitorSeriesDataAnomalyDetectionService,
            ApplicationDbContext dbContext,
            IServiceScopeFactory scopeFactory,
            IMonitorGroupsService monitorGroupsService)
        {
            _monitorSeriesService = monitorSeriesService;
            _monitorGroupsService = monitorGroupsService;
            _monitorSourcesService = monitorSourcesService;
            _monitorSeriesDataService = monitorSeriesDataService;
            _monitorSeriesDataAnomalyDetectionService = monitorSeriesDataAnomalyDetectionService;
            _scopeFactory = scopeFactory;
        }

        [HttpGet]
        public IActionResult Create(int group)
        {
            var model = new MonitorSeries()
            {
                ID = 0,
                MinuteDurationForAnomalyDetection = 60,
                Sensitivity = 50,
                MonitorGroupID = group
            };

            return View("AddEdit", model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MonitorSeries model)
        {
            ModelState.Remove(nameof(MonitorSeries.ID));

            if (!ModelState.IsValid)
            {
                return View("AddEdit", model);
            }

            try
            {
                await _monitorSeriesService.CreateAsync(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("CustomError", ex.Message);
                return View("AddEdit", model);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var item = await _monitorSeriesService.GetByIdAsync(id);

            return View("AddEdit", item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MonitorSeries model)
        {
            if (!ModelState.IsValid)
            {
                return View("AddEdit", model);
            }

            try
            {
                await _monitorSeriesService.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("CustomError", ex.Message);
                return View("AddEdit", model);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            var item = await _monitorSeriesService.GetByIdAsync(id);

            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(MonitorSeries model)
        {
            try
            {
                await _monitorSeriesService.DeleteAsync(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("CustomError", ex.Message);
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Preview(long id, string groupValue)
        {
            var monitorSeries = await _monitorSeriesService.GetByIdAsync(id);

            return View((monitorSeries, groupValue));
        }

        [HttpPut]
        public async Task<IActionResult> SetSensitivity(long id, int sensitivity)
        {
            await _monitorSeriesService.UpdateSensitivityAsync(id, sensitivity);

            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteData(long id, string groupValue)
        {
            await _monitorSeriesService.DeleteDataAsync(id, groupValue);

            return Ok();
        }

        [HttpGet]
        public async Task<JsonResult> DetectionResult(long id, string groupValue)
        {
            var monitorSeries = await _monitorSeriesService.GetByIdAsync(id);

            var monitorSeriesData = await _monitorSeriesDataService.GetLatestAsync(monitorSeries.MinuteDurationForAnomalyDetection, monitorSeries.ID, groupValue, null);

            var anomlyDetectionResult = _monitorSeriesDataAnomalyDetectionService.DetectAnomalies(monitorSeries, monitorSeriesData);

            return Json(anomlyDetectionResult?.PredictionResult);
        }

        #region Import export definitions

        [HttpGet]
        public async Task<IActionResult> ExportDefinitions()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var allSeries = await _dbContext.MonitorSeries
                                .Include(x => x.MonitorSource)
                                .ToListAsync();

                var result = new ImportExportObject()
                {
                    Series = allSeries
                };

                var strResult = JsonConvert.SerializeObject(result, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(strResult);

                var content = new MemoryStream(bytes);
                return File(content, "application/json", "Graylog2Grafana_definitions.json");
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ImportDefinitions([FromBody] ImportExportObject model)
        {
            try
            {
                if (model == null )
                {
                    throw new Exception("Post body cannot be empty");
                }

                if (model.Series == null || !model.Series.Any())
                {
                    throw new Exception("Series cannot be empty");
                }

                var existingSources = await _monitorSourcesService.GetAllAsync();
                var existingGroups = await _monitorGroupsService.GetAllAsync();
                var existingSeries = await _monitorSeriesService.GetAllAsync();

                if (!existingSources.Any())
                {
                    throw new Exception($"At least one source must be present!");
                }

                if (!existingGroups.Any())
                {
                    throw new Exception($"At least one group must be present!");
                }

                using var scope = _scopeFactory.CreateScope();
                using var _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                using (var transaction = _dbContext.Database.BeginTransaction())
                {

                    for(int i=0;i< model.Series.Count;i++)
                    {
                        var incoming = model.Series[i];
                        // Set the ID
                        incoming.ID = existingSeries.Select(s => s.ID).DefaultIfEmpty(0).Max() + i + 1;
                        incoming.Name = incoming.Name.Trim().Replace(" ", "");
                        if (incoming.MonitorSourceID > 0)
                        {
                            // If source does not exist
                            if (!existingSources.Any(s => s.ID == incoming.MonitorSourceID))
                            {
                                throw new Exception($"Source with ID {incoming.MonitorSourceID} not found!");
                            }
                        }
                        else
                        {
                            // If source not set, take the first
                            incoming.MonitorSourceID = existingSources.First().ID;
                        }

                        if (incoming.MonitorGroupID > 0)
                        {
                            // If source does not exist
                            if (!existingGroups.Any(s => s.ID == incoming.MonitorGroupID))
                            {
                                throw new Exception($"Group with ID {incoming.MonitorGroupID} not found!");
                            }
                        }
                        else
                        {
                            // If source not set, take the first
                            incoming.MonitorGroupID = existingGroups.First().ID;
                        }

                        if (existingSeries.Any(x => x.Name.Trim().ToLower().Equals(incoming.Name.Trim().ToLower())))
                        {
                            throw new Exception($"A definition with name '{incoming.Name.Trim()}' already exists");
                        }

                        _dbContext.MonitorSeries.Add(incoming);

                        await _dbContext.SaveChangesAsync();
                    }

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.InnerException is null ? ex.Message : ex.InnerException.Message  });
            }

            return Ok();
        }

        public class ImportExportObject
        {
            public List<MonitorSeries> Series { get; set; }
        }

        #endregion
    }
}
