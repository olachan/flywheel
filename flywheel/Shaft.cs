using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Hosting;
using SignalR.Hubs;

namespace SignalR.Flywheel
{
    public class ShaftHub : Hub, IConnected, IDisconnect
    {
        public Task Connect()
        {
            Stats.ConnectedClientsIds.TryAdd(Context.ConnectionId, null);
            Interlocked.Increment(ref Stats.Connects);
            Interlocked.Increment(ref Stats.ConnectedClients);
            return TaskHelpers.Done;
        }

        public Task Reconnect(IEnumerable<string> groups)
        {
            return null;
        }

        public Task Disconnect()
        {
            object client;
            Stats.ConnectedClientsIds.TryRemove(Context.ConnectionId, out client);
            Interlocked.Increment(ref Stats.Disconnects);
            Interlocked.Decrement(ref Stats.ConnectedClients);
            return TaskHelpers.Done;
        }

        public Task Echo(string data)
        {
            return Caller.invoke(data);
        }

        public Task Broadcast(string data)
        {
            return Clients.invoke(data);
        }
    }

    public class Shaft : PersistentConnection
    {
        internal static EndpointBehavior Behavior { get; set; }

        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {
            Stats.ConnectedClientsIds.TryAdd(connectionId, null);
            Interlocked.Increment(ref Stats.Connects);
            Interlocked.Increment(ref Stats.ConnectedClients);
            return TaskHelpers.Done;
        }

        protected override Task OnDisconnectAsync(string connectionId)
        {
            object client;
            Stats.ConnectedClientsIds.TryRemove(connectionId, out client);
            Interlocked.Increment(ref Stats.Disconnects);
            Interlocked.Decrement(ref Stats.ConnectedClients);
            return TaskHelpers.Done;
        }

        protected override Task OnReceivedAsync(IRequest request, string connectionId, string data)
        {
            if (Behavior == EndpointBehavior.Echo)
            {
                Connection.Send(connectionId, data);
            }
            else if (Behavior == EndpointBehavior.Broadcast)
            {
                Connection.Broadcast(data);
            }
            return TaskHelpers.Done;
        }
    }

    public enum EndpointBehavior
    {
        ListenOnly,
        Echo,
        Broadcast
    }
}