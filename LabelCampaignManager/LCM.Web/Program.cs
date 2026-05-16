using LCM.Infrastructure.Data;
using LCM.Infrastructure.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<LCM.Infrastructure.Services.KampanyaGuncelleyici>();
builder.Services.AddHttpClient<EslApiService>();
builder.Services.AddScoped<EslGonderimService>();
builder.Services.AddHostedService<EslJobService>();
builder.Services.AddHostedService<EslLedJobService>();
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "Keys")))
    .SetApplicationName("LCM");

// Cookie Authentication
builder.Services.AddAuthentication("LCMCookie")
    .AddCookie("LCMCookie", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/YetkiYok";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<CampaignStatusService>();
builder.Services.AddScoped<MailService>();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
// Session (Captcha için)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseSession();
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex) when (
        ex is Microsoft.Data.SqlClient.SqlException ||
        ex is System.ComponentModel.Win32Exception ||
        ex.InnerException is Microsoft.Data.SqlClient.SqlException ||
        ex.InnerException is System.ComponentModel.Win32Exception)
    {
        context.Response.Redirect("/Home/BaglantiHatasi");
    }
});
app.UseAuthorization();
app.UseStatusCodePages(async context => {
    if (context.HttpContext.Response.StatusCode == 403)
    {
        context.HttpContext.Response.Redirect("/Home/YetkiYok");
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
// İlk çalıştırmada admin oluştur
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Users.Any())
    {
        db.Users.Add(new LCM.Domain.Entities.User
        {
            Ad = "Admin",
            Soyad = "User",
            KullaniciAdi = "admin",
            Email = "perakende.destek@mail.com",
            SifreHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            RolId = 1,
            AktifMi = true,
            OlusturmaTarihi = DateTime.Now
        });
        db.SaveChanges();
    }
}
var cultureInfo = new System.Globalization.CultureInfo("en-US");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Hata sayfaları
app.UseStatusCodePagesWithReExecute("/Home/Hata/{0}");
app.Run();
