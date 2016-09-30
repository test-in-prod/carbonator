using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Xml;

namespace Crypton.Carbonator.Config
{
    public class OutputElementCollection : ConfigurationElementCollection
    {

        internal class OutputElementProxy : ConfigurationElement
        {
            public OutputElement Entry
            {
                get;
                private set;
            }

            protected override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
            {
                string type = reader.GetAttribute("type");
                switch (type)
                {
                    case "graphite":
                        Entry = new GraphiteOutputElement();
                        break;
                    case "influxdb":
                        Entry = new InfluxDbOutputElement();
                        break;
                    default:
                        throw new NotSupportedException($"{type} is not a supported Carbonator output");
                }

                Entry.DeserializeElementByProxy(reader, serializeCollectionKey);
            }
        }

        /// <summary>
        /// Specifies which output module will be used
        /// </summary>
        [ConfigurationProperty("name", IsRequired = true)]
        public string DefaultOutput
        {
            get { return (string)base["name"]; }
            set { base["name"] = value; }
        }

        /// <summary>
        /// Returns the current default output element configuration
        /// </summary>
        /// <returns></returns>
        public OutputElement GetDefault()
        {
            foreach(OutputElementProxy proxy in this)
            {
                if (proxy.Entry.Name == DefaultOutput)
                    return proxy.Entry;
            }
            throw new ConfigurationErrorsException($"Output plugin '{DefaultOutput}' is not defined in the list of outputs");
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new OutputElementProxy();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as OutputElementProxy).Entry.Name;
        }
    }
}
