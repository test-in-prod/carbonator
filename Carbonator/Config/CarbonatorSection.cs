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
        
        [ConfigurationProperty("output", IsRequired = true)]
        public OutputElementCollection Output
        {
            get { return (OutputElementCollection)base["output"]; }
            set { base["output"] = value; }
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
        
        /// <summary>
        /// Specifies where Carbonator logs will be sent to (eventlog, log4net, none)
        /// </summary>
        [ConfigurationProperty("logType", IsRequired = false, DefaultValue = "eventlog")]
        public string LogType
        {
            get { return (string)base["logType"]; }
            set { base["logType"] = value; }
        }

        /// <summary>
        /// Species an interval in milliseconds when metrics are collected into a buffer
        /// </summary>
        [ConfigurationProperty("collectionInterval", IsRequired = false, DefaultValue = 1000)]
        public int CollectionInterval
        {
            get { return (int)base["collectionInterval"]; }
            set { base["collectionInterval"] = value; }
        }

    }
}
