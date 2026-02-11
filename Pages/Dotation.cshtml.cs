using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace GamexBusinessPage.Pages
{
    [OutputCache(Duration = 3600)]
    public class DotacjaModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Dotacje UE – Gamex Olkusz";
            ViewData["Description"] = "Informacje o projekcie dofinansowanym z Funduszy Europejskich realizowanym przez Gamex w Olkuszu.";
            ViewData["Keywords"] = "Gamex, Olkusz, dotacje UE, fundusze europejskie, transformacja przedsiębiorstwa";
        }
    }
}
