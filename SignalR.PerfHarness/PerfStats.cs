using System;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Transports;
using System.Text;

namespace SignalR.PerfHarness
{
    internal static class PerfStats
    {
        public static void Init()
        {
            PersistentConnection.ClientConnected += (s) =>
            {
                //Task.Factory.StartNew(() =>
                //{
                    Interlocked.Increment(ref Connects);
                    Interlocked.Increment(ref ConnectedClients);
                //});
            };

            PersistentConnection.ClientDisconnected += (s) =>
            {
                //Task.Factory.StartNew(() =>
                //{
                    Interlocked.Increment(ref Disconnects);
                    Interlocked.Decrement(ref ConnectedClients);
                //});
            };

            PersistentConnection.Sending += () =>
            {
                //Task.Factory.StartNew(() =>
                //{
                //    Interlocked.Increment(ref Sent);
                //});
            };

            PersistentConnection.Receiving += () =>
            {
                //Task.Factory.StartNew(() =>
                //{
                //    Interlocked.Increment(ref Received);
                //});
            };

            Connection.Sending += () =>
            {
                //Task.Factory.StartNew(() =>
                //{
                //    Interlocked.Increment(ref Sent);
                //});
            };

            LongPollingTransport.Sending += (payload) =>
            {
                //Task.Factory.StartNew(() =>
                //{
                    var payloadSize = Encoding.UTF8.GetBytes(payload).Length;
                    Interlocked.Add(ref BytesSent, payloadSize);
                    Interlocked.Add(ref BytesTotal, payloadSize);
                    Interlocked.Increment(ref Sent);
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
                //});
            };
        }

        public static long Sent;
        public static long Received;
        public static long SentPerSecond;
        public static long ReceivedPerSecond;
        public static long TotalPerSecond;
        public static long BytesSent;
        public static long BytesReceived;
        public static long BytesTotal;
        public static long ConnectedClients;
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
                BytesSent,
                BytesReceived,
                BytesTotal,
                ConnectedClients,
                Connects,
                Disconnects
            };
        }
    }
}