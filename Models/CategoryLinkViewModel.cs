using Microsoft.AspNetCore.Mvc;

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
    public CategoryLinkItem(string label, string url, bool isActive = false, string? anchorId = null)
    {
        Label = label;
        Url = url;
        IsActive = isActive;
        AnchorId = anchorId;
    }

    public string Label { get; }

    public string Url { get; }

    public bool IsActive { get; }

    /// <summary>
    /// Id of the section this link belongs to on the unfiltered listing. Lets
    /// the scroll spy pair a link with its section exactly, instead of matching
    /// the two by their visible text.
    /// </summary>
    public string? AnchorId { get; }
}

/// <summary>
/// Builds the category sidebar for the catalog listings.
/// </summary>
/// <remarks>
/// Each category keeps its own URL rather than being an anchor on one long page.
/// They carry their own title, description and canonical, which is what makes
/// them rank for phrases like "wynajem minikoparki"; folding them into a single
/// page would give that up. The switch between them is smoothed on the client
/// instead - see the catalog navigation in site.js.
/// </remarks>
public static class CategoryFilterLinks
{
    /// <summary>Anchor for the top of a listing, used by the "all items" view.</summary>
    public const string AllAnchorId = "katalog";

    /// <summary>
    /// Section id for a category. Uses the machine slugifier so the ids stay
    /// valid HTML and match the anchors referenced by the structured data.
    /// </summary>
    public static string AnchorFor(string key) => MachineCatalog.BuildSlug(key);

    /// <summary>
    /// Builds the links. <paramref name="pagePath"/> is the Razor page path such
    /// as "/Offer/Services/Services" - passing the routed URL instead returns
    /// null from the URL helper, which is what left every link as "#".
    /// </summary>
    public static IReadOnlyList<CategoryLinkItem> Build(
        IUrlHelper url,
        string pagePath,
        string allLabel,
        IEnumerable<(string Key, string DisplayName)> categories,
        string? selectedKey)
    {
        var links = new List<CategoryLinkItem>
        {
            new(allLabel, url.Page(pagePath) ?? "/", string.IsNullOrWhiteSpace(selectedKey), AllAnchorId)
        };

        foreach (var (key, displayName) in categories)
        {
            links.Add(new CategoryLinkItem(
                displayName,
                url.Page(pagePath, new { category = key }) ?? "/",
                string.Equals(selectedKey, key, StringComparison.OrdinalIgnoreCase),
                AnchorFor(key)));
        }

        return links;
    }
}
