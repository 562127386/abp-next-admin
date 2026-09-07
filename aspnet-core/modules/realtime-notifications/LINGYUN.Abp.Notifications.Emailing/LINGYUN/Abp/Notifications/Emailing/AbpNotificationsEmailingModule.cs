using LINGYUN.Abp.Identity;
using Volo.Abp.Emailing;
using Volo.Abp.MailKit;
using Volo.Abp.Modularity;

namespace LINGYUN.Abp.Notifications.Emailing;

[DependsOn(
    typeof(AbpNotificationsModule),
    typeof(AbpMailKitModule),
    typeof(AbpIdentityDomainModule))]
public class AbpNotificationsEmailingModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpNotificationsPublishOptions>(options =>
        {
            options.PublishProviders.Add<EmailingNotificationPublishProvider>();
        });
    }
}
