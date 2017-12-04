using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LeobasChat.Data;
using LeobasChat.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using LeobasChat.Pages.ChatRooms;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace LeobasChat
{
    public class Startup
    {
        public static Dictionary<int, TcpClient> clients = new Dictionary<int, TcpClient>();
        public static Dictionary<int, string> userMsgBox = new Dictionary<int, string>();
        public static Task ReceiveEvent;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 4;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
            });

            services.ConfigureApplicationCookie(options => options.LoginPath = "/Account/LogIn");

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddFacebook(facebookOptions =>
                    {
                        facebookOptions.AppId = "1920849431488194";
                        facebookOptions.AppSecret = "b3b5d6b37dc030dcd9c499574898dc31";
                    })
                    .AddGoogle(googleOptions =>
                    {
                        googleOptions.ClientId = "783686117780-upoka99nc2bfh787au2fch4ojf3jtqse.apps.googleusercontent.com";
                        googleOptions.ClientSecret = "_oD2_6KXHrI3HXIWg9nJPjt1";
                    })
                    .AddCookie(coockieOptions =>
                    {
                        coockieOptions.LoginPath = "/Account/LogIn";
                        coockieOptions.LogoutPath = "/Account/LogOff";
                    });

            services.AddMvc()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AddPageRoute("/ChatRooms/Index", "");
                    options.Conventions.AuthorizeFolder("/Account/Manage");
                    options.Conventions.AuthorizePage("/Account/Logout");
                });

            // Register no-op EmailSender used by account confirmation and password reset during development
            // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=532713
            services.AddSingleton<IEmailSender, EmailSender>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }



            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc();
        }

    }
}
