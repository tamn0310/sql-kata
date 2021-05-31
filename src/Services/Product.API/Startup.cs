using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Product.API.Infrastuctures.Configurations;
using Product.API.Infrastuctures.Providers;
using Product.API.Infrastuctures.Repositiories;
using System;

namespace Product.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Product.API", Version = "v1" });
            });

            #region Bind config

            services.Configure<AppConnectionCfg>(this.Configuration.GetSection("ConnectionStrings"));
            var connectionCfg = new AppConnectionCfg();
            this.Configuration.Bind("ConnectionStrings", connectionCfg);
            services.AddSingleton<AppConnectionCfg>(connectionCfg);

            #endregion Bind config

            services.AddTransient<IDatabaseConnectionFactory>(e =>
            {
                return new SqlConnectionFactory(connectionCfg.DefaultConnection);
            });

            // Inject Repo
            services.AddTransient<IAccountRepository, AccountRepository>();

            //FluentMiration DB
            //services.AddFluentMigratorCore()
            //   .ConfigureRunner(builder => builder
            //   .AddSqlServer()
            //   .WithGlobalConnectionString(connectionCfg.DefaultConnection)
            //   .ScanIn(typeof(AddTable_Product).Assembly).For.Migrations());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
            IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product.API v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Instantiate the runner
            //var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
            // Run the migrations
            //runner.MigrateUp();
        }
    }
}