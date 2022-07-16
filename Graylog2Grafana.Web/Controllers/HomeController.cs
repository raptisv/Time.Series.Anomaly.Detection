using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;

namespace Graylog2Grafana.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IMonitorSourcesService _monitorSourcesService;
        private readonly IMonitorSeriesService _monitorSeriesService;

        public HomeController(
            IMonitorSourcesService monitorSourcesService,
            IMonitorSeriesService monitorSeriesService)
        {
            _monitorSourcesService = monitorSourcesService;
            _monitorSeriesService = monitorSeriesService;
        }

        public async Task<IActionResult> Index()
        {
            var monitorSources = await _monitorSourcesService.GetAllAsync();
            var monitorSeries = await _monitorSeriesService.GetAllAsync();

            return View((monitorSources, monitorSeries));
        }
    }
}
