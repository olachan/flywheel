using System;
using System.Linq;
using System.Threading;
using SignalR.Hubs;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.PerfHarness
{
    [HubName("perf")]
    public class PerfStatsHub : Hub
    {
        private static readonly int _updateInterval = 500; //ms
        private static Timer _updateTimer;
        private static int _broadcastSize = 32;
        private static string _broadcastPayload;
        private static bool _broadcasting = false;
        private static IConnection _connection = Connection.GetConnection<PerfEndpoint>();

        internal static void Init()
        {
            PerfStats.Init();
            var clients = Hub.GetClients<PerfStatsHub>();
            _updateTimer = new Timer(_ =>
            {
                // Broadcast updated stats
                clients.updateStats(PerfStats.GetStats());
            }, null, _updateInterval, _updateInterval);

            GC.SuppressFinalize(_updateTimer);

            SetBroadcastPayload();
        }

        public void SetOnReceive(EndpointBehavior behavior)
        {
            PerfEndpoint.Behavior = behavior;
            Clients.onReceiveChanged(behavior);
        }

        public void SetBroadcastInterval(int interval)
        {
            PerfStats.ResetAverage();
            if (interval <= 0)
            {
                _broadcasting = false;
            }
            else
            {
                _broadcasting = true;
                // TODO: Use CancelationToken here instead of flag?
                Task.Factory.StartNew(() =>
                {
                    while (_broadcasting)
                    {
                        _connection.Broadcast(_broadcastPayload);
                        Thread.Sleep(interval);
                    }
                });
            }
            Clients.onIntervalChanged(interval);
        }

        public void SetBroadcastSize(int size)
        {
            _broadcastSize = size;
            SetBroadcastPayload();
            Clients.onSizeChanged(size);
        }

        public void ResetAverage()
        {
            PerfStats.ResetAverage();
        }

        private static void SetBroadcastPayload()
        {
            _broadcastPayload = String.Join("", Enumerable.Range(0, _broadcastSize - 1).Select(i => "a"));
        }
    }
}