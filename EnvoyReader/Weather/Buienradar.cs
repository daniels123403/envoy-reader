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
        private double? currentTemperature;
        private const string Url = "https://data.buienradar.nl/2.0/feed/json";

        private async Task<double> FetchCurrentTemperature()
        {
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(Url))
            {
                var responseData = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Could not fetch weather: {response.ReasonPhrase} ({response.StatusCode})");
                }

                var weather = JObject.Parse(responseData);
                var station = weather["actual"]["stationmeasurements"].Values<JObject>()
                    .Where(m => m["stationid"].Value<int>() == 6260)
                    .FirstOrDefault();

                var temperature = double.Parse(station["temperature"].Value<string>(), CultureInfo.InvariantCulture);

                return temperature;
            }
        }

        public async Task<double> GetCurrentTemperatureAsync()
        {
            if (currentTemperature == null)
            {
                currentTemperature = await FetchCurrentTemperature();
            }

            return currentTemperature.Value;
        }
    }
}
