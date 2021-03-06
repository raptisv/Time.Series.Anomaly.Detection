using Graylog2Grafana.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IMonitorSeriesDataService _monitorSeriesDataService;
        private readonly IMonitorSeriesDataAnomalyDetectionService _monitorSeriesDataAnomalyDetectionService;
        private readonly ApplicationDbContext _dbContext;

        public MonitorSeriesController(
            IMonitorSeriesService monitorSeriesService,
            IMonitorSourcesService monitorSourcesService,
            IMonitorSeriesDataService monitorSeriesDataService,
            IMonitorSeriesDataAnomalyDetectionService monitorSeriesDataAnomalyDetectionService,
            ApplicationDbContext dbContext)
        {
            _monitorSeriesService = monitorSeriesService;
            _monitorSourcesService = monitorSourcesService;
            _monitorSeriesDataService = monitorSeriesDataService;
            _monitorSeriesDataAnomalyDetectionService = monitorSeriesDataAnomalyDetectionService;
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("AddEdit", null);
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
        public async Task<IActionResult> Preview(long id)
        {
            var monitorSeries = await _monitorSeriesService.GetByIdAsync(id);

            return View(monitorSeries);
        }

        [HttpPut]
        public async Task<IActionResult> SetSensitivity(long id, int sensitivity)
        {
            await _monitorSeriesService.UpdateSensitivityAsync(id, sensitivity);

            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteData(long id)
        {
            await _monitorSeriesService.DeleteDataAsync(id);

            return Ok();
        }

        [HttpGet]
        public async Task<JsonResult> DetectionResult(long id)
        {
            var monitorSeries = await _monitorSeriesService.GetByIdAsync(id);

            var monitorSeriesData = await _monitorSeriesDataService.GetLatestAsync(monitorSeries.MinuteDurationForAnomalyDetection, monitorSeries.ID);

            var anomlyDetectionResult = _monitorSeriesDataAnomalyDetectionService.DetectAnomalies(monitorSeries, monitorSeriesData);

            return Json(anomlyDetectionResult?.PredictionResult);
        }

        #region Import export definitions

        [HttpGet]
        public async Task<IActionResult> ExportDefinitions()
        {
            try
            {
                var allSources = await _monitorSourcesService.GetAllAsync();
                var allSeries = await _monitorSeriesService.GetAllAsync();

                var result = new ImportExportObject()
                {
                    Sources = allSources,
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
                if (model == null)
                {
                    throw new Exception("Post body cannot be empty");
                }

                using (var transaction = _dbContext.Database.BeginTransaction())
                {
                    var existingSources = await _monitorSourcesService.GetAllAsync();
                    foreach (var incoming in model.Sources)
                    {
                        if (!existingSources.Any(x => x.ID == incoming.ID))
                        {
                            await _monitorSourcesService.CreateAsync(incoming);
                        }
                    }

                    var existingSeries = await _monitorSeriesService.GetAllAsync();
                    foreach (var incoming in model.Series)
                    {
                        if (!existingSeries.Any(x => x.ID == incoming.ID))
                        {
                            await _monitorSeriesService.CreateAsync(incoming);
                        }
                    }

                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }

            return Ok();
        }

        public class ImportExportObject
        {
            public List<MonitorSources> Sources { get; set; }
            public List<MonitorSeries> Series { get; set; }
        }

        #endregion
    }
}
