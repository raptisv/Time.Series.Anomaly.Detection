using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Graylog.Grafana.Web.Controllers
{
    public class MonitorSeriesController : Controller
    {
        private readonly IMonitorSeriesService _monitorSeriesService;

        public MonitorSeriesController(IMonitorSeriesService monitorSeriesService)
        {
            _monitorSeriesService = monitorSeriesService;
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
    }
}
