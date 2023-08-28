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
    }
}