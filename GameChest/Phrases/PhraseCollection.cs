using System;
using System.Collections.Generic;
using System.Linq;

namespace GameChest;

public class PhraseCollection {
    private readonly IReadOnlyList<PhraseCategoryMeta> _metas;
    private readonly List<PhrasePool> _pools;
    private readonly Random _rng = new();
    private readonly Dictionary<string, int> _sequenceIndex = new();

    public PhraseCollection(IReadOnlyList<PhraseCategoryMeta> metas, List<PhrasePool> pools) {
        _metas = metas;
        _pools = pools;
    }

    public string? GetRandomPhrase(string categoryId, Dictionary<string, string> vars) {
        if (_metas.All(m => m.Id != categoryId)) return null;

        var pool = _pools.FirstOrDefault(p => p.CategoryId == categoryId);
        if (pool == null || !pool.Enabled || pool.Phrases is not { Count: > 0 }) return null;

        string? template;
        if (pool.UseSequence) {
            _sequenceIndex.TryGetValue(categoryId, out var idx);
            template = pool.Phrases[idx % pool.Phrases.Count].Text;
            _sequenceIndex[categoryId] = idx + 1;
        } else {
            template = PickWeighted(pool.Phrases);
        }

        return template == null ? null : PhraseTemplateRenderer.Render(template, vars);
    }

    public void ResetSequences() => _sequenceIndex.Clear();

    private string? PickWeighted(List<WeightedPhrase> phrases) {
        var total = phrases.Sum(p => p.Weight);
        var roll = (float)(_rng.NextDouble() * total);
        foreach (var phrase in phrases) {
            roll -= phrase.Weight;
            if (roll <= 0) return phrase.Text;
        }
        return phrases[^1].Text;
    }
}
