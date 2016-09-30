using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.DirectoryServices.ActiveDirectory;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypton.Carbonator
{
    /// <summary>
    /// Provides values for templated strings
    /// </summary>
    public static class TemplateValueProvider
    {

        /// <summary>
        /// Cached Windows Active Directory domain name
        /// </summary>
        static string cachedDomainName = null;

        /// <summary>
        /// Replaces variable template strings with variable values in a given template string
        /// </summary>
        /// <param name="template">Given path or metric template</param>
        /// <param name="variables">Collection of variables to use</param>
        /// <param name="escapeFunction">Optional function to escape special characters of names/values</param>
        /// <returns></returns>
        public static string Format(string template, NameValueCollection variables, Func<string, string> escapeFunction = null)
        {
            if (string.IsNullOrEmpty(template))
                throw new ArgumentNullException(nameof(template));
            if (variables == null)
                throw new ArgumentNullException(nameof(variables));

            var localTemplate = template;
            foreach (string key in variables.Keys)
            {
                localTemplate = localTemplate.Replace($"%{key}%", escapeFunction != null ? escapeFunction(variables[key]) : variables[key]);
            }

            return localTemplate;
        }

        /// <summary>
        /// Sets default variables for metric
        /// </summary>
        /// <param name="variables"></param>
        /// <param name="metric"></param>
        /// <returns></returns>
        public static NameValueCollection SetDefaults(this NameValueCollection variables, CollectedMetric metric)
        {
            variables["COUNTER_CATEGORY"] = metric.Category;
            variables["COUNTER_NAME"] = metric.Name;
            variables["COUNTER_INSTANCE"] = metric.Instance ?? string.Empty;
            variables["COUNTER_VALUE"] = metric.Value.ToString("0.000", CultureInfo.InvariantCulture);
            return variables;
        }

        /// <summary>
        /// Returns NameValueCollection containing default variables
        /// </summary>
        /// <returns></returns>
        public static NameValueCollection GetDefaults()
        {
            var vars = new NameValueCollection();

            // prefill with system environment variables
            foreach (string key in Environment.GetEnvironmentVariables().Keys)
            {
                vars[$"ENV_{key}"] = Environment.GetEnvironmentVariable(key);
            }

            vars["HOST"] = Environment.MachineName;
            vars["DOMAIN"] = ResolveDomainName();

            return vars;
        }

        /// <summary>
        /// Attemts to resolve Active Directory domain name
        /// </summary>
        /// <returns></returns>
        public static string ResolveDomainName()
        {
            if (cachedDomainName != null)
                return cachedDomainName;
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
                Log.Debug($"[{nameof(ResolveDomainName)}] unable to use A/D, using UserDomainName: {domain}");
            }

            cachedDomainName = domain;

            return domain;
        }

    }
}
