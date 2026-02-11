using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace GamexBusinessPage.Pages
{
    [OutputCache(Duration = 3600)]
    public class RealizacjeModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Realizacje Gamex Olkusz – Remonty dróg i wynajem maszyn budowlanych";
            ViewData["Description"] = "Zobacz realizacje Gamex z Olkusza – wykonane remonty dróg i projekty z wykorzystaniem wynajmowanego sprzętu budowlanego w woj. Małopolskim.";
            ViewData["Keywords"] = "Gamex, Olkusz, realizacje, remonty dróg, wynajem maszyn budowlanych, minikoparki, koparki, ładowarki, Małopolskie";
        }
    }
}
