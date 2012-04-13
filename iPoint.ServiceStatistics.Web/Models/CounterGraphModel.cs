using System;

namespace iPoint.ServiceStatistics.Web.Models
{
    public class CounterGraphModel
    {
        public Guid Id { get; private set; }
        public CounterGraphModel(Guid drawerId)
        {
            Id = drawerId;
        }
    }
}