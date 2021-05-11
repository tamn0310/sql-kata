using System;

namespace Product.API.Infrastuctures.Providers
{
    public class DateTimeConvert
    {
        /// <summary>
        /// convert to seconds since 1970-01-01
        /// </summary>
        /// <param name="dt">a datetime</param>
        /// <returns></returns>
        public long ToSecondsUnix(DateTime dt)
        {
            return (long)(dt - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        /// <summary>
        /// convert seconds since 1970-01-01 to datetime
        /// </summary>
        /// <param name="time">seconds counted since 1970-01-01</param>
        /// <returns></returns>
        public DateTime ToDateTimeUnix(int time)
        {
            return new DateTime(1970, 1, 1).AddSeconds(time);
        }
    }
}