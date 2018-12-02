using System.Collections.Generic;
using System.Threading.Tasks;
using EnvoyReader.Envoy;

namespace EnvoyReader.Output
{
    interface IOutput
    {
        Task WriteAsync(SystemProduction systemProduction, List<Inverter> inverters);
    }
}