using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EnvoyReader.Weather
{
    interface IWeatherProvider
    {
        Task<double> GetCurrentTemperatureAsync();
    }
}
