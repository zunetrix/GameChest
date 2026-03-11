using System;

using Dalamud.Interface.Windowing;

using GameChest.Debug;

namespace GameChest;

public class PluginUi : IDisposable {
    private Plugin Plugin { get; }

    private WindowSystem WindowSystem { get; } = new();
    public MainWindow MainWindow { get; }
    public SettingsWindow SettingsWindow { get; }
    public DebugWindow DebugWindow { get; }
    public BlocklistWindow BlocklistWindow { get; }
    public GamePhrasesWindow GamePhrasesWindow { get; }
    public FightGameWindow FightGameWindow { get; }
    public FightGameSettingsWindow FightGameSettingsWindow { get; }
    public PrizeRollWindow PrizeRollWindow { get; }
    public PrizeRollSettingsWindow PrizeRollSettingsWindow { get; }
    public DeathRollWindow DeathRollWindow { get; }
    public DeathRollSettingsWindow DeathRollSettingsWindow { get; }
    public DeathRollTournamentWindow DeathRollTournamentWindow { get; }
    public DeathRollTournamentSettingsWindow DeathRollTournamentSettingsWindow { get; }
    public WordGuessWindow WordGuessWindow { get; }
    public WordGuessSettingsWindow WordGuessSettingsWindow { get; }
    public WordGuessQuestionListWindow WordGuessQuestionListWindow { get; }
    public HighRollDuelWindow HighRollDuelWindow { get; }
    public HighRollDuelSettingsWindow HighRollDuelSettingsWindow { get; }
    public TavernBrawlWindow TavernBrawlWindow { get; }
    public TavernBrawlSettingsWindow TavernBrawlSettingsWindow { get; }
    public DiceRoyaleWindow DiceRoyaleWindow { get; }
    public DiceRoyaleSettingsWindow DiceRoyaleSettingsWindow { get; }
    public KingOfTheHillWindow KingOfTheHillWindow { get; }
    public KingOfTheHillSettingsWindow KingOfTheHillSettingsWindow { get; }
    public AssassinGameWindow AssassinGameWindow { get; }
    public AssassinGameSettingsWindow AssassinGameSettingsWindow { get; }

    public PluginUi(Plugin plugin) {
        Plugin = plugin;

        // settings windows
        FightGameSettingsWindow = AddWindow(new FightGameSettingsWindow(Plugin));
        PrizeRollSettingsWindow = AddWindow(new PrizeRollSettingsWindow(Plugin));
        DeathRollSettingsWindow = AddWindow(new DeathRollSettingsWindow(Plugin));
        DeathRollTournamentSettingsWindow = AddWindow(new DeathRollTournamentSettingsWindow(Plugin));
        WordGuessSettingsWindow = AddWindow(new WordGuessSettingsWindow(Plugin));
        HighRollDuelSettingsWindow = AddWindow(new HighRollDuelSettingsWindow(Plugin));
        TavernBrawlSettingsWindow = AddWindow(new TavernBrawlSettingsWindow(Plugin));
        DiceRoyaleSettingsWindow = AddWindow(new DiceRoyaleSettingsWindow(Plugin));
        KingOfTheHillSettingsWindow = AddWindow(new KingOfTheHillSettingsWindow(Plugin));
        AssassinGameSettingsWindow = AddWindow(new AssassinGameSettingsWindow(Plugin));

        // game windows
        FightGameWindow = AddWindow(new FightGameWindow(Plugin));
        PrizeRollWindow = AddWindow(new PrizeRollWindow(Plugin));
        DeathRollWindow = AddWindow(new DeathRollWindow(Plugin));
        DeathRollTournamentWindow = AddWindow(new DeathRollTournamentWindow(Plugin));
        WordGuessWindow = AddWindow(new WordGuessWindow(Plugin));
        WordGuessQuestionListWindow = AddWindow(new WordGuessQuestionListWindow(Plugin));
        HighRollDuelWindow = AddWindow(new HighRollDuelWindow(Plugin));
        TavernBrawlWindow = AddWindow(new TavernBrawlWindow(Plugin));
        DiceRoyaleWindow = AddWindow(new DiceRoyaleWindow(Plugin));
        KingOfTheHillWindow = AddWindow(new KingOfTheHillWindow(Plugin));
        AssassinGameWindow = AddWindow(new AssassinGameWindow(Plugin));

        // shared / global windows
        GamePhrasesWindow = AddWindow(new GamePhrasesWindow(Plugin));
        BlocklistWindow = AddWindow(new BlocklistWindow(Plugin));
        SettingsWindow = AddWindow(new SettingsWindow(Plugin));
        DebugWindow = AddWindow(new DebugWindow(Plugin));
        MainWindow = AddWindow(new MainWindow(Plugin, this));
    }

    private T AddWindow<T>(T window) where T : Window {
        WindowSystem.AddWindow(window);
        return window;
    }

    public void Dispose() {
        WindowSystem.RemoveAllWindows();
    }

    public void Draw() {
        if (!DalamudApi.PlayerState.IsLoaded) return;
        WindowSystem.Draw();
    }
}
