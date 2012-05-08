using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using SignalR.Hosting.AspNet.Routing;

namespace SignalR.Flywheel
{
    public class Global : System.Web.HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(30);

            RouteTable.Routes.MapConnection<Shaft>("shaft", "shaft/{*operation}");
            StatsHub.Init();
        }
    }
}
