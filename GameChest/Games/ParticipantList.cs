using System.Collections.Generic;

namespace GameChest;

public class ParticipantList {
    public List<Participant> Entries { get; } = new();

    public void Add(Participant participant) => Entries.Add(participant);

    public void Remove(string fullName) => Entries.RemoveAll(p => p.FullName == fullName);

    public void Clear() => Entries.Clear();

    public Participant? First => Entries.Count > 0 ? Entries[0] : null;
    public Participant? Last => Entries.Count > 0 ? Entries[^1] : null;
}
