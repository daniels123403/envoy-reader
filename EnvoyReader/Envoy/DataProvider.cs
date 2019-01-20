using Newtonsoft.Json;
using System.Collections.Generic;
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

        public async Task<List<Inverter>> GetInverterProduction()
        {
            using (var httpClient = CreateHttpClient())
            {
                var jsonData = await httpClient.GetStringAsync($"{baseUrl}/api/v1/production/inverters");

                return JsonConvert.DeserializeObject<List<Inverter>>(jsonData);
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
