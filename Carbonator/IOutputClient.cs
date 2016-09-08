using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crypton.Carbonator
{
    /// <summary>
    /// Defines an interface for a metric output
    /// </summary>
    public interface IOutputClient : IDisposable
    {

        void Start();
        bool TryAdd(CollectedMetric metric);

    }
}
