using System;

namespace SpeedtestRunner.Data.Models
{
    public sealed class Speedtest
    {
        public int Id { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public int ServerLatency { get; set; }

        public double DownloadSpeed { get; set; }

        public double UploadSpeed { get; set; }
    }
}
