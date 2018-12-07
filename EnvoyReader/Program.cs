﻿using EnvoyReader.Config;
using EnvoyReader.Envoy;
using EnvoyReader.Output;
using EnvoyReader.Utilities;
using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EnvoyReader
{
    class Program
    {
        private static AppSettings ReadAppConfiguration()
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
            Console.WriteLine(DateTime.Now);

            var appSettings = ReadAppConfiguration();

            Console.WriteLine($"Use Envoy: {appSettings.EnvoyBaseUrl} as {appSettings.EnvoyUsername}");
            Console.WriteLine($"Use InfluxDB: {appSettings.InfluxDb} @ {appSettings.InfluxUrl}");
            Console.WriteLine($"Use PVOutput: {appSettings.PVOutputSystemId}");

            try
            {
                await Retry.Do(async () =>
                {
                    var envoyDataProvider = new EnvoyDataProvider(appSettings.EnvoyUsername, appSettings.EnvoyPassword, appSettings.EnvoyBaseUrl);

                    var systemProduction = await ReadSystemProduction(envoyDataProvider);
                    var inverters = await ReadInverterProduction(envoyDataProvider);

                    await Task.WhenAll(
                        WriteToOutput(inverters, systemProduction, new PVOutput(appSettings)),
                        WriteToOutput(inverters, systemProduction, new Output.InfluxDB(appSettings)));
                }, TimeSpan.FromSeconds(1), 50);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }

#if DEBUG
            Console.ReadKey();
#endif
        }

        private static async Task WriteToOutput(List<Inverter> inverters, SystemProduction systemProduction, IOutput output)
        {
            var name = output.GetType().Name;
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

        private static async Task<List<Inverter>> ReadInverterProduction(EnvoyDataProvider envoyDataProvider)
        {
            Console.WriteLine("Read inverter producton");

            var inverters = await envoyDataProvider.GetInverterProduction();
            Console.WriteLine("  S/N\t\tReportTime\t\t\tWatts");

            foreach (var inverter in inverters)
            {
                if (inverter.LastReportDate > 0)
                {
                    var reportTime = DateTimeOffset.FromUnixTimeSeconds(inverter.LastReportDate);

                    Console.WriteLine($"  {inverter.SerialNumber}\t{reportTime.ToLocalTime()}\t{inverter.LastReportWatts}");
                }
            }

            Console.WriteLine($"  Total watts: {inverters.Sum(i => i.LastReportWatts)}");

            return inverters;
        }
    }
}
