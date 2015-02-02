using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator
{

    /// <summary>
    /// Wrapper for a reported performance counter metric
    /// </summary>
    internal class ReportablePerformanceCounter
    {

        /// <summary>
        /// Gets the performance counter instance
        /// </summary>
        public PerformanceCounter Counter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the metric path that will be reported
        /// </summary>
        public string MetricPath
        {
            get;
            private set;
        }

        public ReportablePerformanceCounter(PerformanceCounter counter, string metricPath)
        {
            this.Counter = counter;
            this.MetricPath = metricPath;
        }

        /// <summary>
        /// Gets metric path from configuration and formats it for reporting
        /// </summary>
        /// <param name="configuredPath"></param>
        /// <returns></returns>
        public static string getMetricPath(string configuredPath)
        {
            return configuredPath
                .Replace("%HOST%", Environment.MachineName); // one and only special variable so far
        }

    }
}
