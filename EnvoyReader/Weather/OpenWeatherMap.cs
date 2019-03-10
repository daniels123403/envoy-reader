using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace EnvoyReader.Weather
{
    class OpenWeatherMap : IWeatherProvider
    {
        private readonly string apiKey;
        private readonly double lat;
        private readonly double lon;

        public OpenWeatherMap(string apiKey, double lat, double lon)
        {
            this.apiKey = apiKey;
            this.lat = lat;
            this.lon = lon;
        }

        public async Task<double> GetCurrentTemperatureAsync()
        {
            var latStr = lat.ToString(CultureInfo.InvariantCulture);
            var lonStr = lon.ToString(CultureInfo.InvariantCulture);
            var url = new Uri($"http://api.openweathermap.org/data/2.5/weather?lat={latStr}&lon={lonStr}&mode=json&units=metric&APPID={apiKey}");

            using (var client = new HttpClient())
            using (var response = await client.GetAsync(url))
            {
                var responseData = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Could not fetch OpenWeatherMap data: {response.ReasonPhrase}");
                }

                var weather = JObject.Parse(responseData);
                var temperature = double.Parse((string)weather["main"]["temp"], CultureInfo.InvariantCulture);

                return temperature;
            }
        }
    }
}
