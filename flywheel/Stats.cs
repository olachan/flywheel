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

                var now = DateTime.UtcNow;
                var timeDiffSecs = (now - _lastRateRead).TotalSeconds;

                var sent = Interlocked.Read(ref Sent);
                var sendDiff = sent - _lastSendCount;
                var sendsPerSec = sendDiff / timeDiffSecs;
                SentPerSecond = sendsPerSec;

                var recv = Interlocked.Read(ref Received);
                var recvDiff = recv - _lastReceivedCount;
                var recvPerSec = recvDiff / timeDiffSecs;
                ReceivedPerSecond = recvPerSec;

                _lastSendCount = sent;
                _lastReceivedCount = recv;
                _lastRateRead = now;

                // Update average
                AvgSentPerSecond = _avgLastSentCount / (now - _avgCalcStart).TotalSeconds;
                AvgReceivedPerSecond = _avgLastReceivedCount / (now - _avgCalcStart).TotalSeconds;

                // Update tracked connected clients
                ConnectedClientsTracking = _connectedClients.Count;

                _measuringRate = false;
            }, null, 1000, 1000);

            PersistentConnection.ClientConnected += (s) =>
            {
                //Task.Factory.StartNew(() =>
                //{
                _connectedClients.TryAdd(s, null);
                    Interlocked.Increment(ref Connects);
                    Interlocked.Increment(ref ConnectedClients);
                //});
            };

            PersistentConnection.ClientDisconnected += (s) =>
            {
                //Task.Factory.StartNew(() =>
                //{
                    object client;
                    _connectedClients.TryRemove(s, out client);
                    Interlocked.Increment(ref Disconnects);
                    Interlocked.Decrement(ref ConnectedClients);
                //});
            };

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

            LongPollingTransport.Sending += (payload) =>
            {
                //Task.Factory.StartNew(() =>
                //{
                    var payloadSize = Encoding.UTF8.GetBytes(payload).Length;
                    Interlocked.Add(ref BytesSent, payloadSize);
                    Interlocked.Add(ref BytesTotal, payloadSize);
                    Interlocked.Increment(ref Sent);
                    Interlocked.Increment(ref _avgLastSentCount);
                //});
            };

            LongPollingTransport.Receiving += (payload) =>
            {
                //Task.Factory.StartNew(() =>
                //{
                    var payloadSize = Encoding.UTF8.GetBytes(payload).Length;
                    Interlocked.Add(ref BytesReceived, payloadSize);
                    Interlocked.Add(ref BytesTotal, payloadSize);
                    Interlocked.Increment(ref Received);
                    Interlocked.Increment(ref _avgLastReceivedCount);

                //});
            };
        }

        public static void ResetAverage()
        {
            _avgCalcStart = DateTime.UtcNow;
            _avgLastSentCount = 0;
            _avgLastReceivedCount = 0;
        }

        public static long Sent;
        public static long Received;
        public static double SentPerSecond;
        public static double ReceivedPerSecond;
        public static double TotalPerSecond;
        public static double AvgSentPerSecond;
        public static double AvgReceivedPerSecond;
        public static long BytesSent;
        public static long BytesReceived;
        public static long BytesTotal;
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
                SentPerSecond,
                ReceivedPerSecond,
                TotalPerSecond,
                AvgSentPerSecond,
                AvgReceivedPerSecond,
                BytesSent,
                BytesReceived,
                BytesTotal,
                ConnectedClients,
                ConnectedClientsTracking,
                Connects,
                Disconnects
            };
        }
    }
}