using System;
using System.Collections.Concurrent;
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
        private static BlockingCollection<CollectedMetric> _metricsList = null;

        private static bool _started = false;
        private static TcpClient _tcpClient = null;
        private static Config.CarbonatorSection conf = null;

        #region Collection Timers State control
        private class StateControl
        {
            public bool IsRunning = false;
        }
        #endregion

        /// <summary>
        /// Starts collection of performance counter metrics and relaying of data to Graphite
        /// </summary>
        [PerformanceCounterPermission(System.Security.Permissions.SecurityAction.Demand)]
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

            if (Config.CarbonatorSection.Current.MaxMetricBufferSize > 0)
            {
                _metricsList = new BlockingCollection<CollectedMetric>(Config.CarbonatorSection.Current.MaxMetricBufferSize);
            }
            else
            {
                _metricsList = new BlockingCollection<CollectedMetric>();
            }

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

            // start collection and reporting timers
            _metricCollectorTimer = new Timer(collectMetrics, new StateControl(), 1000, 1000);
            _metricReporterTimer = new Timer(reportMetrics, new StateControl(), 5000, 5000);

            EventLog.WriteEntry(Program.EVENT_SOURCE, "Carbonator service has been initialized and began reporting metrics", EventLogEntryType.Information);
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

            if (_tcpClient != null && _tcpClient.Connected)
                _tcpClient.Close();

            foreach (var counter in _counters)
                counter.Counter.Dispose();
            _counters.Clear();
            _metricsList.Dispose();
        }


        /// <summary>
        /// Timer callback that collects metrics
        /// </summary>
        /// <param name="state"></param>
        [PerformanceCounterPermission(System.Security.Permissions.SecurityAction.Demand)]
        private static void collectMetrics(object state)
        {
            StateControl control = state as StateControl;
            if (control.IsRunning)
                return; // skip this run if we're already collecting data
            control.IsRunning = true;

            // collect metric samples for each of our counters
            foreach (var counter in _counters)
            {
                float sample = counter.Counter.NextValue();
                string path = counter.MetricPath;

                // BlockingCollection will halt this thread if we are exceeding capacity
                _metricsList.Add(new CollectedMetric(path, sample));
            }

            control.IsRunning = false;
        }

        /// <summary>
        /// Timer callback that reports collected metrics
        /// </summary>
        /// <param name="state"></param>
        private static void reportMetrics(object state)
        {
            StateControl control = state as StateControl;
            if (control.IsRunning)
                return; // skip this run if we're already collecting data
            control.IsRunning = true;

            // if client isn't connected...
            if (_tcpClient == null || !_tcpClient.Connected)
            {
                int reconnectInterval = 100;
                bool reconnect = false;
                const int reconnectMaxInterval = 30000; // max time in MS that reconnectInterval can reach
                const int reconnectStepInterval = 1000; // how much reconnect interval gets increased each time
                do
                {
                    try
                    {
                        _tcpClient = new TcpClient(conf.Graphite.Server, conf.Graphite.Port);
                        reconnect = false;
                    }
                    catch (Exception any)
                    {
                        EventLog.WriteEntry(Program.EVENT_SOURCE, string.Format("Unable to connect to graphite server (retrying after {1}ms): {0}", any.Message, reconnectInterval), EventLogEntryType.Error);
                        reconnectInterval = reconnectInterval + reconnectStepInterval < reconnectMaxInterval ? reconnectInterval + reconnectStepInterval : reconnectMaxInterval;
                        Thread.Sleep(reconnectInterval);
                        reconnect = true;
                    }
                } while (reconnect && _started);
            }

            // send metrics if client is connected
            if (_tcpClient != null && _tcpClient.Connected)
            {
                NetworkStream ns = _tcpClient.GetStream();
                CollectedMetric metric;
                while (_metricsList.TryTake(out metric, 100))
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
                    catch (Exception any)
                    {
                        EventLog.WriteEntry(Program.EVENT_SOURCE, string.Format("Failed to transmit metric {0} to configured graphite server: {1}", metric.Path, any.Message), EventLogEntryType.Error);
                        // put metric back into the queue
                        // metric will be lost if this times out
                        // the TryAdd will block until timeout if the buffer is full for example
                        if (!_metricsList.TryAdd(metric, 100))
                        {
                            EventLog.WriteEntry(Program.EVENT_SOURCE, string.Format("Lost metric because the buffer is full, consider increasing the buffer or diagnosing the underlying problem"), EventLogEntryType.Warning);
                        }
                    }
                }
            }

            control.IsRunning = false;
        }

    }
}
