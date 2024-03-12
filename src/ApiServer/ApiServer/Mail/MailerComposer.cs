using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace WomPlatform.Web.Api.Mail {

    public class MailerComposer {

        private readonly IMailerQueue _queue;
        private readonly ILogger<MailerComposer> _logger;

        private readonly MailAddress _mailFrom;
        private readonly MailAddress _mailBcc = null;

        public MailerComposer(
            IMailerQueue queue,
            IConfiguration configuration,
            ILogger<MailerComposer> logger
        ) {
            _queue = queue;
            _logger = logger;

            var conf = configuration.GetRequiredSection("Mail");

            var mailFromName = conf["SenderName"];
            var mailFromAddress = conf["SenderMail"];
            _mailFrom = new MailAddress(mailFromAddress, mailFromName);
            _logger.LogDebug("Mail will be sent from {0}", _mailFrom);

            var mailShadowBccAddress = conf["ConfirmationBccMail"];
            if(!string.IsNullOrEmpty(mailShadowBccAddress)) {
                _mailBcc = new MailAddress(mailShadowBccAddress);
                _logger.LogDebug("Sending shadow copies to {0}", mailShadowBccAddress);
            }
        }

        public void SendVerificationMail(User user) {
            if(user.Email == null) {
                _logger.LogError("Cannot send email to user #{0}, no email given", user.Id);
                return;
            }

            var sb = new StringBuilder();
            sb.Append("<html><body>");
            sb.Append("<p><img src=\"cid:wom-logo.jpg\" width=\"160\" alt=\"WOM logo\" /></p>");
            sb.AppendFormat("<p><b>Ciao {0}, ti diamo il benvenuto sulla piattaforma&nbsp;WOM!</b></p>", user.Name);
            sb.Append("<p>Per <b>verificare il tuo indirizzo e-mail</b>, ti chiediamo di aprire l’applicazione WOM POS da cui hai registrato il tuo profilo e di inserire il seguente codice:</p>");
            sb.AppendFormat("<p style=\"text-align: center; margin-bottom: 2rem\"><b style=\"font-family: monospace; font-size: 2rem; letter-spacing: 0.3rem\">{0}</b></p>", user.VerificationToken);
            sb.Append("<p>In alternativa, se stai leggendo l’e-mail dallo stesso dispositivo su cui è installata l’app WOM POS, puoi cliccare su questo collegamento:</p>");
            sb.AppendFormat("<p style=\"text-align: center; margin-bottom: 2rem\"><a href=\"{0}\" style=\"display: inline-block; background: #2969FF; background-color: #2969FF; color: white; font-weight: bold; text-decoration: none; margin-bottom: 1rem; padding: 0.5rem 1rem; border-radius: 0.5rem\">Verifica il tuo indirizzo e-mail</a></p>", GetVerificationPosDeepLink(user.Email, user.VerificationToken));
            sb.Append("<p>Se riscontri dei problemi, contattaci all’indirizzo <a href=\"mailto:info@wom.social\">info@wom.social</a>.</p>");
            sb.Append("<p>❤&nbsp;<i>Il team della piattaforma WOM</i></p>");
            sb.Append("</body></html>");

            SendMessage(user.Email,
                $"Verifica del tuo indirizzo e-mail",
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

        private static string GetVerificationPosDeepLink(string email, string token) {
            return new UriBuilder {
                Scheme = "wompos",
                Host = "process",
                Path = "/verify",
                Query = QueryString.Create(
                    new KeyValuePair<string, StringValues>[] {
                        new KeyValuePair<string, StringValues>("email", new StringValues(email)),
                        new KeyValuePair<string, StringValues>("token", new StringValues(token)),
                    }
                ).ToString()
            }.ToString();
        }

        public void SendPasswordResetMail(User user) {
            if(user.Email == null) {
                _logger.LogError("Cannot send email to user #{0}, no e-mail given", user.Id);
                return;
            }

            var sb = new StringBuilder();
            sb.Append("<html><body>");
            sb.Append("<p><img src=\"cid:wom-logo.jpg\" width=\"160\" alt=\"WOM logo\" /></p>");
            sb.AppendFormat("<p><b>Ciao {0}!</b></p>", user.Name);
            sb.Append("<p>È stata avviata la procedura di <b>reimpostazione della password</b> per il tuo profilo. Apri l’applicazione WOM POS da cui hai fatto la richiesta ed inserisci il seguente codice:</p>");
            sb.AppendFormat("<p style=\"text-align: center; margin-bottom: 2rem\"><b style=\"font-family: monospace; font-size: 2rem; letter-spacing: 0.3rem\">{0}</b></p>", user.PasswordResetToken);
            sb.Append("<p>In alternativa, se stai leggendo l’e-mail dallo stesso dispositivo su cui è installata l’app WOM POS, puoi cliccare su questo collegamento:</p>");
            sb.AppendFormat("<p style=\"text-align: center; margin-bottom: 2rem\"><a href=\"{0}\" style=\"display: inline-block; background: #2969FF; background-color: #2969FF; color: white; font-weight: bold; text-decoration: none; margin-bottom: 1rem; padding: 0.5rem 1rem; border-radius: 0.5rem\">Reimposta la tua password nell’app</a></p>", GetPasswordResetPosDeepLink(user.Email, user.PasswordResetToken));
            sb.Append("<p>Se non hai richiesto la reimpostazione della password oppure riscontri altri problemi, contattaci all’indirizzo <a href=\"mailto:info@wom.social\">info@wom.social</a>.</p>");
            sb.Append("<p>❤&nbsp;<i>Il team della piattaforma WOM</i></p>");
            sb.Append("</body></html>");

            SendMessage(user.Email,
                $"Reimpostazione password",
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

        private static string GetPasswordResetPosDeepLink(string email, string token) {
            return new UriBuilder {
                Scheme = "wompos",
                Host = "process",
                Path = "/reset-password",
                Query = QueryString.Create(
                    new KeyValuePair<string, StringValues>[] {
                        new KeyValuePair<string, StringValues>("email", new StringValues(email)),
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
            sb.Append("<html><body>");
            sb.Append("<p><img src=\"cid:wom-logo.jpg\" width=\"160\" alt=\"WOM logo\" /></p>");
            sb.Append("<p><b>Ciao !</b></p>");
            sb.AppendFormat("<p>Sono stati generati dei voucher WOM per te, da parte di <b>{0}</b>, in virtù dello svolgimento dell’attività “{1}”.</p>", instrumentName, activityTitle);
            sb.Append("<p>I WOM sono dei voucher elettronici anonimi, che certificano l’impegno che hai dedicato ad una specifica causa per il bene sociale. Potrai riscattarli semplicemente installando l’<a href=\"https://wom.social/volunteer\">applicazione WOM Pocket</a> e cliccando sul seguente link:</p>");
            sb.AppendFormat("<p style=\"text-align: center; margin-bottom: 2rem\"><a href=\"{0}\" style=\"display: inline-block; background: #2969FF; background-color: #2969FF; color: white; font-weight: bold; text-decoration: none; margin-bottom: 1rem; padding: 0.5rem 1rem; border-radius: 0.5rem\">Riscatta i tuoi WOM</a></p>", otcLink);
            sb.Append("<p>Quando te lo chiederà, fornisci la seguente password all’applicazione:");
            sb.AppendFormat("<p style=\"text-align: center; margin-bottom: 2rem\"><b style=\"font-family: monospace; font-size: 2rem; letter-spacing: 0.3rem\">{0}</b></p>", password);
            sb.Append("<p>Consulta il sito https://wom.social oppure <a href=\"mailto:info@wom.social\">scrivici</a> per qualsiasi dubbio o informazione.</p>");
            sb.Append("<p>❤&nbsp;<i>Il team della piattaforma WOM</i></p>");
            sb.Append("</body></html>");

            SendMessage(email, "Emissione di voucher WOM", sb.ToString());
        }

        private void SendMessage(string recipientAddress, string subject, string contents) {
            var msg = new MailMessage {
                From = _mailFrom,
                Sender = _mailFrom,
                IsBodyHtml = true,
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = contents,
                BodyEncoding = Encoding.UTF8
            };
            msg.To.Add(recipientAddress);

            msg.Attachments.Add(new Attachment(
                Assembly.GetExecutingAssembly().GetManifestResourceStream("WomPlatform.Web.Api.Resources.wom-logo-240w.jpg"),
                "wom-logo.jpg",
                "image/jpeg"
            ) {
                ContentType = new ContentType("image/jpeg"),
                ContentId = "wom-logo.jpg",
            });

            if(_mailBcc != null) {
                msg.Bcc.Add(_mailBcc);
            }

            _queue.Schedule(msg);
        }

    }

}
