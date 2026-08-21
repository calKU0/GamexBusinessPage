using Microsoft.AspNetCore.Html;

namespace GamexBusinessPage.Services;

/// <summary>
/// Serves the bundled stylesheet as markup so the public layout can put it
/// straight into &lt;head&gt;.
/// </summary>
/// <remarks>
/// A linked stylesheet blocks the first paint until it has been requested and
/// returned. On a throttled mobile connection that round trip, not the size of
/// the file, was the cost: minifying the bundle cut it from 23 KB to 14 KB and
/// moved the measured render delay by 90 ms out of 1990. Inlining removes the
/// request from the critical path altogether.
///
/// The trade-off is that the stylesheet is no longer cached on its own, so every
/// page carries it again - about 14 KB compressed. That is the right way round
/// for this site, where most visits arrive from search onto a single page. The
/// admin layout keeps the linked version: it is behind a login, not indexed, and
/// benefits more from the shared cache.
/// </remarks>
public sealed class InlineStylesheet
{
    private readonly IWebHostEnvironment _environment;
    private readonly object _gate = new();

    private HtmlString? _cached;
    private DateTimeOffset _cachedAt;

    public InlineStylesheet(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// The stylesheet, ready to drop inside a &lt;style&gt; element. Empty if the
    /// bundle is missing, in which case the layout falls back to linking it.
    /// </summary>
    public HtmlString Css
    {
        get
        {
            var file = _environment.WebRootFileProvider.GetFileInfo("css/site.css");
            if (!file.Exists)
            {
                return HtmlString.Empty;
            }

            // Re-read when the bundle changes, so a rebuild shows up without an
            // application restart during development.
            if (_cached is not null && _cachedAt == file.LastModified)
            {
                return _cached;
            }

            lock (_gate)
            {
                if (_cached is not null && _cachedAt == file.LastModified)
                {
                    return _cached;
                }

                using var reader = new StreamReader(file.CreateReadStream());
                var css = reader.ReadToEnd();

                // A closing tag inside the text would end the style element early
                // and spill the rest into the document. CSS has no reason to
                // contain one, but the check costs nothing.
                if (css.Contains("</style", StringComparison.OrdinalIgnoreCase))
                {
                    css = css.Replace("</style", "<\\/style", StringComparison.OrdinalIgnoreCase);
                }

                _cachedAt = file.LastModified;
                _cached = new HtmlString(css);
                return _cached;
            }
        }
    }
}
