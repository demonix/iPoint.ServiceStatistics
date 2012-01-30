using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using iPoint.ServiceStatistics.Server.DataLayer;
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
            IEnumerable<string> list = CountersDatabase.Instance.GetCounterCategories(beginDate, endDate);
            var dl = list.Distinct().ToList();
            var data = dl.Select(i => new SelectListItem()
            {
                Text = i,
                Value =  i
            });

            return Json(data , JsonRequestBehavior.AllowGet);
        }

        public virtual JsonResult CounterNames(string sd, string ed, string cc)
        {

            DateTime beginDate = DateTime.ParseExact(sd, "dd.MM.yyyy HH:mm:ss",CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(ed,"dd.MM.yyyy HH:mm:ss",CultureInfo.InvariantCulture);
            IEnumerable<string> list = CountersDatabase.Instance.GetCounterNames(beginDate, endDate,cc);
            var data = list.Distinct().Select(i => new SelectListItem()
            {
                Text = i,
                Value = i
            });
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public virtual JsonResult CounterDetails(string sd, string ed, string cc, string cn)
        {
            DateTime beginDate = DateTime.ParseExact(sd,"dd.MM.yyyy HH:mm:ss",CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(ed,"dd.MM.yyyy HH:mm:ss",CultureInfo.InvariantCulture);
            List<CounterDetail> list = CountersDatabase.Instance.GetCounterDetails(beginDate, endDate, cc, cn).ToList();

            var data = new
                           {
                               Sources = list.Select(i => i.Source).Distinct().Select(s => new SelectListItem()
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
                                                               })
                           };
            

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public virtual ActionResult Index()
        {
            return View(new CounterQueryParameters());
        }

        public virtual JsonResult CounterData(string sd, string ed, string cc, string cn, string cs, string ci, string ced)
        {
            DateTime beginDate = DateTime.ParseExact(sd, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            DateTime endDate;
            if (!DateTime.TryParseExact(ed, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None,  out endDate))
                endDate = DateTime.Now.AddDays(1);
            List<List<object>> series = CountersDatabase.Instance.GetCounterData(beginDate, endDate, cc, cn,cs, ci, ced).ToList();
            return Json(new { lastDate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), seriesData = series}, JsonRequestBehavior.AllowGet);
        }


    }

    
}
