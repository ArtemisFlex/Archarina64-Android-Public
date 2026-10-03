using System.Security.Cryptography;

namespace Archarina64.Core;

public sealed record RomProfile(string Id, string Name, uint SceneTable, uint SceneTableEnd, uint EntranceTable = 0, uint EntranceTableEnd = 0)
{
    public int SceneCount => checked((int)((SceneTableEnd - SceneTable) / 20));
}

public sealed record RomActor(ushort Number, short X, short Y, short Z, short RotationX, short RotationY, short RotationZ, ushort Variable);
public sealed record RomCollisionVertex(short X, short Y, short Z);
public sealed record RomCollisionTriangle(ushort SurfaceType, ushort A, ushort B, ushort C, short NormalX, short NormalY, short NormalZ, short Distance);
public sealed record RomCamera(byte Type, short X, short Y, short Z, short RotationX, short RotationY, short RotationZ, short Fov, ushort Unknown1, ushort Unknown2, short DataCount = 0, IReadOnlyList<RomCameraPathPoint> PathPoints = null);
public sealed record RomCameraPathPoint(short X, short Y, short Z);
public sealed record RomWaterbox(short X, short Y, short Z, short XSize, short ZSize, ushort Unknown, uint Properties);
public sealed record RomCollisionData(short MinX, short MinY, short MinZ, short MaxX, short MaxY, short MaxZ, IReadOnlyList<RomCollisionVertex> Vertices, IReadOnlyList<RomCollisionTriangle> Triangles, IReadOnlyList<ulong> SurfaceTypes, IReadOnlyList<RomWaterbox> Waterboxes = null, IReadOnlyList<RomCamera> Cameras = null);
public sealed record RomGeometryVertex(short X, short Y, short Z, short S, short T, byte R, byte G, byte B, byte A);
public sealed record RomGeometryTriangle(int A, int B, int C);
public sealed record RomDisplayListCommand(int SourceOffset, byte Operation, uint Word0, uint Word1);
public sealed record RomRoomGeometry(IReadOnlyList<RomGeometryVertex> Vertices, IReadOnlyList<RomGeometryTriangle> Triangles, int DisplayLists, IReadOnlyList<int> VertexOffsets = null, IReadOnlyList<int> VertexSlots = null, IReadOnlyList<int> TriangleOffsets = null, IReadOnlyList<uint> TrianglePrefixes = null, int MeshOffset = -1, int MeshType = -1, int MeshCount = 0, int MeshTableA = -1, int MeshTableB = -1, IReadOnlyList<int> DisplayListOffsets = null, IReadOnlyList<RomDisplayListCommand> DisplayListCommands = null);
public sealed record RomRoomSettings(uint Behavior, uint Wind, ushort StartTime, byte TimeSpeed, byte SkyboxFlags, byte Echo);
public sealed record RomRoom(int Id, uint Start, uint End, IReadOnlyList<RomActor> Actors, int ObjectCount, bool HasMesh, bool HasCollision, IReadOnlyList<string> Diagnostics, RomRoomGeometry Geometry = null, IReadOnlyList<ushort> Objects = null, RomRoomSettings Settings = null, IReadOnlyList<ushort> Exits = null);
public sealed record RomSpawnPoint(ushort Number, short X, short Y, short Z, short RotationX, short RotationY, short RotationZ, ushort Variable);
public sealed record RomTransition(byte FrontRoom, byte FrontCamera, byte BackRoom, byte BackCamera, ushort Number, short X, short Y, short Z, short RotationY, ushort Variable);
public sealed record RomEntrance(int Index, byte SceneId, byte SpawnId);
public sealed record RomPathPoint(short X, short Y, short Z);
public sealed record RomPath(int Id, IReadOnlyList<RomPathPoint> Points);
public sealed record RomExit(int Index, ushort Raw) { public int RoomId => (Raw >> 9) & 0x3F; public int SpawnId => Raw & 0x1FF; }
public sealed record RomEnvironment(uint Ambient, uint Diffuse0, uint Direction0, uint Diffuse1, uint Direction1, uint FogColor, ushort FogDistance, ushort FogUnknown, ushort DrawDistance);
public sealed record RomHeaderCommand(byte Command, byte Parameter, uint Pointer);
public sealed record RomAlternateHeader(int Index, uint Offset, bool IsClone, IReadOnlyList<RomHeaderCommand> Commands = null, bool HasRooms = false, bool HasCollision = false, bool HasSettings = false, RomSceneSettings Settings = null, IReadOnlyList<RomRoom> Rooms = null, RomCollisionData Collision = null);
public sealed record RomSceneSettings(byte CameraMovement, byte WorldMap);
public sealed record RomScene(int Id, string Name, uint Start, uint End, IReadOnlyList<RomRoom> Rooms, bool HasCollision, IReadOnlyList<string> Diagnostics, RomCollisionData Collision = null, IReadOnlyList<RomSpawnPoint> SpawnPoints = null, IReadOnlyList<RomTransition> Transitions = null, IReadOnlyList<RomPath> Paths = null, IReadOnlyList<RomExit> Exits = null, IReadOnlyList<RomEnvironment> Environments = null, IReadOnlyList<RomAlternateHeader> AlternateHeaders = null, RomSceneSettings Settings = null, IReadOnlyList<RomHeaderCommand> Commands = null);
public sealed record RomDocument(string Fingerprint, RomProfile Profile, IReadOnlyList<RomScene> Scenes, IReadOnlyList<string> Diagnostics, IReadOnlyList<RomEntrance> Entrances = null);

