namespace GamexBusinessPage.Models;

public sealed class CategoryLinkViewModel
{
    public CategoryLinkViewModel(IReadOnlyList<CategoryLinkItem> links)
    {
        Links = links;
    }

    public IReadOnlyList<CategoryLinkItem> Links { get; }
}

public sealed class CategoryLinkItem
{
    public CategoryLinkItem(string label, string url, bool isActive = false)
    {
        Label = label;
        Url = url;
        IsActive = isActive;
    }

    public string Label { get; }

    public string Url { get; }

    public bool IsActive { get; }
}
