namespace CountersDataLayer.CountersCache
{
    public class CounterExtDataInfo
    {
        public CounterExtDataInfo(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; private set; }
        public int Id { get; private set; }
    }
}