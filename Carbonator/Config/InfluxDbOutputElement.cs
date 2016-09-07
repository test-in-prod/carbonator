using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator.Config
{
    public class InfluxDbOutputElement : ConfigurationElement
    {

        [ConfigurationProperty("postingUrl", IsKey = false, IsRequired = true)]
        public string PostingUrl
        {
            get { return (string)base["postingUrl"]; }
            set { base["postingUrl"] = value; }
        }

    }
}
