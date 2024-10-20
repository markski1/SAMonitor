using System;
using System.Globalization;
using System.Reflection;

namespace SAMPQuery.Utils 
{
    internal static class Helpers
    {
        public static void CheckNullOrEmpty(string value, string parameterName)
        {
            if (value == null)
                throw new ArgumentNullException(parameterName);
        
            if (value.Length == 0)
                throw new ArgumentException("Empty value not allowed", parameterName);
        }

        public static Uri ParseWebUrl(string value)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var parsedUri))
            {
                return parsedUri;
            }

            return Uri.TryCreate(value, UriKind.Absolute, out parsedUri) ? parsedUri : new Uri("http://sa-mp.com/", UriKind.Absolute);
        }

        public static DateTime ParseTime(string value)
        {
            if (!TimeSpan.TryParse(value, out var parsedTime))
            {
                parsedTime = TimeSpan.FromHours(0);
            }

            return DateTime.Today.Add(parsedTime);
        }

        public static object TryParseByte(string value, PropertyInfo property)
        {
            try
            {
                return Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture);
            }
            catch
            {
                // the value could not be parsed, try to return anything at all instead of crashing.
                value = "0";
                return Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture);
            }
        }
    }
}