using System;
using System.Linq;

namespace Time.Series.Anomaly.Detection.Models
{
    public class Utils
    {
        public static long GetUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date - origin;
            return (long)Math.Floor(diff.TotalSeconds);
        }

        public static long GetUnixTimestampMilliseconds(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date - origin;
            return (long)diff.TotalMilliseconds;
        }

        public static DateTime TruncateToMinute(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, 0);
        }

        public static DateTime? GetDateFromUnixTimestamp(string unixTime)
        {
            unixTime ??= string.Empty;

            if (unixTime.All(char.IsDigit))
            {
                if (long.TryParse(unixTime, out long number) && number >= 0)
                {
                    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    switch (unixTime.Length)
                    {
                        case 10:
                            return epoch.AddSeconds(number);
                        case 13:
                            return epoch.AddMilliseconds(number);
                    }
                }
            }

            return null;
        }
    }
}
