using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace iPoint.ServiceStatistics.Web.Models
{
    public class CounterDrawingParameters
    {


        public CounterDrawingParameters(Guid id, DateTime startDate, DateTime endDate, int counterCategory, int counterName, int counterSource, int counterInstance, int counterExtData)
        {
            DrawerVariableName = "drawer-" + id;
            Id = id;
            StartDate = startDate;
            EndDate = endDate;
            CounterCategory = counterCategory;
            CounterName = counterName;
            CounterSource = counterSource;
            CounterInstance = counterInstance;
            CounterExtData = counterExtData;
        }


        public string DrawerVariableName { get; private set; }
        public Guid Id { get; private set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd.MM.yyyy HH:mm:ss}")]
        public DateTime StartDate { get; private set; }
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd.MM.yyyy HH:mm:ss}")]
        public DateTime EndDate { get; private set; }
        [DisplayName("Counter Category")]
        public int CounterCategory { get; private set; }
        [DisplayName("Counter Name")]
        public int CounterName { get; private set; }
        [DisplayName("Counter Source")]
        public int CounterSource { get; private set; }
        [DisplayName("Counter Instance")]
        public int CounterInstance { get; private set; }
        [DisplayName("Counter ExtData")]
        public int CounterExtData { get; private set; }
    }
}