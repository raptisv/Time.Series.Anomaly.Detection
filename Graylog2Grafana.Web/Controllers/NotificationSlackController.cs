using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;
using Time.Series.Anomaly.Detection.Data.Models.Notifications;

namespace Graylog2Grafana.Web.Controllers
{
    [Authorize]
    public class NotificationSlackController : Controller
    {
        private readonly INotificationSlackService _slackNotificationService;

        public NotificationSlackController(
            INotificationSlackService slackNotificationService)
        {
            _slackNotificationService = slackNotificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var item = await _slackNotificationService.GetAsync();

            return View("AddEdit", item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(NotificationSlack model)
        {
            if (!ModelState.IsValid)
            {
                return View("AddEdit", model);
            }

            try
            {
                await _slackNotificationService.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("CustomError", ex.Message);
                return View("AddEdit", model);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
