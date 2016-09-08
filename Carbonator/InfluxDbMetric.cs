using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypton.Carbonator
{
    public struct InfluxDbMetric
    {
        NameValueCollection variables;

        public string Prefix
        {
            get;
            private set;
        }

        public string ValueName
        {
            get;
            private set;
        }

        public float Value
        {
            get;
            private set;
        }

        public long Timestamp
        {
            get;
            private set;
        }

        public InfluxDbMetric(CollectedMetric source)
        {
            variables = TemplateValueProvider.GetDefaults().SetDefaults(source);

            Prefix = source.Template;
            ValueName = !string.IsNullOrWhiteSpace(source.Instance) ? source.Instance : "value";
            Value = source.Value;
            Timestamp = (long)((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds * 1000 * 1000);
        }

        /// <summary>
        /// Returns final on-the-wire string of InfluxDbMetric
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var prefix = TemplateValueProvider.Format(Prefix, variables, x => EscapeString(x));
            var valueName = EscapeString(ValueName);
            var value = Value.ToString("0.00", CultureInfo.InvariantCulture);
            return $"{prefix} {valueName}={value} {Timestamp.ToString(CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// Replaces special characters with escape sequences per InfluxDB line protocol format
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string EscapeString(string item)
        {
            return item.Replace(",", "\\,").Replace(" ", "\\ ").Replace("=", "\\=");
        }

    }
}
