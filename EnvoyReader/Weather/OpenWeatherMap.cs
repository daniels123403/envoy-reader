using EnvoyReader.Config;
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
    class OpenWeatherMap : IWeatherProvider
    {
        private readonly string apiKey;
        private readonly double lat;
        private readonly double lon;

        public OpenWeatherMap(IAppSettings appSettings)
        {
            apiKey = appSettings.OpenWeatherMapApiKey;
            lat = appSettings.OpenWeatherMapLat;
            lon = appSettings.OpenWeatherMapLon;
        }

        public async Task<double> GetCurrentTemperatureAsync()
        {
            var latStr = lat.ToString(CultureInfo.InvariantCulture);
            var lonStr = lon.ToString(CultureInfo.InvariantCulture);
            var url = $"http://api.openweathermap.org/data/2.5/weather?lat={latStr}&lon={lonStr}&mode=json&units=metric&APPID={apiKey}";

            using (var client = new HttpClient())
            using (var response = await client.GetAsync(url))
            {
                var responseData = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Could not fetch weather: {response.ReasonPhrase} ({response.StatusCode})");
                }

                var weather = JObject.Parse(responseData);
                var temperature = double.Parse(weather["main"]["temp"].Value<string>(), CultureInfo.InvariantCulture);

                return temperature;
            }
        }
    }
}
