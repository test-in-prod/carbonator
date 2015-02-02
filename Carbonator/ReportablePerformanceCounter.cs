using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator
{
    internal class ReportablePerformanceCounter
    {

        public PerformanceCounter Counter
        {
            get;
            private set;
        }

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


        public static string getMetricPath(string configuredPath)
        {
            return configuredPath.Replace("%HOST%", Environment.MachineName);
        }

    }
}
