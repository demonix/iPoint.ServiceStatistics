using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AggregationEx
{
    public static class DateExtensions
    {
        public static DateTime RoundTo(this DateTime dt, TimeSpan roundinterval)
        {
            return roundinterval == TimeSpan.Zero ? dt : dt.AddTicks(-dt.Ticks % roundinterval.Ticks);
        }
    }
}
