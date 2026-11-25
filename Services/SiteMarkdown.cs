using GamexBusinessPage.Models;
using System.Text;

namespace GamexBusinessPage.Services;

/// <summary>
/// The site in Markdown, for AI agents: /llms.txt (https://llmstxt.org) and a
/// .md version of each content page. Everything is built from the same sources
/// as the HTML - the catalogs, the FAQ and the LocalBusiness schema - so the
/// two never drift apart, and a change made in the admin panel shows up here too.
/// </summary>
/// <remarks>
/// Served by code rather than as files: the FTP deploy rewrites line endings in
/// .txt files, which breaks static files whose length is recorded at build time.
/// </remarks>
public sealed class SiteMarkdown
{
    public const string BaseUrl = "https://gamex-olkusz.pl";
    public const string MachinesPath = "/oferta/wypozyczenie-maszyn";
    public const string ServicesPath = "/oferta/uslugi";
    public const string TransportPath = "/oferta/transport";

    private readonly CatalogCache _catalogCache;

    public SiteMarkdown(CatalogCache catalogCache)
    {
        _catalogCache = catalogCache;
    }

    /// <summary>The .md address of a page, by the llms.txt convention: index.md for "/".</summary>
    public static string MarkdownUrlFor(string path) => path == "/" ? "/index.md" : path + ".md";

    public string LlmsTxt()
    {
        var machines = _catalogCache.GetMachineCatalog();
        var md = new StringBuilder();

        md.AppendLine("# GAMEX Olkusz");
        md.AppendLine();
        md.AppendLine($"> {Company.Description}");
        md.AppendLine();
        AppendContact(md);
        md.AppendLine("- Maszyny wynajmujemy standardowo z operatorem. Wynajem bez operatora zazwyczaj nie, ale w zależności od maszyny i czasu wynajmu można się dogadać.");
        md.AppendLine("- Ceny ustalamy indywidualnie, wycena jest bezpłatna. Zapytanie można wysłać formularzem na stronie kontaktowej, telefonicznie lub e-mailem.");
        md.AppendLine();

        md.AppendLine("## Oferta");
        md.AppendLine();
        md.AppendLine($"- [Wynajem maszyn budowlanych]({BaseUrl}{MarkdownUrlFor(MachinesPath)}): {machines.Machines.Count} maszyn w {machines.Categories.Count} kategoriach, z rocznikiem, masą, parametrami i wyposażeniem");
        md.AppendLine($"- [Usługi budowlane]({BaseUrl}{MarkdownUrlFor(ServicesPath)}): {List(_catalogCache.GetServiceCatalog().Categories.Select(category => category.DisplayName))}");
        md.AppendLine($"- [Transport]({BaseUrl}{MarkdownUrlFor(TransportPath)}): {List(_catalogCache.GetTransportCatalog().Categories.Select(category => category.DisplayName))}");
        md.AppendLine();

        md.AppendLine("## Firma");
        md.AppendLine();
        md.AppendLine($"- [O firmie, obszar działania i najczęstsze pytania]({BaseUrl}{MarkdownUrlFor("/")})");
        md.AppendLine($"- [Kontakt i formularz zapytania]({BaseUrl}/kontakt): strona HTML z formularzem, adresem i mapą dojazdu");
        md.AppendLine();

        md.AppendLine("## Optional");
        md.AppendLine();
        md.AppendLine($"- [Projekt dofinansowany ze środków UE]({BaseUrl}/dotacja)");
        md.AppendLine($"- [Mapa witryny]({BaseUrl}/sitemap.xml)");

        return md.ToString();
    }

    public string Home()
    {
        var md = new StringBuilder();

        md.AppendLine("# GAMEX Olkusz – wynajem maszyn budowlanych i budowa dróg");
        md.AppendLine();
        md.AppendLine($"> {Company.Description}");
        md.AppendLine();
        AppendContact(md);
        md.AppendLine();

        md.AppendLine("## Oferta");
        md.AppendLine();
        md.AppendLine($"- [Wynajem maszyn budowlanych z operatorem]({BaseUrl}{MarkdownUrlFor(MachinesPath)})");
        md.AppendLine($"- [Usługi: roboty drogowe i ziemne]({BaseUrl}{MarkdownUrlFor(ServicesPath)})");
        md.AppendLine($"- [Transport maszyn i materiałów sypkich]({BaseUrl}{MarkdownUrlFor(TransportPath)})");
        md.AppendLine();

        md.AppendLine("## Obszar działania");
        md.AppendLine();
        md.AppendLine(Join(Company.AreaServed) + ".");
        md.AppendLine();

        md.AppendLine("## Najczęstsze pytania");
        md.AppendLine();
        foreach (var (question, answer) in HomeFaq.Entries)
        {
            md.AppendLine($"### {question}");
            md.AppendLine();
            md.AppendLine(answer);
            md.AppendLine();
        }

        return md.ToString();
    }

