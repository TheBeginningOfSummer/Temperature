namespace Temperature
{
    public class TemperaturePoint
    {
        public string Name { get; set; } = "T1";
        public int Temperature { get; set; }
        public int IntervalTime { get; set; }
        public int Air { get; set; }

        public TemperaturePoint(string name, int temp, int interval, int air)
        {
            Name = name;
            Temperature = temp;
            IntervalTime = interval;
            Air = air;
        }

        public TemperaturePoint() { }
    }
}
