using GamexBusinessPage.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace GamexBusinessPage.Services;

public sealed class CatalogCache
{
    private const string MachineCatalogCacheKey = "MachineCatalog";
    private const string ServiceCatalogCacheKey = "ServiceCatalog";
    private const string TransportCatalogCacheKey = "TransportCatalog";
    private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromHours(1);

    private readonly IWebHostEnvironment _environment;
    private readonly IMemoryCache _cache;

    public CatalogCache(IWebHostEnvironment environment, IMemoryCache cache)
    {
        _environment = environment;
        _cache = cache;
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

    private T GetOrCreate<T>(string cacheKey, Func<T> factory)
    {
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetSlidingExpiration(CacheSlidingExpiration);
            return factory();
        })!;
    }
}