/// <summary>Bounded native reader for decompressed big-endian OoT ROMs. It deliberately keeps the ROM bytes out of layouts and exports.</summary>
public static class RomDecoder
{
    public const int MaxRomBytes = 64 * 1024 * 1024;
    private static readonly RomProfile[] Profiles =
    [
        // Expanded, decompressed OoT hack builds can relocate the 101-row scene table by 0x10 bytes.
        new("oot-expanded-hack", "OoT expanded hack", 0x00B71450, 0x00B71C34),
        new("oot-mq-debug", "OoT MQ debug", 0x00BA0BB0, 0x00BA1488, 0x00B9F360, 0x00BA0BB0),
        new("oot-1.0-ntsc", "OoT 1.0 NTSC", 0x00B71440, 0x00B71C28, 0x00B6FBF0, 0x00B71440),
        new("oot-1.1-ntsc", "OoT 1.1 NTSC", 0x00B73C40, 0x00B74428),
        new("oot-1.2-ntsc", "OoT 1.2 NTSC", 0x00B7A2D0, 0x00B7AAB8)
    ];

    public static async Task<byte[]> ReadBoundedBytesAsync(Stream source, CancellationToken cancellationToken = default)
    {
        using var output = new MemoryStream();
        byte[] buffer = new byte[64 * 1024]; int read, total = 0;
        while ((read = await source.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
        {
            total = checked(total + read);
            if (total > MaxRomBytes) throw new InvalidDataException("ROM exceeds the supported 64 MB limit.");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }

    public static RomDocument Read(byte[] rom)
    {
        if (rom is null || rom.Length < 0x1000 || rom.Length > MaxRomBytes) throw new InvalidDataException("ROM size is outside the supported 64 MB limit.");
        if (U32(rom, 0) != 0x80371240) throw new InvalidDataException("Choose an uncompressed big-endian OoT .z64 ROM.");
        using var sha = SHA256.Create();
        string fingerprint = Convert.ToHexString(sha.ComputeHash(rom)).ToLowerInvariant();
        var profile = Profiles.FirstOrDefault(p => p.SceneTableEnd <= rom.Length && p.SceneTable + 20 <= rom.Length && LooksLikeTable(rom, p))
            ?? throw new InvalidDataException("The ROM header is valid, but no supported OoT scene-table profile was found.");
        var diagnostics = new List<string>();
        var scenes = new List<RomScene>();
        for (int id = 0; id < profile.SceneCount; id++)
        {
            int row = checked((int)profile.SceneTable + id * 20);
            uint start = U32(rom, row), end = U32(rom, row + 4);
            if (start == 0 && end == 0) continue;
            if (start >= end || end > rom.Length) { diagnostics.Add($"Scene {id:X2}: invalid ROM range."); continue; }
            var sceneDiagnostics = new List<string>();
            byte[] sceneBlob = DecodeRange(rom, start, end);
            var alternateHeaders = ReadAlternateHeaders(sceneBlob, rom, sceneDiagnostics);
            var sceneSettings = ReadSceneSettings(sceneBlob);
            var roomRanges = FindRoomRanges(sceneBlob, sceneDiagnostics, out bool sceneCollision, out int collisionOffset);
            var spawnPoints = new List<RomSpawnPoint>(); var transitions = new List<RomTransition>(); var paths = new List<RomPath>(); var exits = new List<RomExit>(); var environments = new List<RomEnvironment>();
            ReadSceneActors(sceneBlob, spawnPoints, transitions, paths, exits, environments, sceneDiagnostics);
            var rooms = new List<RomRoom>();
            foreach (var (roomStart, roomEnd) in roomRanges)
                rooms.Add(ReadRoom(rom, rooms.Count, roomStart, roomEnd, sceneDiagnostics));
            RomCollisionData collision = collisionOffset >= 0 ? ReadCollision(sceneBlob, collisionOffset, sceneDiagnostics) : null;
            var sceneCommands = ReadHeaderCommands(sceneBlob);
            scenes.Add(new RomScene(id, SceneName(id), start, end, rooms, sceneCollision, sceneDiagnostics, collision, spawnPoints, transitions, paths, exits, environments, alternateHeaders, sceneSettings, sceneCommands));
        }
        var entrances = ReadEntrances(rom, profile, diagnostics);
        return new RomDocument(fingerprint, profile, scenes, diagnostics, entrances);
    }

    private static bool LooksLikeTable(byte[] rom, RomProfile profile)
    {
        int valid = 0, sceneHeaders = 0;
        for (int i = 0; i < Math.Min(profile.SceneCount, 32); i++)
        {
            uint start = U32(rom, checked((int)profile.SceneTable + i * 20));
            uint end = U32(rom, checked((int)profile.SceneTable + i * 20 + 4));
            if (start == 0 && end == 0) continue;
            if (start < 0x1000 || start >= end || end > rom.Length || end - start < 32 || end - start > 4 * 1024 * 1024) continue;
            valid++;
            if (LooksLikeSceneHeader(rom, (int)start, (int)end)) sceneHeaders++;
        }
        return valid >= 2 && sceneHeaders >= 1;
    }

    private static bool LooksLikeSceneHeader(byte[] rom, int start, int end)
    {
        if (rom.AsSpan(start, Math.Min(end - start, 4)).SequenceEqual("Yaz0"u8) || rom.AsSpan(start, Math.Min(end - start, 4)).SequenceEqual("MIO0"u8)) return true;
        int commands = 0;
        for (int p = start; p + 8 <= Math.Min(end, start + 160); p += 8)
        {
            byte command = rom[p];
            if (command == 0x14) return commands >= 1;
            if (command > 0x1A) return false;
            commands++;
        }
        return false;
    }

    private static IReadOnlyList<RomHeaderCommand> ReadHeaderCommands(byte[] scene) { var commands = new List<RomHeaderCommand>(); for (int p = 0; p + 8 <= scene.Length && p < 8192; p += 8) { if (scene[p] == 0x14) break; commands.Add(new RomHeaderCommand(scene[p], scene[p + 1], U32(scene, p + 4))); } return commands; }

    private static IReadOnlyList<RomAlternateHeader> ReadAlternateHeaders(byte[] scene, byte[] rom, List<string> diagnostics)
    {
        var headers = new List<RomAlternateHeader>();
        int command = FindSceneCommand(scene, 0x18); if (command < 0) return headers;
        int table = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); if (table < 0 || table >= scene.Length) { diagnostics.Add("Alternate scene header table is outside the decoded scene."); return headers; }
        for (int i = 0; i < 32 && table + i * 4 + 4 <= scene.Length; i++) { uint pointer = U32(scene, table + i * 4); if (pointer == 0) break; int offset = checked((int)(pointer & 0x00FFFFFF)); if (offset < 0 || offset >= scene.Length) { diagnostics.Add($"Alternate scene header {i} is outside the decoded scene."); continue; } var commands = new List<RomHeaderCommand>(); bool rooms = false, collision = false, settings = false; RomSceneSettings headerSettings = null; IReadOnlyList<RomRoom> headerRooms = null; RomCollisionData headerCollision = null; for (int p = offset; p + 8 <= scene.Length && p < offset + 8192; p += 8) { byte headerCommand = scene[p]; if (headerCommand == 0x14) break; commands.Add(new RomHeaderCommand(headerCommand, scene[p + 1], U32(scene, p + 4))); rooms |= headerCommand == 0x04; if (headerCommand == 0x02) { collision = true; int collisionOffset = checked((int)(U32(scene, p + 4) & 0x00FFFFFF)); headerCollision = ReadCollision(scene, collisionOffset, diagnostics); } if (headerCommand == 0x19) { settings = true; headerSettings = new RomSceneSettings(scene[p + 1], scene[p + 7]); } if (headerCommand == 0x04 && (U32(scene, p + 4) >> 24) == 2) { int roomTable = checked((int)(U32(scene, p + 4) & 0x00FFFFFF)); var decodedRooms = new List<RomRoom>(); for (int roomId = 0; roomId < scene[p + 1] && roomTable + roomId * 8 + 8 <= scene.Length; roomId++) { uint roomStart = U32(scene, roomTable + roomId * 8), roomEnd = U32(scene, roomTable + roomId * 8 + 4); if (roomStart < roomEnd) decodedRooms.Add(ReadRoom(rom, roomId, roomStart, roomEnd, diagnostics)); } headerRooms = decodedRooms; } } headers.Add(new RomAlternateHeader(i + 1, pointer, scene[offset] != 0x02, commands, rooms, collision, settings, headerSettings, headerRooms, headerCollision)); }
        return headers;
    }

    private static RomSceneSettings ReadSceneSettings(byte[] scene)
    {
        int command = FindSceneCommand(scene, 0x19); return command < 0 ? new RomSceneSettings(0, 0) : new RomSceneSettings(scene[command + 1], scene[command + 7]);
    }

    private static int FindSceneCommand(byte[] scene, byte command)
    {
        for (int p = 0; p + 8 <= scene.Length && p < 8192; p += 8) { if (scene[p] == 0x14) break; if (scene[p] == command) return p; }
        return -1;
    }

    private static List<(uint Start, uint End)> FindRoomRanges(byte[] scene, List<string> diagnostics, out bool collision, out int collisionOffset)
    {
        collision = false; collisionOffset = -1;
        var rooms = new List<(uint, uint)>();
        for (int p = 0; p + 8 <= scene.Length && p < 8192; p += 8)
        {
            byte command = scene[p]; if (command == 0x14) break;
            if (command == 0x02) { collision = true; collisionOffset = checked((int)(U32(scene, p + 4) & 0x00FFFFFF)); }
            if (command != 0x04) continue;
            int count = scene[p + 1]; uint pointer = U32(scene, p + 4);
            int table = checked((int)(pointer & 0x00FFFFFF));
            if ((pointer >> 24) != 2 || table < 0 || table + count * 8 > scene.Length) { diagnostics.Add("Invalid scene room table."); break; }
            for (int i = 0; i < count; i++)
            {
                uint start = U32(scene, table + i * 8), end = U32(scene, table + i * 8 + 4);
                if (start < end) rooms.Add((start, end)); else diagnostics.Add($"Invalid room range {i}.");
            }
            break;
        }
        return rooms;
    }

    private static void ReadSceneActors(byte[] scene, List<RomSpawnPoint> spawns, List<RomTransition> transitions, List<RomPath> paths, List<RomExit> exits, List<RomEnvironment> environments, List<string> diagnostics)
    {
        for (int p = 0; p + 8 <= scene.Length && p < 8192; p += 8)
        {
            byte command = scene[p]; if (command == 0x14) break;
            int count = scene[p + 1]; uint pointer = U32(scene, p + 4); int data = checked((int)(pointer & 0x00FFFFFF));
            if ((pointer >> 24) != 2 || count == 0) continue;
            if (command == 0x00) // scene spawn list
            {
                if (data < 0 || data + count * 16 > scene.Length) { diagnostics.Add("Spawn list is outside the decoded scene."); continue; }
                for (int i = 0; i < count; i++) { int o = data + i * 16; spawns.Add(new RomSpawnPoint(U16(scene, o), S16(scene, o + 2), S16(scene, o + 4), S16(scene, o + 6), S16(scene, o + 8), S16(scene, o + 10), S16(scene, o + 12), U16(scene, o + 14))); }
            }
            else if (command == 0x0E) // scene transition actor list
            {
                if (data < 0 || data + count * 16 > scene.Length) { diagnostics.Add("Transition list is outside the decoded scene."); continue; }
                for (int i = 0; i < count; i++) { int o = data + i * 16; transitions.Add(new RomTransition(scene[o], scene[o + 1], scene[o + 2], scene[o + 3], U16(scene, o + 4), S16(scene, o + 6), S16(scene, o + 8), S16(scene, o + 10), S16(scene, o + 12), U16(scene, o + 14))); }
            }
            else if (command == 0x0D) // scene path list
            {
                if (data < 0 || data + count * 8 > scene.Length) { diagnostics.Add("Path list is outside the decoded scene."); continue; }
                for (int i = 0; i < count; i++) { int o = data + i * 8; int pointCount = scene[o]; int points = checked((int)(U32(scene, o + 4) & 0x00FFFFFF)); if (points < 0 || points + pointCount * 6 > scene.Length) { diagnostics.Add($"Path {i} points are outside the decoded scene."); continue; } var list = new List<RomPathPoint>(); for (int pointIndex = 0; pointIndex < pointCount; pointIndex++) { int q = points + pointIndex * 6; list.Add(new RomPathPoint(S16(scene, q), S16(scene, q + 2), S16(scene, q + 4))); } paths.Add(new RomPath(i, list)); }
            }
            else if (command == 0x13) // scene exit list
            {
                if (data < 0 || data + count * 2 > scene.Length) { diagnostics.Add("Exit list is outside the decoded scene."); continue; }
                for (int i = 0; i < count; i++) exits.Add(new RomExit(i, U16(scene, data + i * 2)));
            }
            else if (command == 0x0F) // scene environment settings
            {
                const int stride = 22;
                if (data < 0 || data + count * stride > scene.Length) { diagnostics.Add("Environment list is outside the decoded scene."); continue; }
                for (int i = 0; i < count; i++)
                {
                    int o = data + i * stride; environments.Add(new RomEnvironment(Rgb24(scene, o), Rgb24(scene, o + 3), Rgb24(scene, o + 6), Rgb24(scene, o + 9), Rgb24(scene, o + 12), Rgb24(scene, o + 15), (ushort)(U16(scene, o + 18) & 0x03FF), (ushort)(U16(scene, o + 18) >> 10), U16(scene, o + 20)));
                }
            }
        }
    }

    private static RomCollisionData ReadCollision(byte[] scene, int offset, List<string> diagnostics)
    {
        if (offset < 0 || offset + 44 > scene.Length) { diagnostics.Add("Collision header is outside the decoded scene."); return null; }
        short minX = S16(scene, offset), minY = S16(scene, offset + 2), minZ = S16(scene, offset + 4), maxX = S16(scene, offset + 6), maxY = S16(scene, offset + 8), maxZ = S16(scene, offset + 10);
        int vertexCount = U16(scene, offset + 12), vertexOffset = checked((int)(U32(scene, offset + 16) & 0x00FFFFFF));
        int triangleCount = U16(scene, offset + 20), triangleOffset = checked((int)(U32(scene, offset + 24) & 0x00FFFFFF));
        int surfaceOffset = checked((int)(U32(scene, offset + 28) & 0x00FFFFFF));
        if (vertexCount > 100_000 || triangleCount > 200_000 || vertexOffset < 0 || triangleOffset < 0 || vertexOffset + vertexCount * 6 > scene.Length || triangleOffset + triangleCount * 16 > scene.Length)
        { diagnostics.Add("Collision arrays exceed the decoded scene bounds."); return null; }
        var vertices = new List<RomCollisionVertex>(vertexCount);
        for (int i = 0; i < vertexCount; i++) { int p = vertexOffset + i * 6; vertices.Add(new RomCollisionVertex(S16(scene, p), S16(scene, p + 2), S16(scene, p + 4))); }
        var triangles = new List<RomCollisionTriangle>(triangleCount);
        for (int i = 0; i < triangleCount; i++) { int p = triangleOffset + i * 16; triangles.Add(new RomCollisionTriangle(U16(scene, p), U16(scene, p + 2), U16(scene, p + 4), U16(scene, p + 6), S16(scene, p + 8), S16(scene, p + 10), S16(scene, p + 12), S16(scene, p + 14))); }
        var surfaces = new List<ulong>();
        if (surfaceOffset >= 0 && surfaceOffset + triangleCount * 8 <= scene.Length)
            for (int i = 0; i < triangleCount; i++) surfaces.Add(U64(scene, surfaceOffset + i * 8));
        int waterCount = U16(scene, offset + 36), waterOffset = checked((int)(U32(scene, offset + 40) & 0x00FFFFFF));
        var waterboxes = new List<RomWaterbox>(); var cameras = new List<RomCamera>();
        if (waterCount > 0 && waterOffset >= 0 && waterOffset + waterCount * 16 <= scene.Length)
            for (int i = 0; i < waterCount; i++) { int o = waterOffset + i * 16; waterboxes.Add(new RomWaterbox(S16(scene, o), S16(scene, o + 2), S16(scene, o + 4), S16(scene, o + 6), S16(scene, o + 8), U16(scene, o + 10), U32(scene, o + 12))); }
        else if (waterCount > 0) diagnostics.Add("Waterbox array is outside the decoded scene.");
        int cameraOffset = checked((int)(U32(scene, offset + 32) & 0x00FFFFFF)); int cameraCount = surfaces.Count == 0 ? 0 : Math.Min(64, (int)(surfaces.Max(s => (byte)s) + 1));
        if (cameraOffset > 0 && cameraOffset + cameraCount * 8 <= scene.Length) for (int i = 0; i < cameraCount; i++) { int o = cameraOffset + i * 8; ushort type = U16(scene, o); short count = S16(scene, o + 2); int data = checked((int)(U32(scene, o + 4) & 0x00FFFFFF)); if (data < 0 || data >= scene.Length) { diagnostics.Add("Camera record is outside the decoded scene."); continue; } if (type == 0x1E && count > 0) { if (data + count * 6 > scene.Length) { diagnostics.Add("Camera path data is outside the decoded scene."); continue; } var pathPoints = new List<RomCameraPathPoint>(count); for (int point = 0; point < count; point++) { int p = data + point * 6; pathPoints.Add(new RomCameraPathPoint(S16(scene, p), S16(scene, p + 2), S16(scene, p + 4))); } cameras.Add(new RomCamera((byte)type, 0, 0, 0, 0, 0, 0, 0, 0, 0, count, pathPoints)); continue; } if (data + 0x12 > scene.Length) { diagnostics.Add("Camera record is outside the decoded scene."); continue; } cameras.Add(new RomCamera((byte)type, S16(scene, data), S16(scene, data + 2), S16(scene, data + 4), S16(scene, data + 6), S16(scene, data + 8), S16(scene, data + 10), S16(scene, data + 12), U16(scene, data + 14), U16(scene, data + 16), count)); }
        return new RomCollisionData(minX, minY, minZ, maxX, maxY, maxZ, vertices, triangles, surfaces, waterboxes, cameras);
    }

    private static RomRoom ReadRoom(byte[] rom, int id, uint start, uint end, List<string> parentDiagnostics)
    {
        if (start >= end || end > rom.Length) return new RomRoom(id, start, end, [], 0, false, false, ["Room range is outside the ROM."]);
        byte[] room = DecodeRange(rom, start, end);
        var actors = new List<RomActor>(); var objectIds = new List<ushort>(); var roomExits = new List<ushort>(); var diagnostics = new List<string>(); int objects = 0; bool mesh = false, collision = false; int meshOffset = -1; uint behavior = 0, wind = 0; ushort startTime = 0; byte timeSpeed = 0, skyboxFlags = 0, echo = 0;
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8)
        {
            byte command = room[p]; if (command == 0x14) break;
            if (command == 0x0A) { mesh = true; meshOffset = checked((int)(U32(room, p + 4) & 0x00FFFFFF)); }
            if (command == 0x05) wind = U32(room, p); else if (command == 0x08) behavior = U32(room, p); else if (command == 0x10) { startTime = U16(room, p + 4); timeSpeed = room[p + 6]; } else if (command == 0x12) skyboxFlags = (byte)((room[p + 4] & 1) | ((room[p + 5] & 1) << 1)); else if (command == 0x16) echo = room[p + 7];
            if (command == 0x08) { int exitCount = room[p + 1]; uint exitPointer = U32(room, p + 4); int exitData = checked((int)(exitPointer & 0x00FFFFFF)); if ((exitPointer >> 24) == 3 && exitData >= 0 && exitData + exitCount * 2 <= room.Length) for (int i = 0; i < exitCount; i++) roomExits.Add(U16(room, exitData + i * 2)); else diagnostics.Add("Invalid room exit list data."); }
            if (command == 0x02) collision = true;
            if (command != 0x01 && command != 0x0B) continue;
            int count = room[p + 1]; uint pointer = U32(room, p + 4); int data = checked((int)(pointer & 0x00FFFFFF));
            if ((pointer >> 24) != 3 || data < 0 || data + count * (command == 0x01 ? 16 : 2) > room.Length) { diagnostics.Add($"Invalid room command 0x{command:X2} data."); continue; }
            if (command == 0x0B) { for (int i = 0; i < count; i++) objectIds.Add(U16(room, data + i * 2)); objects += count; continue; }
            for (int i = 0; i < count; i++) { int o = data + i * 16; actors.Add(new RomActor(U16(room, o), S16(room, o + 2), S16(room, o + 4), S16(room, o + 6), S16(room, o + 8), S16(room, o + 10), S16(room, o + 12), U16(room, o + 14))); }
        }
        RomRoomGeometry geometry = meshOffset >= 0 ? ReadGeometry(room, meshOffset, diagnostics) : null;
        return new RomRoom(id, start, end, actors, objects, mesh, collision, diagnostics, geometry, objectIds, new RomRoomSettings(behavior, wind, startTime, timeSpeed, skyboxFlags, echo), roomExits);
    }

    private static RomRoomGeometry ReadGeometry(byte[] room, int meshOffset, List<string> diagnostics)
    {
        if (meshOffset < 0 || meshOffset + 12 > room.Length) { diagnostics.Add("Mesh header is outside the decoded room."); return null; }
        int type = room[meshOffset], count = room[meshOffset + 1];
        var lists = new HashSet<int>();
        int tableA = checked((int)(U32(room, meshOffset + 4) & 0x00FFFFFF)), tableB = checked((int)(U32(room, meshOffset + 8) & 0x00FFFFFF));
        int stride = type == 2 ? 16 : 8;
        if (tableA > 0 && tableA + count * stride <= room.Length) for (int i = 0; i < count; i++) AddMeshEntry(room, tableA + i * stride, type == 2, lists);
        if (tableB > 0 && tableB + count * stride <= room.Length) for (int i = 0; i < count; i++) AddMeshEntry(room, tableB + i * stride, type == 2, lists);
        var vertices = new List<RomGeometryVertex>(); var vertexOffsets = new List<int>(); var vertexSlots = new List<int>(); var triangles = new List<RomGeometryTriangle>(); var triangleOffsets = new List<int>(); var trianglePrefixes = new List<uint>(); var commands = new List<RomDisplayListCommand>(); var slots = new Dictionary<int, int>(); var visited = new HashSet<int>();
        foreach (int list in lists) ReadDisplayList(room, list, lists, visited, slots, vertices, vertexOffsets, vertexSlots, triangles, triangleOffsets, trianglePrefixes, commands, diagnostics, 0);
        return new RomRoomGeometry(vertices, triangles, lists.Count, vertexOffsets, vertexSlots, triangleOffsets, trianglePrefixes, meshOffset, type, count, tableA, tableB, lists.OrderBy(offset => offset).ToArray(), commands);
    }

    private static void AddMeshEntry(byte[] room, int offset, bool type2, HashSet<int> lists)
    {
        int first = checked((int)(U32(room, offset + (type2 ? 8 : 0)) & 0x00FFFFFF));
        int second = checked((int)(U32(room, offset + (type2 ? 12 : 4)) & 0x00FFFFFF));
        if (first > 0 && first < room.Length) lists.Add(first); if (second > 0 && second < room.Length) lists.Add(second);
    }

    private static void ReadDisplayList(byte[] room, int offset, HashSet<int> lists, HashSet<int> visited, Dictionary<int, int> slots, List<RomGeometryVertex> vertices, List<int> vertexOffsets, List<int> vertexSlots, List<RomGeometryTriangle> triangles, List<int> triangleOffsets, List<uint> trianglePrefixes, List<RomDisplayListCommand> commands, List<string> diagnostics, int depth)
    {
        if (depth > 16 || offset < 0 || offset + 8 > room.Length || !visited.Add(offset)) return;
        // A mesh table can contain stale/segment-local pointers. Do not walk
        // arbitrary room bytes as a display list unless a bounded scan finds
        // an actual end command. This keeps malformed lists from poisoning
        // the shared vertex-slot map and creating giant triangles.
        bool terminated = false;
        for (int probe = offset; probe + 8 <= room.Length && probe < offset + 0x4000; probe += 8)
        {
            byte probeOp = room[probe];
            if (probeOp == 0xB8 || probeOp == 0xDF) { terminated = true; break; }
        }
        if (!terminated) return;
        for (int p = offset; p + 8 <= room.Length && p < offset + 0x20000; p += 8)
        {
            uint w0 = U32(room, p), w1 = U32(room, p + 4); byte op = (byte)(w0 >> 24);
            commands.Add(new RomDisplayListCommand(p, op, w0, w1));
            // OoT retail rooms use the F3DEX2 G_ENDDL opcode (0xDF). Keep
            // accepting 0xB8 for the older synthetic/legacy room format, but
            // never walk past a real display-list terminator into room data.
            if (op == 0xB8 || op == 0xDF) break;
            if (op == 0x01)
            {
                int number = (int)((w0 >> 12) & 0xFF), first = (int)((w0 >> 1) & 0x7F), source = checked((int)(w1 & 0x00FFFFFF));
                if (number > 32 || source < 0 || source + number * 16 > room.Length) { diagnostics.Add("F3DEX vertex command exceeds room bounds."); continue; }
                for (int i = 0; i < number; i++) { int q = source + i * 16; vertices.Add(new RomGeometryVertex(S16(room, q), S16(room, q + 2), S16(room, q + 4), S16(room, q + 8), S16(room, q + 10), room[q + 12], room[q + 13], room[q + 14], room[q + 15])); vertexOffsets.Add(q); vertexSlots.Add(first + i); slots[first + i] = vertices.Count - 1; }
            }
            // F3DEX2 uses 0x05/0x06 for TRI1/TRI2 and 0xDE for display-list
            // calls. The legacy OoT microcode form uses 0xBF/0xB1. Mixing
            // these up makes triangle payloads look like child pointers and
            // produces the long, invalid spikes seen in the Android viewport.
            else if (op == 0x05) AddF3Dex2Triangle(w0, p, slots, triangles, triangleOffsets, trianglePrefixes);
            else if (op == 0x06) { AddF3Dex2Triangle(w0, p, slots, triangles, triangleOffsets, trianglePrefixes); AddF3Dex2Triangle(w1, p + 4, slots, triangles, triangleOffsets, trianglePrefixes); }
            else if (op == 0xBF) AddTriangle(w1, p + 4, 0, slots, triangles, triangleOffsets, trianglePrefixes);
            else if (op == 0xB1) { AddTriangle(w0, p, 0xB1000000, slots, triangles, triangleOffsets, trianglePrefixes); AddTriangle(w1, p + 4, 0, slots, triangles, triangleOffsets, trianglePrefixes); }
            else if (op == 0xDE) { int child = checked((int)(w1 & 0x00FFFFFF)); ReadDisplayList(room, child, lists, visited, slots, vertices, vertexOffsets, vertexSlots, triangles, triangleOffsets, trianglePrefixes, commands, diagnostics, depth + 1); }
        }
    }

    private static void AddTriangle(uint packed, int sourceOffset, uint prefix, Dictionary<int, int> slots, List<RomGeometryTriangle> triangles, List<int> triangleOffsets, List<uint> trianglePrefixes)
    {
        int a = (int)((packed >> 17) & 0x7F) / 2, b = (int)((packed >> 9) & 0x7F) / 2, c = (int)((packed >> 1) & 0x7F) / 2;
        if (slots.TryGetValue(a, out int ai) && slots.TryGetValue(b, out int bi) && slots.TryGetValue(c, out int ci)) { triangles.Add(new RomGeometryTriangle(ai, bi, ci)); triangleOffsets.Add(sourceOffset); trianglePrefixes.Add(prefix); }
    }

    private static void AddF3Dex2Triangle(uint packed, int sourceOffset, Dictionary<int, int> slots, List<RomGeometryTriangle> triangles, List<int> triangleOffsets, List<uint> trianglePrefixes)
    {
        int a = (int)((packed >> 16) & 0xFF) / 2, b = (int)((packed >> 8) & 0xFF) / 2, c = (int)(packed & 0xFF) / 2;
        if (slots.TryGetValue(a, out int ai) && slots.TryGetValue(b, out int bi) && slots.TryGetValue(c, out int ci)) { triangles.Add(new RomGeometryTriangle(ai, bi, ci)); triangleOffsets.Add(sourceOffset); trianglePrefixes.Add(0); }
    }

    private static byte[] DecodeRange(byte[] rom, uint start, uint end)
    {
        if (start >= end || end > rom.Length) throw new InvalidDataException("ROM file range is outside the selected ROM.");
        return N64Compression.Decode(rom.AsSpan(checked((int)start), checked((int)(end - start))));
    }

    private static string SceneName(int id) => id < 0x80 ? $"Scene {id:X2}" : $"Scene {id}";
    private static IReadOnlyList<RomEntrance> ReadEntrances(byte[] rom, RomProfile profile, List<string> diagnostics)
    {
        var entries = new List<RomEntrance>(); if (profile.EntranceTableEnd <= profile.EntranceTable || profile.EntranceTableEnd > rom.Length) return entries;
        for (uint p = profile.EntranceTable; p + 4 <= profile.EntranceTableEnd; p += 4) entries.Add(new RomEntrance((int)((p - profile.EntranceTable) / 4), rom[checked((int)p)], rom[checked((int)p + 1)]));
        return entries;
    }
    private static ushort U16(byte[] b, int o) => (ushort)(b[o] << 8 | b[o + 1]);
    private static uint Rgb24(byte[] b, int o) => (uint)b[o] << 16 | (uint)b[o + 1] << 8 | b[o + 2];
    private static short S16(byte[] b, int o) => unchecked((short)U16(b, o));
    private static uint U32(byte[] b, int o) => (uint)b[o] << 24 | (uint)b[o + 1] << 16 | (uint)b[o + 2] << 8 | b[o + 3];
    private static ulong U64(byte[] b, int o) => (ulong)U32(b, o) << 32 | U32(b, o + 4);
}
