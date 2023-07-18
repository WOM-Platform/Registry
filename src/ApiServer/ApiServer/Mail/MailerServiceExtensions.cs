using Microsoft.Extensions.DependencyInjection;
using WomPlatform.Web.Api.Mail;

namespace WomPlatform.Web.Api {
    public static class MailerServiceExtensions {
        public static IServiceCollection AddMailComposer(this IServiceCollection services) {
            services.AddSingleton<IMailerQueue, MailerQueue>();
            services.AddHostedService<MailerBackgroundService>();
            services.AddSingleton<MailerComposer>();
            return services;
        }
    }
}
