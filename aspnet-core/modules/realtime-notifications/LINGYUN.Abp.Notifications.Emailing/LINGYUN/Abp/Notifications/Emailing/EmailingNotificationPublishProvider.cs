using LINGYUN.Abp.Identity;
using Markdig;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Emailing;
using Volo.Abp.Features;
using Volo.Abp.Emailing.Smtp;
using Microsoft.Extensions.Options;

namespace LINGYUN.Abp.Notifications.Emailing;

public class EmailingNotificationPublishProvider : NotificationPublishProvider
{
    public const string ProviderName = NotificationProviderNames.Emailing;
    public override string Name => ProviderName;
    protected IEmailSender EmailSender => ServiceProvider.LazyGetRequiredService<IEmailSender>();
    protected IFeatureChecker FeatureChecker => ServiceProvider.LazyGetRequiredService<IFeatureChecker>();
    protected IIdentityUserRepository UserRepository => ServiceProvider.LazyGetRequiredService<IIdentityUserRepository>();
    protected INotificationDataSerializer NotificationDataSerializer => ServiceProvider.LazyGetRequiredService<INotificationDataSerializer>();

    protected async override Task PublishAsync(
        NotificationPublishContext context,
        CancellationToken cancellationToken = default)
    {
        var userIds = context.Users.Select(x => x.UserId).ToList();
        var userList = await UserRepository.GetListByIdListAsync(userIds, cancellationToken: cancellationToken);

        var emailAddress = userList
            .Where(x => x.EmailConfirmed)
            .Select(x =>
            {
                var userEmail = x.Email;
                if (!x.Name.IsNullOrWhiteSpace())
                {
                    // "admin"<admin@abp.io>
                    return $"\"{x.Name}\"<{userEmail}>";
                }

                return $"\"{x.UserName}\"<{userEmail}>";
            })
            .Distinct()
            .JoinAsString(",");


        if (emailAddress.IsNullOrWhiteSpace())
        {
            var reason = "The subscriber did not confirm the email address and could not send email notifications!";
            Logger.LogWarning(reason);
            context.Cancel(reason);
            return;
        }
        var notificationData = await NotificationDataSerializer.ToStandard(context.Notification.Data);
        if (notificationData.Title.IsNullOrWhiteSpace() && notificationData.Message.IsNullOrWhiteSpace())
        {
            context.Cancel("Unable to send email notifications because the title and content of the message must not be empty.");
            Logger.LogWarning(context.Reason);
            return;
        }
        // markdown进行处理
        if (context.Notification.ContentType == NotificationContentType.Markdown)
        {
            notificationData.Message = Markdown.ToHtml(notificationData.Message);
        }

//        // ======================发送前打印SMTP运行时配置（ABP10.x正确写法）======================
//        var smtpConfig = ServiceProvider.LazyGetRequiredService<ISmtpEmailSenderConfiguration>();
//        var host = await smtpConfig.GetHostAsync();
//        var port = await smtpConfig.GetPortAsync();
//        var userName = await smtpConfig.GetUserNameAsync();
//        var enableSsl = await smtpConfig.GetEnableSslAsync();
//        var defaultFrom = await smtpConfig.GetDefaultFromAddressAsync();

//        Logger.LogInformation(@"[NotificationEmail Send‑Before] 运行时SMTP配置
//Host:{Host}
//Port:{Port}
//EnableSsl:{EnableSsl}
//UserName:{UserName}
//DefaultFromAddress:{DefaultFromAddress}",
//            host,
//            port,
//            enableSsl,
//            userName,
//            defaultFrom);
//        // ====================================================================================


        await EmailSender.SendAsync(emailAddress, notificationData.Title, notificationData.Message);

        Logger.LogDebug("The notification: {0} with provider: {1} has successfully published!", context.Notification.Name, Name);
    }
}
