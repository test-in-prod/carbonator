using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypton.Carbonator
{

    /// <summary>
    /// Defines a metric that is slated to be sent to graphite
    /// </summary>
    public struct GraphiteMetric
    {

        /// <summary>
        /// Gets the final metric path
        /// </summary>
        public string Path
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the value
        /// </summary>
        public float Value
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the UNIX timestamp
        /// </summary>
        public long Timestamp
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates a GraphiteMetric from a CollectedMetric
        /// </summary>
        /// <param name="source"></param>
        public GraphiteMetric(CollectedMetric source)
        {
            var pathBuilder = new GraphiteMetricPathBuilder(source.Template)
            {
                CategoryName = source.Category,
                CounterName = source.Name,
                CounterInstance = source.Instance                
            };

            Path = pathBuilder.Format();
            Value = source.Value;
            Timestamp = (long)(source.Timestamp - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        /// <summary>
        /// Returns on-the-wire string representation of GraphiteMetric
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1:0.000} {2}\n", Path, Value, Timestamp);
        }

    }

}
