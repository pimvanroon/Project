namespace Communication
{
    internal static class StringExtensions
    {
        public static string ToCamelCase(this string value)
        {
            if (value == null || value.Length < 1) return value;
            if (value.Length < 2) return value.Substring(0, 1).ToLower();
            return value.Substring(0, 1).ToLower() + value.Substring(1);
        }
    }
}
