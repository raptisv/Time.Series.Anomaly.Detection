using System;

namespace Time.Series.Anomaly.Detection.Models
{
    public struct PredictionResult
    {
        public double Data;
        public DateTime TimeStamp;
        public double Upper;
        public double Lower;
        public bool IsAnomaly;

        public long UnixTimestamp
        {
            get
            {
                return Utils.GetUnixTimestamp(TimeStamp);
            }
        }

        public PredictionResult(double data, DateTime timeStamp, double upper, double lower, bool isAnomaly)
        {
            Data = data;
            TimeStamp = timeStamp;
            Upper = upper;
            Lower = lower;
            IsAnomaly = isAnomaly;
        }
    }
}
