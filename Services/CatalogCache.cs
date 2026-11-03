using GamexBusinessPage.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace GamexBusinessPage.Services;

public sealed class CatalogCache
{
    /// <summary>
    /// Output-cache tag for the public pages built from the catalogs. Clearing a
    /// catalog evicts them too, so an admin change shows up straight away rather
    /// than when the cached page expires.
    /// </summary>
    public const string PageTag = "catalog";

    private const string MachineCatalogCacheKey = "MachineCatalog";
    private const string ServiceCatalogCacheKey = "ServiceCatalog";
    private const string TransportCatalogCacheKey = "TransportCatalog";
    private const string ManufacturersCacheKey = "Manufacturers";
    private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromHours(1);

    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _cache;
    private readonly IOutputCacheStore _outputCache;

    public CatalogCache(IWebHostEnvironment environment, IMemoryCache cache, IOutputCacheStore outputCache)
    {
        _environment = environment;
        _cache = cache;
        _outputCache = outputCache;
    }

    public MachineCatalog GetMachineCatalog()
    {
        return GetOrCreate(MachineCatalogCacheKey, () => MachineCatalog.Load(_environment.ContentRootPath));
    }

    public ServiceCatalog GetServiceCatalog()
    {
        return GetOrCreate(ServiceCatalogCacheKey, () => ServiceCatalog.Load(_environment.ContentRootPath));
    }

    public TransportCatalog GetTransportCatalog()
    {
        return GetOrCreate(TransportCatalogCacheKey, () => TransportCatalog.Load(_environment.ContentRootPath));
    }

    public IReadOnlyList<Manufacturer> GetManufacturers()
    {
        return GetOrCreate<IReadOnlyList<Manufacturer>>(ManufacturersCacheKey, () => ManufacturerList.Load(_environment.ContentRootPath));
    }

    public void ClearMachineCatalog()
    {
        Clear(MachineCatalogCacheKey);
    }

    public void ClearServiceCatalog()
    {
        Clear(ServiceCatalogCacheKey);
    }

    public void ClearTransportCatalog()
    {
        Clear(TransportCatalogCacheKey);
    }

    public void ClearManufacturers()
    {
        Clear(ManufacturersCacheKey);
    }

    private void Clear(string cacheKey)
    {
        _cache.Remove(cacheKey);

        // The in-memory store evicts synchronously, so waiting here costs nothing.
        _outputCache.EvictByTagAsync(PageTag, CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }

    private T GetOrCreate<T>(string cacheKey, Func<T> factory)
    {
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetSlidingExpiration(CacheSlidingExpiration);
            return factory();
        })!;
    }
}
