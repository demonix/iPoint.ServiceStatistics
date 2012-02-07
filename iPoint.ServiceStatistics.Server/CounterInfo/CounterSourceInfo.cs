namespace iPoint.ServiceStatistics.Server.CounterInfo
{
    public class CounterSourceInfo
    {
        public CounterSourceInfo(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; private set; }
        public int Id { get; private set; }
    }
}