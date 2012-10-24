using System;
using System.Globalization;

namespace CountersDataLayer
{
    public class Helpers
    {
        public static DateTime ParseDate(string dateString, DateTime defaultDate)
        {
            DateTime date;
            if (!DateTime.TryParseExact(dateString, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                date = defaultDate;
            return date;
        }
         
    }
}