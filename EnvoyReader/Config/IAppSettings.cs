namespace EnvoyReader.Config
{
    interface IAppSettings
    {
        string EnvoyBaseUrl { get; set; }
        string EnvoyPassword { get; set; }
        string EnvoyUsername { get; set; }
        string InfluxDb { get; set; }
        string InfluxUrl { get; set; }
        string PVOutputApiKey { get; set; }
        string PVOutputSystemId { get; set; }
    }
}