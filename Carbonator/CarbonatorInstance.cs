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
        
        private static Timer _metricCollectorTimer = null;
        private static List<CounterWatcher> _watchers = new List<CounterWatcher>();

        static GraphiteClient graphiteClient = null;

        private static bool _started = false;
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

            graphiteClient = new Carbonator.GraphiteClient(conf.Graphite);
            graphiteClient.Start();

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
            graphiteClient.Dispose();
            
            foreach (var watcher in _watchers)
            {
                watcher.Dispose();
            }
            _watchers.Clear();
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
                if (!graphiteClient.TryAdd(item))
                {
                    Log.Warning("[collectMetrics] Failed to relocate collected metrics to buffer for sending, buffer may be full; increase metric buffer in configuration");
                }
            }

            control.IsRunning = false;
        }
        
    }
}
