using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Game.Text;

using GameChest.Extensions;
using GameChest.Extensions.Dalamud;

namespace GameChest;

public abstract class GameBase : IGame {
    private protected IPluginContext Plugin { get; }

    internal GameBase(IPluginContext plugin) {
        Plugin = plugin;
    }

    public abstract string Name { get; }
    public abstract GameMode Mode { get; }
    public abstract IGameState State { get; }
    public bool IsActive => State.IsActive;
    public virtual bool IsRegistering => false;
    public virtual bool TryJoin(string fullName, JoinSource source) => false;
    public ImGuiMessageDisplay Notification { get; } = new();
    public List<Roll> RollLog { get; } = new();
    public abstract IReadOnlyList<PhraseCategoryMeta> PhraseCategories { get; }
    public PhraseCollection Phrases => _phrases;
    public List<PhrasePool> ConfigPhrases => ConfiguredPhrases;
    protected abstract List<PhrasePool> ConfiguredPhrases { get; }
    protected abstract XivChatType OutputChannel { get; }
    private PhraseCollection _phrases = null!;

    public abstract void Start();
    public abstract void Stop();
    public virtual void Reset() {
        State.Reset();
        RollLog.Clear();
        Phrases.ResetSequences();
    }
    public virtual void RestartMatch() { }
    public abstract void ProcessRoll(Roll roll);
    public virtual void Tick(DateTime now) { }
    public abstract void SetOutputChannel(XivChatType channel);

    public void ReloadPhrases() {
        _phrases = new PhraseCollection(PhraseCategories, ConfiguredPhrases);
    }

    protected void LogRoll(Roll roll) {
        RollLog.Add(roll);
        if (RollLog.Count > 100)
            RollLog.RemoveAtSafe(0);
    }

    protected void EnsurePhraseDefaults() {
        var changed = false;
        foreach (var meta in PhraseCategories) {
            if (ConfiguredPhrases.All(p => p.CategoryId != meta.Id)) {
                ConfiguredPhrases.Add(new PhrasePool {
                    CategoryId = meta.Id,
                    Enabled = meta.DefaultEnabled,
                    Phrases = meta.Defaults.Select(d => new WeightedPhrase { Text = d, Weight = 1.0f }).ToList(),
                });
                changed = true;
            }
        }
        if (changed) Plugin.Config.Save();
    }

    protected virtual void Publish(string text) {
        var prefix = OutputChannel.ToChatPrefix();
        var fullText = prefix.Length > 0 ? $"{prefix} {text}" : text;

        var cfg = Plugin.Config;
        if (cfg.PhraseDelayEnabled && cfg.PhraseDelayMaxMs > 0) {
            var min = Math.Min(cfg.PhraseDelayMinMs, cfg.PhraseDelayMaxMs);
            var max = cfg.PhraseDelayMaxMs;
            var delayMs = Random.Shared.Next(min, max + 1);
            Task.Delay(delayMs).ContinueWith(_ => Chat.SendMessage(fullText));
            return;
        }

        Chat.SendMessage(fullText);
    }

    protected string? GetPhrase(string categoryId, Dictionary<string, string> vars) {
        return Phrases.GetRandomPhrase(categoryId, vars);
    }
}
