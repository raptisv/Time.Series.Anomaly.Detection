using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models;

namespace Graylog2Grafana.Web.Controllers
{
    [Authorize]
    public class MonitorGroupsController : Controller
    {
        private readonly IMonitorGroupsService _monitorGroupsService;

        public MonitorGroupsController(
            IMonitorGroupsService monitorGroupsService)
        {
            _monitorGroupsService = monitorGroupsService;
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new MonitorGroups()
            {
                ID = 0
            };

            return View("AddEdit", model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MonitorGroups model)
        {
            ModelState.Remove(nameof(MonitorGroups.ID));

            if (!ModelState.IsValid)
            {
                return View("AddEdit", model);
            }

            try
            {
                await _monitorGroupsService.CreateAsync(model);
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
            var item = await _monitorGroupsService.GetByIdAsync(id);

            return View("AddEdit", item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MonitorGroups model)
        {
            if (!ModelState.IsValid)
            {
                return View("AddEdit", model);
            }

            try
            {
                await _monitorGroupsService.UpdateAsync(model);
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
            var item = await _monitorGroupsService.GetByIdAsync(id);

            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(MonitorGroups model)
        {
            try
            {
                await _monitorGroupsService.DeleteAsync(model);
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
