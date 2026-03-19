using GamexBusinessPage.Services;
using GamexBusinessPage.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminPolicy");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
});
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddMemoryCache();
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(5)));
});
builder.Services.AddSingleton<CatalogCache>();
builder.Services.AddSingleton<AdminCatalogService>();
builder.Services.AddScoped<ImageService>();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<ContactFormSettings>(builder.Configuration.GetSection("ContactForm"));
builder.Services.Configure<ContactFormProtectionSettings>(builder.Configuration.GetSection("ContactFormProtection"));
builder.Services.AddDataProtection();
builder.Services.AddScoped<IContactFormProtectionService, ContactFormProtectionService>();
builder.Services.AddScoped<IContactEmailService, ContactEmailService>();
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Login";
        options.Cookie.Name = "GamexAdmin";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.UseOutputCache();

app.MapGet("/sitemap.xml", async (CatalogCache catalogCache, IWebHostEnvironment environment, HttpContext context) =>
{
    var baseUrl = "https://gamex-olkusz.pl";
    var sb = new StringBuilder();
    sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

    static string ResolveLastModified(string contentRootPath, string relativePath)
    {
        var fullPath = Path.Combine(contentRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(fullPath)
            ? File.GetLastWriteTimeUtc(fullPath).ToString("yyyy-MM-dd")
            : DateTime.UtcNow.ToString("yyyy-MM-dd");
    }

    var staticPages = new[]
    {
        new { Path = "", LastModFile = "Pages/Index.cshtml" },
        new { Path = "/kontakt", LastModFile = "Pages/Contact.cshtml" },
        new { Path = "/dotacja", LastModFile = "Pages/Dotation.cshtml" },
        new { Path = "/oferta", LastModFile = "Pages/Offer/Offer.cshtml" },
        new { Path = "/oferta/wypozyczenie-maszyn", LastModFile = "Pages/Offer/MachineRental/MachineRental.cshtml" },
        new { Path = "/oferta/uslugi", LastModFile = "Pages/Offer/Services/Services.cshtml" },
        new { Path = "/oferta/transport", LastModFile = "Pages/Offer/Transport/Transport.cshtml" }
    };

    foreach (var page in staticPages)
    {
        var lastMod = ResolveLastModified(environment.ContentRootPath, page.LastModFile);
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{baseUrl}{page.Path}</loc>");
        sb.AppendLine($"    <lastmod>{lastMod}</lastmod>");
        sb.AppendLine("    <changefreq>weekly</changefreq>");
        sb.AppendLine("    <priority>0.8</priority>");
        sb.AppendLine("  </url>");
    }

    // Dynamic pages (Machines)
    var catalog = catalogCache.GetMachineCatalog();
    var machinesLastMod = ResolveLastModified(environment.ContentRootPath, "Data/machines.json");
    foreach (var machine in catalog.Machines)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{baseUrl}/oferta/wypozyczenie-maszyn/{machine.Slug}</loc>");
        sb.AppendLine($"    <lastmod>{machinesLastMod}</lastmod>");
        sb.AppendLine("    <changefreq>monthly</changefreq>");
        sb.AppendLine("    <priority>0.6</priority>");
        sb.AppendLine("  </url>");
    }

    sb.AppendLine("</urlset>");

    context.Response.ContentType = "application/xml";
    await context.Response.WriteAsync(sb.ToString());
});

app.Run();
