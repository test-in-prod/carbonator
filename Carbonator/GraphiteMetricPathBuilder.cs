using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Text.RegularExpressions;

namespace Crypton.Carbonator
{
    /// <summary>
    /// Builds a metric path
    /// </summary>
    public class GraphiteMetricPathBuilder
    {

        /// <summary>
        /// Cached Windows [Active Directory] domain name
        /// </summary>
        private static string cachedDomainName = null;

        /// <summary>
        /// Gets or sets configured path template
        /// </summary>
        public string Template
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets counter name variable - %COUNTER_CATEGORY%
        /// </summary>
        public string CategoryName
        {
            get { return Variables["COUNTER_CATEGORY"]; }
            set { Variables["COUNTER_CATEGORY"] = value; }
        }

        /// <summary>
        /// Gets or sets counter name variable - %COUNTER_NAME%
        /// </summary>
        public string CounterName
        {
            get { return Variables["COUNTER_NAME"]; }
            set { Variables["COUNTER_NAME"] = value; }
        }

        /// <summary>
        /// Gets or sets counter name variable - %COUNTER_INSTANCE%
        /// </summary>
        public string CounterInstance
        {
            get { return Variables["COUNTER_INSTANCE"]; }
            set { Variables["COUNTER_INSTANCE"] = value; }
        }

        /// <summary>
        /// Gets a collection of variables for building a path
        /// </summary>
        public NameValueCollection Variables
        {
            get;
            private set;
        }
        
        /// <summary>
        /// The prefix to add to each metric
        /// </summary>
        private string Prefix;

        /// <summary>
        /// Creates a new instance of MetricPathBuilder
        /// </summary>
        public GraphiteMetricPathBuilder()
        {
            Variables = TemplateValueProvider.GetDefaults();
            
            var conf = Config.CarbonatorSection.Current;

            if (conf != null)
            {
                Prefix = conf.Graphite.Prefix;
            }
        }

        /// <summary>
        /// Creates a new instance of MetricPathBuilder with a path template
        /// </summary>
        /// <param name="template"></param>
        public GraphiteMetricPathBuilder(string template) : this()
        {
            Template = template;
        }

        /// <summary>
        /// Creates a MetricPathBuilder with values from specified performance counter
        /// </summary>
        /// <param name="counter"></param>
        public GraphiteMetricPathBuilder(PerformanceCounter counter) : this()
        {
            CounterName = counter.CounterName;
            CategoryName = counter.CategoryName;
            CounterInstance = counter.InstanceName;
        }

        /// <summary>
        /// Creates a MetricPathBuilder with values from specified performance counter with a path template
        /// </summary>
        /// <param name="counter"></param
        public GraphiteMetricPathBuilder(PerformanceCounter counter, string pathTemplate) : this(counter)
        {
            Template = pathTemplate;
        }

        /// <summary>
        /// Generates final path string for carbon/graphite based on variables
        /// </summary>
        /// <returns></returns>
        public string Format()
        {
            var template = TemplateValueProvider.Format(Template, Variables, (value) => { return ReplaceInvalidCharactersInPath(value); });

            if (!string.IsNullOrEmpty(Prefix))
            {
                template = Prefix + template;
            }

            return template;
        }

        /// <summary>
        /// Replaces characters that would be invalid for a carbon/graphite metric path
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ReplaceInvalidCharactersInPath(string input, char replacementChar = '_')
        {
            if (string.IsNullOrEmpty(input))
                return replacementChar.ToString(); // return just the character if input is null or empty
            return new Regex(@"\W").Replace(input, replacementChar.ToString());
        }

        
    }
}
