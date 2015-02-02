using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Crypton.Carbonator
{
    /// <summary>
    /// Controls operation of Carbonator service
    /// </summary>
    internal static class CarbonatorInstance
    {

        private static Timer _metricReporterTimer = null;
        private static Timer _metricCollectorTimer = null;
        private static List<ReportablePerformanceCounter> _counters = new List<ReportablePerformanceCounter>();
        private static List<CollectedMetric> _metrics = new List<CollectedMetric>();
        private static object _metricReportLock = new object();
        private static object _metricCollectLock = new object();

        private static bool _started = false;
        private static TcpClient _tcpClient = null;
        private static Config.CarbonatorSection conf = null;

        /// <summary>
        /// Starts collection of performance counter metrics and relaying of data to Graphite
        /// </summary>
        public static void StartCollection()
        {
            if (_started)
                return;
            _started = true;

            conf = Config.CarbonatorSection.Current;
            if (conf == null)
            {
                EventLog.WriteEntry(Program.EVENT_SOURCE, "Carbonator configuration is missing. This service cannot start", EventLogEntryType.Error);
                throw new InvalidOperationException("Carbonator configuration is missing. This service cannot start");
            }

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(conf.DefaultCulture);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(conf.DefaultCulture);

            // initialize performance counters
            foreach (Config.PerformanceCounterElement perfcConf in conf.Counters)
            {
                string metricPath = ReportablePerformanceCounter.getMetricPath(perfcConf.Path);
                PerformanceCounter perfc = null;
                try
                {
                    perfc = new PerformanceCounter(perfcConf.CategoryName, perfcConf.CounterName, perfcConf.InstanceName);
                    perfc.NextValue();
                }
                catch (Exception any)
                {
                    EventLog.WriteEntry(Program.EVENT_SOURCE, string.Format("Unable to initialize performance counter with path '{0}': {1}", perfcConf.Path, any.Message), EventLogEntryType.Warning);
                    continue;
                }
                _counters.Add(new ReportablePerformanceCounter(perfc, metricPath));
            }

            // make sure we event have counters
            if (_counters.Count == 0)
            {
                EventLog.WriteEntry(Program.EVENT_SOURCE, "No performance counters have been configured or loaded, verify that configuration is correct or nothing will be reported", EventLogEntryType.Warning);
            }

            // create connection to graphite
            try
            {
                _tcpClient = new TcpClient(conf.Graphite.Server, conf.Graphite.Port);
            }
            catch (Exception any)
            {
                EventLog.WriteEntry(Program.EVENT_SOURCE, "Unable to connect to graphite server: " + any.Message, EventLogEntryType.Error);
            }

            // start collection and reporting timers
            _metricCollectorTimer = new Timer(collectMetrics, null, 100, 1000);
            _metricReporterTimer = new Timer(reportMetrics, null, 100, 5000);
        }

        /// <summary>
        /// Stops collection of performance counter metrics and relaying of data to Graphite
        /// </summary>
        public static void StopCollection()
        {
            if (!_started)
                return;
            _started = false;

            _metricCollectorTimer.Dispose();
            _metricReporterTimer.Dispose();

            if (_tcpClient.Connected)
                _tcpClient.Close();

            foreach (var counter in _counters)
                counter.Counter.Dispose();
            _counters.Clear();
        }


        /// <summary>
        /// Timer callback that collects metrics
        /// </summary>
        /// <param name="state"></param>
        private static void collectMetrics(object state)
        {
            lock (_metricCollectLock)
            {
                // collect metric samples for each of our counters
                foreach (var counter in _counters)
                {
                    float sample = counter.Counter.NextValue();
                    string path = counter.MetricPath;
                    lock (_metrics)
                    {
                        _metrics.Add(new CollectedMetric(path, sample));
                    }
                }
            }
        }

        /// <summary>
        /// Timer callback that reports collected metrics
        /// </summary>
        /// <param name="state"></param>
        private static void reportMetrics(object state)
        {
            lock (_metricReportLock)
            {
                // local array that holds metrics so far
                CollectedMetric[] _arr = null;
                lock (_metrics)
                {
                    _arr = _metrics.ToArray();
                    _metrics.Clear();
                }

                // if client isn't connected...
                if (!_tcpClient.Connected)
                {
                    try
                    {
                        _tcpClient.Connect(conf.Graphite.Server, conf.Graphite.Port);
                    }
                    catch
                    {
                        // silently catch exception since main Start method checks for a connection
                    }
                }

                // send metrics if client is connected
                if (_tcpClient.Connected)
                {
                    NetworkStream ns = _tcpClient.GetStream();
                    foreach (var metric in _arr)
                    {
                        string metricStr = metric.ToString();
                        // see http://graphite.readthedocs.org/en/latest/feeding-carbon.html
                        if (Program.Verbose)
                        {
                            Console.Write(metricStr);
                            Debug.Write(metricStr);
                        }
                        byte[] bytes = Encoding.ASCII.GetBytes(metricStr);
                        try
                        {
                            ns.Write(bytes, 0, bytes.Length);   
                        }
                        catch { }
                    }
                }
                else
                {
                    // add unsent metrics back to list
                    // TODO: this may be a memory leak if we are unable to connect to a server for a while
                    // TODO: drop metrics older than X later
                    lock (_metrics)
                    {
                        _metrics.AddRange(_arr);
                    }
                }
            }
        }

    }
}
