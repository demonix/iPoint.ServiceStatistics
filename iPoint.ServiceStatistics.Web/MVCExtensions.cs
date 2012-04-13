using System.IO;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace iPoint.ServiceStatistics.Web
{
    public static class MvcExtensions
    {
        

        public static string RenderPartialView(this Controller controller, string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = controller.ControllerContext.RouteData.GetRequiredString("action");

            controller.ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);
                var viewContext = new ViewContext(controller.ControllerContext, viewResult.View, controller.ViewData,
                                                  controller.TempData, sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }

        }

         public static string RenderAsString(this IView view, Controller controller)
         {
             using (var sw = new StringWriter())
             {
                 var viewContext = new ViewContext(controller.ControllerContext, view, controller.ViewData,
                                                 controller.TempData, sw);
                 view.Render(viewContext, sw);
                 return sw.GetStringBuilder().ToString();
             }
         }
    }
}