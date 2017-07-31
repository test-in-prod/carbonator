using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;

namespace Crypton.Carbonator
{
    /// <summary>
    /// Watches a specific performance counter, or performance counter set and provides methods to report metrics
    /// </summary>
    public class CounterWatcher : IDisposable
    {
        
        /// <summary>
        /// Initialised list of counters
        /// </summary>
        private List<PerformanceCounter> _counters = new List<PerformanceCounter>();


        /// <summary>
        /// Gets or sets the metric path that would be reported to Graphite/Carbon
        /// </summary>
        public string MetricPath
        {
            get; set;
        }

        /// <summary>
        /// <para>Gets or sets the performance counter category name</para>
        /// </summary>
        public string CategoryName
        {
            get; set;
        }

        /// <summary>
        /// <para>Gets or sets the performance counter names</para>
        /// </summary>
        public string CounterName
        {
            get; set;
        }

        /// <summary>
        /// <para>Gets or sets the performance counter instance names</para>
        /// <para>This property supports a regular expression match</para>
        /// </summary>
        public string InstanceNames
        {
            get; set;
        }

        /// <summary>
        /// Initialises a new instance of CounterWatcher
        /// </summary>
        public CounterWatcher()
        {
        }

        /// <summary>
        /// Initialises a new instance of CounterWatcher based on configuration
        /// </summary>
        /// <param name="configurationElement"></param>
        public CounterWatcher(Config.PerformanceCounterElement configurationElement)
        {
            this.MetricPath = configurationElement.Path;
            this.CategoryName = configurationElement.CategoryName;
            this.CounterName = configurationElement.CounterName;
            this.InstanceNames = configurationElement.InstanceName;
        }

        /// <summary>
        /// Initialises the counter watcher and loads matching performance counters
        /// </summary>
        public void Initialize()
        {
            if (string.IsNullOrEmpty(CategoryName))
                throw new InvalidOperationException("CategoryName is null; Category Name is required");
            if (string.IsNullOrEmpty(MetricPath))
                throw new InvalidOperationException("MetricPath is null; Metric path is required to report to Carbon/Graphite");
            if (string.IsNullOrEmpty(CounterName))
                throw new InvalidOperationException("CounterNames is null; Counter name filter is required to initialise performance counters");

            if (_counters.Count > 0)
            {
                foreach (var counter in _counters)
                {
                    counter.Dispose();
                }
                _counters.Clear();
            }

            PerformanceCounterCategory counterCategory = new PerformanceCounterCategory(CategoryName);
            if (!string.IsNullOrEmpty(InstanceNames))
            {
                // filter counters with instance names we care about
                var categoryInstanceNames = counterCategory.GetInstanceNames();
                var categoryInstanceNamesFiltered = categoryInstanceNames.Where(n => Regex.IsMatch(n, InstanceNames));

                // get counters with instances we have matched
                foreach (var instanceName in categoryInstanceNamesFiltered)
                {
                    var counters = counterCategory.GetCounters(instanceName);

                    // filter by counter names
                    var filtered = counters.Where(c => Regex.IsMatch(c.CounterName, CounterName));
                    AddCounters(filtered);
                }
            }
            else
            {
                // match counters
                var counters = counterCategory.GetCounters();
                var filtered = counters.Where(c => Regex.IsMatch(c.CounterName, CounterName));
                AddCounters(filtered);
            }
        }

        /// <summary>
        /// Called by CarbonatorInstance to report on the latest performance counter metrics
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        public void Report(ICollection<CollectedMetric> metrics, bool throwExceptions = true)
        {
            // sample each counter we have values for
            foreach (var counter in _counters)
            {
                string path = (new MetricPathBuilder(counter, MetricPath)).Format();
                try
                {
                    float value = counter.NextValue();
                    CollectedMetric metric = new CollectedMetric(path, value);
                    metrics.Add(metric);

                    Log.Debug("[CounterWatcher/Report] collected {0}/{1}{2} for path {3}: {4}", counter.CategoryName, counter.CounterName, counter.InstanceName, path, value);
                }
                catch
                {
                    if (throwExceptions)
                        throw;
                }
            }
        }
        
        /// <summary>
        /// Disposes this CounterWatcher instance and releases performance counters loaded by it
        /// </summary>
        public void Dispose()
        {
            foreach (var counter in _counters)
            {
                counter.Dispose();
            }
            _counters.Clear();
        }

        private void AddCounters(IEnumerable<PerformanceCounter> filtered)
        {
            foreach (var counter in filtered)
            {
                // Some performance counters require deltas to give sensible values. By calling NextValue
                // on each of our counters, we prime them to return correct one each subsequent call.
                counter.NextValue();
                _counters.Add(counter);
            }        
        }
    }
}
