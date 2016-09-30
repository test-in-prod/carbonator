using System.Configuration;

namespace Crypton.Carbonator.Config
{
    /// <summary>
    /// Defines configuration properties for exporting data to Graphite
    /// </summary>
    public class GraphiteOutputElement : OutputElement
    {

        /// <summary>
        /// Gets or sets destination Graphite server
        /// </summary>
        [ConfigurationProperty("server", IsRequired=true)]
        public string Server
        {
            get { return (string)base["server"]; }
            set { base["server"] = value; }
        }

        /// <summary>
        /// Gets or sets destination Graphite server port (default 2003)
        /// </summary>
        [ConfigurationProperty("port", IsRequired = false, DefaultValue = 2003)]
        public int Port
        {
            get { return (int)base["port"]; }
            set { base["port"] = value; }
        }

        /// <summary>
        /// Gets or sets prefix that added to each metric sent to Graphite server
        /// </summary>
        [ConfigurationProperty("prefix", IsRequired = false, DefaultValue = "")]
        public string Prefix
        {
            get { return (string)base["prefix"]; }
            set { base["prefix"] = value; }
        }

        /// <summary>
        /// How often metrics are reported to graphite, in seconds
        /// </summary>
        [ConfigurationProperty("reportingInterval", IsRequired = false, DefaultValue = 5)]
        public int ReportingIntervalSeconds
        {
            get { return (int)base["reportingInterval"]; }
            set { base["reportingInterval"] = value; }
        }

        /// <summary>
        /// How many metrics will be kept in the buffer before no new metrics will be accepted
        /// </summary>
        [ConfigurationProperty("bufferSize", IsRequired = false, DefaultValue = 50000)]
        public int BufferSize
        {
            get { return (int)base["bufferSize"]; }
            set { base["bufferSize"] = value; }
        }
        
        /// <summary>
        /// Maximum time, in milliseconds, that Carbonator will try to reconnect to the target Graphite server
        /// </summary>
        [ConfigurationProperty("reconnectIntervalMax", IsRequired = false, DefaultValue = 30000)]
        public int ReconnectIntervalMax
        {
            get { return (int)base["reconnectIntervalMax"]; }
            set { base["reconnectIntervalMax"] = value; }
        }

        /// <summary>
        /// Incremental reconnect interval that Carbonator will use to try reconnecting to Graphite server
        /// </summary>
        [ConfigurationProperty("reconnectIntervalStep", IsRequired = false, DefaultValue = 1000)]
        public int ReconnectIntervalStep
        {
            get { return (int)base["reconnectIntervalStep"]; }
            set { base["reconnectIntervalStep"] = value; }
        }

    }
}
