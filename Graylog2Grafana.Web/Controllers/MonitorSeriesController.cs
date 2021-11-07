using Graylog2Grafana.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Graylog2Grafana.Web.Controllers
{
    [Authorize]
    public class MonitorSeriesController : Controller
    {
        private readonly IMonitorSeriesService _monitorSeriesService;
        private readonly IMonitorSeriesDataService _monitorSeriesDataService;
        private readonly IMonitorSeriesDataAnomalyDetectionService _monitorSeriesDataAnomalyDetectionService;

        public MonitorSeriesController(
            IMonitorSeriesService monitorSeriesService, 
            IMonitorSeriesDataService monitorSeriesDataService,
            IMonitorSeriesDataAnomalyDetectionService monitorSeriesDataAnomalyDetectionService)
        {
            _monitorSeriesService = monitorSeriesService;
            _monitorSeriesDataService = monitorSeriesDataService;
            _monitorSeriesDataAnomalyDetectionService = monitorSeriesDataAnomalyDetectionService;
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

        [HttpGet]
        public async Task<JsonResult> DetectionResult(long id)
        {
            var monitorSeries = await _monitorSeriesService.GetByIdAsync(id);

            var monitorSeriesData = await _monitorSeriesDataService.GetLatestAsync(monitorSeries.MinuteDurationForAnomalyDetection, monitorSeries.ID);

            var anomlyDetectionResult = _monitorSeriesDataAnomalyDetectionService.DetectAnomaliesAsync(monitorSeries, monitorSeriesData);

            return Json(anomlyDetectionResult?.PredictionResult);
        }

        #region Import export definitions

        [HttpGet]
        public async Task<IActionResult> ExportDefinitions()
        {
            try
            {
                var result = await _monitorSeriesService.GetAllAsync();
                var strResult = JsonConvert.SerializeObject(result, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
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
        public async Task<IActionResult> ImportDefinitions([FromBody] MonitorSeries[] model)
        {
            try
            {
                if (model == null)
                {
                    throw new Exception("Post body cannot be empty");
                }

                foreach (var item in model)
                {
                    await _monitorSeriesService.CreateAsync(item);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }

            return Ok();
        }

        #endregion
    }
}
