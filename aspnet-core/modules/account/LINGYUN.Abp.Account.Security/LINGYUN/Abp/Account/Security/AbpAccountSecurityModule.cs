using LINGYUN.Abp.Account.Security.Localization;
using Volo.Abp.Emailing;
using Volo.Abp.Localization;
using Volo.Abp.MailKit;
using Volo.Abp.Modularity;
using Volo.Abp.Sms;
using Volo.Abp.UI.Navigation;
using Volo.Abp.VirtualFileSystem;

namespace LINGYUN.Abp.Account.Security;

[DependsOn(
    typeof(AbpMailKitModule),
    typeof(AbpSmsModule),
    typeof(AbpUiNavigationModule))]
public class AbpAccountSecurityModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<AbpAccountSecurityModule>();
        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Add<AccountSecurityResource>("en")
                .AddVirtualJson("/LINGYUN/Abp/Account/Security/Localization/Resources");
        });
    }
}