using Graylog2Grafana.Models.SimpleJsonGrafanaPlugin.Request;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Graylog2Grafana.Abstractions
{
    public interface IGrafanaSimpleJsonPluginService
    {
        Task<IEnumerable<AnnotationsResponse>> AnnotationsAsync(AnnotationsRequest request);
        Task<IEnumerable<object>> QueryAsync(TimeSeriesRequest request);
        Task<IEnumerable<string>> SearchAsync(SearchRequest request);
    }
}
