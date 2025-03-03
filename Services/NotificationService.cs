using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CivilProcessERP.Services
{
    public class NotificationService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private NpgsqlConnection? _connection;

        public NotificationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connectionString = _configuration.GetConnectionString("OfficeDb");
            _connection = new NpgsqlConnection(connectionString);
            await _connection.OpenAsync(stoppingToken);

            // Subscribe to PostgreSQL notifications on the "record_update" channel.
            _connection.Notification += OnNotification;
            using (var cmd = new NpgsqlCommand("LISTEN record_update;", _connection))
            {
                await cmd.ExecuteNonQueryAsync(stoppingToken);
            }

            // Keep the connection open to receive notifications.
            while (!stoppingToken.IsCancellationRequested)
            {
                await _connection.WaitAsync(stoppingToken);
            }
        }

        private void OnNotification(object sender, NpgsqlNotificationEventArgs e)
        {
            // This is where you process the notification payload.
            Debug.WriteLine($"Notification received: {e.Payload}");
            // You can also dispatch an event to update the UI if needed.
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
            }
            await base.StopAsync(cancellationToken);
        }
    }
}
