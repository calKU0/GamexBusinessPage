namespace GamexBusinessPage.Models;

public sealed class ContactFormViewModel
{
    public ContactFormViewModel(
        IReadOnlyList<MachineItem> machines,
        string? selectedMachineSlug,
        bool isMachineLocked,
        ContactFormInputModel? input = null,
        string? statusMessage = null,
        bool? isSuccess = null)
    {
        Machines = machines;
        SelectedMachineSlug = selectedMachineSlug;
        IsMachineLocked = isMachineLocked;
        Input = input ?? new ContactFormInputModel();
        StatusMessage = statusMessage;
        IsSuccess = isSuccess;
    }

    public IReadOnlyList<MachineItem> Machines { get; }

    public string? SelectedMachineSlug { get; }

    public bool IsMachineLocked { get; }

    public ContactFormInputModel Input { get; }

    public string? StatusMessage { get; }

    public bool? IsSuccess { get; }
}
