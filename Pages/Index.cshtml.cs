using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace GamexBusinessPage.Pages
{
    [OutputCache(Duration = 3600)]
    public class IndexModel : PageModel
    {
        public string? SchemaJson { get; private set; }

        public void OnGet()
        {
            ViewData["Title"] = "Gamex Olkusz - Remonty dróg i wynajem maszyn budowlanych";
            ViewData["Description"] = "GAMEX z Olkusza oferuje profesjonalne remonty dróg oraz wynajem maszyn budowlanych w woj. Małopolskim. Skontaktuj się i sprawdź naszą ofertę!";
            ViewData["Keywords"] = "GAMEX, Olkusz, remonty dróg, wynajem maszyn budowlanych, minikoparki, koparki, ładowarki, usługi budowlane, Małopolskie";
            ViewData["CanonicalUrl"] = "https://gamex-olkusz.pl/"; // Force root URL as canonical

            SchemaJson = """
            {
              "@context": "https://schema.org",
              "@graph": [
                {
                  "@type": "LocalBusiness",
                  "@id": "https://gamex-olkusz.pl/#localbusiness",
                  "name": "Gamex",
                  "image": "https://gamex-olkusz.pl/logo.png",
                  "url": "https://gamex-olkusz.pl",
                  "telephone": "+48 601 450 146",
                  "email": "gamexolkusz@poczta.fm",
                  "priceRange": "$$",
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
                  },
                  "openingHoursSpecification": {
                    "@type": "OpeningHoursSpecification",
                    "dayOfWeek": [
                      "Monday",
                      "Tuesday",
                      "Wednesday",
                      "Thursday",
                      "Friday",
                      "Saturday"
                    ],
                    "opens": "06:00",
                    "closes": "16:00"
                  },
                  "areaServed": {
                    "@type": "City",
                    "name": "Olkusz"
                  },
                  "hasOfferCatalog": {
                    "@type": "OfferCatalog",
                    "name": "Usługi Budowlane i Drogowe",
                    "itemListElement": [
                      {
                        "@type": "Offer",
                        "itemOffered": {
                          "@type": "Service",
                          "name": "Remonty dróg i roboty drogowe"
                        }
                      },
                      {
                        "@type": "Offer",
                        "itemOffered": {
                          "@type": "Service",
                          "name": "Wypożyczenie maszyn budowlanych (koparki, ładowarki)"
                        }
                      },
                      {
                        "@type": "Offer",
                        "itemOffered": {
                          "@type": "Service",
                          "name": "Usługi brukarskie"
                        }
                      },
                      {
                        "@type": "Offer",
                        "itemOffered": {
                          "@type": "Service",
                          "name": "Wykonanie kanalizacji"
                        }
                      }
                    ]
                  }
                },
                {
                  "@type": "WebSite",
                  "@id": "https://gamex-olkusz.pl/#website",
                  "url": "https://gamex-olkusz.pl",
                  "name": "Gamex - Budowa dróg i wynajem maszyn Olkusz",
                  "publisher": {
                    "@id": "https://gamex-olkusz.pl/#localbusiness"
                  }
                }
              ]
            }
            """;
        }
    }
}
