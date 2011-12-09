using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using SignalR.Transports;

namespace SignalR.Flywheel
{
    internal static class Stats
    {
        private static DateTime _avgCalcStart;
        private static long _avgLastSentCount;
        private static long _avgLastReceivedCount;
        private static DateTime _lastRateRead = DateTime.UtcNow;
        private static long _lastSendCount;
        private static long _lastReceivedCount;
        private static bool _measuringRate;
        private static Timer _rateCounter;
        private static ConcurrentDictionary<string, object> _connectedClients;
        private static Lazy<InProcessMessageStore> _messageStore =
            new Lazy<InProcessMessageStore>(() =>
                (InProcessMessageStore)SignalR.Infrastructure.DependencyResolver.Resolve<IMessageStore>());

        public static void Init()
        {
            ResetAverage();
            
            _connectedClients = new ConcurrentDictionary<string, object>();

            _rateCounter = new Timer(_ =>
            {
                if (_measuringRate)
                {
                    return;
                }
                _measuringRate = true;

                try
                {
                    var now = DateTime.UtcNow;
                    var timeDiffSecs = (now - _lastRateRead).TotalSeconds;

                    if (timeDiffSecs <= 0)
                        return;

                    var sent = Interlocked.Read(ref Sent);
                    var sendDiff = sent - _lastSendCount;
                    var sendsPerSec = sendDiff / timeDiffSecs;
                    SendsPerSecond = sendsPerSec;

                    var recv = Interlocked.Read(ref Received);
                    var recvDiff = recv - _lastReceivedCount;
                    var recvPerSec = recvDiff / timeDiffSecs;
                    ReceivesPerSecond = recvPerSec;

                    _lastSendCount = sent;
                    _lastReceivedCount = recv;
                    _lastRateRead = now;

                    // Update the peak
                    if (sendsPerSec < long.MaxValue && sendsPerSec > PeakSendsPerSecond)
                    {
                        Interlocked.Exchange(ref PeakSendsPerSecond, sendsPerSec);
                    }
                    if (recvPerSec < long.MaxValue && recvPerSec > PeakReceivesPerSecond)
                    {
                        Interlocked.Exchange(ref PeakReceivesPerSecond, recvPerSec);
                    }

                    // Update average
                    AvgSendsPerSecond = _avgLastSentCount / (now - _avgCalcStart).TotalSeconds;
                    AvgReceivesPerSecond = _avgLastReceivedCount / (now - _avgCalcStart).TotalSeconds;

                    // Update tracked connected clients
                    ConnectedClientsTracking = _connectedClients.Count;

                    MessageStoreSize = _messageStore.Value.CurrentMessageCount(); 
                }
                finally
                {
                    _measuringRate = false;
                }
            }, null, 1000, 1000);
            
            //PersistentConnection.ClientConnected += (s) =>
            //{
            //    //Task.Factory.StartNew(() =>
            //    //{
            //    _connectedClients.TryAdd(s, null);
            //        Interlocked.Increment(ref Connects);
            //        Interlocked.Increment(ref ConnectedClients);
            //    //});
            //};

            //PersistentConnection.ClientDisconnected += (s) =>
            //{
            //    //Task.Factory.StartNew(() =>
            //    //{
            //        object client;
            //        _connectedClients.TryRemove(s, out client);
            //        Interlocked.Increment(ref Disconnects);
            //        Interlocked.Decrement(ref ConnectedClients);
            //    //});
            //};

            //PersistentConnection.Sending += () =>
            //{
            //    //Task.Factory.StartNew(() =>
            //    //{
            //    //    Interlocked.Increment(ref Sent);
            //    //});
            //};

            //PersistentConnection.Receiving += () =>
            //{
            //    //Task.Factory.StartNew(() =>
            //    //{
            //    //    Interlocked.Increment(ref Received);
            //    //});
            //};

            //Connection.Sending += () =>
            //{
            //    //Task.Factory.StartNew(() =>
            //    //{
            //    //    Interlocked.Increment(ref Sent);
            //    //});
            //};

            Connection.MessagesPending += (sender, args) =>
            {
                Interlocked.Increment(ref ConnectionsReturnedImmediately);
            };

            Connection.WaitingForSignal += (sender, args) =>
            {
                Interlocked.Increment(ref ConnectionsSubscribedToSignal);
            };

            var onSending = new Action<string>(payload =>
            {
                var payloadSize = Encoding.UTF8.GetByteCount(payload);
                Interlocked.Add(ref BytesSent, payloadSize);
                Interlocked.Add(ref BytesTotal, payloadSize);
                Interlocked.Increment(ref Sent);
                Interlocked.Increment(ref _avgLastSentCount);
            });

            var onReceving = new Action<string>(payload =>
            {
                var payloadSize = Encoding.UTF8.GetByteCount(payload);
                Interlocked.Add(ref BytesReceived, payloadSize);
                Interlocked.Add(ref BytesTotal, payloadSize);
                Interlocked.Increment(ref Received);
                Interlocked.Increment(ref _avgLastReceivedCount);
            });

            ServerSentEventsTransport.Sending += onSending;
            ForeverFrameTransport.Sending += onSending;
            LongPollingTransport.Sending += onSending;

            ServerSentEventsTransport.Receiving += onReceving;
            ForeverFrameTransport.Receiving += onReceving;
            LongPollingTransport.Receiving += onReceving;
        }

        public static void ResetAverage()
        {
            _avgCalcStart = DateTime.UtcNow;
            _avgLastSentCount = 0;
            _avgLastReceivedCount = 0;
        }

        public static ConcurrentDictionary<string, object> ConnectedClientsIds
        {
            get
            {
                return _connectedClients;
            }
        }

        public static long Sent;
        public static long Received;
        public static double SendsPerSecond;
        public static double ReceivesPerSecond;
        public static double TotalPerSecond;
        public static double AvgSendsPerSecond;
        public static double AvgReceivesPerSecond;
        public static double PeakSendsPerSecond;
        public static double PeakReceivesPerSecond;
        public static long ConnectionsReturnedImmediately;
        public static long ConnectionsSubscribedToSignal;
        public static long BytesSent;
        public static long BytesReceived;
        public static long BytesTotal;
        public static long MessageStoreSize;
        public static long ConnectedClients;
        public static long ConnectedClientsTracking;
        public static long Connects;
        public static long Disconnects;

        public static object GetStats()
        {
            return new
            {
                Sent,
                Received,
                SendsPerSecond,
                ReceivesPerSecond,
                TotalPerSecond,
                AvgSentPerSecond = AvgSendsPerSecond,
                AvgReceivedPerSecond = AvgReceivesPerSecond,
                PeakSentPerSecond = PeakSendsPerSecond,
                PeakReceivedPerSecond = PeakReceivesPerSecond,
                BytesSent,
                BytesReceived,
                BytesTotal,
                MessageStoreSize,
                ConnectionsReturnedImmediately,
                ConnectionsSubscribedToSignal,
                ConnectedClients,
                ConnectedClientsTracking,
                Connects,
                Disconnects
            };
        }
    }
}