using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Text;

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

        public List<Inverter> GetInverterProduction()
        {
            using (var webClient = new WebClient())
            {
                webClient.Credentials = new NetworkCredential(username, password);
                webClient.Encoding = Encoding.UTF8;

                var jsonData = webClient.DownloadString($"{baseUrl}/api/v1/production/inverters");

                return JsonConvert.DeserializeObject<List<Inverter>>(jsonData);
            }
        }

        public List<SystemProduction> GetSystemProduction()
        {
            using (var webClient = new WebClient())
            {
                webClient.Credentials = new NetworkCredential(username, password);
                webClient.Encoding = Encoding.UTF8;

                var jsonData = webClient.DownloadString($"{baseUrl}/production.json");

                var list = JsonConvert.DeserializeObject<SystemProductionList>(jsonData);

                return list.Production;
            }
        }
    }
}
