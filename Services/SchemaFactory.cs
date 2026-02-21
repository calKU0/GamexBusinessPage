namespace GamexBusinessPage.Services
{
    public static class SchemaFactory
    {
        public static Dictionary<string, object?> GetLocalBusinessSchema(string baseUrl)
        {
            return new Dictionary<string, object?>
            {
                ["@type"] = "LocalBusiness",
                ["@id"] = $"{baseUrl}/#organization",
                ["name"] = "Gamex",
                ["url"] = baseUrl,
                ["logo"] = $"{baseUrl}/images/logo.png",
                ["image"] = $"{baseUrl}/images/logo.png",
                ["telephone"] = "+48 601 450 146",
                ["priceRange"] = "$$",
                ["email"] = "gamexolkusz@poczta.fm",
                ["address"] = new Dictionary<string, object?>
                {
                    ["@type"] = "PostalAddress",
                    ["streetAddress"] = "Osiek 233",
                    ["addressLocality"] = "Olkusz",
                    ["postalCode"] = "32-300",
                    ["addressRegion"] = "Małopolskie",
                    ["addressCountry"] = "PL"
                },
                ["geo"] = new Dictionary<string, object?>
                {
                    ["@type"] = "GeoCoordinates",
                    ["latitude"] = "50.259966",
                    ["longitude"] = "19.603288"
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
