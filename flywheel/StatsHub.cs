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
        private static readonly int _updateInterval = 500; //ms
        private static Timer _updateTimer;
        private static int _broadcastSize = 32;
        private static string _broadcastPayload;
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static IConnection _connection = Connection.GetConnection<Shaft>();

        internal static void Init()
        {
            Stats.Init();
            var clients = Hub.GetClients<StatsHub>();
            _updateTimer = new Timer(_ =>
            {
                // Broadcast updated stats
                clients.updateStats(Stats.GetStats());
            }, null, _updateInterval, _updateInterval);

            GC.SuppressFinalize(_updateTimer);

            SetBroadcastPayload();
        }

        public void SetOnReceive(EndpointBehavior behavior)
        {
            Shaft.Behavior = behavior;
            Clients.onReceiveChanged(behavior);
        }

        public void SetBroadcastInterval(int interval)
        {
            Stats.ResetAverage();
            if (interval <= 0)
            {
                _cts.Cancel();
            }
            else
            {
                Task.Factory.StartNew(() =>
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        _connection.Broadcast(_broadcastPayload);
                        Thread.Sleep(interval);
                    }
                }, _cts.Token);
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
            Stats.ResetAverage();
        }

        private static void SetBroadcastPayload()
        {
            _broadcastPayload = String.Join("", Enumerable.Range(0, _broadcastSize - 1).Select(i => "a"));
        }
    }
}