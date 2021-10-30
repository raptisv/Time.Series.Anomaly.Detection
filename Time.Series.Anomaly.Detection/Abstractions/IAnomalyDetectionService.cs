using System.Collections.Generic;
using Time.Series.Anomaly.Detection.Models;

namespace Time.Series.Anomaly.Detection.Abstractions
{
    public interface IAnomalyDetectionService
    {
        (IEnumerable<RowDataItem> RowData, List<PredictionResult> Series) Detect(
            IEnumerable<RowDataItem> rowData,
            double sensitivity,
            double threshold = 0.1,
            bool detectPeriod = false);
    }
}
