using Envoy;
using EnvoyReader.Config;
using EnvoyReader.Utilities;
using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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

        static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now);

            var appSettings = ReadAppConfiguration();

            Console.WriteLine($"Use envoy: {appSettings.EnvoyBaseUrl} as {appSettings.EnvoyUsername}");
            Console.WriteLine($"Use influx: {appSettings.InfluxDb} @ {appSettings.InfluxUrl}");

            var envoyDataProvider = new EnvoyDataProvider(appSettings.EnvoyUsername, appSettings.EnvoyPassword, appSettings.EnvoyBaseUrl);

            try
            {
                Retry.Do(() =>
                {
                    var payload = new LineProtocolPayload();

                    ReadInverterProduction(envoyDataProvider, payload);
                    ReadSystemProduction(envoyDataProvider, payload);

                    var client = new LineProtocolClient(new Uri(appSettings.InfluxUrl), appSettings.InfluxDb);

                    var writeResult = client.WriteAsync(payload).Result;
                    if (writeResult.Success)
                        Console.WriteLine("Written successfully");
                    else
                        throw new Exception(writeResult.ErrorMessage);
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

        private static void ReadSystemProduction(EnvoyDataProvider envoyDataProvider, LineProtocolPayload payload)
        {
            Console.WriteLine("Read system producton");

            var production = envoyDataProvider.GetSystemProduction();
            var inverters = production.Production.FirstOrDefault(p => p.Type == "inverters");

            if (inverters == null)
                throw new Exception("No system data found");

            var readingTime = DateTimeOffset.FromUnixTimeSeconds(inverters.ReadingTime);

            Console.WriteLine($"  ActiveCount: {inverters.ActiveCount}");
            Console.WriteLine($"  ReadingTime: {readingTime.ToLocalTime()}");
            Console.WriteLine($"  Type: {inverters.Type}");
            Console.WriteLine($"  WhLifeTime: {inverters.WhLifeTime}");
            Console.WriteLine($"  WNow: {inverters.WNow}");

            var systemPoint = new LineProtocolPoint(
                "inverters", //Measurement
                new Dictionary<string, object> //Fields
                {
                    { $"activecount", inverters.ActiveCount },
                    { $"whlifetime", inverters.WhLifeTime },
                    { $"WNow", inverters.WNow },
                },
                new Dictionary<string, string> //Tags
                    {
                },
                readingTime.UtcDateTime); //Timestamp

            if (inverters.ReadingTime > 0)
                payload.Add(systemPoint);
            else
                Console.WriteLine("Invalid reading time, skip adding to payload");
        }

        private static void ReadInverterProduction(EnvoyDataProvider envoyDataProvider, LineProtocolPayload payload)
        {
            Console.WriteLine("Read inverter producton");

            var inverters = envoyDataProvider.GetInverterProduction();
            Console.WriteLine("S/N\t\tReportTime\t\t\tWatts");

            foreach (var inverter in inverters)
            {
                if (inverter.LastReportDate > 0)
                {
                    var reportTime = DateTimeOffset.FromUnixTimeSeconds(inverter.LastReportDate);

                    Console.WriteLine($"{inverter.SerialNumber}\t{reportTime.ToLocalTime()}\t{inverter.LastReportWatts}");

                    var inverterPoint = new LineProtocolPoint(
                        "inverter", //Measurement
                        new Dictionary<string, object> //Fields
                        {
                            { $"lastreportwatts", inverter.LastReportWatts },
                            { $"maxreportwatts", inverter.MaxReportWatts },
                        },
                        new Dictionary<string, string> //Tags
                        {
                            { $"serialnumber", inverter.SerialNumber },
                        },
                        reportTime.UtcDateTime); //Timestamp

                    payload.Add(inverterPoint);
                }
            }

            Console.WriteLine($"Total watts: {inverters.Sum(i => i.LastReportWatts)}");
        }
    }
}
