using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace GamexBusinessPage.Pages
{
    [OutputCache(Duration = 3600)]
    public class OfertaModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Oferta Gamex Olkusz – Maszyny budowlane i remonty dróg";
            ViewData["Description"] = "Poznaj pełną ofertę Gamex w Olkuszu – wynajem maszyn budowlanych i profesjonalne usługi remontu dróg w woj. Małopolskim.";
            ViewData["Keywords"] = "Gamex, Olkusz, oferta, wynajem maszyn budowlanych, remonty dróg, usługi budowlane, Małopolskie";
        }
    }
}
