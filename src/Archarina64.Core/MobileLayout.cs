#nullable enable
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Archarina64.Core;

public sealed record LayoutTile
{
    public string SourceXml { get; init; } = "";
    // PNG bytes keyed by the basename used in desktop map_Kd/map_Ka fields.
    // Base64 keeps the JSON document self-contained for Android document exchange.
    public Dictionary<string, string> EmbeddedTextures { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public int QuarterTurns { get; init; }

    public Vector3 Transform(Vector3 point)
    {
        for (int n = 0; n < ((QuarterTurns % 4) + 4) % 4; n++) point = new Vector3(point.Z, point.Y, -point.X);
        return point + new Vector3(X, Y, Z);
    }
}

public sealed record MobileLayout
{
    public int Version { get; init; } = 1;
    public string Name { get; init; } = "My room layout";
    public LayoutTile[] Tiles { get; init; } = [];
}

public static class LayoutStorage
{
    public const int MaxDocumentBytes = 32 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, MaxDepth = 32 };

    public static MobileLayout Read(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaxDocumentBytes) throw new InvalidDataException("Layout exceeds 32 MB.");
        var layout = JsonSerializer.Deserialize<MobileLayout>(json, Options) ?? throw new InvalidDataException("Empty layout.");
        Validate(layout);
        return layout;
    }

    public static string Write(MobileLayout layout)
    {
        Validate(layout);
        string json = JsonSerializer.Serialize(layout, Options);
        if (Encoding.UTF8.GetByteCount(json) > MaxDocumentBytes) throw new InvalidDataException("Layout exceeds 32 MB.");
        return json;
    }

    public static void SaveAtomic(string path, MobileLayout layout)
    {
        string json = Write(layout);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, json);
        File.Move(temporary, path, true);
    }

    public static void Validate(MobileLayout layout)
    {
        if (layout.Version != 1 || layout.Tiles is null || layout.Tiles.Length > 128 || string.IsNullOrWhiteSpace(layout.Name))
            throw new InvalidDataException("Unsupported layout version, name or room count (maximum 128).");
        int triangles = 0;
        foreach (var tile in layout.Tiles)
        {
            if (tile is null || string.IsNullOrEmpty(tile.SourceXml) || !ValidPosition(tile.X) || !ValidPosition(tile.Y) || !ValidPosition(tile.Z) || tile.QuarterTurns is < 0 or > 3)
                throw new InvalidDataException("Invalid tile transform.");
            triangles += DesktopImport.ReadTile(tile.SourceXml).Indices.Length / 3;
            long textureBytes = 0;
            if (tile.EmbeddedTextures is null || tile.EmbeddedTextures.Count > 32) throw new InvalidDataException("A tile may contain at most 32 embedded textures.");
            foreach (var texture in tile.EmbeddedTextures)
            {
                if (string.IsNullOrWhiteSpace(texture.Key) || texture.Key.IndexOfAny(['/', '\\', ':']) >= 0 || !texture.Key.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Embedded texture names must be PNG basenames.");
                byte[] bytes;
                try { bytes = Convert.FromBase64String(texture.Value); } catch (FormatException) { throw new InvalidDataException("Embedded texture is not valid base64."); }
                if (bytes.Length > 8 * 1024 * 1024 || (textureBytes += bytes.Length) > 24 * 1024 * 1024) throw new InvalidDataException("Embedded textures exceed the 24 MB limit.");
            }
            if (triangles > DesktopImport.MaxTriangles) throw new InvalidDataException("Layout exceeds 200,000 preview triangles.");
        }
    }

    private static bool ValidPosition(float value) => float.IsFinite(value) && MathF.Abs(value) <= 10_000_000;

    public static async Task<string> ReadBoundedAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return Encoding.UTF8.GetString(await ReadBoundedBytesAsync(stream, cancellationToken));
    }

    public static async Task<byte[]> ReadBoundedBytesAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[81920];
        int count;
        while ((count = await stream.ReadAsync(chunk, cancellationToken)) != 0)
        {
            if (buffer.Length + count > MaxDocumentBytes) throw new InvalidDataException("Document exceeds 32 MB.");
            buffer.Write(chunk, 0, count);
        }
        return buffer.ToArray();
    }
}

public sealed class LayoutHistory
{
    public MobileLayout Current { get; private set; } = new();
    private readonly List<MobileLayout> undo = [];
    private readonly List<MobileLayout> redo = [];
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;

    public void Reset(MobileLayout baseline)
    {
        _ = LayoutStorage.Write(baseline);
        Current = baseline;
        undo.Clear(); redo.Clear();
    }

    public void Apply(MobileLayout next)
    {
        // Reject an unsavable document before adding it to the edit history.
        _ = LayoutStorage.Write(next);
        undo.Add(Current);
        if (undo.Count > 30) undo.RemoveAt(0);
        Current = next;
        redo.Clear();
    }

    public void Undo() { if (CanUndo) { redo.Add(Current); Current = undo[^1]; undo.RemoveAt(undo.Count - 1); } }
    public void Redo() { if (CanRedo) { undo.Add(Current); Current = redo[^1]; redo.RemoveAt(redo.Count - 1); } }
}
