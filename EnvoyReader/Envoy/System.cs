using System.Collections.Generic;

namespace Envoy
{
    public class Production
    {
        public string Type { get; set; }
        public int ActiveCount { get; set; }
        public int ReadingTime { get; set; }
        public int WNow { get; set; }
        public int WhLifeTime { get; set; }
    }

    public class SystemProduction
    {
        public List<Production> Production { get; set; }
    }
}
