using System.Collections.Concurrent;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api {

    public interface IMailerQueue {

        void Enqueue(MailMessage message);

        Task<MailMessage> DequeueAsync(CancellationToken cancellationToken);

    }

    public class MailerQueue : IMailerQueue {

        private readonly ConcurrentQueue<MailMessage> _logQueue = new();
        private readonly SemaphoreSlim _signal = new(0);

        public async Task<MailMessage> DequeueAsync(CancellationToken cancellationToken) {
            await _signal.WaitAsync(cancellationToken);

            if(_logQueue.TryDequeue(out var message)) {
                return message;
            }
            else {
                return default;
            }
        }

        public void Enqueue(MailMessage item) {
            _logQueue.Enqueue(item);
            _signal.Release();
        }

    }

}
