using Microsoft.ML;
using Microsoft.ML.TimeSeries;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using Time.Series.Anomaly.Detection.Abstractions;
using Time.Series.Anomaly.Detection.Models;

namespace Time.Series.Anomaly.Detection.Services
{
    public class AnomalyDetectionService : IAnomalyDetectionService
    {
        private readonly ILogger _logger;

        public AnomalyDetectionService(ILogger logger)
        {
            _logger = logger;
        }

        public (IEnumerable<RowDataItem> RowData, List<PredictionResult> Series) Detect(
            IEnumerable<RowDataItem> rowData,
            double sensitivity,
            double threshold = 0.1,
            bool detectPeriod = false)
        {
            var feedData = new List<InputData>(rowData.Select(x => new InputData() 
            { 
                t = Utils.GetUnixTimestamp(x.Timestamp).ToString(), 
                v = x.Count, 
                Timestamp = x.Timestamp 
            }))
            .OrderBy(x => x.t).ToList();

            MLContext mlContext = new MLContext(DateTime.UtcNow.Millisecond);

            IDataView dataView = mlContext.Data.LoadFromEnumerable(feedData);

            var period = detectPeriod ? DetectPeriod(mlContext, dataView) : 0;

            var detectionResult =  DetectAnomaly(mlContext, feedData, dataView, sensitivity, period,  threshold);

            return (rowData, detectionResult);
        }

        private int DetectPeriod(MLContext mlContext, IDataView inputData)
        {
            return mlContext.AnomalyDetection.DetectSeasonality(inputData, nameof(InputData.v));
        }

        private List<PredictionResult> DetectAnomaly(
            MLContext mlContext,
            IEnumerable<InputData> rowData,
            IDataView inputData,
            double sensitivity,
            int period,
            double threshold)
        {
            var options = new SrCnnEntireAnomalyDetectorOptions()
            {
                Threshold = threshold,
                Sensitivity = sensitivity,
                DetectMode = SrCnnDetectMode.AnomalyAndMargin,
                Period = period,
                BatchSize = -1,
                DeseasonalityMode = SrCnnDeseasonalityMode.Median // envoked only for period > 0
            };

            var outputDataView = mlContext.AnomalyDetection.DetectEntireAnomalyBySrCnn(inputData, nameof(DataPrediction.Prediction), nameof(InputData.v), options);
            var predictions = mlContext.Data.CreateEnumerable<DataPrediction>(outputDataView, reuseRowObject: false);
            var index = 0;

            List<PredictionResult> result = new List<PredictionResult>();

            foreach (var p in predictions)
            {
                var timeStamp = rowData.ElementAt(index).Timestamp;
                double data = rowData.ElementAt(index).v;
                double upper = p.Prediction[5];
                double lower = p.Prediction[6];
                bool isAnomaly = p.Prediction[0] == 1;

                result.Add(new PredictionResult(data, timeStamp, upper, lower, isAnomaly));

                ++index;
            }

            return result;
        }
    }
}
