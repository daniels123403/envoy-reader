using System.Collections.Generic;

namespace EnvoyReader.Envoy
{
    public class SystemProduction
    {
        public string Type { get; set; }
        public int ActiveCount { get; set; }
        public int ReadingTime { get; set; }
        public int WNow { get; set; }
        public int WhLifeTime { get; set; }
    }

    public class SystemProductionList
    {
        public List<SystemProduction> Production { get; set; }
    }
}
