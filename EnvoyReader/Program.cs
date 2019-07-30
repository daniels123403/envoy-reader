using EnvoyReader.Config;
using EnvoyReader.Envoy;
using EnvoyReader.Output;
using EnvoyReader.Utilities;
using EnvoyReader.Weather;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EnvoyReader
{
    class Program
    {
        private static IAppSettings ReadAppConfiguration()
        {
            var startupPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var builder = new ConfigurationBuilder()
               .SetBasePath(startupPath)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var configuration = builder.Build();
            var appSettings = new AppSettings();
            configuration.GetSection("AppSettings").Bind(appSettings);

            return appSettings;
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine($"Current date/time: {DateTimeOffset.Now}");
            Console.WriteLine($"Local timezone: {TimeZoneInfo.Local}");

            var appSettings = ReadAppConfiguration();
            var logger = new ConsoleLogger();

            Console.WriteLine($"Use Envoy: {appSettings.EnvoyBaseUrl} as {appSettings.EnvoyUsername}");

            try
            {
                await Retry.Do(async () =>
                {
                    var envoyDataProvider = new EnvoyDataProvider(appSettings.EnvoyUsername, appSettings.EnvoyPassword, appSettings.EnvoyBaseUrl);
                    var weatherProvider = GetWeatherProvider(appSettings, logger);

                    var systemProduction = await ReadSystemProduction(envoyDataProvider);
                    var inverters = await ReadInverterInfo(envoyDataProvider);

                    var outputs = new List<IOutput>();

                    AddOutputs(appSettings, logger, weatherProvider, outputs);

                    if (outputs.Count == 0)
                    {
                        throw new Exception("No output found to write to");
                    }

                    await Task.WhenAll(outputs.Select(o => WriteToOutput(inverters, systemProduction, o)));
                }, retryInterval: TimeSpan.FromSeconds(1), maxAttemptCount: 50);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }

        private static void AddOutputs(IAppSettings appSettings, ConsoleLogger logger, IWeatherProvider weatherProvider, List<IOutput> outputs)
        {
            if (!string.IsNullOrEmpty(appSettings.PVOutputApiKey) && !string.IsNullOrEmpty(appSettings.PVOutputSystemId))
            {
                outputs.Add(new PVOutput(appSettings, logger, weatherProvider));
            }

            if (!string.IsNullOrEmpty(appSettings.InfluxDb) && !string.IsNullOrEmpty(appSettings.InfluxUrl))
            {
                outputs.Add(new Output.InfluxDB(appSettings, logger));
            }

            if (!string.IsNullOrEmpty(appSettings.OutputDataToFile))
            {
                outputs.Add(new FileOutput(appSettings, logger));
            }
        }

        private static IWeatherProvider GetWeatherProvider(IAppSettings appSettings, ILogger logger)
        {
            if (!string.IsNullOrEmpty(appSettings.OpenWeatherMapApiKey) && appSettings.OpenWeatherMapLat != null && appSettings.OpenWeatherMapLon != null)
            {
                return new OpenWeatherMap(appSettings.OpenWeatherMapApiKey, appSettings.OpenWeatherMapLat.Value, appSettings.OpenWeatherMapLon.Value, logger);
            }

            if (appSettings.BuienradarStationId != null)
            {
                return new Buienradar(appSettings.BuienradarStationId.Value, logger);
            }

            return null;
        }

        private static async Task WriteToOutput(List<Inverter> inverters, SystemProduction systemProduction, IOutput output)
        {
            var name = output.GetType().Name;
            Console.WriteLine($"Try to write to {name}");

            var result = await output.WriteAsync(systemProduction, inverters);

            switch (result)
            {
                case WriteResult.NoNeedToWrite:
                    Console.WriteLine($"No need to write to {name}");
                    break;
                case WriteResult.Success:
                    Console.WriteLine($"Successfully written to {name}");
                    break;
            }
        }

        private static async Task<SystemProduction> ReadSystemProduction(EnvoyDataProvider envoyDataProvider)
        {
            Console.WriteLine("Read system producton");

            var production = await envoyDataProvider.GetSystemProduction();

            if (production == null)
                throw new Exception("No production data found");

            var inverters = production.FirstOrDefault(p => p.Type == "inverters");

            if (inverters == null)
                throw new Exception("No inverter data found");

            var readingTime = DateTimeOffset.FromUnixTimeSeconds(inverters.ReadingTime);

            Console.WriteLine($"  ActiveCount: {inverters.ActiveCount}");
            Console.WriteLine($"  ReadingTime: {readingTime.ToLocalTime()}");
            Console.WriteLine($"  Type: {inverters.Type}");
            Console.WriteLine($"  WhLifeTime: {inverters.WhLifeTime}");
            Console.WriteLine($"  WNow: {inverters.WNow}");

            return inverters;
        }

        private static async Task<List<Inverter>> ReadInverterInfo(EnvoyDataProvider envoyDataProvider)
        {
            Console.WriteLine("Read inverter producton");

            var inverters = await envoyDataProvider.GetInverterInfo();

            if (inverters == null)
                throw new Exception("No inverter data found");

            Console.WriteLine("  S/N\t\tReportTime\t\t\tWatts\tMax\tProducing");

            foreach (var inverter in inverters)
            {
                if (inverter.Production.LastReportDate > 0)
                {
                    var reportTime = DateTimeOffset.FromUnixTimeSeconds(inverter.Production.LastReportDate);

                    var line = new List<string>
                    {
                        inverter.DeviceInfo.SerialNum,
                        Convert.ToString(reportTime.ToLocalTime()),
                        Convert.ToString(inverter.Production.LastReportWatts),
                        Convert.ToString(inverter.Production.MaxReportWatts),
                        Convert.ToString(inverter.DeviceInfo.Producing),
                    };

                    Console.WriteLine($"  {string.Join("\t", line)}");
                }
            }

            Console.WriteLine($"  Total watts: {inverters.Sum(i => i.Production.LastReportWatts)}");

            return inverters;
        }
    }
}
