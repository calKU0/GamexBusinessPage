namespace GamexBusinessPage.Pages.Admin;

public sealed class AdminHeaderViewModel
{
    public AdminHeaderViewModel(string title, string subtitle, IReadOnlyList<AdminHeaderAction>? actions = null)
    {
        Title = title;
        Subtitle = subtitle;
        Actions = actions ?? Array.Empty<AdminHeaderAction>();
    }

    public string Title { get; }

    public string Subtitle { get; }

    public IReadOnlyList<AdminHeaderAction> Actions { get; }
}

public sealed class AdminHeaderAction
{
    public AdminHeaderAction(string text, string page, string cssClass, IDictionary<string, string?>? routeValues = null)
    {
        Text = text;
        Page = page;
        CssClass = cssClass;
        RouteValues = routeValues;
    }

    public string Text { get; }

    public string Page { get; }

    public string CssClass { get; }

    public IDictionary<string, string?>? RouteValues { get; }
}
