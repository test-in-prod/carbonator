using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator.Config
{
    public class InfluxDbOutputElement : OutputElement
    {

        /// <summary>
        /// Gets or sets the influxdb URL to which metrics will be submitted
        /// </summary>
        [ConfigurationProperty("postingUrl", IsKey = false, IsRequired = true)]
        public string PostingUrl
        {
            get { return (string)base["postingUrl"]; }
            set { base["postingUrl"] = value; }
        }

        /// <summary>
        /// How many metrics will be kept in the buffer before no new metrics will be accepted
        /// </summary>
        [ConfigurationProperty("bufferSize", IsKey = false, IsRequired = false, DefaultValue = 50000)]
        public int BufferSize
        {
            get { return (int)base["bufferSize"]; }
            set { base["bufferSize"] = value; }
        }

        /// <summary>
        /// Gets or sets the request timeout in seconds
        /// </summary>
        [ConfigurationProperty("timeout", IsKey = false, IsRequired = false, DefaultValue = 15)]
        public int TimeoutSeconds
        {
            get { return (int)base["timeout"]; }
            set { base["timeout"] = value; }
        }

        /// <summary>
        /// Gets or sets the maximum metric batch size that will be part of a single POST
        /// </summary>
        [ConfigurationProperty("maxBatchSize", IsKey = false, IsRequired = false, DefaultValue = 2500)]
        public int MaxBatchSize
        {
            get { return (int)base["maxBatchSize"]; }
            set { base["maxBatchSize"] = value; }
        }

        /// <summary>
        /// Gets or sets the interval in seconds of how often metrics are submitted
        /// </summary>
        [ConfigurationProperty("postingInterval", IsKey = false, IsRequired = false, DefaultValue = 5)]
        public int PostingIntervalSeconds
        {
            get { return (int)base["postingInterval"]; }
            set { base["postingInterval"] = value; }
        }

    }
}
