using System;

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

        public static Uri TryParseWeburl(string value)
        {
            try
            {
                // First try value alone, because some servers include http/s in their Weburl
                return new Uri(value, UriKind.Absolute);
            }
            catch
            {
                try
                {
                    // If that fails, then try to parse with http
                    return new Uri("http://" + value, UriKind.Absolute);
                }
                catch
                {
                    // If that fails, then just return sa-mp.com because we can't return nothing
                    return new Uri("http://sa-mp.com/", UriKind.Absolute);
                }
            }
        }
    }
}