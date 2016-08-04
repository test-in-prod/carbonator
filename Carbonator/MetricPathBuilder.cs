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
    public class MetricPathBuilder
    {

        /// <summary>
        /// Cached Windows [Active Directory] domain name
        /// </summary>
        private static string cachedDomainName = null;

        /// <summary>
        /// Gets or sets configured path template
        /// </summary>
        public string PathTemplate
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
        /// Gets or sets computer domain name for %DOMAIN% variable
        /// </summary>
        public string Domain
        {
            get { return Variables["DOMAIN"]; }
            set { Variables["DOMAIN"] = value; }
        }

        /// <summary>
        /// Gets or sets hostname variable - %HOST%
        /// </summary>
        public string Host
        {
            get { return Variables["HOST"]; }
            set { Variables["HOST"] = value; }
        }

        /// <summary>
        /// Gets or sets variables to be applied for PathTemplate
        /// </summary>
        public NameValueCollection Variables
        {
            get;
            set;
        }

        /// <summary>
        /// The prefix to add to each metric
        /// </summary>
        private string Prefix;

        /// <summary>
        /// Creates a new instance of MetricPathBuilder
        /// </summary>
        public MetricPathBuilder()
        {
            Variables = new NameValueCollection();

            // set defaults
            Domain = cachedDomainName ?? (cachedDomainName = ResolveDomainName()); // resolve & cache the domain name
            Host = Environment.MachineName;
            var conf = Config.CarbonatorSection.Current;

            if (conf != null)
            {
                Prefix = conf.Graphite.Prefix;
            }
        }

        /// <summary>
        /// Creates a new instance of MetricPathBuilder with a path template
        /// </summary>
        /// <param name="pathTemplate"></param>
        public MetricPathBuilder(string pathTemplate) : this()
        {
            PathTemplate = pathTemplate;
        }

        /// <summary>
        /// Creates a MetricPathBuilder with values from specified performance counter
        /// </summary>
        /// <param name="counter"></param>
        public MetricPathBuilder(PerformanceCounter counter) : this()
        {
            CounterName = counter.CounterName;
            CategoryName = counter.CategoryName;
            CounterInstance = counter.InstanceName;
        }

        /// <summary>
        /// Creates a MetricPathBuilder with values from specified performance counter with a path template
        /// </summary>
        /// <param name="counter"></param
        public MetricPathBuilder(PerformanceCounter counter, string pathTemplate) : this(counter)
        {
            PathTemplate = pathTemplate;
        }

        /// <summary>
        /// Generates final path string for carbon/graphite based on variables
        /// </summary>
        /// <returns></returns>
        public string Format()
        {
            if (string.IsNullOrEmpty(PathTemplate))
            {
                throw new InvalidOperationException("PathTemplate property is null");
            }
            string template = PathTemplate;
            foreach (string key in Variables.Keys)
            {
                template = template.Replace("%" + key + "%", ReplaceInvalidCharactersInPath(Variables[key]));
            }

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

        /// <summary>
        /// Resolves computer domain name via Active Directory
        /// </summary>
        /// <returns></returns>
        public static string ResolveDomainName()
        {
            // try with Active Directory first
            string domain = null;
            try
            {
                var adDomain = System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain();
                domain = adDomain.Name;
            }
            catch (ActiveDirectoryObjectNotFoundException)
            {
                // this may be thrown on a workgroup-only machine (e.g. not on a domain)
                // therefore, use user's domain name, which would just include the computer name
                domain = Environment.UserDomainName;
                Log.Debug("[MetricPathBuilder/ResolveDomainName] unable to use A/D, using UserDomainName: {0}", domain);
            }
            return domain;
        }

    }
}
