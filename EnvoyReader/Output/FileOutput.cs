using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EnvoyReader.Config;
using EnvoyReader.Envoy;

namespace EnvoyReader.Output
{
    class FileOutput : IOutput
    {
        private string file;
        private readonly ILogger logger;

        public FileOutput(IAppSettings appSettings, ILogger logger)
        {
            file = appSettings.OutputDataToFile;
            file = file.Replace("{date}", DateTime.Now.ToString("yyyyMMdd"));
            this.logger = logger;

            logger.WriteLine($"Use FileOutput: {file}");
        }

        public async Task<WriteResult> WriteAsync(SystemProduction systemProduction, List<Inverter> inverters)
        {
            var data = new StringBuilder();

            data.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] systemProduction: ");
            data.Append($"ReadingTime: {systemProduction.ReadingTime}, ");
            data.Append($"ActiveCount: {systemProduction.ActiveCount}, ");
            data.Append($"WhLifeTime: {systemProduction.WhLifeTime}, ");
            data.Append($"WNow: {systemProduction.WNow}");
            data.AppendLine();

            foreach (var inverter in inverters)
            {
                data.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] inverter: ");
                data.Append($"SerialNumber: {inverter.SerialNumber}, ");
                data.Append($"LastReportDate: {inverter.LastReportDate}, ");
                data.Append($"LastReportWatts: {inverter.LastReportWatts}, ");
                data.Append($"MaxReportWatts: {inverter.MaxReportWatts}");
                data.AppendLine();
            }

            await File.AppendAllTextAsync(file, data.ToString());

            return WriteResult.Success;
        }
    }
}
