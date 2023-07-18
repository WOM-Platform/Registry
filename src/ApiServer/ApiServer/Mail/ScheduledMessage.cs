using System.Net.Mail;

namespace WomPlatform.Web.Api.Mail {
    public class ScheduledMessage {
        public ScheduledMessage(MailMessage message) {
            Message = message;
        }

        public int Retries { get; set; }

        public MailMessage Message { get; init; }
    }
}
