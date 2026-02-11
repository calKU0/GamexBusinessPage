namespace GamexBusinessPage.Models;

public sealed class ContactFormViewModel
{
    public ContactFormViewModel(IReadOnlyList<MachineItem> machines, string? selectedMachineSlug, bool isMachineLocked)
    {
        Machines = machines;
        SelectedMachineSlug = selectedMachineSlug;
        IsMachineLocked = isMachineLocked;
    }

    public IReadOnlyList<MachineItem> Machines { get; }

    public string? SelectedMachineSlug { get; }

    public bool IsMachineLocked { get; }
}
