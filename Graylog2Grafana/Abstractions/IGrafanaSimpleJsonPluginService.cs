using Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Request;
using Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Graylog2Grafana.Abstractions
{
    public interface IGrafanaSimpleJsonPluginService
    {
        Task<IEnumerable<AnnotationsResponse>> AnnotationsAsync(AnnotationsRequest request);
        Task<IEnumerable<TimeSiriesReponseTargetItem>> QueryAsync(TimeSeriesRequest request);
        Task<IEnumerable<string>> SearchAsync(SearchRequest request);
    }
}