    public string Machines()
    {
        var catalog = _catalogCache.GetMachineCatalog();
        var md = new StringBuilder();

        md.AppendLine("# Wynajem maszyn budowlanych – GAMEX Olkusz");
        md.AppendLine();
        md.AppendLine($"> {catalog.Machines.Count} maszyn w {catalog.Categories.Count} kategoriach. Standardowo z operatorem, rozliczane od godziny. Wynajem bez operatora zazwyczaj nie, ale w zależności od maszyny i czasu wynajmu można się dogadać. Stawki ustalamy indywidualnie, wycena jest bezpłatna.");
        md.AppendLine();
        AppendContact(md);
        md.AppendLine();

        foreach (var category in catalog.Categories)
        {
            md.AppendLine($"## {Escape(category.DisplayName)}");
            md.AppendLine();
            foreach (var machine in category.Machines)
            {
                AppendMachine(md, machine, "###");
            }
        }

        return md.ToString();
    }

    /// <summary>One machine, or null when the slug is unknown.</summary>
    public string? Machine(string slug)
    {
        var machine = _catalogCache.GetMachineCatalog().FindBySlug(slug);
        if (machine is null)
        {
            return null;
        }

        var md = new StringBuilder();
        AppendMachine(md, machine, "#");
        md.AppendLine("## Wynajem");
        md.AppendLine();
        md.AppendLine("Standardowo z operatorem, rozliczane od godziny. Wynajem bez operatora zazwyczaj nie, ale w zależności od maszyny i czasu wynajmu można się dogadać.");
        md.AppendLine();
        AppendContact(md);
        md.AppendLine();
        md.AppendLine($"Pozostałe maszyny: [wynajem maszyn budowlanych]({BaseUrl}{MarkdownUrlFor(MachinesPath)}).");

        return md.ToString();
    }

    public string Services()
    {
        var md = new StringBuilder();

        var categories = _catalogCache.GetServiceCatalog().Categories;

        md.AppendLine("# Usługi budowlane – GAMEX Olkusz");
        md.AppendLine();
        md.AppendLine($"> Usługi wykonywane własnym sprzętem: {List(categories.Select(category => category.DisplayName))}. Wycena jest bezpłatna.");
        md.AppendLine();
        AppendContact(md);
        md.AppendLine();

        foreach (var category in categories)
        {
            md.AppendLine($"## {Escape(category.DisplayName)}");
            md.AppendLine();
            foreach (var service in category.Services)
            {
                md.AppendLine($"### {Escape(service.Name)}");
                md.AppendLine();
                AppendParagraph(md, service.Description);
                AppendParagraph(md, service.Highlight);
            }
        }

        return md.ToString();
    }

    public string Transport()
    {
        var md = new StringBuilder();

        var categories = _catalogCache.GetTransportCatalog().Categories;

        md.AppendLine("# Transport – GAMEX Olkusz");
        md.AppendLine();
        md.AppendLine($"> Własny transport, bez pośredników: {List(categories.Select(category => category.DisplayName))}.");
        md.AppendLine();
        AppendContact(md);
        md.AppendLine();

        foreach (var category in categories)
        {
            md.AppendLine($"## {Escape(category.DisplayName)}");
            md.AppendLine();
            foreach (var transport in category.Transports)
            {
                md.AppendLine($"### {Escape(transport.Vehicle)}");
                md.AppendLine();
                AppendParagraph(md, transport.Description);
                if (!string.IsNullOrWhiteSpace(transport.WeightLimit))
                {
                    md.AppendLine($"Ładowność: {Escape(transport.WeightLimit)}");
                    md.AppendLine();
                }
                AppendParagraph(md, transport.Highlight);
            }
        }

        return md.ToString();
    }

    private static void AppendMachine(StringBuilder md, MachineItem machine, string heading)
    {
        md.AppendLine($"{heading} {Escape(machine.DisplayName)}");
        md.AppendLine();

        var page = $"{BaseUrl}{MachinesPath}/{machine.Slug}";
        md.AppendLine($"- Kategoria: {Escape(machine.CategoryDisplayName)}");
        md.AppendLine($"- Strona: [{page}]({page})");
        if (machine.ProductionYear.HasValue)
        {
            md.AppendLine($"- Rok produkcji: {machine.ProductionYear}");
        }
        if (!string.IsNullOrWhiteSpace(machine.Weight))
        {
            md.AppendLine($"- Masa: {Escape(machine.Weight)}");
        }
        foreach (var (name, value) in machine.Specifications)
        {
            md.AppendLine($"- {Escape(MachineCatalog.GetSpecificationDisplayName(name))}: {Escape(value)}");
        }
        if (machine.Equipment.Count > 0)
        {
            md.AppendLine($"- Wyposażenie: {Escape(string.Join(", ", machine.Equipment))}");
        }
        md.AppendLine();

        AppendParagraph(md, machine.Description);
        AppendParagraph(md, machine.RentalOptions);
        AppendParagraph(md, machine.Notes);
    }

