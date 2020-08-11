using ISFG.Alfresco.Api;
using ISFG.Alfresco.Api.Configurations;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Common.Extensions;
using ISFG.Common.Interfaces;
using ISFG.EmailBox.Authentication;
using ISFG.EmailBox.Interfaces;
using ISFG.EmailBox.Models;
using ISFG.EmailBox.Models.Configuration;
using ISFG.EmailBox.Services;
using ISFG.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace ISFG.EmailBox
{
    public class Startup
    {
        #region Constructors

        public Startup(IConfiguration configuration)
        {
            EmailServerConfiguration = configuration.Bind<IEmailServerConfiguration, EmailServerConfiguration>();
            EmailAlfrescoConfiguration = configuration.Bind<IEmailAlfrescoConfiguration, EmailAlfrescoConfiguration>();
        }

        #endregion

        #region Properties

        private IEmailAlfrescoConfiguration EmailAlfrescoConfiguration { get; }
        private IEmailServerConfiguration EmailServerConfiguration { get; }

        #endregion

        #region Public Methods

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseMiddleware<ErrorHandlingMiddleware>();
            
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

            services.AddExceptions();

            services.AddAlfrescoApi(EmailAlfrescoConfiguration);
            services.AddSingleton(EmailAlfrescoConfiguration);
            services.AddSingleton(EmailServerConfiguration);
            services.AddScoped<IAuthenticationHandler, SystemAuthentication>();

            services.AddSingleton<IHttpUserContextService, SystemUserContextService>();
            services.AddScoped(p => p.GetService<IHttpUserContextService>().Current);
            services.AddScoped<IEmailService, EmailService>();

            var serviceProvider = services.BuildServiceProvider();

            // Init content model first
            var contentModel = serviceProvider.GetService<IEmailService>();
            contentModel.StartAutomaticDownload();

        }

        #endregion
    }
}