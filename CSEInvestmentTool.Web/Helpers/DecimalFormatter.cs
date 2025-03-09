using System.Globalization;

namespace CSEInvestmentTool.Web.Helpers
{
    public static class DecimalFormatter
    {
        /// <summary>
        /// Formats a decimal value to two decimal places for display
        /// </summary>
        public static string ToTwoDecimalPlaces(this decimal value)
        {
            return value.ToString("F2", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formats a large number (like liabilities or equity) for display with K/M/B suffix
        /// </summary>
        public static string ToShortForm(this decimal value)
        {
            if (value == 0)
                return "0";

            string[] suffixes = { "", "K", "M", "B", "T" };
            int suffixIndex = 0;

            decimal absValue = Math.Abs(value);
            while (absValue >= 1000 && suffixIndex < suffixes.Length - 1)
            {
                absValue /= 1000;
                suffixIndex++;
            }

            return $"{(value < 0 ? "-" : "")}{absValue.ToString("F2")} {suffixes[suffixIndex]}";
        }

        /// <summary>
        /// Parses a shortform string (like "1.5M") back to a decimal
        /// </summary>
        public static decimal ParseShortForm(string shortForm)
        {
            if (string.IsNullOrWhiteSpace(shortForm))
                return 0;

            shortForm = shortForm.Trim();

            // If already a number, just parse it
            if (decimal.TryParse(shortForm, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;

            // Otherwise, check for suffix
            char suffix = char.ToUpper(shortForm[shortForm.Length - 1]);
            if (!char.IsLetter(suffix))
                return 0;

            // Extract the number part
            string numberPart = shortForm.Substring(0, shortForm.Length - 1).Trim();
            if (!decimal.TryParse(numberPart, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal number))
                return 0;

            // Apply multiplier based on suffix
            return suffix switch
            {
                'K' => number * 1_000,
                'M' => number * 1_000_000,
                'B' => number * 1_000_000_000,
                'T' => number * 1_000_000_000_000,
                _ => number
            };
        }
    }
}