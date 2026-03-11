using System;
using System.Collections.Generic;

namespace GameChest;

public interface IGame {
    string Name { get; }
    GameMode Mode { get; }
    IGameState State { get; }
    bool IsActive { get; }
    ImGuiMessageDisplay Notification { get; }
    IReadOnlyList<PhraseCategoryMeta> PhraseCategories { get; }
    PhraseCollection Phrases { get; }
    List<PhrasePool> ConfigPhrases { get; }
    void ReloadPhrases();
    void Start();
    void Stop();
    void Reset();
    void RestartMatch();
    void ProcessRoll(Roll roll);
    void Tick(DateTime now);
}
