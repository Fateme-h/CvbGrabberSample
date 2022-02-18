using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Isolated
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private CameraConnection? _cameraConnection;
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }
        private void CleanUp()
        {
            if (_cameraConnection == null)
                return;
            try
            {
                 _cameraConnection.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up connection.");
            }
            finally
            {
                _cameraConnection = null;
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Camera monitor running {time}", DateTimeOffset.Now);
                await Task.Delay(5000, stoppingToken).ConfigureAwait(false);
                try
                {
                    if (_cameraConnection?.IsAlive() == true)
                    {
                        _cameraConnection.Grab(3, true);
                        continue;
                    }
                    if (_cameraConnection != null)
                         _cameraConnection.Dispose();
                    _cameraConnection = CameraConnection.EstablishAsync(stoppingToken, true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed running camera monitor");
                     CleanUp();
                }
            }
        }
    }
}
