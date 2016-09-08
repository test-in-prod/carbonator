using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Crypton.Carbonator
{
    public class InfluxDbClient : IOutputClient
    {

        Config.InfluxDbOutputElement config = null;
        Timer metricReportingTimer = null;
        StateControl stateControl = new StateControl();
        BlockingCollection<CollectedMetric> metricsBuffer = null;

        private class StateControl
        {
            public bool IsRunning = false;
            public bool Started = false;
            public bool Run = true;
        }

        public InfluxDbClient(Config.InfluxDbOutputElement configuration)
        {
            config = configuration;
        }


        public void Start()
        {
            if (stateControl.Started)
                throw new InvalidOperationException("InfluxDbClient has already started");
            metricReportingTimer = new Timer(reportMetricsAsync, stateControl, 100, config.PostingIntervalSeconds * 1000);
        }

        public bool TryAdd(CollectedMetric metric)
        {
            throw new NotImplementedException();
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
                        var batch = new List<CollectedMetric>();

                        CollectedMetric tryTake;
                        while (metricsBuffer.TryTake(out tryTake, 100) && state.Run && batch.Count + 1 < config.MaxBatchSize)
                        {
                            batch.Add(tryTake);
                        }

                        // build line protocol syntax batch (https://docs.influxdata.com/influxdb/v0.13/write_protocols/write_syntax/)
                        var batchString = new StringBuilder();
                        foreach (var metric in batch)
                        {
                            batchString.AppendLine($"measurement,tagKey=tagValue value=12,otherval=32 12313213121");
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
                                    // TODO: check result
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception any)
            {
                Log.Error($"[{nameof(reportMetricsAsync)}] general exception: {any.Message}");
            }
            finally
            {
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
