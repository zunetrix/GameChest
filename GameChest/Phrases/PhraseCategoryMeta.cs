namespace GameChest;

public record PhraseCategoryMeta(
    string Id,
    string DisplayName,
    string[] Variables,
    string[] Defaults,
    bool DefaultEnabled = true
);
