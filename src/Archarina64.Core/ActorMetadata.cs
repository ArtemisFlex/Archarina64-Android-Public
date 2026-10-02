#nullable enable
using System.Reflection;
using System.Xml.Linq;

namespace Archarina64.Core;

public sealed record ActorPropertyDefinition(string Name, string Target, ushort Mask, int Position, string[] DropdownNames);
public sealed record ActorDefinition(ushort Id, string Name, string DebugName, ushort ObjectId, int Category, IReadOnlyList<ActorPropertyDefinition> Properties, IReadOnlyDictionary<ushort, string> Variables);
public sealed record ActorPropertyValue(string Name, string Target, int Value, ushort Mask);
public sealed record ActorDescription(ushort Id, string Name, string DebugName, ushort ObjectId, IReadOnlyList<ActorPropertyValue> Properties, IReadOnlyDictionary<ushort, string> Variables);
public sealed record RomFlagUsage(int SceneId, int RoomId, int ActorIndex, string Kind, int Value, string ActorName);

/// <summary>Portable reader for SharpOcarina's OoT actor database. It preserves the desktop masks and variable notes without loading WinForms.</summary>
public sealed class ActorMetadataDatabase
{
    private readonly IReadOnlyDictionary<ushort, ActorDefinition> actors;
    private ActorMetadataDatabase(IReadOnlyDictionary<ushort, ActorDefinition> actors) => this.actors = actors;

    /// <summary>Returns the desktop actor catalog in a stable order for native pickers.</summary>
    public IReadOnlyList<ActorDefinition> Find(string? query = null, int? category = null)
    {
        IEnumerable<ActorDefinition> result = actors.Values;
        if (category is not null) result = result.Where(actor => actor.Category == category.Value);
        if (!string.IsNullOrWhiteSpace(query))
        {
            string needle = query.Trim();
            result = result.Where(actor => actor.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) || actor.DebugName.Contains(needle, StringComparison.OrdinalIgnoreCase) || $"{actor.Id:X4}".Contains(needle, StringComparison.OrdinalIgnoreCase));
        }
        return result.OrderBy(actor => actor.Id).ToArray();
    }

    public static ActorMetadataDatabase LoadOoT()
    {
        var assembly = typeof(ActorMetadataDatabase).Assembly;
        string resource = assembly.GetManifestResourceNames().First(name => name.EndsWith("OOT-ActorNames.xml", StringComparison.OrdinalIgnoreCase));
        using Stream stream = assembly.GetManifestResourceStream(resource) ?? throw new InvalidDataException("Embedded OoT actor metadata is unavailable.");
        var document = XDocument.Load(stream, LoadOptions.None); var map = new Dictionary<ushort, ActorDefinition>();
        foreach (var node in document.Root?.Elements("Actor") ?? [])
        {
            if (!TryHex((string?)node.Attribute("Key"), out ushort id)) continue;
            TryHex((string?)node.Attribute("Object"), out ushort objectId);
            string[] masks = Split((string?)node.Attribute("Properties")); string[] names = Split((string?)node.Attribute("PropertiesNames")); string[] targets = Split((string?)node.Attribute("PropertiesTarget"));
            var properties = new List<ActorPropertyDefinition>();
            for (int i = 0; i < masks.Length; i++) if (TryHex(masks[i], out ushort mask) && mask != 0) properties.Add(new ActorPropertyDefinition(i < names.Length && names[i].Length > 0 ? names[i] : $"Bits 0x{mask:X4}", i < targets.Length && targets[i].Length > 0 ? targets[i] : "Var", mask, TrailingPosition(mask), []));
            var variables = new Dictionary<ushort, string>();
            foreach (var variable in node.Elements("Variable")) if (TryHex((string?)variable.Attribute("Var"), out ushort value)) variables[value] = variable.Value.Trim();
            int category = int.TryParse((string?)node.Attribute("Category"), out int parsed) ? parsed : -1;
            map[id] = new ActorDefinition(id, (string?)node.Attribute("Name") ?? $"Actor 0x{id:X4}", (string?)node.Attribute("DebugName") ?? "", objectId, category, properties, variables);
        }
        return new ActorMetadataDatabase(map);
    }

    public bool TryDescribe(RomActor actor, out ActorDescription description)
    {
        if (!actors.TryGetValue(actor.Number, out ActorDefinition? definition) || definition is null) { description = new ActorDescription(actor.Number, $"Actor 0x{actor.Number:X4}", "", 0, [], new Dictionary<ushort, string>()); return false; }
        var values = new List<ActorPropertyValue>();
        foreach (var property in definition.Properties)
        {
            ushort source = property.Target switch { "XRot" => unchecked((ushort)actor.RotationX), "YRot" => unchecked((ushort)actor.RotationY), "ZRot" => unchecked((ushort)actor.RotationZ), _ => actor.Variable };
            values.Add(new ActorPropertyValue(property.Name, property.Target, (source & property.Mask) >> property.Position, property.Mask));
        }
        description = new ActorDescription(definition.Id, definition.Name, definition.DebugName, definition.ObjectId, values, definition.Variables); return true;
    }

    public IReadOnlyList<RomFlagUsage> AnalyzeFlags(RomDocument document)
    {
        var result = new List<RomFlagUsage>();
        foreach (var scene in document.Scenes) foreach (var room in scene.Rooms) for (int i = 0; i < room.Actors.Count; i++)
        {
            var actor = room.Actors[i]; if (!TryDescribe(actor, out var description)) continue;
            foreach (var property in description.Properties)
            {
                string kind = property.Name.IndexOf("Switch", StringComparison.OrdinalIgnoreCase) >= 0 ? "Switch" : property.Name.IndexOf("Chest", StringComparison.OrdinalIgnoreCase) >= 0 ? "Chest" : property.Name.IndexOf("Collect", StringComparison.OrdinalIgnoreCase) >= 0 ? "Collectible" : property.Name.IndexOf("Path", StringComparison.OrdinalIgnoreCase) >= 0 ? "Path" : "";
                if (kind.Length > 0) result.Add(new RomFlagUsage(scene.Id, room.Id, i, kind, property.Value, description.Name));
            }
        }
        return result;
    }

    private static int TrailingPosition(ushort mask) { int position = 0; while (position < 16 && ((mask >> position) & 1) == 0) position++; return position; }
    private static string[] Split(string? value) => string.IsNullOrWhiteSpace(value) ? [] : value.Split(',', StringSplitOptions.TrimEntries);
    private static bool TryHex(string? value, out ushort result) => ushort.TryParse(value?.Trim(), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out result);
}
