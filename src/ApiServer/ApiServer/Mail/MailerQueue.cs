using System;
using System.Collections.Concurrent;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WomPlatform.Web.Api.Mail {
    public interface IMailerQueue {
        void Schedule(MailMessage message);

        Task Process(Func<MailMessage, Task<bool>> processor, CancellationToken cancellationToken);
    }

    public class MailerQueue : IMailerQueue {
        public MailerQueue(
            ILogger<MailerQueue> logger
        ) {
            _logger = logger;
        }

        private readonly ConcurrentQueue<ScheduledMessage> _mailQueue = new();
        private readonly SemaphoreSlim _signal = new(0);

        private readonly ILogger<MailerQueue> _logger;

        private class ScheduledMessage {
            public ScheduledMessage(MailMessage message) {
                Message = message;
            }

            public MailMessage Message { get; init; }

            public int Retries { get; set; } = 0;
        }

        public void Schedule(MailMessage message) {
            _mailQueue.Enqueue(new ScheduledMessage(message));

            _signal.Release();
        }

        public async Task Process(Func<MailMessage, Task<bool>> processor, CancellationToken cancellationToken) {
            await _signal.WaitAsync(cancellationToken);

            if(_mailQueue.TryDequeue(out var scheduledMessage)) {
                _logger.LogInformation("Processing mail to {0} with subject “{1}” (try {2})", scheduledMessage.Message.To, scheduledMessage.Message.Subject, scheduledMessage.Retries + 1);

                try {
                    if(await processor(scheduledMessage.Message)) {
                        // Message processed correctly
                        _logger.LogDebug("Mail processed correctly");
                    }
                    else {
                        // Gracefully-handled error
                        _logger.LogWarning("Mail not processed correctly by processor");
                        Reschedule(scheduledMessage);
                    }
                }
                catch(Exception ex) {
                    _logger.LogError(ex, "Failed to send mail to {0}", scheduledMessage.Message.To);
                    Reschedule(scheduledMessage);
                }
            }
        }

        private void Reschedule(ScheduledMessage scheduledMessage) {
            if(scheduledMessage.Retries >= 2) {
                _logger.LogError("Mail to {0} with subject “{1}” exceeded retries, dropping", scheduledMessage.Message.To, scheduledMessage.Message.Subject);
                return;
            }

            _logger.LogDebug("Rescheduling message");
            scheduledMessage.Retries++;
            _mailQueue.Enqueue(scheduledMessage);
        }
    }
}
