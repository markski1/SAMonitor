using System;
using System.Globalization;
using System.Reflection;

namespace SAMPQuery 
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

        public static Uri ParseWeburl(string value)
        {
            Uri.TryCreate(value, UriKind.Absolute, out Uri? parsedUri);

            if (parsedUri is null)
                Uri.TryCreate("http://" + value, UriKind.Absolute, out parsedUri);

            parsedUri ??= new Uri("http://sa-mp.com/", UriKind.Absolute);

            return parsedUri;
        }

        public static DateTime ParseTime(string value)
        {
            bool success = TimeSpan.TryParse(value, out TimeSpan parsedTime);
            if (!success)
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