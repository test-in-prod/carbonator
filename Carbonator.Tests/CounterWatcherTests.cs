using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Crypton.Carbonator;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;
using System.Diagnostics;

namespace Carbonator.Tests
{
    [TestClass]
    public class CounterWatcherTests
    {
        [TestMethod]
        public void TestInstanceCounters_RegexMulti_CPU()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            CounterWatcher watcher = new CounterWatcher();
            watcher.MetricPath = "test.nowhere.%HOST%.%COUNTER_CATEGORY%.load.%COUNTER_INSTANCE%";
            watcher.CategoryName = "Processor Information";
            watcher.CounterName = "% Processor Time"; // NOTE: in system, it's "% Processor Time"
            watcher.InstanceNames = "Total"; // all instances we can find
            watcher.Initialize();

            List<CollectedMetric> metrics = new List<CollectedMetric>();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (var i = 0; i < 10; i++)
            {
                watcher.Report(metrics);
            }
            sw.Stop();
        }

        [TestMethod]
        public void TestFindInstanceNetworkCardRegex()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            CounterWatcher watcher = new CounterWatcher();
            watcher.MetricPath = "PerfMon.MES.%HOST%.NetworkAdapter.BytesTotalSec.%COUNTER_INSTANCE%";
            watcher.CategoryName = "Network Interface";
            watcher.CounterName = "Bytes Total/sec";
            watcher.InstanceNames = @".*^((?!.*6to4.*)(?!.*Local.*)(?!.*WAN.*)(?!.*isatap.*)(?!.*Kernel.*)(?!.*Test.*)).*(\n)";
            watcher.Initialize();

            List<CollectedMetric> metrics = new List<CollectedMetric>();
            watcher.Report(metrics);
            Thread.Sleep(2000);
            watcher.Report(metrics);


        }
    }
}
