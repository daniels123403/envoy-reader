using System.Collections.Generic;
using System.Threading.Tasks;
using EnvoyReader.Envoy;

namespace EnvoyReader.Output
{
    public enum WriteResult { Success, NoNeedToWrite }

    interface IOutput
    {
        Task<WriteResult> WriteAsync(SystemProduction systemProduction, List<Inverter> inverters);
    }
}