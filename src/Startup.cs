﻿using GoldenTicket.Data;
using GoldenTicket.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoldenTicket
{
    /// <summary>
    /// The class for starting the application. Gets the HostingEnvironment and Configuration
    /// </summary>
    public class Startup
    {
        private IHostingEnvironment _hostingEnvironment { get; set; }

        private IConfiguration _configuration { get; set; }

        /// <summary>
        /// Creates a new instance of this class
        /// </summary>
        /// <param name="env">The hosting environment</param>
        /// <param name="config">The configuration settings for the application</param>
        public Startup(IHostingEnvironment env, IConfiguration config)
        {
            _hostingEnvironment = env;
            _configuration = config;
        }

        /// <summary>
        /// Configures the services for this application
        /// </summary>
        /// <param name="services">The service container for this application</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.Configure<MvcOptions>(options =>
            {
                if (!_hostingEnvironment.IsDevelopment())
                {
                    options.Filters.Add(new RequireHttpsAttribute());
                }
            });

            services.AddDbContext<GoldenTicketContext>(options => options.UseSqlite(_configuration["connectionString"]));

            services.AddIdentity<Technician, IdentityRole>().AddEntityFrameworkStores<GoldenTicketContext>().AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 1;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequiredUniqueChars = 1;
            });
        }

        /// <summary>
        /// Configures the application pipeline and pre-startup operations
        /// </summary>
        /// <param name="app">For configuring the application pipeline</param>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <param name="applicationLifetime"></param>
        public void Configure(IApplicationBuilder app, GoldenTicketContext context, ILogger<Startup> logger, IApplicationLifetime applicationLifetime, UserManager<Technician> userManager)
        {
            ConfigureContext(context, userManager);
            logger.LogInformation("Database created and migrated to newest version.");

            if (_hostingEnvironment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                var options = new RewriteOptions().AddRedirectToHttps();
                app.UseRewriter(options);
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void ConfigureContext(GoldenTicketContext context, UserManager<Technician> userManager)
        {
            context.Database.Migrate();
            if (_configuration.GetValue<bool>("useSeedData"))
            {
                SeedData.Initialize(context, userManager);
            }
            userManager.CreateAsync(new Technician { UserName = "admin" }, _configuration["adminPassword"]).Wait();
        }
    }
}