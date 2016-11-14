using Crypton.Carbonator.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Crypton.Carbonator
{

    /// <summary>
    /// Handles acceptance of metrics destined for influxdb database
    /// </summary>
    public class InfluxDbClient : IOutputClient
    {

        InfluxDbOutputElement config = null;
        Timer metricReportingTimer = null;
        StateControl stateControl = new StateControl();
        BlockingCollection<InfluxDbMetric> metricsBuffer = null;

        private class StateControl
        {
            public bool IsRunning = false;
            public bool Started = false;
            public bool Run = true;
            public uint ReportNumber = 1;
        }

        /// <summary>
        /// Creates a new instance of InfluxDbClient
        /// </summary>
        /// <param name="configuration"></param>
        public InfluxDbClient(Config.InfluxDbOutputElement configuration)
        {
            config = configuration;
            metricsBuffer = new BlockingCollection<InfluxDbMetric>(configuration.BufferSize);
        }

        /// <summary>
        /// Starts the InfluxDbClient
        /// </summary>
        public void Start()
        {
            if (stateControl.Started)
                throw new InvalidOperationException("InfluxDbClient has already started");
            metricReportingTimer = new Timer(reportMetricsAsync, stateControl, 100, config.PostingIntervalSeconds * 1000);
        }

        /// <summary>
        /// Adds a new metric to the metric buffer for influxdb
        /// </summary>
        /// <param name="metric"></param>
        /// <returns></returns>
        public bool TryAdd(CollectedMetric metric)
        {
            if (metricsBuffer != null)
                return metricsBuffer.TryAdd(new InfluxDbMetric(metric));
            else
                return false;
        }

        private void reportMetricsAsync(object stateObj)
        {
            StateControl state = (StateControl)stateObj;
            if (!state.Started)
                state.Started = true;
            if (state.IsRunning && state.Run)
                return; // do not run the timer async exec again if it already is

            try
            {
                state.IsRunning = true;

                if (metricsBuffer != null)
                {
                    if (metricsBuffer.Count > 0)
                    {
                        // take batch of messages for sending
                        var batch = new List<InfluxDbMetric>();

                        InfluxDbMetric influxDbMetric;
                        while (metricsBuffer.TryTake(out influxDbMetric, 100) && state.Run && batch.Count + 1 < config.MaxBatchSize)
                        {
                            batch.Add(influxDbMetric);
                        }

                        // build line protocol syntax batch (https://docs.influxdata.com/influxdb/v0.13/write_protocols/write_syntax/)
                        using (var batchString = new StringWriter())
                        {
                            batchString.NewLine = "\n";
                            foreach (var metric in batch)
                            {
                                batchString.WriteLine(metric);
                                Log.Debug($"[{nameof(reportMetricsAsync)}] (#{state.ReportNumber}) metric string: {metric.ToString()}");
                            }

                            batch = null;

                            using (var httpClient = new HttpClient())
                            {
                                httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
                                using (var message = new HttpRequestMessage())
                                {
                                    message.RequestUri = new Uri(config.PostingUrl);
                                    message.Method = HttpMethod.Post;
                                    message.Content = new StringContent(batchString.ToString());

                                    var task = httpClient.SendAsync(message);
                                    if (task.Wait(TimeSpan.FromSeconds(config.TimeoutSeconds)))
                                    {
                                        var result = task.Result;
                                        Log.Debug($"[{nameof(reportMetricsAsync)}] response: HTTP {(int)result.StatusCode} {result.ReasonPhrase}: -> {result.Content.ReadAsStringAsync().Result}");
                                        if (result.StatusCode != HttpStatusCode.NoContent)
                                        {
                                            string responseText = result.Content.ReadAsStringAsync().Result;
                                            Log.Warning($"[{nameof(reportMetricsAsync)}] (#{state.ReportNumber}) response from influxdb {result.StatusCode} {result.ReasonPhrase} -> {responseText}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception any)
            {
                Log.Error($"[{nameof(reportMetricsAsync)}] (#{state.ReportNumber}) general exception: {any.Message}");
            }
            finally
            {
                unchecked
                {
                    state.ReportNumber++;
                }
                state.IsRunning = false;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
