using System;
using System.Linq;
using System.Threading;
using SignalR.Hubs;
using System.Text;

namespace SignalR.PerfHarness
{
    [HubName("perf")]
    public class PerfStatsHub : Hub
    {
        private static readonly int _updateInterval = 500; //ms
        private static Timer _updateTimer;
        private static Timer _broadcastTimer;
        private static int _broadcastSize = 32;
        private static string _broadcastPayload;
        private static object _broadcastTimerLock = new object();
        private static bool _broadcastOnTightLoop = false;
        private static IConnection _connection = Connection.GetConnection<PerfEndpoint>();

        internal static void Init()
        {
            PerfStats.Init();
            _updateTimer = new Timer(_ =>
            {
                // Broadcast updated stats
                var clients = Hub.GetClients<PerfStatsHub>();
                clients.updateStats(PerfStats.GetStats());
            }, null, _updateInterval, _updateInterval);
            
            GC.SuppressFinalize(_updateTimer);
        }

        public void SetOnReceive(EndpointBehavior behavior)
        {
            PerfEndpoint.Behavior = behavior;
            Clients.onReceiveChanged(behavior);
        }

        public void SetBroadcastInterval(int interval)
        {
            EnsureBroadcastTimer();
            PerfStats.ResetAverage();
            if (interval <= 0)
            {
                _broadcastOnTightLoop = false;
                _broadcastTimer.Change(Timeout.Infinite, Timeout.Infinite);
                if (interval == -1)
                {
                    _broadcastOnTightLoop = true;
                    // Go nuts
                    while (_broadcastOnTightLoop)
                    {
                        _connection.Broadcast(_broadcastPayload);
                    }
                }
            }
            else
            {
                _broadcastOnTightLoop = false;
                _broadcastTimer.Change(interval, interval);
            }
            Clients.onIntervalChanged(interval);
        }

        public void SetBroadcastSize(int size)
        {
            _broadcastSize = size;
            _broadcastPayload = String.Join("", Enumerable.Range(0, size - 1).Select(i => "a"));
            Clients.onSizeChanged(size);
        }

        public void ResetAverage()
        {
            PerfStats.ResetAverage();
        }

        private static void EnsureBroadcastTimer()
        {
            if (_broadcastTimer == null)
            {
                lock (_broadcastTimerLock)
                {
                    if (_broadcastTimer == null)
                    {
                        _broadcastTimer = new Timer(_ => _connection.Broadcast(_broadcastPayload), null, Timeout.Infinite, Timeout.Infinite);
                        GC.SuppressFinalize(_broadcastTimer);
                    }
                }
            }
        }
    }
}