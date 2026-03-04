namespace GamexBusinessPage.Services
{
    public static class SchemaFactory
    {
        public static Dictionary<string, object?> GetLocalBusinessSchema(string baseUrl)
        {
            return new Dictionary<string, object?>
            {
                ["@type"] = "LocalBusiness",
                ["additionalType"] = "https://schema.org/RentalBusiness",
                ["@id"] = $"{baseUrl}/#organization",
                ["name"] = "Gamex Olkusz",
                ["url"] = baseUrl,
                ["logo"] = $"{baseUrl}/images/logo.png",
                ["image"] = $"{baseUrl}/images/logo.png",
                ["telephone"] = "+48601450146",
                ["priceRange"] = "$$",
                ["email"] = "gamexolkusz@poczta.fm",
                ["description"] = "Gamex Olkusz - profesjonalna budowa dróg i wynajem maszyn budowlanych z ponad 30-letnim doświadczeniem.",
                ["sameAs"] = new[]
                {
                    "https://www.facebook.com/profile.php?id=61564522035028",
                },
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
                ["areaServed"] = new[]
                {
                    new Dictionary<string, object?> { ["@type"] = "AdministrativeArea", ["name"] = "Województwo Małopolskie" },
                    new Dictionary<string, object?> { ["@type"] = "AdministrativeArea", ["name"] = "Województwo Śląskie" },
                    new Dictionary<string, object?> { ["@type"] = "AdministrativeArea", ["name"] = "Województwo Świętokrzyskie" }
                },
                ["hasOfferCatalog"] = new Dictionary<string, object?>
                {
                    ["@type"] = "OfferCatalog",
                    ["name"] = "Usługi budowlane i transportowe",
                    ["itemListElement"] = new[]
                    {
                        new Dictionary<string, object?> { ["@type"] = "Offer", ["itemOffered"] = new Dictionary<string, object?> { ["@type"] = "Service", ["name"] = "Remonty dróg i nawierzchni" } },
                        new Dictionary<string, object?> { ["@type"] = "Offer", ["itemOffered"] = new Dictionary<string, object?> { ["@type"] = "Service", ["name"] = "Wynajem maszyn budowlanych" } },
                        new Dictionary<string, object?> { ["@type"] = "Offer", ["itemOffered"] = new Dictionary<string, object?> { ["@type"] = "Service", ["name"] = "Transport materiałów sypkich" } }
                    }
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
