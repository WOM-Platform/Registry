using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Mail {

    public class MailerComposer {

        private readonly IMailerQueue _queue;
        private readonly LinkGenerator _linkGenerator;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MailerComposer> _logger;

        private readonly MailAddress _mailFrom;
        private readonly MailAddress _mailBcc = null;

        public MailerComposer(
            IMailerQueue queue,
            LinkGenerator linkGenerator,
            IConfiguration configuration,
            ILogger<MailerComposer> logger
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

        private static string GetVerificatonLink(string id, string token) {
            return new UriBuilder {
                Scheme = "https",
                Host = Environment.GetEnvironmentVariable("SELF_HOST"),
                Path = "/user/verify",
                Query = QueryString.Create(
                    new KeyValuePair<string, StringValues>[] {
                        new KeyValuePair<string, StringValues>("userId", new StringValues(id)),
                        new KeyValuePair<string, StringValues>("token", new StringValues(token)),
                    }
                ).ToString()
            }.ToString();
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

        private static string GetPasswordResetLink(string id, string token) {
            return new UriBuilder {
                Scheme = "https",
                Host = Environment.GetEnvironmentVariable("SELF_HOST"),
                Path = "/authentication/reset-password",
                Query = QueryString.Create(
                    new KeyValuePair<string, StringValues>[] {
                        new KeyValuePair<string, StringValues>("userId", new StringValues(id)),
                        new KeyValuePair<string, StringValues>("token", new StringValues(token)),
                    }
                ).ToString()
            }.ToString();
        }

        public void SendVouchers(string email, string activityTitle, string instrumentName, string otcLink, string password) {
            if(string.IsNullOrEmpty(email)) {
                _logger.LogError("Cannot send email to empty address");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("Ciao!\n\n");
            sb.AppendFormat("Sono stati generati dei voucher WOM per te, da parte di {0} e in virtù dello svolgimento dell’attività “{1}”.\n\n", instrumentName, activityTitle);
            sb.Append("I WOM sono dei voucher elettronici anonimi, che certificano l’impegno che hai dedicato ad una specifica causa per il bene sociale. Potrai riscattarli semplicemente installando l’applicazione WOM Pocket, cliccando sul seguente link e digitando la password riportata sotto:\n\n");
            sb.AppendFormat("{0}\n", otcLink);
            sb.AppendFormat("Password: {0}\n\n", password);
            sb.Append("Consulta il sito https://wom.social per qualsiasi dubbio o informazione.\n\n");
            sb.Append("❤ Piattaforma WOM");

            SendMessage(email, "Emissione di voucher WOM", sb.ToString());
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

            _queue.Schedule(msg);
        }

    }

}
