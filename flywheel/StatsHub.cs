using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hubs;

namespace SignalR.Flywheel
{
    [HubName("flywheel")]
    public class StatsHub : Hub
    {
        private static readonly int _updateInterval = 1000; //ms
        private static Timer _updateTimer;
        private static int _broadcastSize = 32;
        private static string _broadcastPayload;
        private static CancellationTokenSource _cts;
        private static IConnection _connection = GlobalHost.ConnectionManager.GetConnectionContext<Shaft>().Connection;
        private static Task _broadcastTask;

        internal static void Init()
        {
            Stats.Init();
            var context = GlobalHost.ConnectionManager.GetHubContext<StatsHub>();
            _updateTimer = new Timer(_ =>
            {
                // Broadcast updated stats
                context.Clients.updateStats(Stats.GetStats());
            }, null, _updateInterval, _updateInterval);

            SetBroadcastPayload();
        }

        public void SetOnReceive(EndpointBehavior behavior)
        {
            Shaft.Behavior = behavior;
            Clients.onReceiveChanged(behavior);
        }

        public void SetBroadcastRate(double rate)
        {
            // rate is messages per second
            Stats.ResetAverage();
            if (_cts != null)
            {
                _cts.Cancel();
                if (_broadcastTask != null)
                {
                    _broadcastTask.Wait();
                }
            }
            if (rate > 0 || rate < 0)
            {
                var interval = TimeSpan.FromMilliseconds(1000 / rate);
                if (_cts != null)
                {
                    _cts.Dispose();
                }
                
                _cts = new CancellationTokenSource();

                TaskCompletionSource<object> broadcastTcs = new TaskCompletionSource<object>();
                _broadcastTask = broadcastTcs.Task;

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        while (!_cts.IsCancellationRequested)
                        {
                            var t = _connection.Broadcast(_broadcastPayload);
                            if (rate > 0)
                            {
                                Thread.Sleep(interval);
                            }
                            else if (rate < 0)
                            {
                                t.Wait();
                            }
                        }
                    }
                    finally
                    {
                        broadcastTcs.TrySetResult(null);
                    }
                });

            }
            Clients.onRateChanged(rate);
        }

        public void SetBroadcastSize(int size)
        {
            _broadcastSize = size;
            SetBroadcastPayload();
            Clients.onSizeChanged(size);
        }

        public void ResetAverage()
        {
            Stats.ResetAverage();
        }

        public void ForceGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private static void SetBroadcastPayload()
        {
            _broadcastPayload = String.Join("", Enumerable.Range(0, _broadcastSize - 1).Select(i => "a"));
        }
    }
}