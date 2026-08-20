namespace GamexBusinessPage.Services
{
    public static class SchemaFactory
    {
        public static Dictionary<string, object?> GetLocalBusinessSchema(string baseUrl)
        {
            return new Dictionary<string, object?>
            {
                ["@type"] = new[] { "LocalBusiness", "GeneralContractor" },
                ["@id"] = $"{baseUrl}/#organization",
                ["name"] = "GAMEX Olkusz",
                ["legalName"] = "Grzegorz Żurek Przedsiębiorstwo Produkcyjno-Usługowo-Handlowe „GAMEX”",
                ["alternateName"] = new[] { "P.P.U.H. GAMEX", "Gamex Olkusz", "GAMEX Grzegorz Żurek" },
                ["url"] = baseUrl,
                ["logo"] = new Dictionary<string, object?>
                {
                    ["@type"] = "ImageObject",
                    ["url"] = $"{baseUrl}/images/logo.png",
                    ["width"] = 1773,
                    ["height"] = 744
                },
                ["image"] = new[]
                {
                    $"{baseUrl}/images/road-renovation-1.webp",
                    $"{baseUrl}/images/road-renovation-3.webp",
                    $"{baseUrl}/images/machines.webp"
                },
                ["telephone"] = "+48601450146",
                ["email"] = "gamexolkusz@poczta.fm",
                ["priceRange"] = "$$",
                ["currenciesAccepted"] = "PLN",
                ["paymentAccepted"] = "Przelew bankowy, gotówka",
                ["vatID"] = "PL6370112668",
                ["taxID"] = "6370112668",
                ["foundingDate"] = "1994",
                ["slogan"] = "Wynajem maszyn budowlanych i budowa dróg od 1994 roku",
                ["description"] = "GAMEX Olkusz – wypożyczalnia maszyn budowlanych z operatorem oraz budowa i remonty dróg, chodników, parkingów i placów. Działamy w województwie małopolskim, śląskim i świętokrzyskim od 1994 roku.",
                ["knowsAbout"] = new[]
                {
                    "wynajem maszyn budowlanych",
                    "wypożyczalnia koparek",
                    "budowa dróg",
                    "remonty dróg",
                    "budowa chodników",
                    "układanie kostki brukowej",
                    "roboty ziemne",
                    "nawierzchnie asfaltowe",
                    "transport materiałów sypkich",
                    "transport maszyn budowlanych HDS"
                },
                ["sameAs"] = new[]
                {
                    "https://www.facebook.com/profile.php?id=61564522035028",
                },
                ["hasMap"] = "https://maps.google.com/?cid=15947678854369938753",
                ["address"] = new Dictionary<string, object?>
                {
                    ["@type"] = "PostalAddress",
                    ["streetAddress"] = "Osiek 233",
                    ["addressLocality"] = "Osiek",
                    ["postalCode"] = "32-300",
                    ["addressRegion"] = "małopolskie",
                    ["addressCountry"] = "PL"
                },
                ["geo"] = new Dictionary<string, object?>
                {
                    ["@type"] = "GeoCoordinates",
                    ["latitude"] = 50.259966,
                    ["longitude"] = 19.603288
                },
                ["contactPoint"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["@type"] = "ContactPoint",
                        ["telephone"] = "+48601450146",
                        ["email"] = "gamexolkusz@poczta.fm",
                        ["contactType"] = "sales",
                        ["areaServed"] = "PL",
                        ["availableLanguage"] = new[] { "pl" }
                    }
                },
                ["openingHoursSpecification"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["@type"] = "OpeningHoursSpecification",
                        ["dayOfWeek"] = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" },
                        ["opens"] = "06:00",
                        ["closes"] = "16:00"
                    }
                },
                ["areaServed"] = new object[]
                {
                    new Dictionary<string, object?> { ["@type"] = "AdministrativeArea", ["name"] = "województwo małopolskie" },
                    new Dictionary<string, object?> { ["@type"] = "AdministrativeArea", ["name"] = "województwo śląskie" },
                    new Dictionary<string, object?> { ["@type"] = "AdministrativeArea", ["name"] = "województwo świętokrzyskie" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Olkusz" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Bukowno" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Wolbrom" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Trzebinia" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Chrzanów" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Sławków" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Jaworzno" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Dąbrowa Górnicza" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Sosnowiec" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Zawiercie" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Kraków" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Katowice" },
                    new Dictionary<string, object?> { ["@type"] = "City", ["name"] = "Kielce" }
                },
                ["hasOfferCatalog"] = new Dictionary<string, object?>
                {
                    ["@type"] = "OfferCatalog",
                    ["name"] = "Usługi budowlane, wynajem maszyn i transport",
                    ["itemListElement"] = new object[]
                    {
                        BuildOffer("Wynajem maszyn budowlanych z operatorem",
                                   "Krótko- i długoterminowy wynajem koparek, koparko-ładowarek, minikoparek, walców, rozkładarek asfaltu i frezarek wraz z operatorem.",
                                   $"{baseUrl}/oferta/wypozyczenie-maszyn"),
                        BuildOffer("Budowa i remonty dróg oraz chodników",
                                   "Roboty ziemne, podbudowy, nawierzchnie bitumiczne, kostka brukowa, krawężniki, parkingi i place manewrowe.",
                                   $"{baseUrl}/oferta/uslugi"),
                        BuildOffer("Transport materiałów sypkich i maszyn",
                                   "Przewóz kruszyw, piasku i gruzu, transport niskopodwoziowy maszyn budowlanych oraz rozładunek dźwigiem HDS.",
                                   $"{baseUrl}/oferta/transport")
                    }
                }
            };
        }

        private static Dictionary<string, object?> BuildOffer(string name, string description, string url)
        {
            return new Dictionary<string, object?>
            {
                ["@type"] = "Offer",
                ["url"] = url,
                ["itemOffered"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Service",
                    ["name"] = name,
                    ["description"] = description,
                    ["serviceType"] = name,
                    ["areaServed"] = new[] { "województwo małopolskie", "województwo śląskie", "województwo świętokrzyskie" }
                }
            };
        }

        public static void ApplyRating(Dictionary<string, object?> target, string ratingValue, int reviewCount)
        {
            target["aggregateRating"] = new Dictionary<string, object?>
            {
                ["@type"] = "AggregateRating",
                ["ratingValue"] = ratingValue,
                ["reviewCount"] = reviewCount,
                ["bestRating"] = "5",
                ["worstRating"] = "1"
            };
        }
    }
}
