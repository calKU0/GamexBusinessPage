using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Pages
{
    [OutputCache(Duration = 3600)]
    public class IndexModel : PageModel
    {
        private const string BaseUrl = "https://gamex-olkusz.pl";

        private readonly CatalogCache _catalogCache;

        public IndexModel(CatalogCache catalogCache)
        {
            _catalogCache = catalogCache;
        }

        public string? SchemaJson { get; private set; }

        /// <summary>Equipment categories with their machine counts - they feed the tiles linking to the rental listing.</summary>
        public IReadOnlyList<MachineCategory> MachineCategories { get; private set; } = Array.Empty<MachineCategory>();

        public int MachineCount { get; private set; }

        /// <summary>Single source of truth for the FAQ section - the same text goes into the HTML and into the structured data.</summary>
        public IReadOnlyList<(string Question, string Answer)> Faq { get; } = new List<(string, string)>
        {
            ("Ile kosztuje wynajem maszyny budowlanej z operatorem?",
             "Stawkę ustalamy indywidualnie, bo zbyt wiele zależy od konkretów: jaka maszyna, ile godzin pracy, jak daleko od Olkusza i czy potrzebny jest dodatkowy osprzęt. Rozliczamy się godzinowo albo za dobę, a przy dłuższych kontraktach schodzimy z ceny. Wycena nic nie kosztuje i zwykle mamy ją gotową następnego dnia roboczego."),

            ("Czy maszynę można wynająć bez operatora?",
             "Nie. Sprzęt wydajemy wyłącznie z naszym operatorem i to jest świadoma decyzja: koparka warta kilkaset tysięcy złotych w rękach kogoś, kto siada na niej pierwszy raz, kończy się zwykle awarią albo wypadkiem. Dla Ciebie oznacza to o tyle mniej formalności, że nie potrzebujesz uprawnień UDT ani nie odpowiadasz za przeglądy i ubezpieczenie."),

            ("Gdzie pracujecie?",
             "Trzon zleceń to Olkusz i powiat olkuski, ale regularnie jeździmy po całej Małopolsce, na Śląsk i do Świętokrzyskiego. Byliśmy z maszynami w Bukownie, Wolbromiu, Kluczach, Trzebini, Chrzanowie, Sławkowie, Jaworznie, Dąbrowie Górniczej, Zawierciu, Krakowie i Kielcach. Przy większych kontraktach zdarza nam się wyjeżdżać dalej – warto zapytać."),

            ("Jak szybko maszyna stanie na budowie?",
             "Zwykle w ciągu jednej, dwóch dób od potwierdzenia zlecenia. Transport mamy własny – naczepy niskopodwoziowe, lawetę i zestaw z HDS – więc nie czekamy na wolny termin u przewoźnika. Jeśli sprawa jest pilna, zadzwoń: czasem da się przerzucić sprzęt jeszcze tego samego dnia."),

            ("Czy pracujecie dla gmin i inwestorów publicznych?",
             "Tak, i to od początku działalności. Realizowaliśmy zadania dla samorządów, spółdzielni mieszkaniowych, deweloperów i firm budowlanych – drogi gminne, chodniki, parkingi, place manewrowe. Dokumentację odbiorową przygotowujemy w komplecie, referencje wysyłamy na życzenie."),

            ("Jakie prace drogowe i brukarskie wykonujecie?",
             "Roboty ziemne i korytowanie, podbudowy z kruszywa, nawierzchnie z mas bitumicznych, frezowanie starego asfaltu, kostkę brukową, chodniki, krawężniki, zjazdy, parkingi i odwodnienia liniowe. Bierzemy też samo przygotowanie terenu pod inwestycję, jeśli resztę robi kto inny.")
        };

        public void OnGet()
        {
            ViewData["Title"] = "Wypożyczalnia maszyn budowlanych i budowa dróg | GAMEX Olkusz";
            ViewData["Description"] = "Wynajem maszyn budowlanych z operatorem oraz budowa i remonty dróg, chodników i placów. GAMEX Olkusz – od 1994 roku w woj. małopolskim, śląskim i świętokrzyskim. Bezpłatna wycena.";
            ViewData["Keywords"] = "wypożyczalnia maszyn budowlanych, wynajem maszyn budowlanych Olkusz, wynajem koparki, wynajem minikoparki, wynajem walca, budowa dróg, remonty dróg, budowa chodników, kostka brukowa, transport materiałów sypkich, HDS, GAMEX Olkusz, Małopolska, Śląsk, Świętokrzyskie";
            ViewData["CanonicalUrl"] = BaseUrl + "/";
            ViewData["PreloadHero"] = true;

            var catalog = _catalogCache.GetMachineCatalog();
            MachineCategories = catalog.Categories
                .OrderByDescending(category => category.Machines.Count)
                .ThenBy(category => category.DisplayName, StringComparer.CurrentCulture)
                .ToList();
            MachineCount = catalog.Machines.Count;

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
