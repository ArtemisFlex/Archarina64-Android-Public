using System.Globalization;
using System.Xml.Linq;

namespace Archarina64.Core;

/// <summary>Projects decoded room display-list geometry into the existing SectionTile format.</summary>
public static class RomTileExporter
{
    public sealed record ExtractedTile(string Name, int CellX, int CellZ, string Xml);

    public static string Export(RomDocument document, int sceneId, int roomId, float grid = 100)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        if (!float.IsFinite(grid) || grid < 1 || grid > 4096) throw new InvalidDataException("Tile grid must be between 1 and 4096.");
        var scene = document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        var geometry = scene.Rooms[roomId].Geometry ?? throw new InvalidDataException("Room geometry is not decoded.");
        return BuildTile(document, sceneId, roomId, geometry, Enumerable.Range(0, geometry.Triangles.Count).ToArray(), $"Scene {sceneId:X2} Room {roomId:D2}", 0, 0, grid);
    }

    public static IReadOnlyList<ExtractedTile> ExportTiles(RomDocument document, int sceneId, int roomId, float tileSize = 100)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        if (!float.IsFinite(tileSize) || tileSize < 1 || tileSize > 4096) throw new InvalidDataException("Tile size must be between 1 and 4096.");
        var scene = document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        var geometry = scene.Rooms[roomId].Geometry ?? throw new InvalidDataException("Room geometry is not decoded.");
        if (geometry.Vertices.Count == 0 || geometry.Triangles.Count == 0) throw new InvalidDataException("Room geometry has no visual triangles.");
        var groups = new Dictionary<(int X, int Z), List<int>>();
        for (int i = 0; i < geometry.Triangles.Count; i++)
        {
            var t = geometry.Triangles[i]; if (t.A < 0 || t.B < 0 || t.C < 0 || t.A >= geometry.Vertices.Count || t.B >= geometry.Vertices.Count || t.C >= geometry.Vertices.Count) throw new InvalidDataException("Room geometry contains an invalid triangle index.");
            var a = geometry.Vertices[t.A]; var b = geometry.Vertices[t.B]; var c = geometry.Vertices[t.C];
            int cellX = (int)MathF.Floor((a.X + b.X + c.X) / 3f / tileSize), cellZ = (int)MathF.Floor((a.Z + b.Z + c.Z) / 3f / tileSize);
            groups.TryAdd((cellX, cellZ), []); groups[(cellX, cellZ)].Add(i);
        }
        return groups.OrderBy(g => g.Key.Z).ThenBy(g => g.Key.X).Select(g => new ExtractedTile($"Scene {sceneId:X2} Room {roomId:D2} Tile {g.Key.X},{g.Key.Z}", g.Key.X, g.Key.Z, BuildTile(document, sceneId, roomId, geometry, g.Value, $"Scene {sceneId:X2} Room {roomId:D2} Tile {g.Key.X},{g.Key.Z}", g.Key.X, g.Key.Z, tileSize))).ToArray();
    }

    private static string BuildTile(RomDocument document, int sceneId, int roomId, RomRoomGeometry geometry, IReadOnlyList<int> triangleIndices, string name, int cellX, int cellZ, float grid)
    {
        if (geometry.Vertices.Count == 0 || geometry.Triangles.Count == 0) throw new InvalidDataException("Room geometry has no visual triangles.");
        if (geometry.Vertices.Count > DesktopImport.MaxTriangles || geometry.Triangles.Count > DesktopImport.MaxTriangles) throw new InvalidDataException("Room geometry exceeds the mobile tile budget.");

        var used = triangleIndices.SelectMany(i => new[] { geometry.Triangles[i].A, geometry.Triangles[i].B, geometry.Triangles[i].C }).Distinct().ToArray();
        var remap = used.Select((oldIndex, newIndex) => (oldIndex, newIndex)).ToDictionary(x => x.oldIndex, x => x.newIndex);
        var vertices = new XElement("Vertices");
        var uvs = new XElement("UVs");
        foreach (int oldIndex in used)
        {
            var vertex = geometry.Vertices[oldIndex];
            vertices.Add(new XElement("Vertex", Number("X", vertex.X), Number("Y", vertex.Y), Number("Z", vertex.Z)));
            uvs.Add(new XElement("TextureCoord", Number("U", vertex.S), Number("V", vertex.T)));
        }
        var triangles = new XElement("Triangles");
        var materials = new Dictionary<string, XElement>(StringComparer.Ordinal);
        foreach (int triangleIndex in triangleIndices)
        {
            var triangle = geometry.Triangles[triangleIndex];
            if (!remap.TryGetValue(triangle.A, out int a) || !remap.TryGetValue(triangle.B, out int b) || !remap.TryGetValue(triangle.C, out int c)) throw new InvalidDataException("Room geometry contains an invalid triangle index.");
            var va = geometry.Vertices[triangle.A]; var vb = geometry.Vertices[triangle.B]; var vc = geometry.Vertices[triangle.C];
            string materialName = $"VertexColor_{(va.R + vb.R + vc.R) / 3:X2}{(va.G + vb.G + vc.G) / 3:X2}{(va.B + vb.B + vc.B) / 3:X2}";
            materials.TryAdd(materialName, new XElement("Material", new XElement("Name", materialName), new XElement("Kd", Float((va.R + vb.R + vc.R) / 765f), Float((va.G + vb.G + vc.G) / 765f), Float((va.B + vb.B + vc.B) / 765f))));
            triangles.Add(new XElement("Triangle", new XElement("VertIndex", Int(a), Int(b), Int(c)), new XElement("TexCoordIndex", Int(a), Int(b), Int(c)), new XElement("MaterialName", materialName)));
        }
        var visual = new XElement("Visual", vertices, uvs, new XElement("Materials", materials.Values), new XElement("Groups", new XElement("Group", triangles)));
        return new XElement("SectionTile", new XElement("Version", 1), new XElement("Name", name), Number("Grid", grid), new XElement("RomFingerprint", document.Fingerprint), new XElement("SourceScene", sceneId), new XElement("SourceRoom", roomId), new XElement("TileCellX", cellX), new XElement("TileCellZ", cellZ), visual).ToString(SaveOptions.DisableFormatting);
    }

    private static XElement Number(string name, float value) => new(name, value.ToString(CultureInfo.InvariantCulture));
    private static XElement Int(int value) => new("int", value);
    private static XElement Float(float value) => new("float", value.ToString(CultureInfo.InvariantCulture));
}
