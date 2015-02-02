using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator
{
    struct CollectedMetric
    {

        public static readonly DateTime Epoch = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);

        public string Path;

        public float Value;

        public int Timestamp;

        public CollectedMetric(string path, float value)
        {
            this.Path = path;
            this.Value = value;
            this.Timestamp = (int)(DateTime.UtcNow - Epoch).TotalSeconds;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Path, Value, Timestamp);
        }

    }
}
