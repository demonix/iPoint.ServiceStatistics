﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using Aggregation;
using CountersDataLayer;
using CountersDataLayer.CountersCache;
using iPoint.ServiceStatistics.Server;
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
                Value =  i.Id.ToString(CultureInfo.InvariantCulture)
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
                CountersDatabase.Instance.GetCounterSources(cc, cn).Select(s => new SelectListItem()
                                                                                        {
                                                                                            Text = s.Name,
                                                                                            Value = s.Id.ToString(CultureInfo.InvariantCulture)
                                                                                        });
            IEnumerable<SelectListItem> instancesListItems =
                CountersDatabase.Instance.GetCounterInstances(cc, cn).Select(s => new SelectListItem()
                                                                                          {
                                                                                              Text = s.Name,
                                                                                              Value = s.Id.ToString(CultureInfo.InvariantCulture)
                                                                                          });
            IEnumerable<SelectListItem> extDatasListItems =
                CountersDatabase.Instance.GetCounterExtDatas(cc, cn).Select(s => new SelectListItem()
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

        [OutputCache(Location =  OutputCacheLocation.None, NoStore = true)]
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
        {
            CounterDataParameters parameters = new CounterDataParameters(sd, ed, cc, cn, cs, ci, ced, series);

            List<object> allSeriesData = new List<object>();
            DateTime dt = DateTime.Now;

            List<CounterSeriesData> allCounterSeriesData = parameters.Sources.AsParallel().SelectMany(
                source => parameters.Instances.AsParallel().SelectMany(
                    instance => parameters.ExtendedDatas.AsParallel().SelectMany(
                        extData =>
                        CountersDatabase.Instance.GetCounterData(parameters.BeginDate, parameters.EndDate,
                                                                    parameters.CounterCategoryId,
                                                                    parameters.CounterNameId, source.Id, instance.Id,
                                                                    extData.Id, parameters.Series)
                                    ))).ToList();

            foreach (var counterSeriesData in allCounterSeriesData)
            {
                allSeriesData.Add(
                    new
                        {
                            yaxis = counterSeriesData.ValueType == UniversalValue.UniversalClassType.TimeSpan ? 2 : 1,
                            source = counterSeriesData.CounterSource,
                            instance = counterSeriesData.CounterInstance,
                            extData = counterSeriesData.CounterExtData,
                            counterName = counterSeriesData.CounterName,
                            counterCategory = counterSeriesData.CounterCategory,
                            seriesName = counterSeriesData.SeriesName,
                            data = counterSeriesData.Points.Select(p => p==null? null :new List<object> {p.DateTime.ToLocalTime().Ticks/TimeSpan.TicksPerMillisecond, p.Value}),
                            uniqId = counterSeriesData.UniqId
                        });
            }
            if (allSeriesData.Count == 0 && String.IsNullOrEmpty(ed))
                return Json(null, JsonRequestBehavior.AllowGet);
            return
                Json(
                    new
                        {
                            success = true,
                            lastDate =
                        (dt < parameters.EndDate ? dt : parameters.EndDate).ToString("dd.MM.yyyy HH:mm:ss"),
                            seriesData = allSeriesData
                        }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public virtual JsonResult SaveGraph(string data)
        {
            Guid gd = Guid.NewGuid();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "savedGraphs\\" + gd.ToString());
            System.IO.File.WriteAllText(path, data);
            return Json(new {id = gd.ToString()});
        }

        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public virtual JsonResult SavedGraph(string id)
        {
            List<Tuple<string, string>> result = LoadSavedGraphs(id);
            return Json(result.Select(t => new { surfaceName = t.Item1, parameters = t.Item2 }), JsonRequestBehavior.AllowGet);
        }

        private List<Tuple<string, string>> LoadSavedGraphs(string path)
        {
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();
            if (path.Contains("..\\") || path.Contains(".\\") || path.Contains("\\"))
            {
                return null;
            }
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "savedGraphs\\" + path);
            if (!System.IO.File.Exists(path))
            {
                return null;
            }
            string[] savedData = System.IO.File.ReadAllLines(path);
            for (int i = 0; i <= savedData.Length - 2; i = i + 2)
            {
                string surfaceName = savedData[i];
                string parameters = "[" +
                                    String.Join(",",
                                                Encoding.UTF8.GetString(Convert.FromBase64String(savedData[i + 1])).Split(
                                                    new[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries)) + "]";
                result.Add(new Tuple<string,string>(surfaceName, parameters));
            }
            return result;
        }

        public virtual ActionResult SingleGraph(string name, string param, int width = 800, int height = 600, bool saved = false)
        {
            string graphName, parameters;
            if (saved)
            {
                var graphs = LoadSavedGraphs(param);
                    if (graphs == null || graphs.Count == 0) 
                        throw new Exception("cannot load graph from " + param) ;
                    graphName = graphs[0].Item1;
                    parameters = graphs[0].Item2;

            } else
            {
                string[] drawersParameters = Encoding.UTF8.GetString(Convert.FromBase64String(param)).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                graphName = name;
                parameters = "[" + String.Join(",", drawersParameters) +"]";
                
            }

            SingleGraphModel model = new SingleGraphModel(graphName, parameters,width, height);
            return View(model);
        }

        
    }


}
