namespace EnvoyReader.Config
{
    class AppSettings : IAppSettings
    {
        public string InfluxUrl { get; set; }
        public string InfluxDb { get; set; }

        public string EnvoyUsername { get; set; }
        public string EnvoyPassword { get; set; }
        public string EnvoyBaseUrl { get; set; }

        public string PVOutputApiKey { get; set; }
        public string PVOutputSystemId { get; set; }

        public string OutputDataToFile { get; set; }
    }
}
