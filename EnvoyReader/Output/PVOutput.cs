using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using EnvoyReader.Config;
using EnvoyReader.Envoy;
using EnvoyReader.Weather;

namespace EnvoyReader.Output
{
    class PVOutput : IOutput
    {
        private const string AddStatusUrl = "http://pvoutput.org/service/r2/addstatus.jsp";
        private readonly string apiKey;
        private readonly string systemId;
        private readonly ILogger logger;
        private IWeatherProvider weatherProvider;

        public PVOutput(IAppSettings appSettings, ILogger logger, IWeatherProvider weatherProvider)
        {
            apiKey = appSettings.PVOutputApiKey;
            systemId = appSettings.PVOutputSystemId;
            this.logger = logger;
            this.weatherProvider = weatherProvider;

            logger.WriteLine($"Use PVOutput: {systemId}");
        }

        public async Task<WriteResult> WriteAsync(SystemProduction systemProduction, List<Inverter> inverters)
        {
            if (systemProduction.ReadingTime <= 0)
            {
                return WriteResult.NoNeedToWrite;
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Pvoutput-Apikey", apiKey);
                client.DefaultRequestHeaders.Add("X-Pvoutput-SystemId", systemId);

                var currentTemperature = await weatherProvider.GetCurrentTemperature();

                logger.WriteLine($"Current temperature: {currentTemperature}");

                var readingTime = DateTimeOffset.FromUnixTimeSeconds(systemProduction.ReadingTime);
                var localTIme = readingTime.ToLocalTime();

                var postData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("d", localTIme.ToString("yyyyMMdd")),
                    new KeyValuePair<string, string>("t", localTIme.ToString("HH:mm")),
                    new KeyValuePair<string, string>("v1", systemProduction.WhLifeTime.ToString(CultureInfo.InvariantCulture)), //Energy Generation
                    new KeyValuePair<string, string>("v2", systemProduction.WNow.ToString(CultureInfo.InvariantCulture)), //Power Generation
                    new KeyValuePair<string, string>("v5", currentTemperature.ToString(CultureInfo.InvariantCulture)), //Temperature (celcius)
                    new KeyValuePair<string, string>("c1", "1"), //Cumulative
                });

                using (var response = await client.PostAsync(AddStatusUrl, postData))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadAsStringAsync();
                        throw new Exception(responseData);
                    }
                }
            }

            return WriteResult.Success;
        }
    }
}
