using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages
{
    public class DotacjaModel : PageModel
    {
        public string? SchemaJson { get; private set; }

        public void OnGet()
        {
            ViewData["Title"] = "Dotacje UE – projekt FEMP.08.07-IP.01-0145/23 | GAMEX Olkusz";
            ViewData["CanonicalUrl"] = "https://gamex-olkusz.pl/dotacja";
            ViewData["Description"] = "Projekt „Transformacja przedsiębiorstwa GAMEX” dofinansowany z Funduszy Europejskich – zakup maszyn budowlanych i uruchomienie usługi wynajmu sprzętu z operatorem.";
            ViewData["Keywords"] = "Gamex, Olkusz, dotacje UE, fundusze europejskie, transformacja przedsiębiorstwa, wynajem maszyn budowlanych";

            SchemaJson = SchemaBuilder.BuildBreadcrumbGraph(
                ("Strona główna", "https://gamex-olkusz.pl/"),
                ("Dotacje UE", "https://gamex-olkusz.pl/dotacja"));
        }
    }
}
