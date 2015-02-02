using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator.Config
{
    /// <summary>
    /// Defines configuration properties for exporting data to Graphite
    /// </summary>
    public class GraphiteExportElement : ConfigurationElement
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

    }
}
