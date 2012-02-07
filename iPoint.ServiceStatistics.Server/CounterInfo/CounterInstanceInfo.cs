namespace iPoint.ServiceStatistics.Server.CounterInfo
{
    public class CounterInstanceInfo
    {
        public CounterInstanceInfo(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; private set; }
        public int Id { get; private set; }
    }
}