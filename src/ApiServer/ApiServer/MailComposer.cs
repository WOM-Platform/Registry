using System;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api {

    public class MailComposer {

        private readonly IMailerQueue _queue;
        private readonly LinkGenerator _linkGenerator;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MailComposer> _logger;

        private readonly MailAddress _mailFrom;
        private readonly MailAddress _mailBcc = null;

        public MailComposer(
            IMailerQueue queue,
            LinkGenerator linkGenerator,
            IConfiguration configuration,
            ILogger<MailComposer> logger
        ) {
            _queue = queue;
            _linkGenerator = linkGenerator;
            _configuration = configuration;
            _logger = logger;

            var mailFromAddress = Environment.GetEnvironmentVariable("SENDER_MAIL");
            var mailFromName = Environment.GetEnvironmentVariable("SENDER_NAME");
            _mailFrom = new MailAddress(mailFromAddress, mailFromName);
            _logger.LogDebug("Mails will be sent from {0}", _mailFrom);

            var mailShadowBccAddress = Environment.GetEnvironmentVariable("CONFIRMATION_MAIL_BCC");
            if(!string.IsNullOrEmpty(mailShadowBccAddress)) {
                _mailBcc = new MailAddress(mailShadowBccAddress);
                _logger.LogDebug("Sending shadow copy to {0}", mailShadowBccAddress);
            }
        }

        public void SendVerificationMail(User user) {
            if(user.Email == null) {
                _logger.LogError("Cannot send email to user #{0}, no email given", user.Id);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("Welcome to the WOM Platform, {0}!\n\n", user.Name);
            sb.Append("Please verify your e-mail address by clicking on the following link:\n");
            sb.Append(GetVerificatonLink(user.Id.ToString(), user.VerificationToken));
            sb.Append("\n\n❤ The WOM Platform");

            SendMessage(user.Email,
                $"WOM Platform e-mail verification",
                sb.ToString()
            );
        }

        private string GetVerificatonLink(string id, string token) {
            return _linkGenerator.GetUriByAction(
                nameof(Controllers.UserController.Verify),
                "User",
                new {
                    userId = id,
                    token = token
                },
                "https",
                new HostString(Environment.GetEnvironmentVariable("SELF_HOST"))
            );
        }

        public void SendPasswordResetMail(User user) {
            if(user.Email == null) {
                _logger.LogError("Cannot send email to user #{0}, no email given", user.Id);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("Hello {0}!\n\n", user.Name);
            sb.Append("A password reset link was requested for your account. Please click on the following link to set a new password:\n");
            sb.Append(GetPasswordResetLink(user.Id.ToString(), user.PasswordResetToken));
            sb.Append("\n\nIf you didn’t request a password reset, please ignore this e-mail.\n\n❤ The WOM Platform");

            SendMessage(user.Email,
                $"WOM Platform password reset",
                sb.ToString()
            );
        }

        private string GetPasswordResetLink(string id, string token) {
            return _linkGenerator.GetUriByAction(
                nameof(Controllers.UserController.ResetPasswordToken),
                "User",
                new {
                    userId = id,
                    token = token
                },
                "https",
                new HostString(Environment.GetEnvironmentVariable("SELF_HOST"))
            );
        }

        private void SendMessage(string recipientAddress, string subject, string contents) {
            var msg = new MailMessage {
                From = _mailFrom,
                Sender = _mailFrom,
                IsBodyHtml = false,
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = contents,
                BodyEncoding = Encoding.UTF8
            };
            msg.To.Add(recipientAddress);

            if(_mailBcc != null) {
                msg.Bcc.Add(_mailBcc);
            }

            _queue.Enqueue(msg);
        }

    }

    public static class MailComposerExtensions {

        public static IServiceCollection AddMailComposer(this IServiceCollection services) {
            services.AddSingleton<IMailerQueue, MailerQueue>();
            services.AddHostedService<MailerService>();
            services.AddSingleton<MailComposer>();
            return services;
        }

    }

}
