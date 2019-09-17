using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpeedTest;
using SpeedtestRunner.Data;
using SpeedtestRunner.Data.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SpeedtestRunner.Services
{
    public sealed class SpeedtestRunnerService : BackgroundService
    {
        private readonly ILogger<SpeedtestRunnerService> _logger;
        private readonly IServiceProvider _services;
        private readonly SpeedTestClient _client = new SpeedTestClient();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public event Action RunStarted;
        public event Action<Speedtest> RunCompleted;
        public event Action<DateTimeOffset> NextRunScheduled;

        public bool IsRunning { get; private set; }
        public DateTimeOffset NextRun { get; private set; }

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

            var current = DateTimeOffset.Now;
            var next = current.AddMinutes(60 - current.Minute);

            for (; !stoppingToken.IsCancellationRequested; next = next.AddHours(4))
            {
                await ScheduleSpeedtest(next, stoppingToken);
            }

            _logger.LogInformation($"{nameof(SpeedtestRunnerService)} is stopping...");
        }

        private async Task<Speedtest> ScheduleSpeedtest(DateTimeOffset time, CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Next test scheduled for {time}.");

            NextRun = time;
            NextRunScheduled?.Invoke(NextRun);

            await Task.Delay(time - DateTimeOffset.Now, stoppingToken);

            return await RunSpeedtest(time);
        }

        public Task<Speedtest> RunSpeedtest()
        {
            return RunSpeedtest(DateTimeOffset.Now);
        }

        private async Task<Speedtest> RunSpeedtest(DateTimeOffset timestamp)
        {
            await _semaphore.WaitAsync();

            IsRunning = true;
            RunStarted?.Invoke();

            var test = new Speedtest { Timestamp = timestamp };

            try
            {
                var settings = await _client.GetSettingsAsync();
                var server = settings.Servers
                    .OrderBy(s => s.Distance)
                    .FirstOrDefault();

                test.ServerLatency = await _client.TestServerLatencyAsync(server);
                test.DownloadSpeed = await _client.TestDownloadSpeedAsync(server);
                test.UploadSpeed = await _client.TestUploadSpeedAsync(server);

                _logger.LogInformation($"Test ran successfully - Time: {timestamp}, Latency: {test.ServerLatency} ms, " +
                    $"Download: {test.DownloadSpeed / 1000} Mbps, Upload: {test.UploadSpeed / 1000} Mbps");

                using var scope = _services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Speedtests.Add(test);
                await context.SaveChangesAsync();
            }
            finally
            {
                IsRunning = false;
                RunCompleted?.Invoke(test);

                _semaphore.Release();
            }

            return test;
        }
    }
}
