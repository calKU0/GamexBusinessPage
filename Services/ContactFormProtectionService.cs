using GamexBusinessPage.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GamexBusinessPage.Services;

public interface IContactFormProtectionService
{
    string GenerateFormToken();

    bool IsSubmissionAllowed(HttpContext httpContext, ContactFormInputModel inputModel, out string errorMessage);
}

public sealed class ContactFormProtectionService : IContactFormProtectionService
{
    private const string ProtectorPurpose = "GamexBusinessPage.ContactForm.Token";

    private readonly IDataProtector _protector;
    private readonly IMemoryCache _cache;
    private readonly ContactFormProtectionSettings _settings;

    public ContactFormProtectionService(
        IDataProtectionProvider dataProtectionProvider,
        IMemoryCache cache,
        IOptions<ContactFormProtectionSettings> options)
    {
        _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
        _cache = cache;
        _settings = options.Value;
    }

    public string GenerateFormToken()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        return _protector.Protect(timestamp);
    }

    public bool IsSubmissionAllowed(HttpContext httpContext, ContactFormInputModel inputModel, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!_settings.Enabled)
        {
            return true;
        }

        if (!TryReadTokenTimestamp(inputModel.FormToken, out var renderedAt))
        {
            errorMessage = "Nie udało się zweryfikować formularza. Odśwież stronę i spróbuj ponownie.";
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var formAge = now - renderedAt;

        if (formAge < TimeSpan.FromSeconds(Math.Max(_settings.MinimumSecondsBeforeSubmit, 0)))
        {
            errorMessage = "Wyślij formularz po krótkiej chwili od jego otwarcia.";
            return false;
        }

        if (formAge > TimeSpan.FromMinutes(Math.Max(_settings.FormTokenMaxAgeMinutes, 1)))
        {
            errorMessage = "Sesja formularza wygasła. Odśwież stronę i wyślij wiadomość ponownie.";
            return false;
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!TryConsumeRequest($"contact:ip:{ip}", _settings.MaxRequestsPerIpPerWindow, TimeSpan.FromMinutes(_settings.IpWindowMinutes), now, out var retryAfter))
        {
            errorMessage = $"Przekroczono limit prób. Spróbuj ponownie za około {Math.Max((int)Math.Ceiling(retryAfter.TotalMinutes), 1)} minut.";
            return false;
        }

        var normalizedEmail = (inputModel.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var emailKey = $"contact:email:last:{normalizedEmail}";
            if (_cache.TryGetValue<DateTimeOffset>(emailKey, out var lastSubmissionAt))
            {
                var cooldown = TimeSpan.FromSeconds(Math.Max(_settings.MinimumSecondsBetweenSubmissionsPerEmail, 0));
                var elapsed = now - lastSubmissionAt;
                if (elapsed < cooldown)
                {
                    var waitSeconds = (int)Math.Ceiling((cooldown - elapsed).TotalSeconds);
                    errorMessage = $"Kolejną wiadomość z tego adresu e-mail możesz wysłać za {Math.Max(waitSeconds, 1)} s.";
                    return false;
                }
            }

            _cache.Set(emailKey, now, TimeSpan.FromHours(6));
        }

        return true;
    }

    private bool TryReadTokenTimestamp(string? token, out DateTimeOffset renderedAt)
    {
        renderedAt = default;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var unprotected = _protector.Unprotect(token);
            if (!long.TryParse(unprotected, out var unixSeconds))
            {
                return false;
            }

            renderedAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TryConsumeRequest(string key, int permitLimit, TimeSpan window, DateTimeOffset now, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;

        if (permitLimit <= 0)
        {
            return true;
        }

        if (window <= TimeSpan.Zero)
        {
            return true;
        }

        var bucket = _cache.GetOrCreate(key, entry =>
        {
            entry.SlidingExpiration = window;
            return new RequestBucket();
        })!;

        lock (bucket.Sync)
        {
            while (bucket.Timestamps.Count > 0 && now - bucket.Timestamps.Peek() >= window)
            {
                bucket.Timestamps.Dequeue();
            }

            if (bucket.Timestamps.Count >= permitLimit)
            {
                var oldest = bucket.Timestamps.Peek();
                retryAfter = window - (now - oldest);
                return false;
            }

            bucket.Timestamps.Enqueue(now);
            return true;
        }
    }

    private sealed class RequestBucket
    {
        public object Sync { get; } = new();

        public Queue<DateTimeOffset> Timestamps { get; } = new();
    }
}
