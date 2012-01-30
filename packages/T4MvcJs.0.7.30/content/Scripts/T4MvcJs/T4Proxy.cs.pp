using System.Web.Routing;

namespace T4MvcJs
{
    public class T4Proxy
    {
        public static RouteCollection GetRoutes()
        {
            var routes = new RouteCollection();
            //#MvcApplication.RegisterRoutes#(routes);
            return routes;
        }
    }
}