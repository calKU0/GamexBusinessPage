using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

public class RealizationsModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Realizacje - panel administracyjny";
    }

}
