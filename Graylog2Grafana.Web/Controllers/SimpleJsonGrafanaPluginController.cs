using Graylog2Grafana.Abstractions;
using Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Request;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Threading.Tasks;

namespace Graylog2Grafana.Web.Controllers
{
    public class SimpleJsonGrafanaPluginController : Controller
    {
        private readonly IGrafanaSimpleJsonPluginService _grafanaSimpleJsonPluginService;

        public SimpleJsonGrafanaPluginController(
            ILogger logger,
            IGrafanaSimpleJsonPluginService grafanaSimpleJsonPluginService)
        {
            _grafanaSimpleJsonPluginService = grafanaSimpleJsonPluginService;
        }

        [HttpPost]
        [Route("search")]
        public async Task<JsonResult> Search([FromBody] SearchRequest request)
        {
            return Json(await _grafanaSimpleJsonPluginService.SearchAsync(request));
        }

        [HttpPost]
        [Route("query")]
        public async Task<JsonResult> Query([FromBody] TimeSeriesRequest request)
        {
            return Json(await _grafanaSimpleJsonPluginService.QueryAsync(request));
        }

        [HttpPost]
        [Route("annotations")]
        public async Task<JsonResult> Annotations([FromBody] AnnotationsRequest request)
        {
            return Json(await _grafanaSimpleJsonPluginService.AnnotationsAsync(request));
        }
    }
}
