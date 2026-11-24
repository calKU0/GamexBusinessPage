using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Pages
{
    [OutputCache(Duration = 3600, Tags = new[] { CatalogCache.PageTag })]
    public class IndexModel : PageModel
    {
        private const string BaseUrl = "https://gamex-olkusz.pl";

        private readonly CatalogCache _catalogCache;

        public IndexModel(CatalogCache catalogCache)
        {
            _catalogCache = catalogCache;
        }

        public string? SchemaJson { get; private set; }

        /// <summary>Equipment categories with their machine counts, in the order set on /admin/order - they feed the tiles linking to the rental listing.</summary>
        public IReadOnlyList<MachineCategory> MachineCategories { get; private set; } = Array.Empty<MachineCategory>();

        public int MachineCount { get; private set; }

        /// <summary>The makes most of the fleet comes from, most machines first - named in the equipment intro.</summary>
        public IReadOnlyList<string> FleetBrands { get; private set; } = Array.Empty<string>();

        /// <summary>The "renomowanych producentów" strip, in the order set in the admin panel.</summary>
        public IReadOnlyList<Manufacturer> Manufacturers { get; private set; } = Array.Empty<Manufacturer>();

        /// <summary>
        /// How many times one loop of the manufacturer slider repeats the list. A loop
        /// needs about ten logos to be wider than the page, or a gap would show before
        /// the next loop slides in, so a short list is repeated.
        /// </summary>
        public int ManufacturerRepeats => Manufacturers.Count == 0 ? 0 : (int)Math.Ceiling(10.0 / Manufacturers.Count);

        /// <summary>The FAQ section - the same text goes into the HTML and into the structured data.</summary>
        public IReadOnlyList<(string Question, string Answer)> Faq => HomeFaq.Entries;

        public void OnGet()
        {
            ViewData["Title"] = "Wypożyczalnia maszyn budowlanych i budowa dróg | GAMEX Olkusz";
            ViewData["Description"] = "Wynajem maszyn budowlanych z operatorem oraz budowa i remonty dróg, chodników i placów. GAMEX Olkusz – od 1994 roku w woj. małopolskim, śląskim i świętokrzyskim. Bezpłatna wycena.";
            ViewData["Keywords"] = "wypożyczalnia maszyn budowlanych, wynajem maszyn budowlanych Olkusz, wynajem koparki, wynajem minikoparki, wynajem walca, budowa dróg, remonty dróg, budowa chodników, kostka brukowa, transport materiałów sypkich, HDS, GAMEX Olkusz, Małopolska, Śląsk, Świętokrzyskie";
            ViewData["CanonicalUrl"] = BaseUrl + "/";
            ViewData["MarkdownUrl"] = SiteMarkdown.MarkdownUrlFor("/");
            ViewData["PreloadHero"] = true;

            var catalog = _catalogCache.GetMachineCatalog();
            MachineCategories = catalog.Categories;
            MachineCount = catalog.Machines.Count;
            FleetBrands = catalog.Machines
                .Where(machine => !string.IsNullOrWhiteSpace(machine.Brand))
                .GroupBy(machine => machine.Brand!.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(brand => brand.Count())
                .ThenBy(brand => brand.Key, StringComparer.CurrentCulture)
                .Select(brand => brand.Key)
                .Take(5)
                .ToList();

            Manufacturers = _catalogCache.GetManufacturers()
                .Where(manufacturer => !string.IsNullOrWhiteSpace(manufacturer.Name))
                .ToList();

            SchemaJson = BuildSchema();
        }

        private string BuildSchema()
        {
            var localBusiness = SchemaFactory.GetLocalBusinessSchema(BaseUrl);

            var webSite = new Dictionary<string, object?>
            {
                ["@type"] = "WebSite",
                ["@id"] = $"{BaseUrl}/#website",
                ["url"] = BaseUrl,
                ["name"] = "GAMEX Olkusz",
                ["inLanguage"] = "pl-PL",
                ["publisher"] = new Dictionary<string, object?> { ["@id"] = localBusiness["@id"] }
            };

            var webPage = new Dictionary<string, object?>
            {
                ["@type"] = "WebPage",
                ["@id"] = $"{BaseUrl}/#webpage",
                ["url"] = BaseUrl + "/",
                ["name"] = ViewData["Title"] as string,
                ["description"] = ViewData["Description"] as string,
                ["inLanguage"] = "pl-PL",
                ["isPartOf"] = new Dictionary<string, object?> { ["@id"] = webSite["@id"] },
                ["about"] = new Dictionary<string, object?> { ["@id"] = localBusiness["@id"] },
                ["primaryImageOfPage"] = $"{BaseUrl}/images/road-renovation-1.webp"
            };

            var faqPage = new Dictionary<string, object?>
            {
                ["@type"] = "FAQPage",
                ["@id"] = $"{BaseUrl}/#faq",
                ["mainEntity"] = Faq.Select(entry => new Dictionary<string, object?>
                {
                    ["@type"] = "Question",
                    ["name"] = entry.Question,
                    ["acceptedAnswer"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Answer",
                        ["text"] = entry.Answer
                    }
                }).ToArray()
            };

            var graph = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = new object[] { localBusiness, webSite, webPage, faqPage }
            };

            return JsonSerializer.Serialize(graph, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}
