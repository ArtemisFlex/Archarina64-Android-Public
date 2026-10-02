#nullable enable
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO.Compression;
using SharpOcarina;

namespace Archarina64.Core;

public sealed record TilePreview(string Name, float Grid, Vector3[] Vertices, int[] Indices,
    Vector3[] FaceColors, string[] TextureReferences, Vector2[] UVs, int[] TexCoordIndices, string[] FaceTextures);

public sealed record TileBundle(string Xml, IReadOnlyDictionary<string, byte[]> Textures);

/// <summary>Reads desktop XML without taking a dependency on WinForms/OpenTK.
/// The full source XML is retained by LayoutTile; this projection is preview-only.</summary>
public static class DesktopImport
{
    public const int MaxXmlCharacters = 16 * 1024 * 1024;
    public const int MaxTriangles = 200_000;

    public static XmlReader CreateReader(string xml) => XmlReader.Create(new StringReader(xml), new XmlReaderSettings
    {
        DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
        MaxCharactersInDocument = MaxXmlCharacters
    });

    public static TilePreview ReadTile(string xml)
    {
        using var reader = CreateReader(xml);
        var root = XDocument.Load(reader).Root ?? throw new InvalidDataException("Empty tile document.");
        if (root.Name != "SectionTile") throw new InvalidDataException("Choose a desktop SectionTile XML file from the tile library.");
        if ((int?)root.Element("Version") != 1) throw new InvalidDataException("Unsupported desktop tile version.");
        var visual = root.Element("Visual") ?? throw new InvalidDataException("Tile has no visual mesh.");
        var vertices = visual.Element("Vertices")?.Elements("Vertex").Take(200_001)
            .Select(v => new Vector3(Number(v, "X"), Number(v, "Y"), Number(v, "Z"))).ToArray() ?? [];
        if (vertices.Length == 0 || vertices.Length > 200_000) throw new InvalidDataException("Tile needs 1–200,000 vertices for mobile preview.");
        var indices = new List<int>();
        var faceColors = new List<Vector3>();
        var texCoordIndices = new List<int>();
        var faceTextures = new List<string>();
        var materials = new Dictionary<string, (Vector3 Color, string? Texture)>(StringComparer.Ordinal);
        foreach (var material in visual.Element("Materials")?.Elements("Material") ?? [])
        {
            string name = (string?)material.Element("Name") ?? "";
            var kd = material.Element("Kd")?.Elements("float").Select(x => float.Parse(x.Value, NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray() ?? [];
            Vector3 color = kd.Length >= 3 && kd.All(float.IsFinite) ? new Vector3(Math.Clamp(kd[0], 0, 1), Math.Clamp(kd[1], 0, 1), Math.Clamp(kd[2], 0, 1)) : new Vector3(0.72f);
            string? texture = (string?)material.Element("map_Kd") ?? (string?)material.Element("map_Ka");
            materials[name] = (color, string.IsNullOrWhiteSpace(texture) ? null : Path.GetFileName(texture));
        }
        var textureReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uvs = visual.Element("UVs")?.Elements("TextureCoord").Select(coord => new Vector2(Number(coord, "U"), Number(coord, "V"))).ToArray() ?? [];
        foreach (var triangle in visual.Element("Groups")?.Elements("Group").SelectMany(g => g.Element("Triangles")?.Elements("Triangle") ?? []) ?? [])
        {
            var corners = triangle.Element("VertIndex")?.Elements("int").Select(x => (int)x).ToArray() ?? [];
            if (corners.Length != 3 || corners.Any(i => i < 0 || i >= vertices.Length)) throw new InvalidDataException("Tile contains an invalid triangle index.");
            if (indices.Count >= MaxTriangles * 3) throw new InvalidDataException("Tile exceeds the mobile triangle budget.");
            indices.AddRange(corners);
            var uvCorners = triangle.Element("TexCoordIndex")?.Elements("int").Select(x => (int)x).ToArray() ?? [-1, -1, -1];
            if (uvCorners.Length != 3 || uvCorners.Any(i => i < -1 || i >= uvs.Length)) throw new InvalidDataException("Tile contains an invalid texture-coordinate index.");
            texCoordIndices.AddRange(uvCorners);
            string materialName = (string?)triangle.Element("MaterialName") ?? "";
            var material = materials.TryGetValue(materialName, out var found) ? found : (new Vector3(0.72f), null);
            faceColors.Add(material.Color);
            faceTextures.Add(material.Texture ?? "");
            if (!string.IsNullOrWhiteSpace(material.Texture)) textureReferences.Add(material.Texture!);
        }
        if (indices.Count == 0) throw new InvalidDataException("Tile has no visual triangles.");
        float grid = Number(root, "Grid");
        if (grid < 1 || grid > 4096) throw new InvalidDataException("Tile grid must be between 1 and 4096.");
        return new TilePreview((string?)root.Element("Name") ?? "Untitled room", grid, vertices, indices.ToArray(), faceColors.ToArray(), textureReferences.Order(StringComparer.OrdinalIgnoreCase).ToArray(), uvs, texCoordIndices.ToArray(), faceTextures.ToArray());
    }

    public static TileBundle ReadBundle(Stream source)
    {
        using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: false);
        if (archive.Entries.Count > 129) throw new InvalidDataException("Tile bundle has too many files.");
        var tile = archive.GetEntry("tile.xml") ?? throw new InvalidDataException("Tile bundle must contain tile.xml.");
        string xml;
        using (var reader = new StreamReader(tile.Open(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false)) xml = reader.ReadToEnd();
        var preview = ReadTile(xml);
        var textures = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith("textures/", StringComparison.OrdinalIgnoreCase) || entry.FullName.EndsWith("/")) continue;
            string name = Path.GetFileName(entry.FullName);
            if (string.IsNullOrWhiteSpace(name) || name.Length > 128 || !name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Only PNG files directly under textures/ are supported.");
            if (entry.Length > 8 * 1024 * 1024 || (total += entry.Length) > 24 * 1024 * 1024) throw new InvalidDataException("Tile bundle textures exceed the 24 MB limit.");
            using var input = entry.Open(); using var output = new MemoryStream(); input.CopyTo(output);
            textures[name] = output.ToArray();
        }
        var missing = preview.TextureReferences.Where(path => !textures.ContainsKey(path)).ToArray();
        if (missing.Length > 0) throw new InvalidDataException("Tile bundle is missing: " + string.Join(", ", missing.Take(3)));
        return new TileBundle(xml, textures);
    }

    public static SceneTileLibrary ReadCatalog(string xml)
    {
        using var reader = CreateReader(xml);
        var catalog = (SceneTileLibrary?)new XmlSerializer(typeof(SceneTileLibrary)).Deserialize(reader)
            ?? throw new InvalidDataException("Empty room catalog.");
        if (catalog.Version != 1 || catalog.Rooms.Count > 20_000 || catalog.Rooms.Any(r => r == null || r.SceneId < 0 || r.RoomId < 0 || r.Start >= r.End))
            throw new InvalidDataException("Unsupported or invalid room catalog.");
        return catalog;
    }

    private static float Number(XElement element, string name)
    {
        if (!float.TryParse((string?)element.Element(name), NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            || !float.IsFinite(value) || MathF.Abs(value) > 10_000_000)
            throw new InvalidDataException($"Invalid {name} coordinate or value.");
        return value;
    }
}
