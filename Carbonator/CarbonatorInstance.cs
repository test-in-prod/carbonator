using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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
        private static List<CounterWatcher> _watchers = new List<CounterWatcher>();

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
                Log.Fatal("[StartCollection] Carbonator configuration is missing. This service cannot start");
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

            // load counter watchers that will actually collect metrics for us
            foreach (Config.PerformanceCounterElement counterConfig in Config.CarbonatorSection.Current.Counters)
            {
                CounterWatcher watcher = new CounterWatcher(counterConfig);
                try
                {
                    watcher.Initialize();
                }
                catch (Exception any)
                {
                    Log.Error("[StartCollection] Failed to initialize performance counter watcher for path '{0}'; this configuration element will be skipped: {1} (inner: {2})", counterConfig.Path, any.Message, any.InnerException != null ? any.InnerException.Message : "(null)");
                    continue;
                }
                _watchers.Add(watcher);
            }

            // start collection and reporting timers
            _metricCollectorTimer = new Timer(collectMetrics, new StateControl(), conf.CollectionInterval, conf.CollectionInterval);
            _metricReporterTimer = new Timer(reportMetrics, new StateControl(), conf.ReportingInterval, conf.ReportingInterval);

            Log.Info("[StartCollection] Carbonator service loaded {0} watchers", _watchers.Count);
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

            foreach (var watcher in _watchers)
            {
                watcher.Dispose();
            }
            _watchers.Clear();

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

            // restore configured culture setting for this async thread
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(conf.DefaultCulture);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(conf.DefaultCulture);

            // gather metrics from all watchers
            List<CollectedMetric> metrics = new List<CollectedMetric>();
            foreach (var watcher in _watchers)
            {
                try
                {
                    watcher.Report(metrics);
                }
                catch (Exception any)
                {
                    Log.Warning("[collectMetrics] Failed to Report on counter watcher for path '{0}'; this report will be skipped for now: {1} (inner: {2})", watcher.MetricPath, any.Message, any.InnerException != null ? any.InnerException.Message : "(null)");
                    continue;
                }
            }

            // transfer metrics over for sending
            foreach (var item in metrics)
            {
                if (!_metricsList.TryAdd(item))
                {
                    Log.Warning("[collectMetrics] Failed to relocate collected metrics to buffer for sending, buffer may be full; increase metric buffer in configuration");
                }
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
                        Log.Error("[reportMetrics] Unable to connect to graphite server (retrying after {1}ms): {0}", any.Message, reconnectInterval);
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
                    Log.Debug("[reportMetrics] reporting: {0}", metricStr.Substring(0, metricStr.Length - 1)); // the Length-1 is to remove the newline at the end for nicer log
                    byte[] bytes = Encoding.ASCII.GetBytes(conf.Graphite.Prefix + metricStr);
                    try
                    {
                        ns.Write(bytes, 0, bytes.Length);
                    }
                    catch (Exception any)
                    {
                        Log.Error("[reportMetrics] Failed to transmit metric {0} to configured graphite server: {1}", metric.Path, any.Message);
                        // put metric back into the queue
                        // metric will be lost if this times out
                        // the TryAdd will block until timeout if the buffer is full for example
                        if (!_metricsList.TryAdd(metric, 100))
                        {
                            Log.Error("[reportMetrics] Metric buffer may be full, consider increasing the metric buffer or determine if carbon server is reachable", metric.Path, any.Message);
                        }
                    }
                }
            }

            control.IsRunning = false;
        }

    }
}
