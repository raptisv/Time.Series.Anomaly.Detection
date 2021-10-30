using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;

namespace Graylog.Grafana.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMonitorSeriesService _monitorSeriesService;

        public HomeController(IMonitorSeriesService monitorSeriesService)
        {
            _monitorSeriesService = monitorSeriesService;
        }

        public async Task<IActionResult> Index()
        {
            var monitorSeries = await _monitorSeriesService.GetAllAsync();

            return View(monitorSeries);
        }
    }
}
