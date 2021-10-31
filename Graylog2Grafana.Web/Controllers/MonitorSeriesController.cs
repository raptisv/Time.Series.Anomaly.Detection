using Graylog2Grafana.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Graylog2Grafana.Web.Controllers
{
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
        public async Task<JsonResult> ExportDefinitions()
        {
            try
            {
                var result = await _monitorSeriesService.GetAllAsync();
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public async Task<JsonResult> ImportDefinitions([FromBody] MonitorSeries[] model)
        {
            try
            {
                foreach(var item in model)
                {
                    await _monitorSeriesService.CreateAsync(item);
                }
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }

            return Json("OK");
        }

        #endregion
    }
}
