using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace iPoint.ServiceStatistics.Web.Models
{
    public class CounterQueryParameters
    {
        public CounterQueryParameters()
        {
            StartDate = DateTime.Now.AddMinutes(-15);
            EndDate= DateTime.Now;
            CounterCategories = new SelectList(Enumerable.Empty<string>());
            CounterNames = new SelectList(Enumerable.Empty<string>());
            CounterSources = new SelectList(Enumerable.Empty<string>());
            CounterInstances = new SelectList(Enumerable.Empty<string>());
            CounterExtDatas = new SelectList(Enumerable.Empty<string>());
        }

        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd.MM.yyyy HH:mm:ss}")]
        public DateTime StartDate { get; private set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd.MM.yyyy HH:mm:ss}")]
        public DateTime EndDate { get; private set; }
        [DisplayName("Counter Category")]
        public string CounterCategory { get; private set; }
        public SelectList CounterCategories { get; private set; }
        [DisplayName("Counter Name")]
        public string CounterName { get; private set; }
        public SelectList CounterNames { get; private set; }
        [DisplayName("Counter Source")]
        public string CounterSource { get; private set; }
        public SelectList CounterSources { get; private set; }
        [DisplayName("Counter Instance")]
        public string CounterInstance { get; private set; }
        public SelectList CounterInstances { get; private set; }
        [DisplayName("Counter ExtData")]
        public string CounterExtData { get; private set; }
        public SelectList CounterExtDatas { get; private set; }
    }
}