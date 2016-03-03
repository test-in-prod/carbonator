using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Crypton.Carbonator
{
    /// <summary>
    /// Watches a specific performance counter, or performance counter set and provides methods to report metrics
    /// </summary>
    public class CounterWatcher
    {
        /// <summary>
        /// Defines a cache between a configured metric path and performance counter properties: Category, Name, Instance (tuple)
        /// </summary>
        private Dictionary<Tuple<string, string, string>, string> _metricNameCache = new Dictionary<Tuple<string, string, string>, string>();

        /// <summary>
        /// Gets or sets the metric path that would be reported to Graphite/Carbon
        /// </summary>
        public string MetricPath
        {
            get; set;
        }

        /// <summary>
        /// <para>Gets or sets the performance counter categories</para>
        /// <para>This property supports a regular expression match</para>
        /// </summary>
        public string CategoryNames
        {
            get; set;
        }

        /// <summary>
        /// <para>Gets or sets the performance counter names</para>
        /// <para>This property supports a regular expression match</para> 
        /// </summary>
        public string CounterNames
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
            this.CategoryNames = configurationElement.CategoryName;
            this.CounterNames = configurationElement.CounterName;
            this.InstanceNames = configurationElement.InstanceName;
        }

        /// <summary>
        /// Called by CarbonatorInstance to report on the latest performance counter metrics
        /// </summary>
        /// <exception cref="System.InvalidOperationException"></exception>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        public void Report(ICollection<CollectedMetric> metrics)
        {
            if (string.IsNullOrEmpty(CategoryNames))
                throw new InvalidOperationException("CategoryNames is null; Category Name filter is required to filter performance counters");
            if (string.IsNullOrEmpty(MetricPath))
                throw new InvalidOperationException("MetricPath is null; Metric path is required to report to Carbon/Graphite");
            if (string.IsNullOrEmpty(CounterNames))
                throw new InvalidOperationException("CounterNames is null; Counter name filter is required to initialise performance counters");

            // start with all categories in system
            var counterCategories = PerformanceCounterCategory.GetCategories();

            // filter categories we need (or a single category if full name is entered anyway)
            var counterCategoriesFiltered = counterCategories.Where(c => Regex.IsMatch(c.CategoryName, CategoryNames));

            List<PerformanceCounter> matchedCounters = new List<PerformanceCounter>();

            foreach (var counterCategory in counterCategoriesFiltered)
            {
                IEnumerable<PerformanceCounter> countersFiltered = null;

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
                        countersFiltered = counters.Where(c => Regex.IsMatch(c.CounterName, CounterNames));
                    }
                }
                else
                {
                    // match counters
                    var counters = counterCategory.GetCounters();
                    countersFiltered = counters.Where(c => Regex.IsMatch(c.CounterName, CounterNames));
                }

                matchedCounters.AddRange(countersFiltered);
            }

            // sample each counter we have values for
            foreach (var counter in matchedCounters)
            {
                string path = FormatMetricPath(counter, MetricPath);
                float value = counter.NextValue();
                CollectedMetric metric = new CollectedMetric(path, value);
                metrics.Add(metric);
            }
        }

        private string formatMetricPathCached(PerformanceCounter counter, string configuredPath)
        {
            var counterMetricTuple = new Tuple<string, string, string>(counter.CategoryName, counter.CounterName, counter.InstanceName);
            // Original idea by @msmpeteclark in PR#4
            if (!_metricNameCache.ContainsKey(counterMetricTuple))
            {
                string path = FormatMetricPath(counter, configuredPath);
                _metricNameCache.Add(counterMetricTuple, path);
                return path;
            }
            else
            {
                return _metricNameCache[counterMetricTuple];
            }
        }

        /// <summary>
        /// Returns final carbon/graphite metric path based on a given PerformanceCounter and configured metric path
        /// </summary>
        /// <param name="counter"></param>
        /// <param name="configuredPath"></param>
        /// <returns></returns>
        public static string FormatMetricPath(PerformanceCounter counter, string configuredPath)
        {
            string finalPath = configuredPath;

            // map counter variables
            finalPath = finalPath
                .Replace("%COUNTER_CATEGORY%", replaceInvalidCharsInMetricPath(counter.CategoryName))
                .Replace("%COUNTER_NAME%", replaceInvalidCharsInMetricPath(counter.CounterName))
                .Replace("%COUNTER_INSTANCE%", replaceInvalidCharsInMetricPath(counter.InstanceName));

            // Original idea by @msmpeteclark in PR#4
            // map environment variables
            foreach (DictionaryEntry envPair in Environment.GetEnvironmentVariables())
            {
                string key = envPair.Key.ToString().ToUpper();
                string value = replaceInvalidCharsInMetricPath(envPair.Value.ToString());

                finalPath = finalPath.Replace(string.Format("%{0}%", key), value);
            }

            // preserve existing known variables
            // NOTE: these would already be replaced if they are system environment variables                
            finalPath = finalPath
                .Replace("%HOST%", Environment.MachineName)
                .Replace("%host%", Environment.MachineName.ToLowerInvariant())
                .Replace("%DOMAIN%", resolveDomainName());
            
            return finalPath;
        }

        /// <summary>
        /// Replaces characters that would not be valid in a carbon/graphite metric path
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string replaceInvalidCharsInMetricPath(string input, char replacementChar = '_')
        {
            if (string.IsNullOrEmpty(input))
                return replacementChar.ToString(); // return just the character if input is null
            return new Regex(@"\W").Replace(input, replacementChar.ToString());
        }

        /// <summary>
        /// Resolve computer domain name for %DOMAIN% variable, suggested by @GriffReborn in #6
        /// </summary>
        /// <returns></returns>
        private static string resolveDomainName()
        {
            // try with Active Directory first
            string domain = null;
            try
            {
                var adDomain = Domain.GetComputerDomain();
                domain = adDomain.Name;
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                // this may be thrown on a workgroup-only machine (e.g. not on a domain)
                // therefore, use user's domain name, which would just include the computer name
                domain = Environment.UserDomainName;
            }
            return domain;
        }

    }
}
