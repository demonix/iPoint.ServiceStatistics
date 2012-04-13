using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using iPoint.ServiceStatistics.Server;
using iPoint.ServiceStatistics.Server.DataLayer;
using iPoint.ServiceStatistics.Server.КэшСчетчиков;
using iPoint.ServiceStatistics.Web.Models;

namespace iPoint.ServiceStatistics.Web.Controllers
{
    public partial class CountersController : Controller
    {
        protected string RenderPartialViewToString(string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.RouteData.GetRequiredString("action");

            ViewData.Model = model;

            using (StringWriter sw = new StringWriter())
            {
                ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }

        
        public virtual JsonResult Data(string sd, string ed, string cc, string cn, string cs, string ci, string ced)
        {
            return Json(new
                            {
                                Success = true,
                                StartDate = sd,
                                EndDate = ed,
                                CounterCategory = cc,
                                CounterName = cn,
                                CounterSource = cs,
                                CounterInstance = ci,
                                CounterExtData = ced,
                            },
                        JsonRequestBehavior.AllowGet);
        }

        public virtual JsonResult CounterCategories(string sd, string ed)
        {
            DateTime beginDate = DateTime.Parse(sd);
            DateTime endDate = DateTime.Parse(ed);
            IEnumerable<CounterCategoryInfo> list = CountersDatabase.Instance.GetCounterCategories2();
            var dl = list.ToList();
            var data = dl.Select(i => new SelectListItem()
            {
                Text = i.Name,
                Value =  i.Id.ToString()
            });

            return Json(data , JsonRequestBehavior.AllowGet);
        }

        public virtual JsonResult CounterNames(string sd, string ed, int cc)
        {
            DateTime beginDate = DateTime.ParseExact(sd, "dd.MM.yyyy HH:mm:ss",CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(ed,"dd.MM.yyyy HH:mm:ss",CultureInfo.InvariantCulture);
            IEnumerable<CounterNameInfo> list = CountersDatabase.Instance.GetCounterNames2(cc);
            var data = list.Distinct().Select(i => new SelectListItem()
            {
                Text = i.Name,
                Value = i.Id.ToString()
            });
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public virtual JsonResult CounterDetails(string sd, string ed, int cc, int cn)
        {
            DateTime beginDate = DateTime.ParseExact(sd, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(ed, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            IEnumerable<SelectListItem> sourcesListItems =
                CountersDatabase.Instance.New_GetCounterSources(cc, cn).Select(s => new SelectListItem()
                                                                                        {
                                                                                            Text = s.Name,
                                                                                            Value = s.Id.ToString(CultureInfo.InvariantCulture)
                                                                                        });
            IEnumerable<SelectListItem> instancesListItems =
                CountersDatabase.Instance.New_GetCounterInstances(cc, cn).Select(s => new SelectListItem()
                                                                                          {
                                                                                              Text = s.Name,
                                                                                              Value = s.Id.ToString(CultureInfo.InvariantCulture)
                                                                                          });
            IEnumerable<SelectListItem> extDatasListItems =
                CountersDatabase.Instance.New_GetCounterExtDatas(cc, cn).Select(s => new SelectListItem()
                                                                                         {
                                                                                             Text = s.Name,
                                                                                             Value = s.Id.ToString(CultureInfo.InvariantCulture)
                                                                                         });
            var data = new
                           {
                               Sources = sourcesListItems,
                               Instances = instancesListItems,
                               ExtDatas = extDatasListItems
                           };


            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public virtual ActionResult Index()
        {
            return View(new CounterQueryParameters());
        }

        [OutputCache(Location =  OutputCacheLocation.None)]
        public virtual JsonResult CountersGraph()
        {
            Guid id = Guid.NewGuid();
            var view = RenderPartialViewToString("CountersGraph", new CounterGraphModel(id));
            return Json(new {id = id, graphingSurface = view},JsonRequestBehavior.AllowGet);
        }

        private DateTime ParseDate(string dateString, DateTime defaultDate)
        {
            DateTime date;
            if (!DateTime.TryParseExact(dateString, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                date = defaultDate;
            return date;
        }

        
        
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public virtual JsonResult CounterData(string sd, string ed, int cc, int cn, int cs, int ci, int ced, string series)
        //public virtual JsonResult CounterData([Bind(Exclude = "")]CounterDataParameters parameters)
        {
            CounterDataParameters parameters = new CounterDataParameters(sd, ed, cc, cn, cs, ci, ced, series);

            List<object> allSeriesData = new List<object>();
            DateTime dt = DateTime.Now;


            var allSeriesRawData = parameters.Sources.AsParallel().SelectMany(
                source => parameters.Instances.AsParallel().SelectMany(
                instance => parameters.ExtendedDatas.AsParallel().Select(
                extData => 
                    new Tuple<string, string, string, Dictionary<string, List<CounterData>>> (source.Name, instance.Name,extData.Name,
                    CountersDatabase.Instance.GetCounterData(parameters.BeginDate,parameters.EndDate,
                    parameters.CounterCategoryId,parameters.CounterNameId,source.Id,instance.Id,extData.Id, parameters.Series))))).ToList();

            foreach (var seriesRawData in allSeriesRawData)
            {
                allSeriesData.AddRange(seriesRawData.Item4.Select(d => new
                {
                    source = seriesRawData.Item1,
                    instance = seriesRawData.Item2,
                    extData = seriesRawData.Item3,
                    seriesName = d.Key,
                    data = d.Value.Select(v => new List<object> { v.DateTime.ToLocalTime().Ticks / TimeSpan.TicksPerMillisecond, v.Value })
                }));
            }
            #region todelete
            /*
            foreach (CounterSourceInfo source in parameters.Sources)
            {
                foreach (CounterInstanceInfo instance in parameters.Instances)
                {
                    foreach (CounterExtDataInfo extData in parameters.ExtendedDatas)
                    {
                        Dictionary<string, List<CounterData>> series =
                            CountersDatabase.Instance.GetCounterData(parameters.BeginDate,
                                                                     parameters.EndDate, parameters.CounterCategoryId,
                                                                     parameters.CounterNameId, source.Id, instance.Id,
                                                                     extData.Id);

                        dt = series.Count == 0
                                 ? dt
                                 : series.First().Value.Last().DateTime.ToLocalTime();

                        allSeriesData.AddRange(series.Select(d => new
                        {
                            label = source.Name + "_"+ instance.Name +"_" + extData.Name + "_" + d.Key,
                            data = d.Value.Select(v => new List<object> {v.DateTime.ToLocalTime().Ticks/TimeSpan.TicksPerMillisecond, v.Value })
                        }));
                    }
                }
            }*/
#endregion

            return Json(new { lastDate = allSeriesRawData.Max(s => s.Item4.Count == 0 ? dt : s.Item4.Last().Value.Max(v => v.DateTime)).ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"), seriesData = allSeriesData }, JsonRequestBehavior.AllowGet);
        }

        
    }


}
