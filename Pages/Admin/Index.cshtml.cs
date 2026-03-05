using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

public class IndexModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "Panel administracyjny";
    }

}
