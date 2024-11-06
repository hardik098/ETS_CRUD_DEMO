using ETS_CRUD_DEMO.Data;
using ETS_CRUD_DEMO.Models;
using ETS_CRUD_DEMO.Services.Implementations;
using ETS_CRUD_DEMO.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Set the license context for EPPlus
ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // or LicenseContext.Commercial

// Add session services
builder.Services.AddDistributedMemoryCache(); // Required for session management
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Ensure the session cookie is created even when the user hasn't consented to non-essential cookies.
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("db_connection"), op => op.CommandTimeout(60)));

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);

        // Set headers for redirect events
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

// Configure Mailgun settings
builder.Services.Configure<MailgunSettings>(builder.Configuration.GetSection("Mailgun"));



// Add repository and services
builder.Services.AddTransient<IEmailService, MailgunEmailService>();
builder.Services.AddTransient<IVerificationService, MailGunVerificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // Add this line to enable session management
app.UseAuthentication(); // Ensure authentication middleware is included
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Employees}/{action=Index}/{id?}");

app.Run();
