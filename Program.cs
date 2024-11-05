using ETS_CRUD_DEMO.Data;
using ETS_CRUD_DEMO.Models;
using ETS_CRUD_DEMO.Services.Implementations;
using ETS_CRUD_DEMO.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Set the license context for EPPlus
ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // or LicenseContext.Commercial


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);

        //Added
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Redirect(options.AccessDeniedPath);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Redirect(options.LoginPath);
            return Task.CompletedTask;
        };
    });

// Add authorization policies


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanCreate", policy => policy.RequireRole("Admin"));
    options.AddPolicy("CanRead", policy => policy.RequireRole("Admin", "Sub-admin", "Employee"));
    options.AddPolicy("CanUpdate", policy => policy.RequireRole("Admin"));
});



builder.Services.Configure<MailgunSettings>(builder.Configuration.GetSection("Mailgun"));

builder.Services.AddTransient<IEmailService, MailgunEmailService>();
builder.Services.AddTransient<IVerificationService, MailGunVerificationService>();


builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("db_connection")));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Employees}/{action=Index}/{id?}");

app.Run();
