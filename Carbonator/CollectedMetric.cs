using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator
{

    /// <summary>
    /// Defines a structure for collected metric
    /// </summary>
    public struct CollectedMetric
    {

        public static readonly DateTime Epoch = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);

        public string Template;

        /// <summary>
        /// Gets or sets counter category from which metric was read
        /// </summary>
        public string Category;

        /// <summary>
        /// Gets or sets counter name from which metric was read
        /// </summary>
        public string Name;

        /// <summary>
        /// Gets or sets counter instance from which metric was read
        /// </summary>
        public string Instance;

        /// <summary>
        /// Gets metric value
        /// </summary>
        public float Value;

        /// <summary>
        /// Gets the UTC date and time when metric was collected
        /// </summary>
        public DateTime Timestamp;

        public CollectedMetric(string template, string category, string name, float value, string instance = null)
        {
            Template = template;
            Category = category;
            Name = name;
            Value = value;
            Instance = instance;
            Timestamp = DateTime.UtcNow;
        }
        
    }
}
