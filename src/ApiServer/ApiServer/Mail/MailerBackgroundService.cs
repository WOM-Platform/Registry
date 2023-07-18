using System.Net.Mail;
using System.Net;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Mail {
    public class MailerBackgroundService : BackgroundService {

        private readonly IMailerQueue _queue;
        private readonly ILogger<MailerBackgroundService> _logger;

        public MailerBackgroundService(
            IMailerQueue queue,
            ILogger<MailerBackgroundService> logger
        ) {
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
            var smtpPort = Convert.ToInt32(Environment.GetEnvironmentVariable("SMTP_PORT"));
            var smtpUser = Environment.GetEnvironmentVariable("SMTP_USERNAME");
            var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");

            _logger.LogDebug("Creating new client for SMTP server {0}:{1} for user {2}", smtpHost, smtpPort, smtpUser);

            using var client = new SmtpClient(smtpHost, smtpPort) {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            _logger.LogInformation("SMTP client ready");

            // Start e-mail processing loop
            while(!stoppingToken.IsCancellationRequested) {
                await _queue.Process(async (message) => {
                    await client.SendMailAsync(message, stoppingToken);
                    return true;
                }, stoppingToken);

                // Wait 3 seconds to throttle delivery
                await Task.Delay(3000, stoppingToken);
            }
        }

    }
}
