using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Mail {
    public class MailerBackgroundService : BackgroundService {

        private readonly IMailerQueue _queue;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MailerBackgroundService> _logger;

        public MailerBackgroundService(
            IMailerQueue queue,
            IConfiguration configuration,
            ILogger<MailerBackgroundService> logger
        ) {
            _queue = queue;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            var conf = _configuration.GetRequiredSection("Mail").GetRequiredSection("Smtp");

            var smtpHost = conf["Host"];
            var smtpPort = Convert.ToInt32(conf["Port"]);
            var smtpUser = conf["Username"];
            var smtpPassword = conf["Password"];

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
                await Task.Delay(1000, stoppingToken);
            }
        }

    }
}
