using LINGYUN.Abp.Account.Security;
using LINGYUN.Abp.Account.Security.Localization;
using LINGYUN.Abp.Identity;
using LINGYUN.Abp.WeChat.MiniProgram;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Principal;
using Volo.Abp.Account.Localization;
using Volo.Abp.BlobStoring;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;
using static Org.BouncyCastle.Math.EC.ECCurve;
using static Volo.Abp.UI.Navigation.DefaultMenuNames.Application;
using Volo.Abp.UI.Navigation.Urls;

namespace LINGYUN.Abp.Account;

[DependsOn(
    typeof(Volo.Abp.Account.AbpAccountApplicationModule),
    typeof(AbpAccountApplicationContractsModule),
    typeof(AbpAccountSecurityModule),
    typeof(AbpIdentityDomainModule),
    typeof(AbpBlobStoringModule),
    typeof(AbpWeChatMiniProgramModule))]
public class AbpAccountApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMapperlyObjectMapper<AbpAccountApplicationModule>();

        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<AbpAccountApplicationModule>();
        });

        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].Urls[AccountUrlNames.EmailConfirm] = "Account/EmailConfirm";
            options.Applications["MVC"].Urls[AccountUrlNames.UserLogin] = "Account/Login";

            options.Applications["MyAngularApp"].Urls[AccountUrlNames.EmailConfirm] = "/account/email‑confirm";
            options.Applications["MyAngularApp"].Urls[AccountUrlNames.UserLogin] = "/passport/login";


            //因为在应用模块有拼接RootUrl就在此处注释  要不这个地方代码需要打开
            //var config = context.Services.GetConfiguration();
            ////读取appsettings中 MyAngularApp 的 RootUrl
            //var angularRoot = config["App:Applications:MyAngularApp:RootUrl"]!.TrimEnd('/');
            //// 直接存入完整绝对URL，GetUrlAsync会直接返回，不经过Mappings逻辑
            //options.Applications["MyAngularApp"].Urls[AccountUrlNames.EmailConfirm] = $"{angularRoot}/account/email-confirm";
            //options.Applications["MyAngularApp"].Urls[AccountUrlNames.UserLogin] = $"{angularRoot}/passport/login";

        });

        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<AccountResource>()
                .AddBaseTypes(typeof(AccountSecurityResource));
        });
    }
}
