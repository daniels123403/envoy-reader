using EnvoyReader.Config;
using EnvoyReader.Envoy;
using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvoyReader.Output
{
    class InfluxDB : IOutput
    {
        private readonly Uri url;
        private readonly string database;

        public InfluxDB(AppSettings appSettings)
        {
            url = new Uri(appSettings.InfluxUrl);
            database = appSettings.InfluxDb;
        }

        public async Task<WriteResult> WriteAsync(SystemProduction systemProduction, List<Inverter> inverters)
        {
            var payload = new LineProtocolPayload();

            var systemPayloadAdded = AddSystemProductionToPayload(systemProduction, payload);
            var invertersPayloadAdded = AddInvertersToPayload(inverters, payload);

            if (!systemPayloadAdded && !invertersPayloadAdded)
                return WriteResult.NoNeedToWrite;

            var client = new LineProtocolClient(url, database);

            var writeResult = await client.WriteAsync(payload);

            if (!writeResult.Success)
                throw new Exception(writeResult.ErrorMessage);

            return WriteResult.Success;
        }

        private bool AddSystemProductionToPayload(SystemProduction systemProduction, LineProtocolPayload payload)
        {
            if (systemProduction.ReadingTime <= 0)
                return false;

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

            return true;
        }

        private bool AddInvertersToPayload(List<Inverter> inverters, LineProtocolPayload payload)
        {
            var added = false;

            foreach (var inverter in inverters.Where(i => i.LastReportDate > 0))
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
                added = true;
            }

            return added;
        }
    }
}
