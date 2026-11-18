using DevExpress.EntityFrameworkCore.Security;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.ApplicationBuilder;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using DevExpress.XtraEditors;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MailMergeTree.Win
{
    public class ApplicationBuilder : IDesignTimeApplicationFactory
    {
        public static WinApplication BuildApplication()
        {
            var builder = WinApplication.CreateBuilder();
            // Register custom services for Dependency Injection. For more information, refer to the following topic: https://docs.devexpress.com/eXpressAppFramework/404430/
            // builder.Services.AddScoped<CustomService>();
            // Register 3rd-party IoC containers (like Autofac, Dryloc, etc.)
            // builder.UseServiceProviderFactory(new DryIocServiceProviderFactory());
            // builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            builder.UseApplication<MailMergeTreeWindowsFormsApplication>();
            builder.Modules
                .AddCloning()
                .AddConditionalAppearance()
                .AddFileAttachments()
                .AddNotifications()
                .AddOffice(options =>
                {
                    options.RichTextMailMergeDataType = typeof(DevExpress.Persistent.BaseImpl.EF.RichTextMailMergeData);
                })
                .AddReports(options =>
                {
                    options.EnableInplaceReports = true;
                    options.ReportDataType = typeof(DevExpress.Persistent.BaseImpl.EF.ReportDataV2);
                    options.ReportStoreMode = DevExpress.ExpressApp.ReportsV2.ReportStoreModes.XML;
                })
                .AddTreeListEditors()
                .AddValidation(options =>
                {
                    options.AllowValidationDetailsAccess = false;
                })
                .AddViewVariants()
                .Add<MailMergeTree.Module.MailMergeTreeModule>()
                .Add<MailMergeTreeWinModule>();
            builder.ObjectSpaceProviders
                .AddEFCore(options =>
                {
                    options.PreFetchReferenceProperties();
                })
                    .WithDbContext<MailMergeTree.Module.BusinessObjects.MailMergeTreeEFCoreDbContext>((application, options) =>
                    {
                        options.UseMiddleTier(application.Security);
                        options.UseChangeTrackingProxies();
                        options.UseObjectSpaceLinkProxies();
                    })
                .AddNonPersistent();
            builder.Security
                .UseMiddleTierMode(options =>
                {
#if !RELEASE
                    options.WaitForMiddleTierServerReady();
                    options.BaseAddress = new Uri("http://localhost:5002/");
#else
                    options.BaseAddress = new Uri("https://localhost:44350/");
#endif
                })
                .AddPasswordAuthentication();
            builder.AddBuildStep(application =>
            {
                application.DatabaseUpdateMode = DatabaseUpdateMode.Never;
            });
            var winApplication = builder.Build();
            return winApplication;
        }

        XafApplication IDesignTimeApplicationFactory.Create()
        {
            DevExpress.EntityFrameworkCore.Security.MiddleTier.ClientServer.MiddleTierClientSecurity.DesignModeUserType = typeof(MailMergeTree.Module.BusinessObjects.ApplicationUser);
            DevExpress.EntityFrameworkCore.Security.MiddleTier.ClientServer.MiddleTierClientSecurity.DesignModeRoleType = typeof(DevExpress.Persistent.BaseImpl.EF.PermissionPolicy.PermissionPolicyRole);
            return BuildApplication();
        }
    }
}
