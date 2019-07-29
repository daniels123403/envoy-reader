using System;
using System.Collections.Generic;
using System.Text;

namespace EnvoyReader.Envoy
{
    public class DeviceInfo
    {
        public string PartNum { get; set; }
        public int Installed { get; set; }
        public string SerialNum { get; set; }
        public List<string> DeviceStatus { get; set; }
        public int LastReportDate { get; set; }
        public int AdminState { get; set; }
        public int DevType { get; set; }
        public int CreatedDate { get; set; }
        public int ImageLoadDate { get; set; }
        public string ImagePnumRunning { get; set; }
        public string Ptpn { get; set; }
        public string ChaneId { get; set; }
        public bool Producing { get; set; }
        public bool Communicating { get; set; }
        public bool Provisioned { get; set; }
        public bool Operating { get; set; }
    }
}
