using EnvoyReader.Config;
using EnvoyReader.Envoy;
using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnvoyReader.Output
{
    class InfluxDB : IOutput
    {
        private Uri url;
        private string database;

        public InfluxDB(AppSettings appSettings)
        {
            url = new Uri(appSettings.InfluxUrl);
            database = appSettings.InfluxDb;
        }

        public async Task WriteAsync(SystemProduction systemProduction, List<Inverter> inverters)
        {
            var payload = new LineProtocolPayload();

            if (systemProduction.ReadingTime > 0)
            {
                AddSystemProductionToPayload(systemProduction, payload);
            }

            if (inverters.Count > 0)
            {
                AddInvertersToPayload(inverters, payload);
            }

            var client = new LineProtocolClient(url, database);

            var writeResult = await client.WriteAsync(payload);

            if (!writeResult.Success)
                throw new Exception(writeResult.ErrorMessage);
        }

        private void AddSystemProductionToPayload(SystemProduction systemProduction, LineProtocolPayload payload)
        {
            var readingTime = DateTimeOffset.FromUnixTimeSeconds(systemProduction.ReadingTime);

            var systemPoint = new LineProtocolPoint(
                "inverters", //Measurement
                new Dictionary<string, object> //Fields
                {
                    { $"activecount", systemProduction.ActiveCount },
                    { $"whlifetime", systemProduction.WhLifeTime },
                    { $"WNow", systemProduction.WNow },
                },
                new Dictionary<string, string> //Tags
                {
                },
                readingTime.UtcDateTime); //Timestamp

            payload.Add(systemPoint);
        }

        private void AddInvertersToPayload(List<Inverter> inverters, LineProtocolPayload payload)
        {
            foreach (var inverter in inverters)
            {
                if (inverter.LastReportDate > 0)
                {
                    var reportTime = DateTimeOffset.FromUnixTimeSeconds(inverter.LastReportDate);

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
        }
    }
}
