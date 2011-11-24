using System.Threading;
using System.Web;

namespace SignalR.Flywheel
{
    public class Shaft : PersistentConnection
    {
        internal static EndpointBehavior Behavior { get; set; }

        protected override void OnConnected(HttpContextBase context, string clientId)
        {
            Stats.ConnectedClientsIds.TryAdd(clientId, null);
            Interlocked.Increment(ref Stats.Connects);
            Interlocked.Increment(ref Stats.ConnectedClients);
        }

        protected override void OnDisconnect(string clientId)
        {
            object client;
            Stats.ConnectedClientsIds.TryRemove(clientId, out client);
            Interlocked.Increment(ref Stats.Disconnects);
            Interlocked.Decrement(ref Stats.ConnectedClients);
        }

        protected override void OnReceived(string clientId, string data)
        {
            if (Behavior == EndpointBehavior.DirectEcho)
            {
                Send(data);
            }
            else if (Behavior == EndpointBehavior.Echo)
            {
                Connection.Send(data);
            }
            else if (Behavior == EndpointBehavior.Broadcast)
            {
                Connection.Broadcast(data);
            }
        }
    }

    public enum EndpointBehavior
    {
        ListenOnly,
        DirectEcho,
        Echo,
        Broadcast
    }
}