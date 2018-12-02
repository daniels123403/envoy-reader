using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using EnvoyReader.Config;
using EnvoyReader.Envoy;

namespace EnvoyReader.Output
{
    class PVOutput : IOutput
    {
        private const string AddStatusUrl = "http://pvoutput.org/service/r2/addstatus.jsp";
        private string apiKey;
        private string systemId;

        public PVOutput(AppSettings appSettings)
        {
            apiKey = appSettings.PVOutputApiKey;
            systemId = appSettings.PVOutputSystemId;
        }

        public async Task WriteAsync(SystemProduction systemProduction, List<Inverter> inverters)
        {
            var readingTime = DateTimeOffset.FromUnixTimeSeconds(systemProduction.ReadingTime);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Pvoutput-Apikey", apiKey);
                client.DefaultRequestHeaders.Add("X-Pvoutput-SystemId", systemId);

                var postData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("d", readingTime.ToLocalTime().ToString("yyyyMMdd")),
                    new KeyValuePair<string, string>("t", readingTime.ToLocalTime().ToString("HH:mm")),
                    new KeyValuePair<string, string>("v1", systemProduction.WhLifeTime.ToString(CultureInfo.InvariantCulture)), //Energy Generation
                    new KeyValuePair<string, string>("v2", systemProduction.WNow.ToString(CultureInfo.InvariantCulture)), //Power Generation
                    new KeyValuePair<string, string>("c1", "1"), //Cumulative
                });

                var response = await client.PostAsync(AddStatusUrl, postData);

                if (!response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    throw new Exception(responseData);
                }
            }
        }
    }
}
