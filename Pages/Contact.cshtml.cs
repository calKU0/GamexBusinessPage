using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GamexBusinessPage.Pages
{
    public class KontaktModel : PageModel
    {
        private readonly CatalogCache _catalogCache;
        private readonly IContactEmailService _contactEmailService;
        private readonly IContactFormProtectionService _contactFormProtectionService;
        private readonly ILogger<KontaktModel> _logger;

        public KontaktModel(
            CatalogCache catalogCache,
            IContactEmailService contactEmailService,
            IContactFormProtectionService contactFormProtectionService,
            ILogger<KontaktModel> logger)
        {
            _catalogCache = catalogCache;
            _contactEmailService = contactEmailService;
            _contactFormProtectionService = contactFormProtectionService;
            _logger = logger;
        }

        [BindProperty]
        public ContactFormInputModel Input { get; set; } = new();

        [TempData]
        public string? ContactFormStatusMessage { get; set; }

        [TempData]
        public bool? ContactFormStatusSuccess { get; set; }

        public ContactFormViewModel ContactForm { get; private set; } = new(Array.Empty<MachineItem>(), null, false);

        public string? SchemaJson { get; private set; }

        public void OnGet(string? machine = null, bool lockMachine = false)
        {
            ConfigurePageMetadata();
            Input.FormToken = _contactFormProtectionService.GenerateFormToken();
            PrepareContactForm(Input, machine, lockMachine, ContactFormStatusMessage, ContactFormStatusSuccess);
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ConfigurePageMetadata();

            if (!string.IsNullOrWhiteSpace(Input.Website))
            {
                _logger.LogWarning("Contact form honeypot field was filled. Request ignored.");
                ContactFormStatusMessage = "Dziękujemy za wiadomość. Skontaktujemy się z Tobą najszybciej jak to możliwe.";
                ContactFormStatusSuccess = true;
                return RedirectToReturnUrlOrContact(returnUrl);
            }

            if (!_contactFormProtectionService.IsSubmissionAllowed(HttpContext, Input, out var protectionErrorMessage))
            {
                ModelState.AddModelError(string.Empty, protectionErrorMessage);
            }

            var catalog = _catalogCache.GetMachineCatalog();
            var machines = catalog.Machines
                .OrderBy(machine => machine.CategoryDisplayName)
                .ThenBy(machine => machine.DisplayName)
                .ToList();

            if (!string.IsNullOrWhiteSpace(Input.Machine)
                && !machines.Any(machine => string.Equals(machine.Slug, Input.Machine, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Input.Machine", "Wybrano nieprawidłową maszynę.");
            }

            if (!ModelState.IsValid)
            {
                Input.FormToken = _contactFormProtectionService.GenerateFormToken();
                PrepareContactForm(Input, Input.Machine, false, null, null);
                return Page();
            }

            try
            {
                await _contactEmailService.SendContactEmailAsync(Input);
                ContactFormStatusMessage = "Wiadomość została wysłana. Dziękujemy za kontakt!";
                ContactFormStatusSuccess = true;
                return RedirectToReturnUrlOrContact(returnUrl);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "SMTP configuration is incomplete for contact form.");
                ModelState.AddModelError(string.Empty, "Formularz kontaktowy jest chwilowo niedostępny. Spróbuj ponownie później.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while sending contact form message.");
                ModelState.AddModelError(string.Empty, "Wystąpił nieoczekiwany błąd. Spróbuj ponownie później.");
            }

            Input.FormToken = _contactFormProtectionService.GenerateFormToken();
            PrepareContactForm(Input, Input.Machine, false, null, null);
            return Page();
        }

        private IActionResult RedirectToReturnUrlOrContact(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToPage();
        }

        private void ConfigurePageMetadata()
        {
            ViewData["Title"] = "Kontakt – wynajem maszyn i roboty drogowe | GAMEX Olkusz";
            ViewData["CanonicalUrl"] = "https://gamex-olkusz.pl/kontakt";
            ViewData["Description"] = "Kontakt do GAMEX Olkusz, tel. 601 450 146. Bezpłatna wycena wynajmu maszyn budowlanych i robót drogowych. Woj. małopolskie, śląskie i świętokrzyskie.";
            ViewData["Keywords"] = "Gamex, Olkusz, kontakt, wynajem maszyn budowlanych, remonty dróg, usługi budowlane, Małopolska, Śląsk, Świętokrzyskie";

            var baseUrl = "https://gamex-olkusz.pl";

            var localBusiness = SchemaFactory.GetLocalBusinessSchema(baseUrl);

            var breadcrumbSchema = new Dictionary<string, object?>
            {
                ["@type"] = "BreadcrumbList",
                ["itemListElement"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = 1,
                        ["name"] = "Strona główna",
                        ["item"] = baseUrl
                    },
                    new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = 2,
                        ["name"] = "Kontakt",
                        ["item"] = $"{baseUrl}/kontakt"
                    }
                }
            };

            var schemaGraph = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = new object[] { breadcrumbSchema, localBusiness }
            };

            SchemaJson = JsonSerializer.Serialize(schemaGraph, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }

        private void PrepareContactForm(
            ContactFormInputModel input,
            string? selectedMachineSlug,
            bool isMachineLocked,
            string? statusMessage,
            bool? isSuccess)
        {
            var catalog = _catalogCache.GetMachineCatalog();
            var machines = catalog.Machines
                .OrderBy(machine => machine.CategoryDisplayName)
                .ThenBy(machine => machine.DisplayName)
                .ToList();

            var normalizedMachineSlug = string.IsNullOrWhiteSpace(selectedMachineSlug)
                ? null
                : machines.FirstOrDefault(machine => string.Equals(machine.Slug, selectedMachineSlug, StringComparison.OrdinalIgnoreCase))?.Slug;

            input.Machine = normalizedMachineSlug;

            ContactForm = new ContactFormViewModel(machines, normalizedMachineSlug, isMachineLocked, input, statusMessage, isSuccess);
        }
    }
}