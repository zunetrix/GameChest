using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Plugin;

namespace GameChest;

internal class Configuration : IPluginConfiguration {
    public int Version { get; set; } = 1;
    public event Action? OnConfigurationChanged;
    private IDalamudPluginInterface PluginInterface { get; set; } = null;

    // Interface
    public bool OpenOnStartup { get; set; } = false;
    public bool OpenOnLogin { get; set; } = false;
    public bool AllowMovement { get; set; } = true;
    public bool AllowResize { get; set; } = true;
    public bool ShowSettingsButton { get; set; } = true;
    public bool AllowCloseWithEscape { get; set; } = false;
    public bool DebugMode { get; set; } = false;
    public Vector4 HighlightColor { get; set; } = Style.Colors.Yellow;

    public bool ListenToChatMessages { get; set; } = true;
    public HashSet<XivChatType> ListenedChatTypes { get; set; } = new() { XivChatType.Say };
    public bool ListenToRollMessages { get; set; } = true;
    public bool IsBlockListActive { get; set; } = true;
    public string CustomCommand { get; set; } = "/gc";
    public List<string> Blocklist = new();

    // Per-game configuration
    public FightGameConfiguration FightGame { get; set; } = new();
    public PrizeRollConfiguration PrizeRoll { get; set; } = new();
    public DeathRollConfiguration DeathRoll { get; set; } = new();
    public DeathRollTournamentConfiguration DeathRollTournament { get; set; } = new();
    public WordGuessConfiguration WordGuess { get; set; } = new();

    public void Initialize(IDalamudPluginInterface pluginInterface) {
        PluginInterface = pluginInterface;
    }

    public void Save() {
        PluginInterface.SavePluginConfig(this);
        OnConfigurationChanged?.Invoke();
    }

    internal sealed class FightGameConfiguration {
        public List<PhrasePool> Phrases { get; set; } = new();

        public string JoinGamePhrase { get; set; } = "I want to fight!";
        public int PlayerAHealth { get; set; } = 100;
        public int PlayerBHealth { get; set; } = 100;
        public int PlayerAMp { get; set; } = 100;
        public int PlayerBMp { get; set; } = 100;
        public int MaxRollAllowed { get; set; } = 20;
        public bool Automode { get; set; }
        public float AutoSendDelaySeconds { get; set; } = 1.35f;
        public XivChatType OutputChannel { get; set; } = XivChatType.Say;
        public float RegistrationReminderSeconds { get; set; } = 30.0f;
        public float InactivityReminderSeconds { get; set; } = 30.0f;
        public float OutOfTurnCooldownSeconds { get; set; } = 5.0f;
        public List<FightGamePreset> Presets { get; set; } = new();
        public int ActivePresetIndex { get; set; } = -1;

        public void ApplyPreset(FightGamePreset preset) {
            PlayerAHealth = preset.PlayerAHealth;
            PlayerBHealth = preset.PlayerBHealth;
            PlayerAMp = preset.PlayerAMp;
            PlayerBMp = preset.PlayerBMp;
            MaxRollAllowed = preset.MaxRollAllowed;
            Automode = preset.Automode;
            AutoSendDelaySeconds = preset.AutoSendDelaySeconds;
            RegistrationReminderSeconds = preset.RegistrationReminderSeconds;
            InactivityReminderSeconds = preset.InactivityReminderSeconds;
            OutOfTurnCooldownSeconds = preset.OutOfTurnCooldownSeconds;
        }

        public FightGamePreset SaveAsPreset(string name) => new() {
            Name = name,
            PlayerAHealth = PlayerAHealth,
            PlayerBHealth = PlayerBHealth,
            PlayerAMp = PlayerAMp,
            PlayerBMp = PlayerBMp,
            MaxRollAllowed = MaxRollAllowed,
            Automode = Automode,
            AutoSendDelaySeconds = AutoSendDelaySeconds,
            RegistrationReminderSeconds = RegistrationReminderSeconds,
            InactivityReminderSeconds = InactivityReminderSeconds,
            OutOfTurnCooldownSeconds = OutOfTurnCooldownSeconds,
        };
    }

    internal sealed class PrizeRollConfiguration {
        public List<PhrasePool> Phrases { get; set; } = new();

        public XivChatType OutputChannel { get; set; } = XivChatType.Say;
        public PrizeRollSortingMode SortingMode { get; set; } = PrizeRollSortingMode.Highest;
        public int NearestRoll { get; set; } = 500;
        public bool RerollAllowed { get; set; } = false;
        public int MaxRerollsPerPlayer { get; set; } = 1;
        public int MaxRoll { get; set; } = 999;

        public bool UseTimer { get; set; } = false;
        public int TimerDurationSeconds { get; set; } = 60;
    }

    internal sealed class DeathRollConfiguration {
        public List<PhrasePool> Phrases { get; set; } = new();
        public XivChatType OutputChannel { get; set; } = XivChatType.Say;
        public int StartingRoll { get; set; } = 999;
    }

    internal sealed class DeathRollTournamentConfiguration {
        public List<PhrasePool> Phrases { get; set; } = new();
        public XivChatType OutputChannel { get; set; } = XivChatType.Say;
        public int StartingRoll { get; set; } = 999;
    }

    internal sealed class WordGuessQuestion {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
        public string? Hint { get; set; }
        public int? TimerSecs { get; set; }
    }

    internal sealed class WordGuessConfiguration {
        public List<PhrasePool> Phrases { get; set; } = new();
        public List<WordGuessQuestion> Questions { get; set; } = new();
        public XivChatType OutputChannel { get; set; } = XivChatType.Say;
        public bool AllowPartialMatch { get; set; } = false;
        public bool CaseSensitive { get; set; } = false;
        public bool AutoAdvance { get; set; } = false;
        public bool RevealHint { get; set; } = false;
        public int RevealHintAfterSecs { get; set; } = 30;
        public bool UseGlobalTimer { get; set; } = false;
        public int GlobalTimerSecs { get; set; } = 60;
        public WordGuessVictoryMode VictoryMode { get; set; } = WordGuessVictoryMode.Single;
    }
}
