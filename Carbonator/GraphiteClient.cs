using Crypton.Carbonator.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Crypton.Carbonator
{

    /// <summary>
    /// Represents a stateful connection to the graphite server
    /// </summary>
    public class GraphiteClient : IOutputClient
    {

        TcpClient tcpClient = null;
        GraphiteOutputElement config = null;
        Timer metricReportingTimer = null;
        StateControl stateControl = new StateControl();
        BlockingCollection<CollectedMetric> metricsBuffer = null;

        private class StateControl
        {
            public bool IsRunning = false;
            public bool Started = false;
            public bool Run = true;
        }

        public bool Connected
        {
            get
            {
                return tcpClient != null && tcpClient.Connected;
            }
        }

        public GraphiteClient(GraphiteOutputElement configuration)
        {
            config = configuration;
            metricsBuffer = new BlockingCollection<CollectedMetric>(config.BufferSize);
        }

        /// <summary>
        /// Begins reporting metrics to graphite
        /// </summary>
        public void Start()
        {
            if (stateControl.Started)
                throw new InvalidOperationException("GraphiteClient has already started");
            metricReportingTimer = new Timer(reportMetricsAsync, stateControl, 100, config.ReportingIntervalSeconds * 1000);
        }

        /// <summary>
        /// Tries adding a metric to the local buffer
        /// </summary>
        /// <param name="metrics"></param>
        /// <returns></returns>
        public bool TryAdd(CollectedMetric metric)
        {
            if (metricsBuffer != null)
                return metricsBuffer.TryAdd(metric);
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

                // reconnect automatically
                if (!Connected && state.Run)
                {
                    int reconnectInterval = config.ReconnectIntervalStep;
                    bool reconnect = false;
                    do
                    {
                        try
                        {
                            tcpClient = new TcpClient(config.Server, config.Port);
                            reconnect = false;
                        }
                        catch (Exception any)
                        {
                            Log.Error($"[{nameof(reportMetricsAsync)}] Unable to connect to graphite server (retrying after {reconnectInterval}ms): {any.Message}");
                            reconnectInterval = 
                                reconnectInterval + config.ReconnectIntervalStep < config.ReconnectIntervalMax ? reconnectInterval + config.ReconnectIntervalStep : config.ReconnectIntervalMax;
                            Thread.Sleep(reconnectInterval);
                            reconnect = true;
                        }
                    } while (reconnect && state.Run);
                }

                if (Connected && state.Run)
                {
                    NetworkStream ns = tcpClient.GetStream();
                    CollectedMetric metric;
                    while (metricsBuffer.TryTake(out metric, 100) && Connected && state.Run)
                    {
                        var graphiteMetric = new GraphiteMetric(metric);

                        // see http://graphite.readthedocs.org/en/latest/feeding-carbon.html
                        byte[] bytes = Encoding.ASCII.GetBytes(graphiteMetric.ToString());
                        try
                        {
                            ns.Write(bytes, 0, bytes.Length);
                        }
                        catch (Exception any)
                        {
                            Log.Error("[reportMetrics] Failed to transmit metric {0} to configured graphite server: {1}", metric.Template, any.Message);
                            // put metric back into the queue
                            // metric will be lost if this times out
                            // the TryAdd will block until timeout if the buffer is full for example
                            if (!metricsBuffer.TryAdd(metric, 100))
                            {
                                Log.Error("[reportMetrics] Metric buffer may be full, consider increasing the metric buffer or determine if carbon server is reachable", graphiteMetric.Path, any.Message);
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
                    stateControl.Run = false;
                    metricReportingTimer.Dispose();
                    if (tcpClient.Connected)
                        tcpClient.Close();
                    metricsBuffer.Dispose();
                }

                tcpClient = null;
                metricReportingTimer = null;
                stateControl = null;
                metricsBuffer = null;

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
