using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using CountersDataLayer;
using iPoint.ServiceStatistics.Server;
using iPoint.ServiceStatistics.Web.Models;

namespace iPoint.ServiceStatistics.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );


            /*routes.MapRoute(
                "SingleGraphDefaultDimensions", // Route name
                "{controller}/{action}/{param}", // URL with parameters
                new { controller = "Counters", action = "SingleGraph"} // Parameter defaults
            );*/


        }

        protected void Application_Start()
        {
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine());
            //ModelBinders.Binders[typeof(CounterQueryParameters)] = new CounterQueryParametersBinder();
            AreaRegistration.RegisterAllAreas();
            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
            InitDatabase();
            new Settings();
        }

        private void InitDatabase()
        {
            string mongoUrl = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings\\mongoConnection"));
            CountersDatabase.InitConnection(mongoUrl);
        }
    }
}