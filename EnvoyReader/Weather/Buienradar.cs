using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EnvoyReader.Weather
{
    class Buienradar : IWeatherProvider
    {
        private readonly int stationId;
        private const string Url = "https://data.buienradar.nl/2.0/feed/json";
        private readonly ILogger logger;

        public Buienradar(int stationId, ILogger logger)
        {
            this.stationId = stationId;
            this.logger = logger;
        }

        public async Task<double> GetCurrentTemperatureAsync()
        {
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(Url))
            {
                var responseData = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Could not fetch Buienradar data: {response.ReasonPhrase}");
                }

                var weather = JObject.Parse(responseData);
                var station = weather["actual"]["stationmeasurements"].Values<JObject>()
                    .Where(m => (int)m["stationid"] == stationId)
                    .FirstOrDefault();

                if (station == null)
                {
                    throw new Exception($"Station with id {stationId} not found");
                }

                var temperature = double.Parse((string)station["temperature"], CultureInfo.InvariantCulture);

                logger.WriteLine($"Temperature from Buienradar: {temperature}");

                return temperature;
            }
        }
    }
}
