using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpeedTest;
using SpeedtestRunner.Data;
using SpeedtestRunner.Data.Models;

namespace SpeedtestRunner.Services
{
    public sealed class SpeedtestRunnerService : IHostedService, IDisposable
    {
        private readonly ILogger<SpeedtestRunnerService> _logger;
        private readonly IServiceProvider _services;
        private readonly SpeedTestClient _client = new SpeedTestClient();

        private Timer _timer;

        public SpeedtestRunnerService(
            ILogger<SpeedtestRunnerService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(SpeedtestRunnerService)} is starting...");
            _timer = new Timer(RunSpeedtest, null, TimeSpan.Zero, TimeSpan.FromHours(1));

            return Task.CompletedTask;
        }

        private void RunSpeedtest(object state)
        {
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var time = DateTimeOffset.Now;

            _logger.LogInformation($"Running test at {time}...");

            var settings = _client.GetSettings();
            var server = settings.Servers
                .OrderBy(s => s.Distance)
                .FirstOrDefault();
            var latency = _client.TestServerLatency(server);
            var download = _client.TestDownloadSpeed(server);
            var upload = _client.TestUploadSpeed(server);

            var test = new Speedtest
            {
                Timestamp = time,
                ServerLatency = latency,
                DownloadSpeed = download,
                UploadSpeed = upload
            };

            context.Speedtests.Add(test);
            context.SaveChanges();

            _logger.LogInformation($"Test ran successfully - Latency: {latency} ms, Download: {download / 1000} Mbps, Upload: {upload / 1000} Mbps");
            _logger.LogInformation($"Next test scheduled for {time.AddHours(1)}");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{nameof(SpeedtestRunnerService)} is stopping...");
            _timer.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
