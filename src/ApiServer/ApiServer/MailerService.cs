using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api {

    public class MailerService : BackgroundService {

        private readonly IMailerQueue _queue;
        private readonly ILogger<MailerService> _logger;

        public MailerService(
            IMailerQueue queue,
            ILogger<MailerService> logger) {
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
                var message = await _queue.DequeueAsync(stoppingToken);

                _logger.LogInformation("Sending mail to {0} '{1}'", message.To, message.Subject);

                try {
                    await client.SendMailAsync(message);
                    _logger.LogInformation("Mail to {0} sent", message.To);

                    // Wait 3 seconds to throttle delivery
                    await Task.Delay(3000);
                }
                catch(Exception ex) {
                    _logger.LogError(ex, "Failed to send email to {0}", message.To);
                }
            }
        }

    }

}