    private static void AppendContact(StringBuilder md)
    {
        md.AppendLine($"- Telefon: {Company.PhoneDisplay} ({Company.OpeningHours})");
        md.AppendLine($"- E-mail: {Company.Email}");
        md.AppendLine($"- Adres: {Company.Address}");
        md.AppendLine($"- Formularz zapytania: {BaseUrl}/kontakt");
    }

    private static void AppendParagraph(StringBuilder md, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        md.AppendLine(Escape(text.Trim()));
        md.AppendLine();
    }

    // Category names can contain "i" themselves ("Transport niskopodwoziowy i
    // laweta"), so lists of them are separated by commas only.
    private static string List(IEnumerable<string> items) => string.Join(", ", items);

    private static string Join(IEnumerable<string> items)
    {
        var list = items.ToList();
        return list.Count < 2 ? string.Concat(list) : string.Join(", ", list.SkipLast(1)) + " i " + list[^1];
    }

    // Catalog text is plain prose; only characters that would turn it into
    // links, emphasis or code are escaped.
    private static string Escape(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch is '\\' or '*' or '_' or '[' or ']' or '`')
            {
                builder.Append('\\');
            }
            builder.Append(ch);
        }

        return builder.ToString();
    }

    /// <summary>Company facts, read from the LocalBusiness schema so they are kept in one place.</summary>
    private static class Company
    {
        private static readonly Dictionary<string, object?> Schema = SchemaFactory.GetLocalBusinessSchema(BaseUrl);

        public static string Description => (string)Schema["description"]!;

        public static string Email => (string)Schema["email"]!;

        // +48601450146 -> +48 601 450 146
        public static string PhoneDisplay
        {
            get
            {
                var digits = ((string)Schema["telephone"]!).TrimStart('+');
                return digits.Length == 11
                    ? $"+{digits[..2]} {digits[2..5]} {digits[5..8]} {digits[8..]}"
                    : "+" + digits;
            }
        }

        public static string Address
        {
            get
            {
                var address = (Dictionary<string, object?>)Schema["address"]!;
                return $"{address["streetAddress"]}, {address["postalCode"]} {address["addressLocality"]} (woj. {address["addressRegion"]})";
            }
        }

        public static string OpeningHours
        {
            get
            {
                var hours = ((Dictionary<string, object?>[])Schema["openingHoursSpecification"]!)[0];
                var days = (string[])hours["dayOfWeek"]!;
                var range = days.Length == 6 && days[^1] == "Saturday" ? "pon.–sob." : "pon.–pt.";
                return $"{range} {hours["opens"]}–{hours["closes"]}";
            }
        }

        public static IEnumerable<string> AreaServed =>
            ((object[])Schema["areaServed"]!)
                .Cast<Dictionary<string, object?>>()
                .Select(area => (string)area["name"]!);
    }

    /// <summary>
    /// Maps /llms.txt and the .md version of each content page. Each Markdown
    /// response names its HTML page as canonical, so search engines index the
    /// page rather than this copy of it.
    /// </summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/llms.txt", (SiteMarkdown site, HttpContext context) =>
            Respond(context, site.LlmsTxt(), canonicalPath: null));

        app.MapGet("/index.md", (SiteMarkdown site, HttpContext context) =>
            Respond(context, site.Home(), "/"));

        app.MapGet(MachinesPath + ".md", (SiteMarkdown site, HttpContext context) =>
            Respond(context, site.Machines(), MachinesPath));

        app.MapGet(MachinesPath + "/{slug}.md", (string slug, SiteMarkdown site, HttpContext context) =>
            site.Machine(slug) is { } markdown
                ? Respond(context, markdown, $"{MachinesPath}/{slug}")
                : Results.NotFound());

        app.MapGet(ServicesPath + ".md", (SiteMarkdown site, HttpContext context) =>
            Respond(context, site.Services(), ServicesPath));

        app.MapGet(TransportPath + ".md", (SiteMarkdown site, HttpContext context) =>
            Respond(context, site.Transport(), TransportPath));
    }

    private static IResult Respond(HttpContext context, string markdown, string? canonicalPath)
    {
        if (canonicalPath is not null)
        {
            context.Response.Headers.Link = $"<{BaseUrl}{canonicalPath}>; rel=\"canonical\"";
        }

        context.Response.Headers.CacheControl = "public, max-age=3600";
        return Results.Text(markdown, "text/markdown; charset=utf-8");
    }
}
