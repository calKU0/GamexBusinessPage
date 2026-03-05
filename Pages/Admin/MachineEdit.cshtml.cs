using System.ComponentModel.DataAnnotations;
using GamexBusinessPage.Models;
using GamexBusinessPage.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GamexBusinessPage.Pages.Admin;

public class MachineEditModel : PageModel
{
    private readonly AdminCatalogService _catalogService;

    public MachineEditModel(AdminCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [BindProperty]
    public MachineEditor MachineInput { get; set; } = new();

    [BindProperty]
    public List<string> EquipmentItems { get; set; } = [];

    [BindProperty]
    public List<SpecificationEntry> SpecificationItems { get; set; } = [];

    [BindProperty]
    public string? OriginalMachineCategory { get; set; }

    [BindProperty]
    public string? OriginalMachineSlug { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Slug { get; set; }

    public List<string> MachineCategoryKeys { get; private set; } = [];

    public bool IsEdit => !string.IsNullOrWhiteSpace(OriginalMachineSlug) && !string.IsNullOrWhiteSpace(OriginalMachineCategory);

    public void OnGet()
    {
        ViewData["Title"] = "Maszyny - edycja";
        LoadCategories();
        PopulateEditor();
    }

    public IActionResult OnPost()
    {
        LoadCategories();

        if (!ModelState.IsValid)
        {
            EnsureLists();
            return Page();
        }

        var data = _catalogService.LoadMachineData();
        var categoryKey = NormalizeKey(MachineInput.CategoryKey);
        if (string.IsNullOrWhiteSpace(categoryKey))
        {
            ModelState.AddModelError(nameof(MachineInput.CategoryKey), "Podaj kategorię.");
            EnsureLists();
            return Page();
        }

        var newItem = CreateMachineItem();
        var newSlug = newItem.Slug;
        if (string.IsNullOrWhiteSpace(newSlug))
        {
            ModelState.AddModelError(string.Empty, "Podaj markę, model lub typ maszyny.");
            EnsureLists();
            return Page();
        }

        if (IsEdit)
        {
            if (!data.TryGetValue(OriginalMachineCategory!, out var items))
            {
                ModelState.AddModelError(string.Empty, "Nie znaleziono kategorii maszyny.");
                EnsureLists();
                return Page();
            }

            var existing = items.FirstOrDefault(item => string.Equals(item.Slug, OriginalMachineSlug, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                ModelState.AddModelError(string.Empty, "Nie znaleziono maszyny do edycji.");
                EnsureLists();
                return Page();
            }

            items.Remove(existing);
        }

        if (!data.TryGetValue(categoryKey, out var targetItems))
        {
            targetItems = [];
            data[categoryKey] = targetItems;
        }

        if (targetItems.Any(item => string.Equals(item.Slug, newSlug, StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(string.Empty, "Maszyna o tej nazwie już istnieje w tej kategorii.");
            EnsureLists();
            return Page();
        }

        targetItems.Add(newItem);
        _catalogService.SaveMachineData(data);

        return RedirectToPage("/Admin/Machines");
    }

    private void LoadCategories()
    {
        var machineData = _catalogService.LoadMachineData();
        MachineCategoryKeys = machineData.Keys.OrderBy(key => key).ToList();
    }

    private void PopulateEditor()
    {
        if (string.IsNullOrWhiteSpace(Category) || string.IsNullOrWhiteSpace(Slug))
        {
            EnsureLists();
            return;
        }

        var data = _catalogService.LoadMachineData();
        if (!data.TryGetValue(Category, out var items))
        {
            EnsureLists();
            return;
        }

        var machine = items.FirstOrDefault(item => string.Equals(item.Slug, Slug, StringComparison.OrdinalIgnoreCase));
        if (machine is null)
        {
            EnsureLists();
            return;
        }

        MachineInput = new MachineEditor
        {
            CategoryKey = Category,
            Brand = machine.Brand,
            Model = machine.Model,
            Type = machine.Type,
            Image = machine.MainImagePath,
            ProductionYear = machine.ProductionYear,
            Weight = machine.Weight,
            RentalOptions = machine.RentalOptions,
            Notes = machine.Notes
        };

        EquipmentItems = machine.Equipment?.ToList() ?? [];
        SpecificationItems = machine.Specifications
            .Select(spec => new SpecificationEntry { Key = spec.Key, Value = spec.Value })
            .ToList();

        OriginalMachineCategory = Category;
        OriginalMachineSlug = Slug;

        EnsureLists();
    }

    private void EnsureLists()
    {
        if (EquipmentItems.Count == 0)
        {
            EquipmentItems.Add(string.Empty);
        }

        if (SpecificationItems.Count == 0)
        {
            SpecificationItems.Add(new SpecificationEntry());
        }
    }

    private MachineItem CreateMachineItem()
    {
        var imagePath = NormalizeKey(MachineInput.Image);
        return new MachineItem
        {
            Brand = NormalizeKey(MachineInput.Brand),
            Model = NormalizeKey(MachineInput.Model),
            Type = NormalizeKey(MachineInput.Type),
            Images = BuildImages(imagePath),
            ProductionYear = MachineInput.ProductionYear,
            Weight = NormalizeKey(MachineInput.Weight),
            RentalOptions = NormalizeKey(MachineInput.RentalOptions),
            Notes = NormalizeKey(MachineInput.Notes),
            Equipment = EquipmentItems
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .ToList(),
            Specifications = SpecificationItems
                .Where(item => !string.IsNullOrWhiteSpace(item.Key) && !string.IsNullOrWhiteSpace(item.Value))
                .GroupBy(item => item.Key.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last().Value!.Trim(), StringComparer.OrdinalIgnoreCase)
        };
    }

    private static List<CatalogImage> BuildImages(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return [];
        }

        return [new CatalogImage { IsMain = true, Path = imagePath }];
    }

    private static string NormalizeKey(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    public sealed class MachineEditor
    {
        [Required(ErrorMessage = "Podaj kategorię.")]
        public string CategoryKey { get; set; } = string.Empty;

        public string? Brand { get; set; }

        public string? Model { get; set; }

        public string? Type { get; set; }

        public string? Image { get; set; }

        public int? ProductionYear { get; set; }

        public string? Weight { get; set; }

        public string? RentalOptions { get; set; }

        public string? Notes { get; set; }
    }

    public sealed class SpecificationEntry
    {
        public string? Key { get; set; }

        public string? Value { get; set; }
    }
}
