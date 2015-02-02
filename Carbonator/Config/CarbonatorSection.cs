using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator.Config
{
    /// <summary>
    /// Defines carbonator configuration section
    /// </summary>
    public class CarbonatorSection : ConfigurationSection
    {

        /// <summary>
        /// Gets current carbonator configuration
        /// </summary>
        public static CarbonatorSection Current
        {
            get
            {
                return ConfigurationManager.GetSection("carbonator") as CarbonatorSection;
            }
        }


        /// <summary>
        /// Gets Graphite server settings
        /// </summary>
        [ConfigurationProperty("graphite", IsRequired = true)]
        public GraphiteExportElement Graphite
        {
            get { return (GraphiteExportElement)base["graphite"]; }
            set { base["graphite"] = value; }
        }

        /// <summary>
        /// Gets the configured performance counters
        /// </summary>
        [ConfigurationProperty("counters", IsRequired = true)]
        public PerformanceCounterCollection Counters
        {
            get { return (PerformanceCounterCollection)base["counters"]; }
            set { base["counters"] = value; }
        }

        /// <summary>
        /// Gets the configured default thread culture
        /// </summary>
        [ConfigurationProperty("defaultCulture", IsRequired = false, DefaultValue = "en-US")]
        public string DefaultCulture
        {
            get { return (string)base["defaultCulture"]; }
            set { base["defaultCulture"] = value; }
        }

    }
}
