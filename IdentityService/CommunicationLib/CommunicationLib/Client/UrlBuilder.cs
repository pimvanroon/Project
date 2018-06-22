using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Communication.Client
{
    /// <summary>
    /// Convenience class to build an URL with its query parameters
    /// Contains a lot over <c>Add</c>-overloads to format primitive types correctly.
    /// </summary>
    public class UrlBuilder
    {
        private readonly Uri baseUri;
        private readonly StringBuilder sb = new StringBuilder();
        private readonly string path;
        private static readonly CultureInfo ci = CultureInfo.InvariantCulture;

        public UrlBuilder(Uri baseUri, string path)
        {
            this.baseUri = baseUri;
            this.path = path;
        }

        private StringBuilder AddKey(string key)
        {
            sb.Append(sb.Length == 0 ? '?' : '&').Append(key).Append('=');
            return sb;
        }

        public UrlBuilder Add(string key, bool value)
        {
            AddKey(key).Append(value.ToString(ci));
            return this;
        }

        public UrlBuilder Add(string key, byte value)
        {
            AddKey(key).Append(value.ToString(ci));
            return this;
        }

        public UrlBuilder Add(string key, int value)
        {
            AddKey(key).Append(value.ToString(ci));
            return this;
        }

        public UrlBuilder Add(string key, long value)
        {
            AddKey(key).Append(value.ToString(ci));
            return this;
        }

        public UrlBuilder Add(string key, float value)
        {
            AddKey(key).Append(value.ToString(ci));
            return this;
        }

        public UrlBuilder Add(string key, double value)
        {
            AddKey(key).Append(value.ToString(ci));
            return this;
        }

        public UrlBuilder Add(string key, decimal value)
        {
            AddKey(key).Append(value.ToString(ci));
            return this;
        }

        public UrlBuilder Add(string key, string value)
        {
            AddKey(key).Append(WebUtility.UrlEncode(value));
            return this;
        }

        public UrlBuilder Add(string key, Guid value)
        {
            AddKey(key).Append(value.ToString());
            return this;
        }

        public UrlBuilder Add(string key, DateTime value)
        {
            AddKey(key).Append(value.ToString("s", ci));
            return this;
        }

        public UrlBuilder Add(string key, DateTimeOffset value)
        {
            AddKey(key).Append(value.ToString("s", ci));
            return this;
        }

        public UrlBuilder Add(string key, TimeSpan value)
        {
            AddKey(key).Append(value.ToString("c", ci));
            return this;
        }

        public UrlBuilder Add(string key, bool? value)
        {
            if (value.HasValue) Add(key, value.Value); else AddKey(key);
            return this;
        }

        public UrlBuilder Add(string key, byte? value)
        {
            if (value.HasValue) Add(key, value.Value); else AddKey(key);
            return this;
        }

        public UrlBuilder Add(string key, int? value)
        {
            if (value.HasValue) Add(key, value.Value); else AddKey(key);
            return this;
        }

        public UrlBuilder Add(string key, long? value)
        {
            if (value.HasValue) Add(key, value.Value); else AddKey(key);
            return this;
        }

        public UrlBuilder Add(string key, float? value)
        {
            if (value.HasValue) Add(key, value.Value); else AddKey(key);
            return this;
        }

        public UrlBuilder Add(string key, double? value)
        {
            if (value.HasValue) Add(key, value.Value); else AddKey(key);
            return this;
        }

        public UrlBuilder Add(string key, decimal? value)
        {
            if (value.HasValue) Add(key, value.Value); else AddKey(key);
            return this;
        }

        public UrlBuilder Add(string key, Guid? value)
        {
            if (value.HasValue) Add(key, value.Value); else AddKey(key);
            return this;
        }

        public UrlBuilder Add(string key, DateTime? value)
        {
            if (value.HasValue) Add(key, value.Value); else AddKey(key);
            return this;
        }

        public UrlBuilder Add(string key, DateTimeOffset? value)
        {
            if (value.HasValue) Add(key, value.Value); else AddKey(key);
            return this;
        }

        public UrlBuilder Add(string key, TimeSpan? value)
        {
            if (value.HasValue) Add(key, value.Value); else AddKey(key);
            return this;
        }

        public override string ToString()
        {
            return sb.ToString();
        }

        public Uri Uri
        {
            get
            {
                return new Uri(baseUri, path + sb.ToString());
            }
        }
    }
}
