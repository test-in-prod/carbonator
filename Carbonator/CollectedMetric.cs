using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator
{

    /// <summary>
    /// Defines a structure for collected metric
    /// </summary>
    [DebuggerDisplay("Path={Path} Value={Value}")]
    public struct CollectedMetric
    {

        public static readonly DateTime Epoch = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);

        /// <summary>
        /// Gets metric path
        /// </summary>
        public string Path;

        /// <summary>
        /// Gets metric value
        /// </summary>
        public float Value;

        /// <summary>
        /// Gets UNIX timestamp of when metric was samples (UTC)
        /// </summary>
        public int Timestamp;

        public CollectedMetric(string path, float value)
        {
            this.Path = path;
            this.Value = value;
            this.Timestamp = (int)(DateTime.UtcNow - Epoch).TotalSeconds;
        }

        /// <summary>
        /// Gets metric message ready to be sent to Graphite/Carbon
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1:0.000} {2}\n", Path, Value, Timestamp);
        }

    }
}
