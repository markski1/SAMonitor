namespace SAMonitor.Utils
{
    public static class Helpers
    {
        public static string ValidateIPv4(string ip_addr)
        {
            var items = ip_addr.Split('.');
            if (items.Length != 4 || ip_addr.Any(x => char.IsLetter(x))) return "Not a valid IPv4 address."; ;

            items = ip_addr.Split(':');
            if (items.Length != 2) return "No port specified.";

            return "valid";
        }

        public static string BodgedEncodingFix(string text)
        {

            text = text.Replace('с', 'ñ');
            text = text.Replace('к', 'ê');
            text = text.Replace('Ў', '¡');
            text = text.Replace('У', 'Ó');
            text = text.Replace('б', 'á');
            return text;
        }
    }
}