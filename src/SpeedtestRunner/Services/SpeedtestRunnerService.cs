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
    public sealed class SpeedtestRunnerService : BackgroundService
    {
        private readonly ILogger<SpeedtestRunnerService> _logger;
        private readonly IServiceProvider _services;
        private readonly SpeedTestClient _client = new SpeedTestClient();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public SpeedtestRunnerService(
            ILogger<SpeedtestRunnerService> logger,
            IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(SpeedtestRunnerService)} is starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                var test = await RunSpeedtest();

                _logger.LogInformation($"Next test scheduled for {test.Timestamp.AddHours(1)}");

                await Task.Delay(TimeSpan.FromHours(1));
            }

            _logger.LogInformation($"{nameof(SpeedtestRunnerService)} is stopping...");
        }

        public async Task<Speedtest> RunSpeedtest()
        {
            await _semaphore.WaitAsync();

            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var time = DateTimeOffset.Now;
            var test = new Speedtest { Timestamp = time };

            try
            {
                var settings = await _client.GetSettingsAsync();
                var server = settings.Servers
                    .OrderBy(s => s.Distance)
                    .FirstOrDefault();

                test.ServerLatency = await _client.TestServerLatencyAsync(server);
                test.DownloadSpeed = await _client.TestDownloadSpeedAsync(server);
                test.UploadSpeed = await _client.TestUploadSpeedAsync(server);
            }
            finally
            {
                _semaphore.Release();
            }

            _logger.LogInformation($"Test ran successfully - Time: {time}, Latency: {test.ServerLatency} ms, " +
                $"Download: {test.DownloadSpeed / 1000} Mbps, Upload: {test.UploadSpeed / 1000} Mbps");

            context.Speedtests.Add(test);
            await context.SaveChangesAsync();

            return test;
        }
    }
}
