using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Pages
{
    public class RealizacjeModel : PageModel
    {
        public string? SchemaJson { get; private set; }

        public void OnGet()
        {
            ViewData["Title"] = "Realizacje – remonty dróg i wynajem maszyn | GAMEX Olkusz";
            ViewData["CanonicalUrl"] = "https://gamex-olkusz.pl/realizacje";
            ViewData["Description"] = "Zobacz realizacje GAMEX z Olkusza – wykonane remonty dróg i projekty z wykorzystaniem wynajmowanego sprzętu budowlanego w woj. Małopolskim.";
            ViewData["Keywords"] = "Gamex, Olkusz, realizacje, remonty dróg, wynajem maszyn budowlanych, minikoparki, koparki, ładowarki, Małopolskie";
            ViewData["Robots"] = "noindex, follow";

            SchemaJson = SchemaBuilder.BuildBreadcrumbGraph(
                ("Strona główna", "https://gamex-olkusz.pl/"),
                ("Realizacje", "https://gamex-olkusz.pl/realizacje"));
        }
    }
}
