using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Worktree.Data;
using Worktree.Data.Entities;
using Worktree.Helpers;

namespace Worktree
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // dependency injection
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddIdentity<User, IdentityRole>(cfg =>
            {
                cfg.User.RequireUniqueEmail = true;
                cfg.Password.RequireDigit = false;
                cfg.Password.RequiredUniqueChars = 0;
                cfg.Password.RequireUppercase = false;
                cfg.Password.RequireLowercase = false;
                cfg.Password.RequireNonAlphanumeric = false;
                cfg.Password.RequiredLength = 6;
            })
                .AddDefaultTokenProviders()
                .AddEntityFrameworkStores<DataContext>();

            services.AddDbContext<DataContext>(cfg =>
            {
                cfg.UseSqlServer(this.Configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddTransient<SeedDb>();

            services.AddScoped<IUserHelper, UserHelper>();
            services.AddScoped<IMailHelper, MailHelper>();
            services.AddScoped<IProjectRepository, ProjectRepository>();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/NotAuthorized";
            });

            services.AddControllersWithViews();
        }

        // request pipeline
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/error/{0}");

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
