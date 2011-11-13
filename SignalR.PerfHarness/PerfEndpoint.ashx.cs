using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SignalR.PerfHarness
{
    public class PerfEndpoint : PersistentConnection
    {
        internal static EndpointBehavior Behavior { get; set; }



        protected override void OnReceived(string clientId, string data)
        {
            if (Behavior == EndpointBehavior.Echo)
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
        Echo,
        Broadcast
    }
}