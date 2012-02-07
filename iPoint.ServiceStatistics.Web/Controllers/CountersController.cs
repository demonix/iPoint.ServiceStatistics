using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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
            DateTime beginDate = DateTime.ParseExact(sd,"dd.MM.yyyy HH:mm:ss",CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(ed,"dd.MM.yyyy HH:mm:ss",CultureInfo.InvariantCulture);
            //List<CounterDetail> list = CountersDatabase.Instance.GetCounterDetails(beginDate, endDate, cc, cn).ToList();

            var data = new
                           {
                               Sources = CountersDatabase.Instance.New_GetCounterSources(cc,cn).Select(s => new SelectListItem()
                                                              {
                                                                  Text = s.Name,
                                                                  Value = s.Id.ToString()
                                                              }),
                               Instances = CountersDatabase.Instance.New_GetCounterInstances(cc, cn).Select(s => new SelectListItem()
                               {
                                   Text = s.Name,
                                   Value = s.Id.ToString()
                               }),
                               ExtDatas = CountersDatabase.Instance.New_GetCounterExtDatas(cc, cn).Select(s => new SelectListItem()
                               {
                                   Text = s.Name,
                                   Value = s.Id.ToString()
                               }),
                               /*Sources = list.Select(i => i.Source).Distinct().Select(s => new SelectListItem()
                                                              {
                                                                  Text = s,
                                                                  Value = s
                                                              }),
                               Instances = list.Select(i => i.Instance).Distinct().Select(i => new SelectListItem()
                                                                {
                                                                    Text = i,
                                                                    Value = i
                                                                }),
                               ExtDatas = list.Where(i => i.ExtData !=null).Select(i => i.ExtData).Distinct().Select(e => new SelectListItem()
                                                               {
                                                                   Text = e,
                                                                   Value = e
                                                               })*/
                           };
            

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public virtual ActionResult Index()
        {
            return View(new CounterQueryParameters());
        }

        public virtual JsonResult CounterData(string sd, string ed, int cc, int cn, int cs, int ci, int ced)
        {
            
            DateTime beginDate = DateTime.ParseExact(sd, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            DateTime endDate;
            if (!DateTime.TryParseExact(ed, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None,  out endDate))
                endDate = DateTime.Now.AddDays(1);
            Dictionary<string, List<List<object>>> series = CountersDatabase.Instance.GetCounterData2(beginDate, endDate, cc, cn, cs, ci, ced);
            DateTime dt = series.Count == 0 ? DateTime.Now : new DateTime((long)series.First().Value.Last()[0] * TimeSpan.TicksPerMillisecond);
            var seriesData2 = series.Select(d => new 
                                                    {
                                                        label = d.Key,
                                                        data = d.Value
                                                    });
            return Json(new { lastDate = dt.ToString("dd.MM.yyyy HH:mm:ss"), seriesData = seriesData2 }, JsonRequestBehavior.AllowGet);
        }


      
    }


    
}
