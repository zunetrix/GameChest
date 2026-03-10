using System.Collections.Generic;

namespace GameChest;

public class PhrasePool {
    public string CategoryId { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public bool UseSequence { get; set; } = false;
    public List<WeightedPhrase> Phrases { get; set; } = new();
}
