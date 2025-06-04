using System;

namespace WebApplicationCentralino.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool TryParseWithYearHandling(string? dateString, out DateTime result)
        {
            if (string.IsNullOrEmpty(dateString))
            {
                result = DateTime.MinValue;
                return false;
            }

            // Se la stringa contiene l'anno 0000, sostituiscilo con l'anno corrente
            if (dateString.Contains("-0000-"))
            {
                dateString = dateString.Replace("-0000-", $"-{DateTime.Now.Year}-");
            }

            return DateTime.TryParse(dateString, out result);
        }
    }
} 