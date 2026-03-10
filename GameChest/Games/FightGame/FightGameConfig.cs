namespace GameChest;

public sealed class FightGamePreset {
    public string Name { get; set; } = string.Empty;
    public int PlayerAHealth { get; set; } = 100;
    public int PlayerBHealth { get; set; } = 100;
    public int PlayerAMp { get; set; } = 100;
    public int PlayerBMp { get; set; } = 100;
    public int MaxRollAllowed { get; set; } = 20;
    public bool Automode { get; set; }
    public float AutoSendDelaySeconds { get; set; } = 1.5f;
    public float RegistrationReminderSeconds { get; set; } = 30.0f;
    public float InactivityReminderSeconds { get; set; } = 30.0f;
    public float OutOfTurnCooldownSeconds { get; set; } = 5.0f;
}
