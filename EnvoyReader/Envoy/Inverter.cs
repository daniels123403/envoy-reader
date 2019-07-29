using System;
using System.Collections.Generic;
using System.Text;

namespace EnvoyReader.Envoy
{
    public class Inverter
    {
        public InverterProduction Production { get; private set; }
        public DeviceInfo DeviceInfo { get; private set; }

        public Inverter(InverterProduction production, DeviceInfo deviceInfo)
        {
            Production = production;
            DeviceInfo = deviceInfo;
        }
    }
}
