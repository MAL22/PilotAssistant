using System.Globalization;

namespace PilotAssistant
{
    public static class WeightFormatter
    {
        private const float KilogramThreshold = 1000000f;

        public static string Format(float kilograms)
        {
            return Format(kilograms, "N2");
        }

        public static string Format(float kilograms, string numericFormat)
        {
            var useKilotons = kilograms >= KilogramThreshold;
            var value = useKilotons ? kilograms / KilogramThreshold : kilograms;
            var unit = useKilotons ? "kt" : "kg";
            var format = string.IsNullOrWhiteSpace(numericFormat) ? "N2" : numericFormat;

            return string.Format(CultureInfo.InvariantCulture, "{0:" + format + "} {1}", value, unit);
        }
    }
}
