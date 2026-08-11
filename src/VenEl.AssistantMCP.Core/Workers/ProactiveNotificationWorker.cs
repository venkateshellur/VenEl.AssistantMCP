using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenEl.AssistantMCP.Core.Proactive;

namespace VenEl.AssistantMCP.Core.Workers;

public class ProactiveNotificationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProactiveNotificationWorker> _logger;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly IEnumerable<VenEl.AssistantMCP.Core.Proactive.IProactiveSource> _sources;
    private readonly VenEl.AssistantMCP.Core.Proactive.IAlertsManager _alertsManager;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(2);

    public ProactiveNotificationWorker(
        IServiceProvider serviceProvider, 
        ILogger<ProactiveNotificationWorker> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        IEnumerable<VenEl.AssistantMCP.Core.Proactive.IProactiveSource> sources,
        VenEl.AssistantMCP.Core.Proactive.IAlertsManager alertsManager)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
        _sources = sources;
        _alertsManager = alertsManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Proactive Notification Worker started.");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_pollingInterval, stoppingToken);
            
            var enabledStr = _configuration["EnableProactiveNotifications"];
            bool isEnabled = true;
            if (!string.IsNullOrWhiteSpace(enabledStr) && bool.TryParse(enabledStr, out var parsed))
            {
                isEnabled = parsed;
            }

            if (!isEnabled)
            {
                continue;
            }

            try
            {
                var newAlerts = new List<string>();
                foreach (var source in _sources)
                {
                    try
                    {
                        var alert = await source.CheckForNewAlertsAsync(stoppingToken);
                        if (!string.IsNullOrWhiteSpace(alert))
                        {
                            newAlerts.Add(alert);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error polling source {source.GetType().Name}");
                    }
                }

                if (newAlerts.Count > 0)
                {
                    await _alertsManager.PublishAlertsAsync(newAlerts, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during proactive notifications polling.");
            }
        }
    }
}
