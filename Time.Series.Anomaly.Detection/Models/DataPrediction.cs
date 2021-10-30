using Microsoft.ML.Data;

namespace Time.Series.Anomaly.Detection.Models
{
    public class DataPrediction
    {
        //vector to hold anomaly detection results. Including isAnomaly, anomalyScore, magnitude, expectedValue, boundaryUnits, upperBoundary and lowerBoundary.
        [VectorType(7)]
        public double[] Prediction { get; set; }
    }
}
