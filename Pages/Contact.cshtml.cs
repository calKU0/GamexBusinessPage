using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using System.Linq;
using Microsoft.AspNetCore.OutputCaching;

namespace GamexBusinessPage.Pages
{
    [OutputCache(Duration = 3600)]
    public class KontaktModel : PageModel
    {
        private readonly CatalogCache _catalogCache;

        public KontaktModel(CatalogCache catalogCache)
        {
            _catalogCache = catalogCache;
        }

        public ContactFormViewModel ContactForm { get; private set; } = new(Array.Empty<MachineItem>(), null, false);

        public string? SchemaJson { get; private set; }

        public void OnGet()
        {
            ViewData["Title"] = "Kontakt Gamex Olkusz – Remonty dróg i wynajem maszyn budowlanych";
            ViewData["Description"] = "Skontaktuj się z Gamex w Olkuszu – wynajem maszyn budowlanych i remonty dróg w woj. Małopolskim. Telefon, email, adres i formularz kontaktowy.";
            ViewData["Keywords"] = "Gamex, Olkusz, kontakt, wynajem maszyn budowlanych, remonty dróg, usługi budowlane, Małopolskie";

            SchemaJson = """
            {
              "@context": "https://schema.org",
              "@graph": [
                {
                  "@type": "BreadcrumbList",
                  "itemListElement": [
                    {
                      "@type": "ListItem",
                      "position": 1,
                      "name": "Strona główna",
                      "item": "https://gamex-olkusz.pl"
                    },
                    {
                      "@type": "ListItem",
                      "position": 2,
                      "name": "Kontakt",
                      "item": "https://gamex-olkusz.pl/kontakt"
                    }
                  ]
                },
                {
                  "@type": "LocalBusiness",
                  "name": "Gamex",
                  "image": "https://gamex-olkusz.pl/logo.png",
                  "url": "https://gamex-olkusz.pl",
                  "telephone": "+48 601 450 146",
                  "email": "gamexolkusz@poczta.fm",
                  "address": {
                    "@type": "PostalAddress",
                    "streetAddress": "Osiek 233",
                    "addressLocality": "Olkusz",
                    "postalCode": "32-300",
                    "addressCountry": "PL"
                  },
                  "geo": {
                    "@type": "GeoCoordinates",
                    "latitude": 50.25996667365257,
                    "longitude": 19.603288130684327
                  }
                }
              ]
            }
            """;

            var catalog = _catalogCache.GetMachineCatalog();
            var machines = catalog.Machines
                .OrderBy(machine => machine.CategoryDisplayName)
                .ThenBy(machine => machine.DisplayName)
                .ToList();
            ContactForm = new ContactFormViewModel(machines, null, false);
        }
    }
}
