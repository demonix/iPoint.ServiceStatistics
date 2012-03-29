using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
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
        public virtual ActionResult CountersGraph(string sd, string ed, int cc, int cn, int cs, int ci, int ced)
        {
            DateTime beginDate = ParseDate(sd, DateTime.Now.AddHours(-1)); 
            DateTime endDate = ParseDate(ed, DateTime.Now.AddHours(1));
            Guid id = Guid.NewGuid();

            return PartialView(new CounterDrawingParameters(id, beginDate,endDate,cc,cn,cs,ci,ced));
        }

        private DateTime ParseDate(string dateString, DateTime defaultDate)
        {
            DateTime date;
            if (!DateTime.TryParseExact(dateString, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                date = defaultDate;
            return date;
        }
        
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public virtual JsonResult CounterData(string sd, string ed, int cc, int cn, int cs, int ci, int ced)
        {
            DateTime beginDate = ParseDate(sd, DateTime.Now.AddHours(-1));
            DateTime endDate = ParseDate(ed, DateTime.Now.AddHours(1));

            List<CounterExtDataInfo> extDatas;
            List<CounterInstanceInfo> insts;
            List<CounterSourceInfo> sources;
            extDatas =
                CountersDatabase.Instance.New_GetCounterExtDatas(cc, cn).Where(
                    d => (ced == -1 && d.Name != "ALL_EXTDATA") || (ced != -1 && d.Id == ced)).ToList();
            insts =
                CountersDatabase.Instance.New_GetCounterInstances(cc, cn).Where(
                    d => (ci == -1 && d.Name != "ALL_INSTANCES") || (ci != -1 && d.Id == ci)).ToList();
            sources =
                CountersDatabase.Instance.New_GetCounterSources(cc, cn).Where(
                    d => (cs == -1 && d.Name != "ALL_SOURCES") || (cs != -1 && d.Id == cs)).ToList();

            List<object> seriesData2 = new List<object>();
            DateTime dt = DateTime.Now;
            foreach (CounterSourceInfo source in sources)
            {
                foreach (CounterInstanceInfo instance in insts)
                {
                    foreach (CounterExtDataInfo extData in extDatas)
                    {
                        Dictionary<string, List<CounterData>> series = CountersDatabase.Instance.GetCounterData2(beginDate,
                                                                                                                  endDate, cc,
                                                                                                                  cn, source.Id, instance.Id,
                                                                                                                  extData.Id);

                        dt = series.Count == 0
                                 ? dt
                                 : series.First().Value.Last().DateTime.ToLocalTime();

                        seriesData2.AddRange(series.Select(d => new
                        {
                            label = source.Name + "_"+ instance.Name +"_" + extData.Name + "_" + d.Key,
                            data = d.Value.Select(v => new List<object> {v.DateTime.ToLocalTime().Ticks/TimeSpan.TicksPerMillisecond, v.Value })
                        }));
                    }
                }
            }

            return Json(new { osd = sd, oed = ed, originalBegin = beginDate.ToString("dd.MM.yyyy HH:mm:ss"), originalEnd = endDate.ToString("dd.MM.yyyy HH:mm:ss"), lastDate = dt.ToString("dd.MM.yyyy HH:mm:ss"), seriesData = seriesData2 }, JsonRequestBehavior.AllowGet);
        }
    }
}
