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
        private static BlockingCollection<CollectedMetric> _metricsList = null;
        private static List<Tuple<string, string, string, PerformanceCounter>> _counters = new List<Tuple<string, string, string, PerformanceCounter>>();

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
                if (conf.LogLevel >= 3)
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

            // start collection and reporting timers
            _metricCollectorTimer = new Timer(collectMetrics, new StateControl(), 1000, 1000);
            _metricReporterTimer = new Timer(reportMetrics, new StateControl(), 5000, 5000);

            if (conf.LogLevel >= 1)
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
            {
                counter.Item4.Dispose();
            }
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

            // determine how long it takes for us to collect metrics
            // we'll adjust timer so that collecting this data is slightly more accurate
            // but not too much to cause skew in performance (e.g. our CPU usage goes up when we do this)
            Stopwatch timeTaken = new Stopwatch();
            timeTaken.Start();

            // collect metric samples for each of our counters
            foreach (Config.PerformanceCounterElement counterConfig in Config.CarbonatorSection.Current.Counters)
            {
                float sampleValue = 0f;
                string metricPath = getMetricPath(counterConfig.Path);
                // counter in list
                var perfCounterEntry = _counters.FirstOrDefault(c => c.Item1 == counterConfig.CategoryName && c.Item2 == counterConfig.CounterName && c.Item3 == counterConfig.InstanceName);
                if (perfCounterEntry == null)
                {
                    PerformanceCounter counter = null;
                    try
                    {
                        counter = new PerformanceCounter(counterConfig.CategoryName, counterConfig.CounterName, counterConfig.InstanceName);
                        counter.NextValue();
                        perfCounterEntry = new Tuple<string, string, string, PerformanceCounter>(counterConfig.CategoryName, counterConfig.CounterName, counterConfig.InstanceName, counter);
                    }
                    catch (Exception any)
                    {
                        if (counter != null)
                            counter.Dispose();
                        if (Config.CarbonatorSection.Current.LogLevel >= 2)
                            EventLog.WriteEntry(Program.EVENT_SOURCE, string.Format("Unable to initialize performance counter with path '{0}': {1}", metricPath, any.Message), EventLogEntryType.Warning);
                        continue;
                    }
                    _counters.Add(perfCounterEntry);
                }

                try
                {
                    sampleValue = perfCounterEntry.Item4.NextValue();
                }
                catch (Exception any)
                {
                    if (Config.CarbonatorSection.Current.LogLevel >= 2)
                        EventLog.WriteEntry(Program.EVENT_SOURCE, string.Format("Unable to collect performance counter with path '{0}': {1}", metricPath, any.Message), EventLogEntryType.Warning);
                    // remove from list
                    perfCounterEntry.Item4.Dispose();
                    _counters.Remove(perfCounterEntry);
                    continue;
                }

                // BlockingCollection will halt this thread if we are exceeding capacity
                _metricsList.Add(new CollectedMetric(metricPath, sampleValue));
            }

            timeTaken.Stop();

            // adjust how often we collect metrics to stay within hysteresis
            int periodTime = (int)Math.Abs(1000 - (int)timeTaken.ElapsedMilliseconds);
            if (periodTime > 100 && periodTime <= 1000)
            {
                _metricCollectorTimer.Change(100, periodTime);
                if (Config.CarbonatorSection.Current.LogLevel >= 4)
                    EventLog.WriteEntry(Program.EVENT_SOURCE, string.Format("Adjusted _metricCollector periodTime={0}ms", periodTime), EventLogEntryType.Information);
            }

            control.IsRunning = false;
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
                        if (Config.CarbonatorSection.Current.LogLevel >= 3)
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
                while (_metricsList.TryTake(out metric, 100) && _tcpClient.Connected)
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
                        if (Config.CarbonatorSection.Current.LogLevel >= 3)
                            EventLog.WriteEntry(Program.EVENT_SOURCE, string.Format("Failed to transmit metric {0} to configured graphite server: {1}", metric.Path, any.Message), EventLogEntryType.Error);
                        // put metric back into the queue
                        // metric will be lost if this times out
                        // the TryAdd will block until timeout if the buffer is full for example
                        if (!_metricsList.TryAdd(metric, 100))
                        {
                            if (Config.CarbonatorSection.Current.LogLevel >= 2)
                                EventLog.WriteEntry(Program.EVENT_SOURCE, string.Format("Lost metric because the buffer is full, consider increasing the buffer or diagnosing the underlying problem"), EventLogEntryType.Warning);
                        }
                    }
                }
            }

            control.IsRunning = false;
        }

    }
}
