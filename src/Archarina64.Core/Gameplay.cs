#nullable enable
using System.Globalization;
using System.Xml.Linq;

namespace Archarina64.Core;

public sealed record GameplayFlag(string Kind, string Target, int Value, int Position, int Mask);
public sealed record GameplayActor(string Id, string Role, string Name, float X, float Y, float Z, int RotationY, int Variable, bool IsTransition, IReadOnlyList<GameplayFlag> Flags);
public sealed record GameplayLink(string SourceId, string TargetId, string Kind, string Parameter);
public sealed record GameplaySummary(int Scene, int Room, string Name, IReadOnlyList<GameplayActor> Actors, IReadOnlyList<GameplayLink> Links, IReadOnlyList<string> Warnings);

public static class GameplayEditor
{
    public static GameplaySummary Read(string xml)
    {
        using var reader = DesktopImport.CreateReader(xml);
        var root = XDocument.Load(reader).Root ?? throw new InvalidDataException("Empty tile document.");
        var gameplay = root.Element("Gameplay") ?? new XElement("Gameplay");
        int scene = Int(gameplay, "Scene", -1), room = Int(gameplay, "Room", -1);
        var actors = new List<GameplayActor>();
        foreach (var node in gameplay.Element("Actors")?.Elements("RoomPackageActor") ?? [])
        {
            var position = node.Element("Position"); var rotation = node.Element("Rotation");
            var flags = (node.Element("Flags")?.Elements("DungeonFlagReference") ?? []).Select(flag => new GameplayFlag(
                Text(flag, "Kind"), Text(flag, "Target"), Int(flag, "Value"), Int(flag, "Position"), Int(flag, "Mask"))).ToArray();
            actors.Add(new GameplayActor(Text(node, "Id"), Text(node, "Role"), Text(node, "Name"), Number(position, "X"), Number(position, "Y"), Number(position, "Z"), Int(rotation, "Y"), Int(node, "Variable"), Bool(node, "IsTransition"), flags));
        }
        var links = (gameplay.Element("Links")?.Elements("LogicLink") ?? []).Select(link => new GameplayLink(Text(link, "SourceId"), Text(link, "TargetId"), Text(link, "Kind"), Text(link, "Parameter"))).ToArray();
        var warnings = (gameplay.Element("Warnings")?.Elements("string") ?? []).Select(x => x.Value).ToArray();
        return new GameplaySummary(scene, room, Text(gameplay, "Name"), actors, links, warnings);
    }

    public static string UpdateActor(string xml, string actorId, float x, float y, float z, int rotationY, int variable, int? firstFlagValue = null)
    {
        var document = Load(xml); var actor = FindActor(document, actorId);
        Set(actor.Element("Position")!, "X", x); Set(actor.Element("Position")!, "Y", y); Set(actor.Element("Position")!, "Z", z);
        Set(actor.Element("Rotation")!, "Y", rotationY); Set(actor, "Variable", variable);
        if (firstFlagValue.HasValue)
        {
            var flag = actor.Element("Flags")?.Element("DungeonFlagReference");
            if (flag != null) Set(flag, "Value", firstFlagValue.Value);
        }
        return document.ToString(SaveOptions.DisableFormatting);
    }

    public static string AddLink(string xml, string sourceId, string targetId, string kind, string parameter)
    {
        var document = Load(xml); var gameplay = document.Root!.Element("Gameplay");
        if (gameplay == null) { gameplay = new XElement("Gameplay"); document.Root!.AddFirst(gameplay); }
        var links = gameplay.Element("Links") ?? new XElement("Links"); if (links.Parent == null) gameplay.Add(links);
        links.Add(new XElement("LogicLink", new XElement("SourceId", sourceId), new XElement("TargetId", targetId), new XElement("Kind", kind), new XElement("Parameter", parameter)));
        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static XDocument Load(string xml) { using var reader = DesktopImport.CreateReader(xml); return XDocument.Load(reader); }
    private static XElement FindActor(XDocument document, string id) => document.Descendants("RoomPackageActor").FirstOrDefault(x => Text(x, "Id") == id) ?? throw new InvalidDataException("Gameplay actor was not found: " + id);
    private static string Text(XElement? node, string name) => (string?)node?.Element(name) ?? "";
    private static int Int(XElement? node, string name, int fallback = 0) => int.TryParse(Text(node, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
    private static float Number(XElement? node, string name) => float.TryParse(Text(node, name), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && float.IsFinite(value) ? value : 0;
    private static bool Bool(XElement node, string name) => bool.TryParse(Text(node, name), out bool value) && value;
    private static void Set(XElement node, string name, object value) { var child = node.Element(name); if (child == null) { child = new XElement(name); node.AddFirst(child); } child.Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""; }
}
