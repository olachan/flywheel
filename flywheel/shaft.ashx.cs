
namespace SignalR.Flywheel
{
    public class Shaft : PersistentConnection
    {
        internal static EndpointBehavior Behavior { get; set; }

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