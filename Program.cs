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
// No base policy. It covered every request, static files and the contact form
// included, which caused two real problems:
//   * CSS and JS already carry their own cache headers from MapStaticAssets, and
//     the extra buffering could serve an empty or stale response for five minutes
//     after a new version was deployed,
//   * a cached contact page would hand every visitor the same antiforgery token,
//     making a share of the form submissions fail.
// Pages that genuinely can be cached declare it with the [OutputCache] attribute
// on their models.
builder.Services.AddOutputCache();
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

    static void AppendUrl(StringBuilder builder, string loc, string lastMod, string changeFreq, string priority)
    {
        builder.AppendLine("  <url>");
        builder.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(loc)}</loc>");
        builder.AppendLine($"    <lastmod>{lastMod}</lastmod>");
        builder.AppendLine($"    <changefreq>{changeFreq}</changefreq>");
        builder.AppendLine($"    <priority>{priority}</priority>");
        builder.AppendLine("  </url>");
    }

    var staticPages = new[]
    {
        new { Path = "/", LastModFile = "Pages/Index.cshtml", Freq = "weekly", Priority = "1.0" },
        new { Path = "/oferta/wypozyczenie-maszyn", LastModFile = "Pages/Offer/MachineRental/MachineRental.cshtml", Freq = "weekly", Priority = "0.9" },
        new { Path = "/oferta/uslugi", LastModFile = "Pages/Offer/Services/Services.cshtml", Freq = "weekly", Priority = "0.9" },
        new { Path = "/oferta/transport", LastModFile = "Pages/Offer/Transport/Transport.cshtml", Freq = "monthly", Priority = "0.8" },
        new { Path = "/oferta", LastModFile = "Pages/Offer/Offer.cshtml", Freq = "monthly", Priority = "0.8" },
        new { Path = "/kontakt", LastModFile = "Pages/Contact.cshtml", Freq = "monthly", Priority = "0.7" },
        new { Path = "/dotacja", LastModFile = "Pages/Dotation.cshtml", Freq = "yearly", Priority = "0.3" }
    };

    foreach (var page in staticPages)
    {
        var lastMod = ResolveLastModified(environment.ContentRootPath, page.LastModFile);
        AppendUrl(sb, $"{baseUrl}{page.Path}", lastMod, page.Freq, page.Priority);
    }

    var catalog = catalogCache.GetMachineCatalog();
    var machinesLastMod = ResolveLastModified(environment.ContentRootPath, "Data/machines.json");

    foreach (var category in catalog.Categories)
    {
        AppendUrl(sb,
            $"{baseUrl}/oferta/wypozyczenie-maszyn?category={Uri.EscapeDataString(category.Key)}",
            machinesLastMod, "monthly", "0.7");
    }

    foreach (var machine in catalog.Machines)
    {
        AppendUrl(sb,
            $"{baseUrl}/oferta/wypozyczenie-maszyn/{machine.Slug}",
            machinesLastMod, "monthly", "0.6");
    }

    sb.AppendLine("</urlset>");

    context.Response.ContentType = "application/xml";
    await context.Response.WriteAsync(sb.ToString());
});

app.Run();
