using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace EnvoyReader.Envoy
{
    public class EnvoyDataProvider
    {
        private readonly string username;
        private readonly string password;
        private readonly string baseUrl;

        public EnvoyDataProvider(string username, string password, string baseUrl)
        {
            this.username = username;
            this.password = password;
            this.baseUrl = baseUrl;
        }

        private HttpClient CreateHttpClient()
        {
            var credentials = new NetworkCredential(username, password);
            var handler = new HttpClientHandler { Credentials = credentials };

            var httpClient = new HttpClient(handler, disposeHandler: true);

            return httpClient;
        }

        public async Task<List<Inverter>> GetInverterInfo()
        {
            using (var httpClient = CreateHttpClient())
            {
                var invertersTask = httpClient.GetStringAsync($"{baseUrl}/api/v1/production/inverters");
                var inventoryTask = httpClient.GetStringAsync($"{baseUrl}/inventory.json");

                var invertersData = await invertersTask;
                var inventoryData = await inventoryTask;

                var inventory = JsonConvert.DeserializeObject<dynamic>(inventoryData) as IEnumerable<dynamic>;

                if (inventory == null)
                {
                    return null;
                }

                var pcuDevices =
                    from i in inventory
                    where i.type == "PCU"
                    select i.devices as IEnumerable<dynamic>;

                if (pcuDevices == null || pcuDevices.Count() == 0)
                {
                    return null;
                }

                var inverterDevices =
                    from device in pcuDevices.FirstOrDefault()
                    select new DeviceInfo()
                    {
                        PartNum = device.part_num,
                        Installed = device.installed,
                        SerialNum = device.serial_num,
                        DeviceStatus = device.device_status.ToObject<List<string>>(),
                        LastReportDate = device.last_rpt_date,
                        AdminState = device.admin_state,
                        DevType = device.dev_type,
                        CreatedDate = device.created_date,
                        ImageLoadDate = device.img_load_date,
                        ImagePnumRunning = device.img_pnum_running,
                        Ptpn = device.ptpn,
                        ChaneId = device.chaneid,
                        Producing = device.producing,
                        Communicating = device.communicating,
                        Provisioned = device.provisioned,
                        Operating = device.operating
                    };

                var inverters = JsonConvert.DeserializeObject<List<InverterProduction>>(invertersData);

                if (inverters == null)
                {
                    return null;
                }

                return inverters
                    .Select(i => new Inverter(i, inverterDevices.FirstOrDefault(d => d.SerialNum == i.SerialNumber)))
                    .ToList();
            }
        }

        public async Task<List<SystemProduction>> GetSystemProduction()
        {
            using (var httpClient = CreateHttpClient())
            {
                var jsonData = await httpClient.GetStringAsync($"{baseUrl}/production.json");

                var list = JsonConvert.DeserializeObject<SystemProductionList>(jsonData);

                return list?.Production;
            }
        }
    }
}
