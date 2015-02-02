using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator.Config
{
    public class PerformanceCounterCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new PerformanceCounterElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as PerformanceCounterElement).Path;
        }
    }
}
