using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace Server;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.melx.lootvalue";
    public string Name { get; init; } = "Melx-LootValue";
    public string Author { get; init; } = "Melx";
    public List<string>? Contributors { get; init; } = null;

    public SemanticVersioning.Version Version { get; init; } = new("1.1.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("4.1.6");

    public List<string>? Incompatibilities { get; init; } = null;
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = null;
    public string? Url { get; init; } = null;
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}