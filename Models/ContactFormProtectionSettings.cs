namespace GamexBusinessPage.Models;

public sealed class ContactFormProtectionSettings
{
    public bool Enabled { get; set; } = true;

    public int MaxRequestsPerIpPerWindow { get; set; } = 5;

    public int IpWindowMinutes { get; set; } = 15;

    public int MinimumSecondsBeforeSubmit { get; set; } = 3;

    public int FormTokenMaxAgeMinutes { get; set; } = 120;

    public int MinimumSecondsBetweenSubmissionsPerEmail { get; set; } = 60;
}
