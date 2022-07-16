using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Graylog2Grafana.Web.Controllers
{
    [Authorize]
    public class MonitorSourcesController : Controller
    {
        private readonly IMonitorSourcesService _monitorSourcesService;

        public MonitorSourcesController(
            IMonitorSourcesService monitorSourcesService)
        {
            _monitorSourcesService = monitorSourcesService;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View("AddEdit", null);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MonitorSources model)
        {
            ModelState.Remove(nameof(MonitorSources.ID));

            if (!ModelState.IsValid)
            {
                return View("AddEdit", model);
            }

            try
            {
                await _monitorSourcesService.CreateAsync(model);
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
            var item = await _monitorSourcesService.GetByIdAsync(id);

            return View("AddEdit", item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MonitorSources model)
        {
            if (!ModelState.IsValid)
            {
                return View("AddEdit", model);
            }

            try
            {
                await _monitorSourcesService.UpdateAsync(model);
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
            var item = await _monitorSourcesService.GetByIdAsync(id);

            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(MonitorSources model)
        {
            try
            {
                await _monitorSourcesService.DeleteAsync(model);
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
