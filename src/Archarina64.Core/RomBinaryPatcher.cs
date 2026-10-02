using System.Security.Cryptography;

namespace Archarina64.Core;

public static class RomBinaryPatcher
{
    public static byte[] Apply(byte[] original, RomDocument document, RomWorkspacePatch patch)
    {
        if (original is null || document is null || patch is null) throw new ArgumentNullException();
        using var sha = SHA256.Create(); string fingerprint = Convert.ToHexString(sha.ComputeHash(original)).ToLowerInvariant();
        if (!string.Equals(fingerprint, patch.RomFingerprint, StringComparison.OrdinalIgnoreCase) || !string.Equals(fingerprint, document.Fingerprint, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The patch does not match the loaded ROM fingerprint.");
        var output = (byte[])original.Clone();
        foreach (var edit in patch.SceneTopology ?? []) ApplySceneTopologyRelocating(output, document.Profile, edit);
        foreach (var edit in patch.RoomTopology ?? []) ApplyRoomTopologyRelocating(output, document.Profile, edit);
        foreach (var edit in patch.AlternateRoomTopology ?? []) ApplyAlternateRoomTopologyRelocating(output, document.Profile, edit);
        foreach (var edit in patch.AlternateRoomOrders ?? []) ApplyAlternateRoomOrder(output, document.Profile, edit);
        foreach (var edit in patch.Actors) ApplyActorRelocating(output, document.Profile, edit);
        foreach (var edit in patch.Objects ?? []) ApplyObjectRelocating(output, document.Profile, edit);
        foreach (var edit in patch.ObjectLists ?? []) ApplyObjectListRelocating(output, document.Profile, edit);
        foreach (var edit in patch.ActorLists ?? []) ApplyActorListRelocating(output, document.Profile, edit);
        foreach (var edit in patch.RoomOrders ?? []) ApplyRoomOrder(output, document.Profile, edit);
        foreach (var edit in patch.RoomSettings ?? []) ApplyRoomSettingsRelocating(output, document.Profile, edit);
        foreach (var edit in patch.RoomExits ?? []) ApplyRoomExit(output, document.Profile, edit);
        foreach (var edit in patch.Cameras ?? []) ApplyCamera(output, document.Profile, edit);
        foreach (var edit in patch.SceneSettings ?? []) ApplySceneSettings(output, document.Profile, edit);
        foreach (var edit in patch.SceneCommands ?? []) ApplySceneCommand(output, document.Profile, edit);
        foreach (var edit in patch.AlternateHeaders ?? []) ApplyAlternateHeader(output, document.Profile, edit);
        foreach (var edit in patch.AlternateCommands ?? []) ApplyAlternateCommandRelocating(output, document.Profile, edit);
        foreach (var edit in patch.AlternateSceneSettings ?? []) ApplyAlternateSceneSettings(output, document.Profile, edit);
        foreach (var edit in patch.AlternateActors ?? []) ApplyAlternateActor(output, document.Profile, edit);
        foreach (var edit in patch.AlternateActorLists ?? []) ApplyAlternateActorListRelocating(output, document.Profile, edit);
        foreach (var edit in patch.AlternateObjects ?? []) ApplyAlternateObject(output, document.Profile, edit);
        foreach (var edit in patch.AlternateObjectLists ?? []) ApplyAlternateObjectListRelocating(output, document.Profile, edit);
        foreach (var edit in patch.AlternateRoomSettings ?? []) ApplyAlternateRoomSettings(output, document.Profile, edit);
        foreach (var edit in patch.AlternateRoomExits ?? []) ApplyAlternateRoomExit(output, document.Profile, edit);
        foreach (var edit in patch.AlternateGeometryVertices ?? []) ApplyAlternateGeometryVertex(output, document.Profile, edit);
        foreach (var edit in patch.AlternateGeometryTriangles ?? []) ApplyAlternateGeometryTriangle(output, document.Profile, edit);
        foreach (var edit in patch.AlternateCollisionVertices ?? []) ApplyAlternateCollisionVertex(output, document.Profile, edit);
        foreach (var edit in patch.AlternateCollisionTriangles ?? []) ApplyAlternateCollisionTriangle(output, document.Profile, edit);
        foreach (var edit in patch.AlternateCollisionSurfaces ?? []) ApplyAlternateCollisionSurface(output, document.Profile, edit);
        foreach (var edit in patch.AlternateCollisionBounds ?? []) ApplyAlternateCollisionBounds(output, document.Profile, edit);
        foreach (var edit in patch.AlternateCameras ?? []) ApplyAlternateCamera(output, document.Profile, edit);
        foreach (var edit in patch.AlternateWaterboxes ?? []) ApplyAlternateWaterbox(output, document.Profile, edit);
        foreach (var edit in patch.AlternateCollisionTopologies ?? []) ApplyAlternateCollisionTopologyRelocating(output, document.Profile, edit);
        foreach (var edit in patch.CollisionBounds ?? []) ApplyCollisionBounds(output, document.Profile, edit);
        foreach (var edit in patch.Entrances ?? []) ApplyEntrance(output, document.Profile, edit);
        foreach (var sceneId in (patch.Paths ?? []).Select(e => e.SceneId).Concat((patch.PathLists ?? []).Select(e => e.SceneId)).Concat((patch.Exits ?? []).Select(e => e.SceneId)).Concat((patch.Waterboxes ?? []).Select(e => e.SceneId)).Concat((patch.Environments ?? []).Select(e => e.SceneId)).Concat((patch.CollisionVertices ?? []).Select(e => e.SceneId)).Concat((patch.CollisionTriangles ?? []).Select(e => e.SceneId)).Concat((patch.CollisionSurfaces ?? []).Select(e => e.SceneId)).Concat((patch.Spawns ?? []).Select(e => e.SceneId)).Concat((patch.Transitions ?? []).Select(e => e.SceneId)).Distinct())
            ApplySceneEdits(output, document.Profile, sceneId, patch.Paths ?? [], patch.PathLists ?? [], patch.Exits ?? [], patch.Waterboxes ?? [], patch.Environments ?? [], patch.CollisionVertices ?? [], patch.CollisionTriangles ?? [], patch.CollisionSurfaces ?? [], patch.Spawns ?? [], patch.Transitions ?? []);
        foreach (var edit in patch.CollisionTopologies ?? []) ApplyCollisionTopologyRelocating(output, document.Profile, edit);
        foreach (var edit in patch.GeometryRebuilds ?? []) ApplyGeometryRebuildRelocating(output, document.Profile, edit);
        foreach (var edit in patch.GeometryVertices ?? []) ApplyGeometryVertexRelocating(output, document.Profile, edit);
        foreach (var edit in patch.GeometryTriangles ?? []) ApplyGeometryTriangleRelocating(output, document.Profile, edit);
        foreach (var edit in patch.GeometryCommands ?? []) ApplyGeometryCommandRelocating(output, document.Profile, edit);
        foreach (var edit in patch.Textures ?? []) ApplyTextureRelocating(output, document.Profile, edit);
        return output;
    }

    private static void ApplyCollisionTopologyRelocating(byte[] rom, RomProfile profile, RomCollisionTopologyEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Collision topology scene index is outside the profile.");
        if (edit.Vertices is null || edit.Vertices.Count == 0 || edit.Vertices.Count > ushort.MaxValue) throw new InvalidDataException("Collision vertex count is outside the native range.");
        if (edit.Triangles is null || edit.Triangles.Count > ushort.MaxValue) throw new InvalidDataException("Collision triangle count is outside the native range.");
        if (edit.SurfaceTypes is null || edit.SurfaceTypes.Count != edit.Triangles.Count) throw new InvalidDataException("Collision surface count must match the triangle count.");
        if (edit.Triangles.Any(t => t.A >= edit.Vertices.Count || t.B >= edit.Vertices.Count || t.C >= edit.Vertices.Count)) throw new InvalidDataException("Collision triangle references a vertex outside the replacement topology.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene);
        int command = FindCommand(scene, 0x02); if (command < 0) throw new InvalidDataException("Scene has no collision command.");
        int collision = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); EnsureRange(scene, collision, 44, "collision header");
        int vertexOffset = Align(scene.Length, 2); int triangleOffset = Align(vertexOffset + edit.Vertices.Count * 6, 2); int surfaceOffset = Align(triangleOffset + edit.Triangles.Count * 16, 8); int sceneLength = checked(surfaceOffset + edit.SurfaceTypes.Count * 8);
        if (vertexOffset > 0x00FFFFFF || triangleOffset > 0x00FFFFFF || surfaceOffset > 0x00FFFFFF) throw new InvalidDataException("Collision topology exceeds the segmented scene address range.");
        Array.Resize(ref scene, sceneLength);
        Put16(scene, collision + 12, checked((ushort)edit.Vertices.Count)); Put32(scene, collision + 16, checked(0x02000000u | (uint)vertexOffset));
        Put16(scene, collision + 20, checked((ushort)edit.Triangles.Count)); Put32(scene, collision + 24, checked(0x02000000u | (uint)triangleOffset)); Put32(scene, collision + 28, edit.SurfaceTypes.Count == 0 ? 0u : checked(0x02000000u | (uint)surfaceOffset));
        for (int i = 0; i < edit.Vertices.Count; i++) { int o = vertexOffset + i * 6; Put16(scene, o, unchecked((ushort)edit.Vertices[i].X)); Put16(scene, o + 2, unchecked((ushort)edit.Vertices[i].Y)); Put16(scene, o + 4, unchecked((ushort)edit.Vertices[i].Z)); }
        for (int i = 0; i < edit.Triangles.Count; i++) { int o = triangleOffset + i * 16; var t = edit.Triangles[i]; Put16(scene, o, t.SurfaceType); Put16(scene, o + 2, t.A); Put16(scene, o + 4, t.B); Put16(scene, o + 6, t.C); Put16(scene, o + 8, unchecked((ushort)t.NormalX)); Put16(scene, o + 10, unchecked((ushort)t.NormalY)); Put16(scene, o + 12, unchecked((ushort)t.NormalZ)); Put16(scene, o + 14, unchecked((ushort)t.Distance)); }
        for (int i = 0; i < edit.SurfaceTypes.Count; i++) Put64(scene, surfaceOffset + i * 8, edit.SurfaceTypes[i]);
        WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static void ApplyGeometryRebuildRelocating(byte[] rom, RomProfile profile, RomGeometryRebuildEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Geometry rebuild scene index is outside the profile.");
        if (edit.Vertices is null || edit.Vertices.Count == 0 || edit.Vertices.Count > 32 || edit.Triangles is null || edit.Triangles.Count == 0) throw new InvalidDataException("Native display-list rebuild requires 1-32 vertices and at least one triangle.");
        foreach (var triangle in edit.Triangles)
            if (triangle.A < 0 || triangle.A >= edit.Vertices.Count || triangle.B < 0 || triangle.B >= edit.Vertices.Count || triangle.C < 0 || triangle.C >= edit.Vertices.Count)
                throw new InvalidDataException("Native display-list rebuild contains an invalid triangle index.");

        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20);
        uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4);
        byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene");
        byte[] scene = N64Compression.Decode(storedScene);
        int roomCommand = FindRoomTable(scene);
        if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Geometry rebuild room index is outside the scene.");
        int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF));
        int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "room table");
        uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4);
        byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room");
        byte[] room = N64Compression.Decode(storedRoom);
        EnsureRange(room, edit.MeshEntryOffset, 8, "mesh table entry");

        int displayListOffset = Align(room.Length, 8);
        int commandBytes = checked(16 + edit.Triangles.Count * 8);
        int vertexDataOffset = Align(displayListOffset + commandBytes, 16);
        int roomLength = checked(vertexDataOffset + edit.Vertices.Count * 16);
        if (displayListOffset > 0x00FFFFFF || vertexDataOffset > 0x00FFFFFF) throw new InvalidDataException("Rebuilt display-list data exceeds the segmented room address range.");
        Array.Resize(ref room, roomLength);
        Put32(room, edit.MeshEntryOffset, checked(0x03000000u | (uint)displayListOffset));
        int p = displayListOffset;
        Put32(room, p, checked(0x01000000u | ((uint)edit.Vertices.Count << 12))); Put32(room, p + 4, checked(0x03000000u | (uint)vertexDataOffset)); p += 8;
        foreach (var triangle in edit.Triangles)
        {
            Put32(room, p, 0xBF000000); Put32(room, p + 4, checked((((uint)triangle.A * 2) << 17) | (((uint)triangle.B * 2) << 9) | (((uint)triangle.C * 2) << 1))); p += 8;
        }
        Put32(room, p, 0xB8000000); Put32(room, p + 4, 0);
        for (int i = 0; i < edit.Vertices.Count; i++) WriteGeometryVertex(room, vertexDataOffset + i * 16, edit.Vertices[i]);

        byte[] replacement = EncodeStoredRange(storedRoom, room);
        if (replacement.Length <= roomEnd - roomStart) { WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
        int relocatedRoom = FindZeroRange(rom, replacement.Length, checked((int)roomEnd), checked((int)roomStart), checked((int)roomEnd));
        if (relocatedRoom < 0) throw new InvalidDataException("Rebuilt room needs more space but no safe free ROM range was found for relocation.");
        Buffer.BlockCopy(replacement, 0, rom, relocatedRoom, replacement.Length);
        Put32(scene, roomEntry, checked((uint)relocatedRoom)); Put32(scene, roomEntry + 4, checked((uint)(relocatedRoom + replacement.Length)));
        WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static void ApplySceneEdits(byte[] rom, RomProfile profile, int sceneId, IReadOnlyList<RomPathEdit> paths, IReadOnlyList<RomPathListEdit> pathLists, IReadOnlyList<RomExitEdit> exits, IReadOnlyList<RomWaterboxEdit> waterboxes, IReadOnlyList<RomEnvironmentEdit> environments, IReadOnlyList<RomCollisionVertexEdit> collisionVertices, IReadOnlyList<RomCollisionTriangleEdit> collisionTriangles, IReadOnlyList<RomCollisionSurfaceEdit> collisionSurfaces, IReadOnlyList<RomSpawnEdit> spawns, IReadOnlyList<RomTransitionEdit> transitions)
    {
        if (sceneId < 0 || sceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + sceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4);
        byte[] stored = StoredRange(rom, sceneStart, sceneEnd, "scene");
        bool compressed = IsCompressed(stored); byte[] scene = compressed ? N64Compression.Decode(stored) : (byte[])stored.Clone();
        foreach (var edit in pathLists.Where(e => e.SceneId == sceneId)) scene = ApplyPathList(scene, edit);
        foreach (var edit in paths.Where(e => e.SceneId == sceneId)) ApplyPath(scene, edit);
        foreach (var edit in exits.Where(e => e.SceneId == sceneId)) ApplyExit(scene, edit);
        foreach (var edit in waterboxes.Where(e => e.SceneId == sceneId)) ApplyWaterbox(scene, edit);
        foreach (var edit in environments.Where(e => e.SceneId == sceneId)) ApplyEnvironment(scene, edit);
        foreach (var edit in collisionVertices.Where(e => e.SceneId == sceneId)) ApplyCollisionVertex(scene, edit);
        foreach (var edit in collisionTriangles.Where(e => e.SceneId == sceneId)) ApplyCollisionTriangle(scene, edit);
        foreach (var edit in collisionSurfaces.Where(e => e.SceneId == sceneId)) ApplyCollisionSurface(scene, edit);
        foreach (var edit in spawns.Where(e => e.SceneId == sceneId)) ApplySpawn(scene, edit);
        foreach (var edit in transitions.Where(e => e.SceneId == sceneId)) ApplyTransition(scene, edit);
        if (compressed)
        {
            byte[] replacement = EncodeStoredRange(stored, scene); int capacity = checked((int)(sceneEnd - sceneStart));
            if (replacement.Length <= capacity) WriteStoredRange(rom, sceneStart, sceneEnd, stored, scene);
            else
            {
                int relocatedStart = FindZeroRange(rom, replacement.Length, checked((int)sceneEnd), checked((int)sceneStart), checked((int)sceneEnd));
                if (relocatedStart < 0) throw new InvalidDataException($"Edited compressed scene needs {replacement.Length} bytes but no safe free ROM range was found for relocation.");
                Buffer.BlockCopy(replacement, 0, rom, relocatedStart, replacement.Length);
                Put32(rom, sceneRow, checked((uint)relocatedStart)); Put32(rom, sceneRow + 4, checked((uint)(relocatedStart + replacement.Length)));
            }
        }
        else
        {
            int capacity = checked((int)(sceneEnd - sceneStart));
            if (scene.Length <= capacity) Buffer.BlockCopy(scene, 0, rom, checked((int)sceneStart), scene.Length);
            else
            {
                int relocatedStart = FindZeroRange(rom, scene.Length, checked((int)sceneEnd), checked((int)sceneStart), checked((int)sceneEnd));
                if (relocatedStart < 0) throw new InvalidDataException($"Edited scene needs {scene.Length} bytes but no safe free ROM range was found for relocation.");
                Buffer.BlockCopy(scene, 0, rom, relocatedStart, scene.Length); Put32(rom, sceneRow, checked((uint)relocatedStart)); Put32(rom, sceneRow + 4, checked((uint)(relocatedStart + scene.Length)));
            }
        }
    }

    private static byte[] ApplyPathList(byte[] scene, RomPathListEdit edit)
    {
        int command = FindCommand(scene, 0x0D); if (command < 0) throw new InvalidDataException("Scene has no supported path command.");
        if (edit.Paths is null || edit.Paths.Count > byte.MaxValue) throw new InvalidDataException("Path count is outside the native range.");
        if (edit.Paths.Any(path => path is null || path.Points is null || path.Points.Count > byte.MaxValue)) throw new InvalidDataException("Path waypoint count is outside the native range.");
        int table = Align(scene.Length, 4); int points = checked(table + edit.Paths.Count * 8); int total = points;
        foreach (var path in edit.Paths) total = checked(total + path.Points.Count * 6);
        if (table > 0x00FFFFFF || points > 0x00FFFFFF || total > 0x00FFFFFF) throw new InvalidDataException("Path data exceeds the segmented scene address range.");
        Array.Resize(ref scene, total); byte segment = (byte)(U32(scene, command + 4) >> 24); scene[command + 1] = checked((byte)edit.Paths.Count); Put32(scene, command + 4, checked(((uint)segment << 24) | (uint)table));
        int pointData = points;
        for (int index = 0; index < edit.Paths.Count; index++)
        {
            var path = edit.Paths[index]; int entry = table + index * 8; scene[entry] = checked((byte)path.Points.Count); scene[entry + 1] = 0; scene[entry + 2] = 0; scene[entry + 3] = 0; Put32(scene, entry + 4, checked(((uint)segment << 24) | (uint)pointData));
            foreach (var point in path.Points) { Put16(scene, pointData, unchecked((ushort)point.X)); Put16(scene, pointData + 2, unchecked((ushort)point.Y)); Put16(scene, pointData + 4, unchecked((ushort)point.Z)); pointData += 6; }
        }
        return scene;
    }

    private static void ApplyGeometryVertexRelocating(byte[] rom, RomProfile profile, RomGeometryVertexEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene."); int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "room table");
        uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); EnsureRange(room, edit.SourceOffset, 16, "geometry vertex");
        Put16(room, edit.SourceOffset, unchecked((ushort)edit.Vertex.X)); Put16(room, edit.SourceOffset + 2, unchecked((ushort)edit.Vertex.Y)); Put16(room, edit.SourceOffset + 4, unchecked((ushort)edit.Vertex.Z)); Put16(room, edit.SourceOffset + 8, unchecked((ushort)edit.Vertex.S)); Put16(room, edit.SourceOffset + 10, unchecked((ushort)edit.Vertex.T)); room[edit.SourceOffset + 12] = edit.Vertex.R; room[edit.SourceOffset + 13] = edit.Vertex.G; room[edit.SourceOffset + 14] = edit.Vertex.B; room[edit.SourceOffset + 15] = edit.Vertex.A;
        byte[] replacement = EncodeStoredRange(storedRoom, room); if (replacement.Length <= roomEnd - roomStart) { WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
        int relocatedRoom = FindZeroRange(rom, replacement.Length, checked((int)roomEnd), checked((int)roomStart), checked((int)roomEnd)); if (relocatedRoom < 0) throw new InvalidDataException("Edited room needs more space but no safe free ROM range was found for relocation."); Buffer.BlockCopy(replacement, 0, rom, relocatedRoom, replacement.Length); Put32(scene, roomEntry, checked((uint)relocatedRoom)); Put32(scene, roomEntry + 4, checked((uint)(relocatedRoom + replacement.Length))); WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static void ApplyGeometryVertex(byte[] rom, RomProfile profile, RomGeometryVertexEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] scene = ReadDecodedRange(rom, sceneStart, sceneEnd, "scene"); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene.");
        int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "room table"); uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom);
        EnsureRange(room, edit.SourceOffset, 16, "geometry vertex"); Put16(room, edit.SourceOffset, unchecked((ushort)edit.Vertex.X)); Put16(room, edit.SourceOffset + 2, unchecked((ushort)edit.Vertex.Y)); Put16(room, edit.SourceOffset + 4, unchecked((ushort)edit.Vertex.Z)); Put16(room, edit.SourceOffset + 8, unchecked((ushort)edit.Vertex.S)); Put16(room, edit.SourceOffset + 10, unchecked((ushort)edit.Vertex.T)); room[edit.SourceOffset + 12] = edit.Vertex.R; room[edit.SourceOffset + 13] = edit.Vertex.G; room[edit.SourceOffset + 14] = edit.Vertex.B; room[edit.SourceOffset + 15] = edit.Vertex.A;
        WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room);
    }

    private static void ApplyGeometryTriangleRelocating(byte[] rom, RomProfile profile, RomGeometryTriangleEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene."); int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "room table");
        uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); EnsureRange(room, edit.SourceOffset, 4, "geometry triangle"); uint packed = edit.Prefix | (uint)(edit.SlotA * 2) << 17 | (uint)(edit.SlotB * 2) << 9 | (uint)(edit.SlotC * 2 << 1); Put32(room, edit.SourceOffset, packed);
        byte[] replacement = EncodeStoredRange(storedRoom, room); if (replacement.Length <= roomEnd - roomStart) { WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
        int relocatedRoom = FindZeroRange(rom, replacement.Length, checked((int)roomEnd), checked((int)roomStart), checked((int)roomEnd)); if (relocatedRoom < 0) throw new InvalidDataException("Edited room needs more space but no safe free ROM range was found for relocation."); Buffer.BlockCopy(replacement, 0, rom, relocatedRoom, replacement.Length); Put32(scene, roomEntry, checked((uint)relocatedRoom)); Put32(scene, roomEntry + 4, checked((uint)(relocatedRoom + replacement.Length))); WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static void ApplyGeometryTriangle(byte[] rom, RomProfile profile, RomGeometryTriangleEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] scene = ReadDecodedRange(rom, sceneStart, sceneEnd, "scene"); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene."); int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "room table"); uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); EnsureRange(room, edit.SourceOffset, 4, "geometry triangle"); uint packed = edit.Prefix | (uint)(edit.SlotA * 2) << 17 | (uint)(edit.SlotB * 2) << 9 | (uint)(edit.SlotC * 2) << 1; Put32(room, edit.SourceOffset, packed); WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room);
    }

    private static void ApplyGeometryCommandRelocating(byte[] rom, RomProfile profile, RomGeometryCommandEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene."); int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "room table");
        uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); EnsureRange(room, edit.SourceOffset, 8, "display-list command");
        Put32(room, edit.SourceOffset, edit.Word0); Put32(room, edit.SourceOffset + 4, edit.Word1);
        byte[] replacement = EncodeStoredRange(storedRoom, room); if (replacement.Length <= roomEnd - roomStart) { WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
        int relocatedRoom = FindZeroRange(rom, replacement.Length, checked((int)roomEnd), checked((int)roomStart), checked((int)roomEnd)); if (relocatedRoom < 0) throw new InvalidDataException("Edited room needs more space but no safe free ROM range was found for relocation."); Buffer.BlockCopy(replacement, 0, rom, relocatedRoom, replacement.Length); Put32(scene, roomEntry, checked((uint)relocatedRoom)); Put32(scene, roomEntry + 4, checked((uint)(relocatedRoom + replacement.Length))); WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static void ApplyTextureRelocating(byte[] rom, RomProfile profile, RomTextureEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Texture edit scene index is outside the profile.");
        if (edit.Segment != 3) throw new InvalidDataException("Only room-local texture segments can be replaced safely.");
        if (edit.Width > 1024 || edit.Height > 1024) throw new InvalidDataException("Native texture dimensions exceed the supported tile-size range.");
        byte[] encoded = RomTextureDecoder.EncodePixels(edit.Rgba ?? [], edit.Format, edit.Size, edit.Width, edit.Height, edit.PaletteRgba, edit.PaletteBase);
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Texture edit room index is outside the scene."); int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "room table");
        uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); EnsureRange(room, edit.SourceOffset, 8, "texture image command");
        var command = new RomDisplayListCommand(edit.SourceOffset, (byte)(U32(room, edit.SourceOffset) >> 24), U32(room, edit.SourceOffset), U32(room, edit.SourceOffset + 4)); int originalWidth = edit.OriginalWidth > 0 ? edit.OriginalWidth : edit.Width; int originalHeight = edit.OriginalHeight > 0 ? edit.OriginalHeight : edit.Height; if (!RomTextureCommandDecoder.TryDecode(command, out var image) || image.Kind != "G_SETTIMG" || image.Segment != edit.Segment || image.Address != edit.Address || !string.Equals(image.FormatName, edit.Format, StringComparison.Ordinal) || !string.Equals(image.SizeName, edit.Size, StringComparison.Ordinal) || image.Width != originalWidth) throw new InvalidDataException("Texture source command no longer matches the staged replacement.");
        bool dimensionChanged = edit.Width != originalWidth || edit.Height != originalHeight; int address = edit.Address;
        if (dimensionChanged)
        {
            address = Align(room.Length, 8); int appendedLength = checked(address + encoded.Length); Array.Resize(ref room, appendedLength); Put32(room, edit.SourceOffset, (U32(room, edit.SourceOffset) & 0xFFFFF000u) | checked((uint)(edit.Width - 1))); Put32(room, edit.SourceOffset + 4, (U32(room, edit.SourceOffset + 4) & 0xFF000000u) | checked((uint)address));
            bool updatedTile = false;
            for (int p = edit.SourceOffset + 8; p + 8 <= room.Length; p += 8)
            {
                byte op = (byte)(U32(room, p) >> 24); if (op == 0xFD || op == 0xB8) break;
                if (op != 0xF2) continue;
                int lrs = checked((edit.Width - 1) * 4), lrt = checked((edit.Height - 1) * 4); if (lrs > 0xFFF || lrt > 0xFFF) throw new InvalidDataException("Native texture dimensions exceed G_SETTILESIZE precision."); Put32(room, p + 4, checked((uint)(lrs << 12 | lrt))); updatedTile = true; break;
            }
            if (!updatedTile) throw new InvalidDataException("Texture dimension change requires a following G_SETTILESIZE command.");
        }
        EnsureRange(room, address, encoded.Length, "texture image data"); Buffer.BlockCopy(encoded, 0, room, address, encoded.Length);
        byte[] replacement = EncodeStoredRange(storedRoom, room); if (replacement.Length <= roomEnd - roomStart) { WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
        int relocatedRoom = FindZeroRange(rom, replacement.Length, checked((int)roomEnd), checked((int)roomStart), checked((int)roomEnd)); if (relocatedRoom < 0) throw new InvalidDataException("Edited texture room needs more space but no safe free ROM range was found for relocation."); Buffer.BlockCopy(replacement, 0, rom, relocatedRoom, replacement.Length); Put32(scene, roomEntry, checked((uint)relocatedRoom)); Put32(scene, roomEntry + 4, checked((uint)(relocatedRoom + replacement.Length))); WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static void ApplyPath(byte[] scene, RomPathEdit edit)
    {
        int command = FindCommand(scene, 0x0D); if (command < 0) throw new InvalidDataException("Scene has no supported path command.");
        int count = scene[command + 1]; if (edit.PathId < 0 || edit.PathId >= count) throw new InvalidDataException("Patch path index is outside the scene.");
        int table = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); int entry = checked(table + edit.PathId * 8); EnsureRange(scene, entry, 8, "path table");
        int pointCount = scene[entry]; int points = checked((int)(U32(scene, entry + 4) & 0x00FFFFFF)); if (edit.PointIndex < 0 || edit.PointIndex >= pointCount) throw new InvalidDataException("Patch path point index is outside the path.");
        int o = checked(points + edit.PointIndex * 6); EnsureRange(scene, o, 6, "path points"); Put16(scene, o, unchecked((ushort)edit.Point.X)); Put16(scene, o + 2, unchecked((ushort)edit.Point.Y)); Put16(scene, o + 4, unchecked((ushort)edit.Point.Z));
    }

    private static void ApplyExit(byte[] scene, RomExitEdit edit)
    {
        int command = FindCommand(scene, 0x13); if (command < 0) throw new InvalidDataException("Scene has no supported exit command.");
        int count = scene[command + 1]; if (edit.ExitIndex < 0 || edit.ExitIndex >= count) throw new InvalidDataException("Patch exit index is outside the scene.");
        int data = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); int o = checked(data + edit.ExitIndex * 2); EnsureRange(scene, o, 2, "exit list"); Put16(scene, o, edit.Raw);
    }

    private static void ApplyWaterbox(byte[] scene, RomWaterboxEdit edit)
    {
        int command = FindCommand(scene, 0x02); if (command < 0) throw new InvalidDataException("Scene has no supported collision command.");
        int collision = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); EnsureRange(scene, collision, 44, "collision header");
        int count = U16(scene, collision + 36); if (edit.WaterboxIndex < 0 || edit.WaterboxIndex >= count) throw new InvalidDataException("Patch waterbox index is outside the scene.");
        int data = checked((int)(U32(scene, collision + 40) & 0x00FFFFFF)); int o = checked(data + edit.WaterboxIndex * 16); EnsureRange(scene, o, 16, "waterbox list");
        Put16(scene, o, unchecked((ushort)edit.Waterbox.X)); Put16(scene, o + 2, unchecked((ushort)edit.Waterbox.Y)); Put16(scene, o + 4, unchecked((ushort)edit.Waterbox.Z)); Put16(scene, o + 6, unchecked((ushort)edit.Waterbox.XSize)); Put16(scene, o + 8, unchecked((ushort)edit.Waterbox.ZSize)); Put16(scene, o + 10, edit.Waterbox.Unknown); Put32(scene, o + 12, edit.Waterbox.Properties);
    }

    private static void ApplyEnvironment(byte[] scene, RomEnvironmentEdit edit)
    {
        int command = FindCommand(scene, 0x0F); if (command < 0) throw new InvalidDataException("Scene has no supported environment command.");
        int count = scene[command + 1]; if (edit.EnvironmentIndex < 0 || edit.EnvironmentIndex >= count) throw new InvalidDataException("Patch environment index is outside the scene.");
        int data = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); int o = checked(data + edit.EnvironmentIndex * 22); EnsureRange(scene, o, 22, "environment list");
        PutRgb(scene, o, edit.Environment.Ambient); PutRgb(scene, o + 3, edit.Environment.Diffuse0); PutRgb(scene, o + 6, edit.Environment.Direction0); PutRgb(scene, o + 9, edit.Environment.Diffuse1); PutRgb(scene, o + 12, edit.Environment.Direction1); PutRgb(scene, o + 15, edit.Environment.FogColor); Put16(scene, o + 18, (ushort)(edit.Environment.FogDistance & 0x03FF | (edit.Environment.FogUnknown << 10))); Put16(scene, o + 20, edit.Environment.DrawDistance);
    }

    private static void ApplyCollisionVertex(byte[] scene, RomCollisionVertexEdit edit)
    {
        int command = FindCommand(scene, 0x02); if (command < 0) throw new InvalidDataException("Scene has no supported collision command.");
        int collision = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); EnsureRange(scene, collision, 44, "collision header");
        int count = U16(scene, collision + 12); if (edit.VertexIndex < 0 || edit.VertexIndex >= count) throw new InvalidDataException("Patch collision vertex index is outside the scene.");
        int data = checked((int)(U32(scene, collision + 16) & 0x00FFFFFF)); int o = checked(data + edit.VertexIndex * 6); EnsureRange(scene, o, 6, "collision vertex list");
        Put16(scene, o, unchecked((ushort)edit.Vertex.X)); Put16(scene, o + 2, unchecked((ushort)edit.Vertex.Y)); Put16(scene, o + 4, unchecked((ushort)edit.Vertex.Z));
    }

    private static void ApplyCollisionTriangle(byte[] scene, RomCollisionTriangleEdit edit)
    {
        int command = FindCommand(scene, 0x02); if (command < 0) throw new InvalidDataException("Scene has no supported collision command.");
        int collision = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); EnsureRange(scene, collision, 44, "collision header");
        int count = U16(scene, collision + 20); if (edit.TriangleIndex < 0 || edit.TriangleIndex >= count) throw new InvalidDataException("Patch collision triangle index is outside the scene.");
        int data = checked((int)(U32(scene, collision + 24) & 0x00FFFFFF)); int o = checked(data + edit.TriangleIndex * 16); EnsureRange(scene, o, 16, "collision triangle list");
        Put16(scene, o, edit.Triangle.SurfaceType); Put16(scene, o + 2, edit.Triangle.A); Put16(scene, o + 4, edit.Triangle.B); Put16(scene, o + 6, edit.Triangle.C); Put16(scene, o + 8, unchecked((ushort)edit.Triangle.NormalX)); Put16(scene, o + 10, unchecked((ushort)edit.Triangle.NormalY)); Put16(scene, o + 12, unchecked((ushort)edit.Triangle.NormalZ)); Put16(scene, o + 14, unchecked((ushort)edit.Triangle.Distance));
    }

    private static void ApplyCollisionSurface(byte[] scene, RomCollisionSurfaceEdit edit)
    {
        int command = FindCommand(scene, 0x02); if (command < 0) throw new InvalidDataException("Scene has no supported collision command.");
        int collision = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); EnsureRange(scene, collision, 44, "collision header");
        int count = U16(scene, collision + 20); if (edit.SurfaceIndex < 0 || edit.SurfaceIndex >= count) throw new InvalidDataException("Patch collision surface index is outside the scene.");
        int data = checked((int)(U32(scene, collision + 28) & 0x00FFFFFF)); int o = checked(data + edit.SurfaceIndex * 8); EnsureRange(scene, o, 8, "collision surface list");
        Put64(scene, o, edit.SurfaceType);
    }

    private static void ApplyCollisionBounds(byte[] rom, RomProfile profile, RomCollisionBoundsEdit edit) { int row = checked((int)profile.SceneTable + edit.SceneId * 20); uint start = U32(rom, row), end = U32(rom, row + 4); byte[] scene = ReadDecodedRange(rom, start, end, "scene"); int command = FindCommand(scene, 0x02); if (command < 0) throw new InvalidDataException("Scene has no collision command."); int collision = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); EnsureRange(scene, collision, 12, "collision bounds"); Put16(scene, collision, unchecked((ushort)edit.MinX)); Put16(scene, collision + 2, unchecked((ushort)edit.MinY)); Put16(scene, collision + 4, unchecked((ushort)edit.MinZ)); Put16(scene, collision + 6, unchecked((ushort)edit.MaxX)); Put16(scene, collision + 8, unchecked((ushort)edit.MaxY)); Put16(scene, collision + 10, unchecked((ushort)edit.MaxZ)); WriteStoredRange(rom, start, end, StoredRange(rom, start, end, "scene"), scene); }

    private static void ApplySpawn(byte[] scene, RomSpawnEdit edit)
    {
        int command = FindCommand(scene, 0x00); if (command < 0) throw new InvalidDataException("Scene has no supported spawn command."); int count = scene[command + 1]; if (edit.SpawnIndex < 0 || edit.SpawnIndex >= count) throw new InvalidDataException("Patch spawn index is outside the scene."); int data = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); int o = checked(data + edit.SpawnIndex * 16); EnsureRange(scene, o, 16, "spawn list"); Put16(scene, o, edit.Spawn.Number); Put16(scene, o + 2, unchecked((ushort)edit.Spawn.X)); Put16(scene, o + 4, unchecked((ushort)edit.Spawn.Y)); Put16(scene, o + 6, unchecked((ushort)edit.Spawn.Z)); Put16(scene, o + 8, unchecked((ushort)edit.Spawn.RotationX)); Put16(scene, o + 10, unchecked((ushort)edit.Spawn.RotationY)); Put16(scene, o + 12, unchecked((ushort)edit.Spawn.RotationZ)); Put16(scene, o + 14, edit.Spawn.Variable);
    }

    private static void ApplyTransition(byte[] scene, RomTransitionEdit edit)
    {
        int command = FindCommand(scene, 0x0E); if (command < 0) throw new InvalidDataException("Scene has no supported transition command.");
        int count = scene[command + 1]; if (edit.TransitionIndex < 0 || edit.TransitionIndex >= count) throw new InvalidDataException("Patch transition index is outside the scene.");
        int data = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); int o = checked(data + edit.TransitionIndex * 16); EnsureRange(scene, o, 16, "transition list");
        scene[o] = edit.Transition.FrontRoom; scene[o + 1] = edit.Transition.FrontCamera; scene[o + 2] = edit.Transition.BackRoom; scene[o + 3] = edit.Transition.BackCamera;
        Put16(scene, o + 4, edit.Transition.Number); Put16(scene, o + 6, unchecked((ushort)edit.Transition.X)); Put16(scene, o + 8, unchecked((ushort)edit.Transition.Y)); Put16(scene, o + 10, unchecked((ushort)edit.Transition.Z)); Put16(scene, o + 12, unchecked((ushort)edit.Transition.RotationY)); Put16(scene, o + 14, edit.Transition.Variable);
    }

    private static int FindCommand(byte[] scene, byte command)
    {
        for (int p = 0; p + 8 <= scene.Length && p < 8192; p += 8) { if (scene[p] == 0x14) break; if (scene[p] == command) return p; }
        return -1;
    }

    private static void EnsureRange(byte[] data, int offset, int length, string kind) { if (offset < 0 || length < 0 || offset > data.Length - length) throw new InvalidDataException($"Patch {kind} is outside the decoded scene."); }
    private static bool IsCompressed(byte[] data) => data.Length >= 4 && ((data[0] == 'Y' && data[1] == 'a' && data[2] == 'z' && data[3] == '0') || (data[0] == 'M' && data[1] == 'I' && data[2] == 'O' && data[3] == '0'));

    private static void ApplyEntrance(byte[] rom, RomProfile profile, RomEntranceEdit edit)
    {
        if (profile.EntranceTableEnd <= profile.EntranceTable || edit.Index < 0 || profile.EntranceTable + (uint)(edit.Index + 1) * 4 > profile.EntranceTableEnd) throw new InvalidDataException("Entrance edit is outside the loaded ROM profile.");
        int offset = checked((int)profile.EntranceTable + edit.Index * 4); rom[offset] = edit.SceneId; rom[offset + 1] = edit.SpawnId;
    }

    private static void ApplyActorRelocating(byte[] rom, RomProfile profile, RomActorEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene);
        int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene.");
        int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "room table");
        uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); int remaining = edit.ActorIndex;
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8)
        {
            byte command = room[p]; if (command == 0x14) break; if (command != 0x01 || (U32(room, p + 4) >> 24) != 3) continue;
            int count = room[p + 1], data = checked((int)(U32(room, p + 4) & 0x00FFFFFF)); EnsureRange(room, data, count * 16, "room actor list");
            if (remaining >= count) { remaining -= count; continue; }
            WriteActor(room, data + remaining * 16, edit.Actor);
            byte[] replacement = EncodeStoredRange(storedRoom, room); if (replacement.Length <= roomEnd - roomStart) { WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
            int relocatedRoom = FindZeroRange(rom, replacement.Length, checked((int)roomEnd), checked((int)roomStart), checked((int)roomEnd)); if (relocatedRoom < 0) throw new InvalidDataException("Edited room needs more space but no safe free ROM range was found for relocation.");
            Buffer.BlockCopy(replacement, 0, rom, relocatedRoom, replacement.Length); Put32(scene, roomEntry, checked((uint)relocatedRoom)); Put32(scene, roomEntry + 4, checked((uint)(relocatedRoom + replacement.Length))); WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene); return;
        }
        throw new InvalidDataException("Patch actor index was not found in the room.");
    }

    private static void ApplyActor(byte[] rom, RomProfile profile, RomActorEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4);
        byte[] scene = ReadDecodedRange(rom, sceneStart, sceneEnd, "scene"); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene.");
        int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4);
        byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); int remaining = edit.ActorIndex;
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8)
        {
            byte command = room[p]; if (command == 0x14) break; if (command != 0x01 || (U32(room, p + 4) >> 24) != 3) continue;
            int count = room[p + 1], data = checked((int)(U32(room, p + 4) & 0x00FFFFFF));
            if (data < 0 || data + count * 16 > room.Length) throw new InvalidDataException("Room actor list is outside the decoded room.");
            if (remaining < count) { WriteActor(room, data + remaining * 16, edit.Actor); WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
            remaining -= count;
        }
        throw new InvalidDataException("Patch actor index was not found in the room.");
    }

    private static void ApplyObjectRelocating(byte[] rom, RomProfile profile, RomObjectEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene."); int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "room table");
        uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); int remaining = edit.ObjectIndex;
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8)
        {
            byte command = room[p]; if (command == 0x14) break; if (command != 0x0B || (U32(room, p + 4) >> 24) != 3) continue;
            int count = room[p + 1], data = checked((int)(U32(room, p + 4) & 0x00FFFFFF)); EnsureRange(room, data, count * 2, "room object list"); if (remaining >= count) { remaining -= count; continue; }
            Put16(room, data + remaining * 2, edit.ObjectId); byte[] replacement = EncodeStoredRange(storedRoom, room); if (replacement.Length <= roomEnd - roomStart) { WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
            int relocatedRoom = FindZeroRange(rom, replacement.Length, checked((int)roomEnd), checked((int)roomStart), checked((int)roomEnd)); if (relocatedRoom < 0) throw new InvalidDataException("Edited room needs more space but no safe free ROM range was found for relocation."); Buffer.BlockCopy(replacement, 0, rom, relocatedRoom, replacement.Length); Put32(scene, roomEntry, checked((uint)relocatedRoom)); Put32(scene, roomEntry + 4, checked((uint)(relocatedRoom + replacement.Length))); WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene); return;
        }
        throw new InvalidDataException("Patch room object index was not found in the room.");
    }

    private static void ApplyObjectListRelocating(byte[] rom, RomProfile profile, RomObjectListEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene."); int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "room table");
        uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); bool found = false;
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8)
        {
            if (room[p] != 0x0B || (U32(room, p + 4) >> 24) != 3) { if (room[p] == 0x14) break; continue; }
            int data = checked((int)(U32(room, p + 4) & 0x00FFFFFF)); int bytes = checked(edit.ObjectIds.Count * 2); int target = data;
            if (bytes > 0 && (data < 0 || data + bytes > room.Length)) { target = FindZeroRange(room, bytes); if (target < 0) throw new InvalidDataException("Room has no safe free range for the expanded object list."); Put32(room, p + 4, (uint)(0x03000000 | target)); }
            room[p + 1] = checked((byte)edit.ObjectIds.Count); EnsureRange(room, target, bytes, "room object list"); for (int i = 0; i < edit.ObjectIds.Count; i++) Put16(room, target + i * 2, edit.ObjectIds[i]); found = true; break;
        }
        if (!found) throw new InvalidDataException("Room has no object command.");
        byte[] replacement = EncodeStoredRange(storedRoom, room); if (replacement.Length <= roomEnd - roomStart) { WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
        int relocatedRoom = FindZeroRange(rom, replacement.Length, checked((int)roomEnd), checked((int)roomStart), checked((int)roomEnd)); if (relocatedRoom < 0) throw new InvalidDataException("Edited room needs more space but no safe free ROM range was found for relocation."); Buffer.BlockCopy(replacement, 0, rom, relocatedRoom, replacement.Length); Put32(scene, roomEntry, checked((uint)relocatedRoom)); Put32(scene, roomEntry + 4, checked((uint)(relocatedRoom + replacement.Length))); WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static void ApplyActorListRelocating(byte[] rom, RomProfile profile, RomActorListEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene."); int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "room table");
        uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); bool found = false;
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8)
        {
            if (room[p] != 0x01 || (U32(room, p + 4) >> 24) != 3) { if (room[p] == 0x14) break; continue; }
            int data = checked((int)(U32(room, p + 4) & 0x00FFFFFF)); int bytes = checked(edit.Actors.Count * 16); int target = data;
            if (bytes > 0 && (data < 0 || data + bytes > room.Length)) { target = FindZeroRange(room, bytes); if (target < 0) throw new InvalidDataException("Room has no safe free range for the expanded actor list."); Put32(room, p + 4, (uint)(0x03000000 | target)); }
            room[p + 1] = checked((byte)edit.Actors.Count); EnsureRange(room, target, bytes, "room actor list"); for (int i = 0; i < edit.Actors.Count; i++) WriteActor(room, target + i * 16, edit.Actors[i]); found = true; break;
        }
        if (!found) throw new InvalidDataException("Room has no actor command.");
        byte[] replacement = EncodeStoredRange(storedRoom, room); if (replacement.Length <= roomEnd - roomStart) { WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
        int relocatedRoom = FindZeroRange(rom, replacement.Length, checked((int)roomEnd), checked((int)roomStart), checked((int)roomEnd)); if (relocatedRoom < 0) throw new InvalidDataException("Edited room needs more space but no safe free ROM range was found for relocation."); Buffer.BlockCopy(replacement, 0, rom, relocatedRoom, replacement.Length); Put32(scene, roomEntry, checked((uint)relocatedRoom)); Put32(scene, roomEntry + 4, checked((uint)(relocatedRoom + replacement.Length))); WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static int FindZeroRange(byte[] data, int length)
    {
        for (int start = 0; start + length <= data.Length; start = (start + 7) & ~7)
        {
            bool clear = true; for (int i = 0; i < length; i++) if (data[start + i] != 0) { clear = false; break; }
            if (clear) return start;
        }
        return -1;
    }

    private static void ApplyRoomOrder(byte[] rom, RomProfile profile, RomRoomOrderEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint start = U32(rom, sceneRow), end = U32(rom, sceneRow + 4); byte[] stored = StoredRange(rom, start, end, "scene"); byte[] scene = N64Compression.Decode(stored); int command = FindRoomTable(scene); if (command < 0) throw new InvalidDataException("Scene has no supported room table."); int count = scene[command + 1];
        if (edit.Order.Count != count || edit.Order.Distinct().Count() != count || edit.Order.Any(i => i < 0 || i >= count)) throw new InvalidDataException("Room order must be a complete permutation.");
        int table = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); byte[] original = new byte[count * 8]; EnsureRange(scene, table, original.Length, "room table"); Buffer.BlockCopy(scene, table, original, 0, original.Length);
        for (int i = 0; i < count; i++) Buffer.BlockCopy(original, edit.Order[i] * 8, scene, table + i * 8, 8);
        WriteStoredRange(rom, start, end, stored, scene);
    }

    private static void ApplyAlternateRoomOrder(byte[] rom, RomProfile profile, RomAlternateRoomOrderEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount || edit.HeaderIndex < 1 || edit.HeaderIndex > 32) throw new InvalidDataException("Alternate room order address is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint start = U32(rom, sceneRow), end = U32(rom, sceneRow + 4); byte[] stored = StoredRange(rom, start, end, "scene"); byte[] scene = N64Compression.Decode(stored);
        int headerCommand = FindCommand(scene, 0x18); if (headerCommand < 0) throw new InvalidDataException("Scene has no alternate headers."); int headerTable = checked((int)(U32(scene, headerCommand + 4) & 0x00FFFFFF)); int headerSlot = checked(headerTable + (edit.HeaderIndex - 1) * 4); EnsureRange(scene, headerSlot, 4, "alternate header table"); uint pointer = U32(scene, headerSlot); if (pointer == 0) throw new InvalidDataException("Alternate header is empty."); int header = checked((int)(pointer & 0x00FFFFFF)); int roomCommand = FindCommandAt(scene, header, 0x04); if (roomCommand < 0) throw new InvalidDataException("Alternate header has no supported room table."); int count = scene[roomCommand + 1]; if (edit.Order.Count != count || edit.Order.Distinct().Count() != count || edit.Order.Any(index => index < 0 || index >= count)) throw new InvalidDataException("Alternate room order must be a complete permutation.");
        int table = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); byte[] original = new byte[checked(count * 8)]; EnsureRange(scene, table, original.Length, "alternate room table"); Buffer.BlockCopy(scene, table, original, 0, original.Length); for (int index = 0; index < count; index++) Buffer.BlockCopy(original, edit.Order[index] * 8, scene, table + index * 8, 8); WriteSceneWithRelocation(rom, sceneRow, start, end, stored, scene);
    }

    private static void ApplySceneTopologyRelocating(byte[] rom, RomProfile profile, RomSceneTopologyEdit edit)
    {
        if (edit.Slots is { } slots && edit.Order is { } order)
        {
            if (slots.Count == 0 || order.Count != slots.Count || slots.Distinct().Count() != slots.Count || order.Distinct().Count() != order.Count || slots.Any(id => id < 0 || id >= profile.SceneCount) || order.Any(id => !slots.Contains(id))) throw new InvalidDataException("Scene order must be a complete permutation of the loaded scene slots.");
            byte[] original = new byte[checked(slots.Count * 20)]; for (int index = 0; index < slots.Count; index++) Buffer.BlockCopy(rom, checked((int)profile.SceneTable + slots[index] * 20), original, index * 20, 20);
            for (int index = 0; index < slots.Count; index++) { int sourceIndex = Array.IndexOf(slots.ToArray(), order[index]); Buffer.BlockCopy(original, sourceIndex * 20, rom, checked((int)profile.SceneTable + slots[index] * 20), 20); }
            return;
        }
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Scene topology target is outside the profile.");
        int targetRow = checked((int)profile.SceneTable + edit.SceneId * 20); EnsureRange(rom, targetRow, 20, "scene table entry");
        if (edit.Delete)
        {
            if (U32(rom, targetRow) == 0 && U32(rom, targetRow + 4) == 0) throw new InvalidDataException("Scene delete target is already empty.");
            Array.Clear(rom, targetRow, 20); return;
        }
        if (edit.SourceSceneId < 0 || edit.SourceSceneId >= profile.SceneCount || edit.SourceSceneId == edit.SceneId) throw new InvalidDataException("Scene clone source is outside the profile.");
        int sourceRow = checked((int)profile.SceneTable + edit.SourceSceneId * 20); EnsureRange(rom, sourceRow, 20, "source scene table entry");
        if (U32(rom, targetRow) != 0 || U32(rom, targetRow + 4) != 0) throw new InvalidDataException("Scene clone target is already occupied.");
        uint sourceStart = U32(rom, sourceRow), sourceEnd = U32(rom, sourceRow + 4); byte[] sourceStored = StoredRange(rom, sourceStart, sourceEnd, "source scene");
        int cloneStart = FindZeroRange(rom, sourceStored.Length, checked((int)sourceEnd), checked((int)profile.SceneTable), checked((int)profile.SceneTableEnd));
        if (cloneStart < 0) throw new InvalidDataException("Cloned scene needs free ROM space but no safe range was found.");
        Buffer.BlockCopy(sourceStored, 0, rom, cloneStart, sourceStored.Length); Buffer.BlockCopy(rom, sourceRow, rom, targetRow, 20);
        Put32(rom, targetRow, checked((uint)cloneStart)); Put32(rom, targetRow + 4, checked((uint)(cloneStart + sourceStored.Length)));
    }

    private static void ApplyRoomTopologyRelocating(byte[] rom, RomProfile profile, RomRoomTopologyEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Room topology scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4);
        byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene); int roomCommand = FindRoomTable(scene);
        if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        int count = scene[roomCommand + 1]; if (count == 0) throw new InvalidDataException("Scene has no rooms.");
        int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); EnsureRange(scene, roomTable, checked(count * 8), "room table");
        if (edit.Delete)
        {
            if (count <= 1) throw new InvalidDataException("A scene must retain at least one room.");
            if (edit.RoomId < 0 || edit.RoomId >= count) throw new InvalidDataException("Room delete index is outside the scene.");
            for (int index = edit.RoomId; index < count - 1; index++) Buffer.BlockCopy(scene, roomTable + (index + 1) * 8, scene, roomTable + index * 8, 8);
            scene[roomCommand + 1] = checked((byte)(count - 1)); WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene); return;
        }

        if (edit.SourceRoomId < 0 || edit.SourceRoomId >= count) throw new InvalidDataException("Room clone source is outside the scene.");
        if (edit.RoomId < 0 || edit.RoomId > count) throw new InvalidDataException("Room insertion index is outside the scene.");
        uint sourceStart = U32(scene, roomTable + edit.SourceRoomId * 8), sourceEnd = U32(scene, roomTable + edit.SourceRoomId * 8 + 4);
        byte[] sourceStored = StoredRange(rom, sourceStart, sourceEnd, "source room"); int cloneStart = FindZeroRange(rom, sourceStored.Length, checked((int)sourceEnd), checked((int)sceneStart), checked((int)sceneEnd));
        if (cloneStart < 0) throw new InvalidDataException("Cloned room needs free ROM space but no safe range was found.");
        Buffer.BlockCopy(sourceStored, 0, rom, cloneStart, sourceStored.Length);
        int newTable = Align(scene.Length, 8); int newCount = checked(count + 1); if (newCount > byte.MaxValue) throw new InvalidDataException("A scene cannot contain more than 255 rooms.");
        int oldTable = roomTable; byte[] oldEntries = new byte[checked(count * 8)]; Buffer.BlockCopy(scene, oldTable, oldEntries, 0, oldEntries.Length);
        Array.Resize(ref scene, checked(newTable + newCount * 8));
        for (int index = 0; index < newCount; index++)
        {
            int entry = newTable + index * 8;
            if (index == edit.RoomId) { Put32(scene, entry, checked((uint)cloneStart)); Put32(scene, entry + 4, checked((uint)(cloneStart + sourceStored.Length))); }
            else Buffer.BlockCopy(oldEntries, (index < edit.RoomId ? index : index - 1) * 8, scene, entry, 8);
        }
        uint tableSegment = U32(scene, roomCommand + 4) & 0xFF000000; Put32(scene, roomCommand + 4, tableSegment | checked((uint)newTable)); scene[roomCommand + 1] = checked((byte)newCount);
        WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static void ApplyObject(byte[] rom, RomProfile profile, RomObjectEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] scene = ReadDecodedRange(rom, sceneStart, sceneEnd, "scene"); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene.");
        int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); int remaining = edit.ObjectIndex;
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8)
        {
            byte command = room[p]; if (command == 0x14) break; if (command != 0x0B || (U32(room, p + 4) >> 24) != 3) continue;
            int count = room[p + 1], data = checked((int)(U32(room, p + 4) & 0x00FFFFFF)); EnsureRange(room, data, count * 2, "room object list");
            if (remaining < count) { Put16(room, data + remaining * 2, edit.ObjectId); WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
            remaining -= count;
        }
        throw new InvalidDataException("Patch room object index was not found in the room.");
    }

    private static void ApplyRoomSettingsRelocating(byte[] rom, RomProfile profile, RomRoomSettingsEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene."); int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "room table");
        uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); bool found = false;
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8)
        {
            byte command = room[p]; if (command == 0x14) break;
            if (command == 0x05) { Put32(room, p, edit.Settings.Wind); found = true; }
            else if (command == 0x08) { Put32(room, p, edit.Settings.Behavior); found = true; }
            else if (command == 0x10) { Put16(room, p + 4, edit.Settings.StartTime); room[p + 6] = edit.Settings.TimeSpeed; found = true; }
            else if (command == 0x12) { room[p + 4] = (byte)(edit.Settings.SkyboxFlags & 1); room[p + 5] = (byte)((edit.Settings.SkyboxFlags >> 1) & 1); found = true; }
            else if (command == 0x16) { room[p + 7] = edit.Settings.Echo; found = true; }
        }
        if (!found) throw new InvalidDataException("Room has no supported settings commands.");
        byte[] replacement = EncodeStoredRange(storedRoom, room); if (replacement.Length <= roomEnd - roomStart) { WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
        int relocatedRoom = FindZeroRange(rom, replacement.Length, checked((int)roomEnd), checked((int)roomStart), checked((int)roomEnd)); if (relocatedRoom < 0) throw new InvalidDataException("Edited room needs more space but no safe free ROM range was found for relocation."); Buffer.BlockCopy(replacement, 0, rom, relocatedRoom, replacement.Length); Put32(scene, roomEntry, checked((uint)relocatedRoom)); Put32(scene, roomEntry + 4, checked((uint)(relocatedRoom + replacement.Length))); WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static void ApplyRoomSettings(byte[] rom, RomProfile profile, RomRoomSettingsEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] scene = ReadDecodedRange(rom, sceneStart, sceneEnd, "scene"); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table.");
        if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene."); int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom);
        bool behavior = false, wind = false, time = false, skybox = false, echo = false;
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8)
        {
            byte command = room[p]; if (command == 0x14) break;
            if (command == 0x05) { Put32(room, p, edit.Settings.Wind); wind = true; } else if (command == 0x08) { Put32(room, p, edit.Settings.Behavior); behavior = true; } else if (command == 0x10) { Put16(room, p + 4, edit.Settings.StartTime); room[p + 6] = edit.Settings.TimeSpeed; time = true; } else if (command == 0x12) { room[p + 4] = (byte)(edit.Settings.SkyboxFlags & 1); room[p + 5] = (byte)((edit.Settings.SkyboxFlags >> 1) & 1); skybox = true; } else if (command == 0x16) { room[p + 7] = edit.Settings.Echo; echo = true; }
        }
        if (!(behavior || wind || time || skybox || echo)) throw new InvalidDataException("Room has no supported settings commands."); WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room);
    }

    private static void ApplyRoomExit(byte[] rom, RomProfile profile, RomRoomExitEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile."); int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] scene = ReadDecodedRange(rom, sceneStart, sceneEnd, "scene"); int roomCommand = FindRoomTable(scene); if (roomCommand < 0) throw new InvalidDataException("Scene has no supported room table."); if (edit.RoomId < 0 || edit.RoomId >= scene[roomCommand + 1]) throw new InvalidDataException("Patch room index is outside the scene."); int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom);
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8) { if (room[p] == 0x14) break; if (room[p] != 0x08 || (U32(room, p + 4) >> 24) != 3) continue; int count = room[p + 1], data = checked((int)(U32(room, p + 4) & 0x00FFFFFF)); if (edit.ExitIndex < 0 || edit.ExitIndex >= count) throw new InvalidDataException("Patch room exit index is outside the room."); EnsureRange(room, data + edit.ExitIndex * 2, 2, "room exit list"); Put16(room, data + edit.ExitIndex * 2, edit.Raw); WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
        throw new InvalidDataException("Room has no supported exit command.");
    }

    private static void ApplyCamera(byte[] rom, RomProfile profile, RomCameraEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile."); int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] scene = ReadDecodedRange(rom, sceneStart, sceneEnd, "scene"); int command = FindCommand(scene, 0x02); if (command < 0) throw new InvalidDataException("Scene has no supported collision command."); int collision = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); EnsureRange(scene, collision, 44, "collision header"); int table = checked((int)(U32(scene, collision + 32) & 0x00FFFFFF)); if (table <= 0 || edit.CameraIndex < 0) throw new InvalidDataException("Scene collision has no camera table."); int entry = checked(table + edit.CameraIndex * 8); EnsureRange(scene, entry, 8, "camera table"); int data = checked((int)(U32(scene, entry + 4) & 0x00FFFFFF)); Put16(scene, entry, edit.Camera.Type); Put16(scene, entry + 2, unchecked((ushort)edit.Camera.DataCount)); if (edit.Camera.PathPoints is { Count: > 0 } pathPoints) { if (edit.Camera.DataCount != pathPoints.Count) throw new InvalidDataException("Camera path point count must match the camera page count."); EnsureRange(scene, data, checked(pathPoints.Count * 6), "camera path data"); for (int point = 0; point < pathPoints.Count; point++) { int p = data + point * 6; Put16(scene, p, unchecked((ushort)pathPoints[point].X)); Put16(scene, p + 2, unchecked((ushort)pathPoints[point].Y)); Put16(scene, p + 4, unchecked((ushort)pathPoints[point].Z)); } } else { EnsureRange(scene, data, 18, "camera record"); Put16(scene, data, unchecked((ushort)edit.Camera.X)); Put16(scene, data + 2, unchecked((ushort)edit.Camera.Y)); Put16(scene, data + 4, unchecked((ushort)edit.Camera.Z)); Put16(scene, data + 6, unchecked((ushort)edit.Camera.RotationX)); Put16(scene, data + 8, unchecked((ushort)edit.Camera.RotationY)); Put16(scene, data + 10, unchecked((ushort)edit.Camera.RotationZ)); Put16(scene, data + 12, unchecked((ushort)edit.Camera.Fov)); Put16(scene, data + 14, edit.Camera.Unknown1); Put16(scene, data + 16, edit.Camera.Unknown2); } WriteStoredRange(rom, sceneStart, sceneEnd, IsCompressed(StoredRange(rom, sceneStart, sceneEnd, "scene")) ? StoredRange(rom, sceneStart, sceneEnd, "scene") : scene, scene);
    }

    private static void ApplySceneSettings(byte[] rom, RomProfile profile, RomSceneSettingsEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4);
        byte[] stored = StoredRange(rom, sceneStart, sceneEnd, "scene"); bool compressed = IsCompressed(stored); byte[] scene = compressed ? N64Compression.Decode(stored) : (byte[])stored.Clone();
        int command = FindCommand(scene, 0x19); if (command < 0) throw new InvalidDataException("Scene has no supported settings command.");
        EnsureRange(scene, command, 8, "scene settings"); scene[command + 1] = edit.Settings.CameraMovement; scene[command + 7] = edit.Settings.WorldMap;
        if (!compressed) { Buffer.BlockCopy(scene, 0, rom, checked((int)sceneStart), scene.Length); return; }
        byte[] replacement = EncodeStoredRange(stored, scene); int capacity = checked((int)(sceneEnd - sceneStart));
        if (replacement.Length <= capacity) { WriteStoredRange(rom, sceneStart, sceneEnd, stored, scene); return; }
        int relocatedStart = FindZeroRange(rom, replacement.Length, checked((int)sceneEnd), checked((int)sceneStart), checked((int)sceneEnd));
        if (relocatedStart < 0) throw new InvalidDataException($"Edited compressed scene needs {replacement.Length} bytes but no safe free ROM range was found for relocation.");
        Buffer.BlockCopy(replacement, 0, rom, relocatedStart, replacement.Length); Put32(rom, sceneRow, checked((uint)relocatedStart)); Put32(rom, sceneRow + 4, checked((uint)(relocatedStart + replacement.Length)));
    }

    private static void ApplyAlternateHeader(byte[] rom, RomProfile profile, RomAlternateHeaderEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4);
        byte[] stored = StoredRange(rom, sceneStart, sceneEnd, "scene"); bool compressed = IsCompressed(stored); byte[] scene = compressed ? N64Compression.Decode(stored) : (byte[])stored.Clone();
        int command = FindCommand(scene, 0x18); if (command < 0) throw new InvalidDataException("Scene has no supported alternate-header command.");
        if (edit.HeaderIndex < 1 || edit.HeaderIndex > 32) throw new InvalidDataException("Alternate header index is outside the supported range.");
        int table = checked((int)(U32(scene, command + 4) & 0x00FFFFFF)); int slot = checked(table + (edit.HeaderIndex - 1) * 4); EnsureRange(scene, slot, 4, "alternate header table");
        if (!edit.Delete && edit.Pointer == 0) throw new InvalidDataException("Alternate header pointer is empty.");
        Put32(scene, slot, edit.Delete ? 0u : edit.Pointer);
        if (!compressed) { Buffer.BlockCopy(scene, 0, rom, checked((int)sceneStart), scene.Length); return; }
        byte[] replacement = EncodeStoredRange(stored, scene); int capacity = checked((int)(sceneEnd - sceneStart));
        if (replacement.Length <= capacity) { WriteStoredRange(rom, sceneStart, sceneEnd, stored, scene); return; }
        int relocatedStart = FindZeroRange(rom, replacement.Length, checked((int)sceneEnd), checked((int)sceneStart), checked((int)sceneEnd));
        if (relocatedStart < 0) throw new InvalidDataException($"Edited compressed scene needs {replacement.Length} bytes but no safe free ROM range was found for relocation.");
        Buffer.BlockCopy(replacement, 0, rom, relocatedStart, replacement.Length); Put32(rom, sceneRow, checked((uint)relocatedStart)); Put32(rom, sceneRow + 4, checked((uint)(relocatedStart + replacement.Length)));
    }

    private static void ApplySceneCommand(byte[] rom, RomProfile profile, RomSceneCommandEdit edit) { int row = checked((int)profile.SceneTable + edit.SceneId * 20); uint start = U32(rom, row), end = U32(rom, row + 4); byte[] stored = StoredRange(rom, start, end, "scene"); byte[] scene = N64Compression.Decode(stored); int offset = checked(edit.CommandIndex * 8); EnsureRange(scene, offset, 8, "scene command"); if (edit.Insert) { int terminator = 0; while (terminator + 8 <= scene.Length && scene[terminator] != 0x14 && terminator < 8192) terminator += 8; if (terminator < offset) throw new InvalidDataException("Scene command insertion index is outside the command list."); EnsureRange(scene, terminator, 8, "scene command terminator"); for (int p = terminator; p >= offset; p -= 8) Buffer.BlockCopy(scene, p, scene, p + 8, 8); scene[offset] = edit.Command.Command; scene[offset + 1] = edit.Command.Parameter; Put32(scene, offset + 4, edit.Command.Pointer); } else if (edit.Delete) { if (scene[offset] == 0x14) throw new InvalidDataException("Scene command delete points at the terminator."); int terminator = offset; while (terminator + 8 <= scene.Length && scene[terminator] != 0x14 && terminator < 8192) terminator += 8; if (terminator != offset + 8) throw new InvalidDataException("Only the final scene command can be deleted safely."); Array.Clear(scene, offset, 8); scene[offset] = 0x14; } else { if (scene[offset] == 0x14) throw new InvalidDataException("Scene command index points at the terminator."); scene[offset] = edit.Command.Command; scene[offset + 1] = edit.Command.Parameter; Put32(scene, offset + 4, edit.Command.Pointer); } WriteSceneWithRelocation(rom, row, start, end, stored, scene); }

    private static void ApplyAlternateCommandRelocating(byte[] rom, RomProfile profile, RomAlternateCommandEdit edit)
    {
        int row = checked((int)profile.SceneTable + edit.SceneId * 20); uint start = U32(rom, row), end = U32(rom, row + 4);
        byte[] stored = StoredRange(rom, start, end, "scene"); byte[] scene = N64Compression.Decode(stored);
        int tableCommand = FindCommand(scene, 0x18); if (tableCommand < 0) throw new InvalidDataException("Scene has no alternate headers.");
        int table = checked((int)(U32(scene, tableCommand + 4) & 0x00FFFFFF)); uint pointer = U32(scene, checked(table + (edit.HeaderIndex - 1) * 4)); if (pointer == 0) throw new InvalidDataException("Alternate header is empty.");
        int header = checked((int)(pointer & 0x00FFFFFF)); int command = checked(header + edit.CommandIndex * 8); EnsureRange(scene, command, 8, "alternate header command");
        if (edit.Insert)
        {
            int terminator = header; while (terminator + 8 <= scene.Length && scene[terminator] != 0x14 && terminator < header + 8192) terminator += 8;
            if (terminator < command) throw new InvalidDataException("Alternate command insertion index is outside the command list."); EnsureRange(scene, terminator, 8, "alternate command terminator");
            for (int p = terminator; p >= command; p -= 8) Buffer.BlockCopy(scene, p, scene, p + 8, 8);
            scene[command] = edit.Command.Command; scene[command + 1] = edit.Command.Parameter; Put32(scene, command + 4, edit.Command.Pointer);
        }
        else if (edit.Delete)
        {
            if (scene[command] == 0x14) throw new InvalidDataException("Alternate command delete points at the terminator.");
            int terminator = command; while (terminator + 8 <= scene.Length && scene[terminator] != 0x14 && terminator < header + 8192) terminator += 8;
            if (terminator != command + 8) throw new InvalidDataException("Only the final alternate command can be deleted safely."); Array.Clear(scene, command, 8); scene[command] = 0x14;
        }
        else
        {
            if (scene[command] == 0x14) throw new InvalidDataException("Alternate header command index points at the terminator.");
            scene[command] = edit.Command.Command; scene[command + 1] = edit.Command.Parameter; Put32(scene, command + 4, edit.Command.Pointer);
        }
        WriteSceneWithRelocation(rom, row, start, end, stored, scene);
    }

    private static void ApplyAlternateRoomTopologyRelocating(byte[] rom, RomProfile profile, RomAlternateRoomTopologyEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount || edit.HeaderIndex < 1 || edit.HeaderIndex > 32) throw new InvalidDataException("Alternate room topology address is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene);
        int headerTableCommand = FindCommand(scene, 0x18); if (headerTableCommand < 0) throw new InvalidDataException("Scene has no alternate headers."); int headerTable = checked((int)(U32(scene, headerTableCommand + 4) & 0x00FFFFFF)); int headerSlot = checked(headerTable + (edit.HeaderIndex - 1) * 4); EnsureRange(scene, headerSlot, 4, "alternate header table"); uint headerPointer = U32(scene, headerSlot); if (headerPointer == 0) throw new InvalidDataException("Alternate header is empty.");
        int header = checked((int)(headerPointer & 0x00FFFFFF)); int roomCommand = FindCommandAt(scene, header, 0x04); if (roomCommand < 0) throw new InvalidDataException("Alternate header has no supported room table."); int count = scene[roomCommand + 1]; if (count == 0) throw new InvalidDataException("Alternate header has no rooms."); int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); EnsureRange(scene, roomTable, checked(count * 8), "alternate room table");
        if (edit.Delete)
        {
            if (count <= 1) throw new InvalidDataException("An alternate header must retain at least one room."); if (edit.RoomId < 0 || edit.RoomId >= count) throw new InvalidDataException("Alternate room delete index is outside the header.");
            for (int index = edit.RoomId; index < count - 1; index++) Buffer.BlockCopy(scene, roomTable + (index + 1) * 8, scene, roomTable + index * 8, 8);
            scene[roomCommand + 1] = checked((byte)(count - 1)); WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene); return;
        }
        if (edit.SourceRoomId < 0 || edit.SourceRoomId >= count) throw new InvalidDataException("Alternate room clone source is outside the header."); if (edit.RoomId != count) throw new InvalidDataException("Native alternate room cloning currently appends the clone to the room table.");
        uint sourceStart = U32(scene, roomTable + edit.SourceRoomId * 8), sourceEnd = U32(scene, roomTable + edit.SourceRoomId * 8 + 4); byte[] sourceStored = StoredRange(rom, sourceStart, sourceEnd, "alternate source room"); int cloneStart = FindZeroRange(rom, sourceStored.Length, checked((int)sourceEnd), checked((int)sceneStart), checked((int)sceneEnd)); if (cloneStart < 0) throw new InvalidDataException("Cloned alternate room needs free ROM space but no safe range was found.");
        Buffer.BlockCopy(sourceStored, 0, rom, cloneStart, sourceStored.Length); int newTable = Align(scene.Length, 8); int newCount = checked(count + 1); if (newCount > byte.MaxValue) throw new InvalidDataException("An alternate header cannot contain more than 255 rooms."); int oldTable = roomTable; Array.Resize(ref scene, checked(newTable + newCount * 8)); Buffer.BlockCopy(scene, oldTable, scene, newTable, count * 8); uint tableSegment = U32(scene, roomCommand + 4) & 0xFF000000; Put32(scene, roomCommand + 4, tableSegment | checked((uint)newTable)); Put32(scene, newTable + count * 8, checked((uint)cloneStart)); Put32(scene, newTable + count * 8 + 4, checked((uint)(cloneStart + sourceStored.Length))); scene[roomCommand + 1] = checked((byte)newCount);
        WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static void ApplyAlternateSceneSettings(byte[] rom, RomProfile profile, RomAlternateSceneSettingsEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4);
        byte[] stored = StoredRange(rom, sceneStart, sceneEnd, "scene"); bool compressed = IsCompressed(stored); byte[] scene = compressed ? N64Compression.Decode(stored) : (byte[])stored.Clone();
        int tableCommand = FindCommand(scene, 0x18); if (tableCommand < 0) throw new InvalidDataException("Scene has no supported alternate-header command.");
        if (edit.HeaderIndex < 1 || edit.HeaderIndex > 32) throw new InvalidDataException("Alternate header index is outside the supported range.");
        int table = checked((int)(U32(scene, tableCommand + 4) & 0x00FFFFFF)); int slot = checked(table + (edit.HeaderIndex - 1) * 4); EnsureRange(scene, slot, 4, "alternate header table"); uint pointer = U32(scene, slot); if (pointer == 0) throw new InvalidDataException("Alternate header is empty.");
        int header = checked((int)(pointer & 0x00FFFFFF)); int command = FindCommandAt(scene, header, 0x19); if (command < 0) throw new InvalidDataException("Alternate header has no supported settings command.");
        EnsureRange(scene, command, 8, "alternate scene settings"); scene[command + 1] = edit.Settings.CameraMovement; scene[command + 7] = edit.Settings.WorldMap;
        WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, stored, scene);
    }

    private static void ApplyAlternateActor(byte[] rom, RomProfile profile, RomAlternateActorEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] scene = ReadDecodedRange(rom, sceneStart, sceneEnd, "scene");
        int headerTableCommand = FindCommand(scene, 0x18); if (headerTableCommand < 0) throw new InvalidDataException("Scene has no supported alternate-header command."); if (edit.HeaderIndex < 1 || edit.HeaderIndex > 32) throw new InvalidDataException("Alternate header index is outside the supported range.");
        int headerTable = checked((int)(U32(scene, headerTableCommand + 4) & 0x00FFFFFF)); int headerSlot = checked(headerTable + (edit.HeaderIndex - 1) * 4); EnsureRange(scene, headerSlot, 4, "alternate header table"); uint headerPointer = U32(scene, headerSlot); if (headerPointer == 0) throw new InvalidDataException("Alternate header is empty.");
        int roomCommand = FindCommandAt(scene, checked((int)(headerPointer & 0x00FFFFFF)), 0x04); if (roomCommand < 0) throw new InvalidDataException("Alternate header has no supported room table."); int roomCount = scene[roomCommand + 1]; if (edit.RoomId < 0 || edit.RoomId >= roomCount) throw new InvalidDataException("Alternate room index is outside the header.");
        int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "alternate room table"); uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom);
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8) { if (room[p] == 0x14) break; if (room[p] != 0x01 || (U32(room, p + 4) >> 24) != 3) continue; int count = room[p + 1]; if (edit.ActorIndex < 0 || edit.ActorIndex >= count) throw new InvalidDataException("Alternate actor index is outside the room."); int data = checked((int)(U32(room, p + 4) & 0x00FFFFFF)); int actorOffset = checked(data + edit.ActorIndex * 16); EnsureRange(room, actorOffset, 16, "alternate actor list"); WriteActor(room, actorOffset, edit.Actor); WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
        throw new InvalidDataException("Alternate room has no supported actor command.");
    }

    private static void ApplyAlternateActorListRelocating(byte[] rom, RomProfile profile, RomAlternateActorListEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        if (edit.HeaderIndex < 1 || edit.HeaderIndex > 32) throw new InvalidDataException("Alternate header index is outside the supported range.");
        if (edit.Actors is null || edit.Actors.Count > byte.MaxValue) throw new InvalidDataException("Alternate actor list count is outside the native range.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4);
        byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene"); byte[] scene = N64Compression.Decode(storedScene);
        int headerTableCommand = FindCommand(scene, 0x18); if (headerTableCommand < 0) throw new InvalidDataException("Scene has no supported alternate-header command.");
        int headerTable = checked((int)(U32(scene, headerTableCommand + 4) & 0x00FFFFFF)); int headerSlot = checked(headerTable + (edit.HeaderIndex - 1) * 4); EnsureRange(scene, headerSlot, 4, "alternate header table");
        uint headerPointer = U32(scene, headerSlot); if (headerPointer == 0) throw new InvalidDataException("Alternate header is empty.");
        int roomCommand = FindCommandAt(scene, checked((int)(headerPointer & 0x00FFFFFF)), 0x04); if (roomCommand < 0) throw new InvalidDataException("Alternate header has no supported room table.");
        int roomCount = scene[roomCommand + 1]; if (edit.RoomId < 0 || edit.RoomId >= roomCount) throw new InvalidDataException("Alternate room index is outside the header.");
        int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF)); int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "alternate room table");
        uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4); byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); bool found = false;
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8)
        {
            if (room[p] == 0x14) break;
            if (room[p] != 0x01 || (U32(room, p + 4) >> 24) != 3) continue;
            int data = checked((int)(U32(room, p + 4) & 0x00FFFFFF)); int bytes = checked(edit.Actors.Count * 16); int target = data;
            if (bytes > 0 && (data < 0 || data + bytes > room.Length))
            {
                target = FindZeroRange(room, bytes); if (target < 0) throw new InvalidDataException("Alternate room has no safe free range for the expanded actor list.");
                Put32(room, p + 4, checked(0x03000000u | (uint)target));
            }
            room[p + 1] = checked((byte)edit.Actors.Count); EnsureRange(room, target, bytes, "alternate room actor list");
            for (int i = 0; i < edit.Actors.Count; i++) WriteActor(room, target + i * 16, edit.Actors[i]); found = true; break;
        }
        if (!found) throw new InvalidDataException("Alternate room has no actor command.");
        byte[] replacement = EncodeStoredRange(storedRoom, room); if (replacement.Length <= roomEnd - roomStart) { WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
        int relocatedRoom = FindZeroRange(rom, replacement.Length, checked((int)roomEnd), checked((int)roomStart), checked((int)roomEnd)); if (relocatedRoom < 0) throw new InvalidDataException("Edited alternate room needs more space but no safe free ROM range was found for relocation.");
        Buffer.BlockCopy(replacement, 0, rom, relocatedRoom, replacement.Length); Put32(scene, roomEntry, checked((uint)relocatedRoom)); Put32(scene, roomEntry + 4, checked((uint)(relocatedRoom + replacement.Length))); WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static byte[] AlternateRoom(byte[] rom, RomProfile profile, int sceneId, int headerIndex, int roomId, out uint roomStart, out uint roomEnd)
    {
        int sceneRow = checked((int)profile.SceneTable + sceneId * 20); byte[] scene = ReadDecodedRange(rom, U32(rom, sceneRow), U32(rom, sceneRow + 4), "scene"); int hc = FindCommand(scene, 0x18); if (hc < 0) throw new InvalidDataException("Scene has no alternate headers."); int table = checked((int)(U32(scene, hc + 4) & 0x00FFFFFF)); uint pointer = U32(scene, checked(table + (headerIndex - 1) * 4)); if (pointer == 0) throw new InvalidDataException("Alternate header is empty."); int rc = FindCommandAt(scene, checked((int)(pointer & 0x00FFFFFF)), 0x04); if (rc < 0 || roomId < 0 || roomId >= scene[rc + 1]) throw new InvalidDataException("Alternate room is not loaded."); int rt = checked((int)(U32(scene, rc + 4) & 0x00FFFFFF)); int re = checked(rt + roomId * 8); EnsureRange(scene, re, 8, "alternate room table"); roomStart = U32(scene, re); roomEnd = U32(scene, re + 4); return N64Compression.Decode(StoredRange(rom, roomStart, roomEnd, "room"));
    }
    private static void ApplyAlternateObjectListRelocating(byte[] rom, RomProfile profile, RomAlternateObjectListEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount) throw new InvalidDataException("Patch scene index is outside the profile.");
        if (edit.HeaderIndex < 1 || edit.HeaderIndex > 32) throw new InvalidDataException("Alternate header index is outside the supported range.");
        if (edit.ObjectIds is null || edit.ObjectIds.Count > byte.MaxValue) throw new InvalidDataException("Alternate object list count is outside the native range.");
        int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20);
        uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4);
        byte[] storedScene = StoredRange(rom, sceneStart, sceneEnd, "scene");
        byte[] scene = N64Compression.Decode(storedScene);
        int headerTableCommand = FindCommand(scene, 0x18);
        if (headerTableCommand < 0) throw new InvalidDataException("Scene has no supported alternate-header command.");
        int headerTable = checked((int)(U32(scene, headerTableCommand + 4) & 0x00FFFFFF));
        int headerSlot = checked(headerTable + (edit.HeaderIndex - 1) * 4); EnsureRange(scene, headerSlot, 4, "alternate header table");
        uint headerPointer = U32(scene, headerSlot); if (headerPointer == 0) throw new InvalidDataException("Alternate header is empty.");
        int roomCommand = FindCommandAt(scene, checked((int)(headerPointer & 0x00FFFFFF)), 0x04);
        if (roomCommand < 0) throw new InvalidDataException("Alternate header has no supported room table.");
        int roomCount = scene[roomCommand + 1]; if (edit.RoomId < 0 || edit.RoomId >= roomCount) throw new InvalidDataException("Alternate room index is outside the header.");
        int roomTable = checked((int)(U32(scene, roomCommand + 4) & 0x00FFFFFF));
        int roomEntry = checked(roomTable + edit.RoomId * 8); EnsureRange(scene, roomEntry, 8, "alternate room table");
        uint roomStart = U32(scene, roomEntry), roomEnd = U32(scene, roomEntry + 4);
        byte[] storedRoom = StoredRange(rom, roomStart, roomEnd, "room"); byte[] room = N64Compression.Decode(storedRoom); bool found = false;
        for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8)
        {
            if (room[p] == 0x14) break;
            if (room[p] != 0x0B || (U32(room, p + 4) >> 24) != 3) continue;
            int data = checked((int)(U32(room, p + 4) & 0x00FFFFFF)); int bytes = checked(edit.ObjectIds.Count * 2); int target = data;
            if (bytes > 0 && (data < 0 || data + bytes > room.Length))
            {
                target = FindZeroRange(room, bytes); if (target < 0) throw new InvalidDataException("Alternate room has no safe free range for the expanded object list.");
                Put32(room, p + 4, checked(0x03000000u | (uint)target));
            }
            room[p + 1] = checked((byte)edit.ObjectIds.Count); EnsureRange(room, target, bytes, "alternate room object list");
            for (int i = 0; i < edit.ObjectIds.Count; i++) Put16(room, target + i * 2, edit.ObjectIds[i]); found = true; break;
        }
        if (!found) throw new InvalidDataException("Alternate room has no object command.");
        byte[] replacement = EncodeStoredRange(storedRoom, room);
        if (replacement.Length <= roomEnd - roomStart) { WriteStoredRange(rom, roomStart, roomEnd, storedRoom, room); return; }
        int relocatedRoom = FindZeroRange(rom, replacement.Length, checked((int)roomEnd), checked((int)roomStart), checked((int)roomEnd));
        if (relocatedRoom < 0) throw new InvalidDataException("Edited alternate room needs more space but no safe free ROM range was found for relocation.");
        Buffer.BlockCopy(replacement, 0, rom, relocatedRoom, replacement.Length);
        Put32(scene, roomEntry, checked((uint)relocatedRoom)); Put32(scene, roomEntry + 4, checked((uint)(relocatedRoom + replacement.Length)));
        WriteSceneWithRelocation(rom, sceneRow, sceneStart, sceneEnd, storedScene, scene);
    }

    private static void ApplyAlternateObject(byte[] rom, RomProfile profile, RomAlternateObjectEdit edit) { byte[] room = AlternateRoom(rom, profile, edit.SceneId, edit.HeaderIndex, edit.RoomId, out uint start, out uint end); for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8) if (room[p] == 0x0B) { int count = room[p + 1]; if (edit.ObjectIndex < 0 || edit.ObjectIndex >= count) throw new InvalidDataException("Alternate object index is outside the room."); int data = checked((int)(U32(room, p + 4) & 0x00FFFFFF)); EnsureRange(room, data + edit.ObjectIndex * 2, 2, "alternate object list"); Put16(room, data + edit.ObjectIndex * 2, edit.ObjectId); WriteStoredRange(rom, start, end, StoredRange(rom, start, end, "room"), room); return; } throw new InvalidDataException("Alternate room has no object command."); }
    private static void ApplyAlternateRoomSettings(byte[] rom, RomProfile profile, RomAlternateRoomSettingsEdit edit) { byte[] room = AlternateRoom(rom, profile, edit.SceneId, edit.HeaderIndex, edit.RoomId, out uint start, out uint end); for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8) { if (room[p] == 0x14) break; if (room[p] == 0x05) Put32(room, p, edit.Settings.Wind); else if (room[p] == 0x08) Put32(room, p, edit.Settings.Behavior); else if (room[p] == 0x10) { Put16(room, p + 4, edit.Settings.StartTime); room[p + 6] = edit.Settings.TimeSpeed; } else if (room[p] == 0x12) { room[p + 4] = (byte)(edit.Settings.SkyboxFlags & 1); room[p + 5] = (byte)((edit.Settings.SkyboxFlags >> 1) & 1); } else if (room[p] == 0x16) room[p + 7] = edit.Settings.Echo; else continue; WriteStoredRange(rom, start, end, StoredRange(rom, start, end, "room"), room); return; } throw new InvalidDataException("Alternate room has no supported settings command."); }
    private static void ApplyAlternateRoomExit(byte[] rom, RomProfile profile, RomAlternateRoomExitEdit edit) { byte[] room = AlternateRoom(rom, profile, edit.SceneId, edit.HeaderIndex, edit.RoomId, out uint start, out uint end); for (int p = 0; p + 8 <= room.Length && p < 8192; p += 8) if (room[p] == 0x08 && (U32(room, p + 4) >> 24) == 3) { int count = room[p + 1]; if (edit.ExitIndex < 0 || edit.ExitIndex >= count) throw new InvalidDataException("Alternate room exit index is outside the room."); int data = checked((int)(U32(room, p + 4) & 0x00FFFFFF)); Put16(room, data + edit.ExitIndex * 2, edit.Raw); WriteStoredRange(rom, start, end, StoredRange(rom, start, end, "room"), room); return; } throw new InvalidDataException("Alternate room has no exit command."); }
    private static void ApplyAlternateGeometryVertex(byte[] rom, RomProfile profile, RomAlternateGeometryVertexEdit edit) { byte[] room = AlternateRoom(rom, profile, edit.SceneId, edit.HeaderIndex, edit.RoomId, out uint start, out uint end); EnsureRange(room, edit.SourceOffset, 16, "alternate geometry vertex"); Put16(room, edit.SourceOffset, unchecked((ushort)edit.Vertex.X)); Put16(room, edit.SourceOffset + 2, unchecked((ushort)edit.Vertex.Y)); Put16(room, edit.SourceOffset + 4, unchecked((ushort)edit.Vertex.Z)); Put16(room, edit.SourceOffset + 8, unchecked((ushort)edit.Vertex.S)); Put16(room, edit.SourceOffset + 10, unchecked((ushort)edit.Vertex.T)); room[edit.SourceOffset + 12] = edit.Vertex.R; room[edit.SourceOffset + 13] = edit.Vertex.G; room[edit.SourceOffset + 14] = edit.Vertex.B; room[edit.SourceOffset + 15] = edit.Vertex.A; WriteStoredRange(rom, start, end, StoredRange(rom, start, end, "room"), room); }
    private static void ApplyAlternateGeometryTriangle(byte[] rom, RomProfile profile, RomAlternateGeometryTriangleEdit edit) { byte[] room = AlternateRoom(rom, profile, edit.SceneId, edit.HeaderIndex, edit.RoomId, out uint start, out uint end); EnsureRange(room, edit.SourceOffset, 4, "alternate geometry triangle"); uint packed = edit.Prefix | (uint)(edit.SlotA * 2) << 17 | (uint)(edit.SlotB * 2) << 9 | (uint)(edit.SlotC * 2) << 1; Put32(room, edit.SourceOffset, packed); WriteStoredRange(rom, start, end, StoredRange(rom, start, end, "room"), room); }
    private static void ApplyAlternateCollisionTopologyRelocating(byte[] rom, RomProfile profile, RomAlternateCollisionTopologyEdit edit)
    {
        if (edit.SceneId < 0 || edit.SceneId >= profile.SceneCount || edit.HeaderIndex < 1 || edit.HeaderIndex > 32) throw new InvalidDataException("Alternate collision topology address is outside the profile.");
        if (edit.Vertices is null || edit.Vertices.Count == 0 || edit.Vertices.Count > ushort.MaxValue) throw new InvalidDataException("Alternate collision vertex count is outside the native range.");
        if (edit.Triangles is null || edit.Triangles.Count > ushort.MaxValue) throw new InvalidDataException("Alternate collision triangle count is outside the native range.");
        if (edit.SurfaceTypes is null || edit.SurfaceTypes.Count != edit.Triangles.Count) throw new InvalidDataException("Alternate collision surface count must match the triangle count.");
        if (edit.Triangles.Any(t => t.A >= edit.Vertices.Count || t.B >= edit.Vertices.Count || t.C >= edit.Vertices.Count)) throw new InvalidDataException("Alternate collision triangle references a vertex outside the replacement topology.");
        var x = AlternateCollision(rom, profile, edit.SceneId, edit.HeaderIndex); byte[] scene = x.Scene; EnsureRange(scene, x.Collision, 44, "alternate collision header");
        int vertexOffset = Align(scene.Length, 2); int triangleOffset = Align(vertexOffset + edit.Vertices.Count * 6, 2); int surfaceOffset = Align(triangleOffset + edit.Triangles.Count * 16, 8); int sceneLength = checked(surfaceOffset + edit.SurfaceTypes.Count * 8);
        if (vertexOffset > 0x00FFFFFF || triangleOffset > 0x00FFFFFF || surfaceOffset > 0x00FFFFFF) throw new InvalidDataException("Alternate collision topology exceeds the segmented scene address range.");
        Array.Resize(ref scene, sceneLength); Put16(scene, x.Collision + 12, checked((ushort)edit.Vertices.Count)); Put32(scene, x.Collision + 16, checked(0x02000000u | (uint)vertexOffset)); Put16(scene, x.Collision + 20, checked((ushort)edit.Triangles.Count)); Put32(scene, x.Collision + 24, checked(0x02000000u | (uint)triangleOffset)); Put32(scene, x.Collision + 28, edit.SurfaceTypes.Count == 0 ? 0u : checked(0x02000000u | (uint)surfaceOffset));
        for (int i = 0; i < edit.Vertices.Count; i++) { int o = vertexOffset + i * 6; Put16(scene, o, unchecked((ushort)edit.Vertices[i].X)); Put16(scene, o + 2, unchecked((ushort)edit.Vertices[i].Y)); Put16(scene, o + 4, unchecked((ushort)edit.Vertices[i].Z)); }
        for (int i = 0; i < edit.Triangles.Count; i++) { int o = triangleOffset + i * 16; var t = edit.Triangles[i]; Put16(scene, o, t.SurfaceType); Put16(scene, o + 2, t.A); Put16(scene, o + 4, t.B); Put16(scene, o + 6, t.C); Put16(scene, o + 8, unchecked((ushort)t.NormalX)); Put16(scene, o + 10, unchecked((ushort)t.NormalY)); Put16(scene, o + 12, unchecked((ushort)t.NormalZ)); Put16(scene, o + 14, unchecked((ushort)t.Distance)); }
        for (int i = 0; i < edit.SurfaceTypes.Count; i++) Put64(scene, surfaceOffset + i * 8, edit.SurfaceTypes[i]);
        WriteSceneWithRelocation(rom, checked((int)profile.SceneTable + edit.SceneId * 20), x.Start, x.End, StoredRange(rom, x.Start, x.End, "scene"), scene);
    }

    private static void ApplyAlternateCollisionVertex(byte[] rom, RomProfile profile, RomAlternateCollisionVertexEdit edit) { int sceneRow = checked((int)profile.SceneTable + edit.SceneId * 20); uint sceneStart = U32(rom, sceneRow), sceneEnd = U32(rom, sceneRow + 4); byte[] scene = ReadDecodedRange(rom, sceneStart, sceneEnd, "scene"); int hc = FindCommand(scene, 0x18); if (hc < 0) throw new InvalidDataException("Scene has no alternate headers."); int table = checked((int)(U32(scene, hc + 4) & 0x00FFFFFF)); uint pointer = U32(scene, checked(table + (edit.HeaderIndex - 1) * 4)); if (pointer == 0) throw new InvalidDataException("Alternate header is empty."); int cc = FindCommandAt(scene, checked((int)(pointer & 0x00FFFFFF)), 0x02); if (cc < 0) throw new InvalidDataException("Alternate header has no collision command."); int collision = checked((int)(U32(scene, cc + 4) & 0x00FFFFFF)); EnsureRange(scene, collision, 44, "alternate collision header"); int count = U16(scene, collision + 12); if (edit.VertexIndex < 0 || edit.VertexIndex >= count) throw new InvalidDataException("Alternate collision vertex index is outside the header."); int data = checked((int)(U32(scene, collision + 16) & 0x00FFFFFF)); int o = checked(data + edit.VertexIndex * 6); EnsureRange(scene, o, 6, "alternate collision vertex list"); Put16(scene, o, unchecked((ushort)edit.Vertex.X)); Put16(scene, o + 2, unchecked((ushort)edit.Vertex.Y)); Put16(scene, o + 4, unchecked((ushort)edit.Vertex.Z)); WriteStoredRange(rom, sceneStart, sceneEnd, StoredRange(rom, sceneStart, sceneEnd, "scene"), scene); }
    private static (byte[] Scene, uint Start, uint End, int Collision) AlternateCollision(byte[] rom, RomProfile profile, int sceneId, int headerIndex) { int row = checked((int)profile.SceneTable + sceneId * 20); uint start = U32(rom, row), end = U32(rom, row + 4); byte[] scene = ReadDecodedRange(rom, start, end, "scene"); int hc = FindCommand(scene, 0x18); int table = checked((int)(U32(scene, hc + 4) & 0x00FFFFFF)); uint pointer = U32(scene, table + (headerIndex - 1) * 4); int cc = FindCommandAt(scene, checked((int)(pointer & 0x00FFFFFF)), 0x02); return (scene, start, end, checked((int)(U32(scene, cc + 4) & 0x00FFFFFF))); }
    private static void ApplyAlternateCollisionTriangle(byte[] rom, RomProfile profile, RomAlternateCollisionTriangleEdit edit) { var x = AlternateCollision(rom, profile, edit.SceneId, edit.HeaderIndex); int count = U16(x.Scene, x.Collision + 20); if (edit.TriangleIndex < 0 || edit.TriangleIndex >= count) throw new InvalidDataException("Alternate collision triangle index is outside the header."); int data = checked((int)(U32(x.Scene, x.Collision + 24) & 0x00FFFFFF)); int o = checked(data + edit.TriangleIndex * 16); Put16(x.Scene, o, edit.Triangle.SurfaceType); Put16(x.Scene, o + 2, edit.Triangle.A); Put16(x.Scene, o + 4, edit.Triangle.B); Put16(x.Scene, o + 6, edit.Triangle.C); Put16(x.Scene, o + 8, unchecked((ushort)edit.Triangle.NormalX)); Put16(x.Scene, o + 10, unchecked((ushort)edit.Triangle.NormalY)); Put16(x.Scene, o + 12, unchecked((ushort)edit.Triangle.NormalZ)); Put16(x.Scene, o + 14, unchecked((ushort)edit.Triangle.Distance)); WriteStoredRange(rom, x.Start, x.End, StoredRange(rom, x.Start, x.End, "scene"), x.Scene); }
    private static void ApplyAlternateCollisionSurface(byte[] rom, RomProfile profile, RomAlternateCollisionSurfaceEdit edit) { var x = AlternateCollision(rom, profile, edit.SceneId, edit.HeaderIndex); int count = U16(x.Scene, x.Collision + 20); if (edit.SurfaceIndex < 0 || edit.SurfaceIndex >= count) throw new InvalidDataException("Alternate collision surface index is outside the header."); int data = checked((int)(U32(x.Scene, x.Collision + 28) & 0x00FFFFFF)); Put64(x.Scene, data + edit.SurfaceIndex * 8, edit.SurfaceType); WriteStoredRange(rom, x.Start, x.End, StoredRange(rom, x.Start, x.End, "scene"), x.Scene); }
    private static void ApplyAlternateCollisionBounds(byte[] rom, RomProfile profile, RomAlternateCollisionBoundsEdit edit) { var x = AlternateCollision(rom, profile, edit.SceneId, edit.HeaderIndex); Put16(x.Scene, x.Collision, unchecked((ushort)edit.MinX)); Put16(x.Scene, x.Collision + 2, unchecked((ushort)edit.MinY)); Put16(x.Scene, x.Collision + 4, unchecked((ushort)edit.MinZ)); Put16(x.Scene, x.Collision + 6, unchecked((ushort)edit.MaxX)); Put16(x.Scene, x.Collision + 8, unchecked((ushort)edit.MaxY)); Put16(x.Scene, x.Collision + 10, unchecked((ushort)edit.MaxZ)); WriteStoredRange(rom, x.Start, x.End, StoredRange(rom, x.Start, x.End, "scene"), x.Scene); }
    private static void ApplyAlternateCamera(byte[] rom, RomProfile profile, RomAlternateCameraEdit edit) { var x = AlternateCollision(rom, profile, edit.SceneId, edit.HeaderIndex); int table = checked((int)(U32(x.Scene, x.Collision + 32) & 0x00FFFFFF)); if (edit.CameraIndex < 0) throw new InvalidDataException("Alternate camera index is outside the camera table."); int entry = checked(table + edit.CameraIndex * 8); EnsureRange(x.Scene, entry, 8, "alternate camera table"); int data = checked((int)(U32(x.Scene, entry + 4) & 0x00FFFFFF)); Put16(x.Scene, entry, edit.Camera.Type); Put16(x.Scene, entry + 2, unchecked((ushort)edit.Camera.DataCount)); if (edit.Camera.PathPoints is { Count: > 0 } pathPoints) { if (edit.Camera.DataCount != pathPoints.Count) throw new InvalidDataException("Alternate camera path point count must match the camera page count."); EnsureRange(x.Scene, data, checked(pathPoints.Count * 6), "alternate camera path data"); for (int point = 0; point < pathPoints.Count; point++) { int p = data + point * 6; Put16(x.Scene, p, unchecked((ushort)pathPoints[point].X)); Put16(x.Scene, p + 2, unchecked((ushort)pathPoints[point].Y)); Put16(x.Scene, p + 4, unchecked((ushort)pathPoints[point].Z)); } } else { EnsureRange(x.Scene, data, 18, "alternate camera record"); Put16(x.Scene, data, unchecked((ushort)edit.Camera.X)); Put16(x.Scene, data + 2, unchecked((ushort)edit.Camera.Y)); Put16(x.Scene, data + 4, unchecked((ushort)edit.Camera.Z)); Put16(x.Scene, data + 6, unchecked((ushort)edit.Camera.RotationX)); Put16(x.Scene, data + 8, unchecked((ushort)edit.Camera.RotationY)); Put16(x.Scene, data + 10, unchecked((ushort)edit.Camera.RotationZ)); Put16(x.Scene, data + 12, unchecked((ushort)edit.Camera.Fov)); Put16(x.Scene, data + 14, edit.Camera.Unknown1); Put16(x.Scene, data + 16, edit.Camera.Unknown2); } WriteStoredRange(rom, x.Start, x.End, StoredRange(rom, x.Start, x.End, "scene"), x.Scene); }
    private static void ApplyAlternateWaterbox(byte[] rom, RomProfile profile, RomAlternateWaterboxEdit edit) { var x = AlternateCollision(rom, profile, edit.SceneId, edit.HeaderIndex); int count = U16(x.Scene, x.Collision + 36); if (edit.WaterboxIndex < 0 || edit.WaterboxIndex >= count) throw new InvalidDataException("Alternate waterbox index is outside the header."); int data = checked((int)(U32(x.Scene, x.Collision + 40) & 0x00FFFFFF)); int o = checked(data + edit.WaterboxIndex * 16); Put16(x.Scene, o, unchecked((ushort)edit.Waterbox.X)); Put16(x.Scene, o + 2, unchecked((ushort)edit.Waterbox.Y)); Put16(x.Scene, o + 4, unchecked((ushort)edit.Waterbox.Z)); Put16(x.Scene, o + 6, unchecked((ushort)edit.Waterbox.XSize)); Put16(x.Scene, o + 8, unchecked((ushort)edit.Waterbox.ZSize)); Put16(x.Scene, o + 10, edit.Waterbox.Unknown); Put32(x.Scene, o + 12, edit.Waterbox.Properties); WriteStoredRange(rom, x.Start, x.End, StoredRange(rom, x.Start, x.End, "scene"), x.Scene); }

    private static int FindRoomTable(byte[] scene)
    {
        for (int p = 0; p + 8 <= scene.Length && p < 8192; p += 8) { if (scene[p] == 0x14) break; if (scene[p] == 0x04 && (U32(scene, p + 4) >> 24) == 2) { int table = checked((int)(U32(scene, p + 4) & 0x00FFFFFF)); if (table >= 0 && table + scene[p + 1] * 8 <= scene.Length) return p; } }
        return -1;
    }

    private static int FindCommandAt(byte[] scene, int start, byte command)
    {
        for (int p = start; p + 8 <= scene.Length && p < start + 8192; p += 8) { if (scene[p] == 0x14) break; if (scene[p] == command) return p; }
        return -1;
    }

    private static byte[] StoredRange(byte[] rom, uint start, uint end, string kind)
    {
        if (start >= end || end > rom.Length) throw new InvalidDataException($"Patch {kind} range is outside the ROM.");
        return rom.AsSpan(checked((int)start), checked((int)(end - start))).ToArray();
    }

    private static byte[] ReadDecodedRange(byte[] rom, uint start, uint end, string kind) => N64Compression.Decode(StoredRange(rom, start, end, kind));

    private static void WriteStoredRange(byte[] rom, uint start, uint end, byte[] stored, byte[] decoded)
    {
        int capacity = checked((int)(end - start)); byte[] replacement = EncodeStoredRange(stored, decoded);
        if (replacement.Length > capacity) throw new InvalidDataException($"Edited room needs {replacement.Length} bytes but its ROM slot only has {capacity}; the original ROM was not changed.");
        int offset = checked((int)start); Buffer.BlockCopy(replacement, 0, rom, offset, replacement.Length); Array.Clear(rom, offset + replacement.Length, capacity - replacement.Length);
    }

    private static void WriteSceneWithRelocation(byte[] rom, int sceneRow, uint start, uint end, byte[] stored, byte[] decoded)
    {
        byte[] replacement = EncodeStoredRange(stored, decoded);
        int capacity = checked((int)(end - start));
        if (replacement.Length <= capacity)
        {
            WriteStoredRange(rom, start, end, stored, decoded);
            return;
        }
        int relocatedStart = FindZeroRange(rom, replacement.Length, checked((int)end), checked((int)start), checked((int)end));
        if (relocatedStart < 0) throw new InvalidDataException($"Edited scene needs {replacement.Length} bytes but no safe free ROM range was found for relocation.");
        Buffer.BlockCopy(replacement, 0, rom, relocatedStart, replacement.Length);
        Put32(rom, sceneRow, checked((uint)relocatedStart));
        Put32(rom, sceneRow + 4, checked((uint)(relocatedStart + replacement.Length)));
    }

    private static byte[] EncodeStoredRange(byte[] stored, byte[] decoded)
    {
        if (stored.Length >= 4 && stored[0] == 'Y' && stored[1] == 'a' && stored[2] == 'z' && stored[3] == '0') return N64Compression.EncodeYaz0(decoded);
        if (stored.Length >= 4 && stored[0] == 'M' && stored[1] == 'I' && stored[2] == 'O' && stored[3] == '0') return N64Compression.EncodeMio0(decoded);
        return decoded;
    }

    private static int FindZeroRange(byte[] rom, int length, int searchStart, int excludeStart, int excludeEnd)
    {
        if (length <= 0) throw new InvalidDataException("Relocation size is invalid.");
        int start = (searchStart + 0x0F) & ~0x0F;
        for (int candidate = start; candidate <= rom.Length - length; candidate += 0x10)
        {
            if (candidate < excludeEnd && candidate + length > excludeStart) continue;
            bool clear = true; for (int i = 0; i < length; i++) if (rom[candidate + i] != 0) { clear = false; break; }
            if (clear) return candidate;
        }
        return -1;
    }

    private static void WriteActor(byte[] data, int o, RomActor actor)
    {
        Put16(data, o, actor.Number); Put16(data, o + 2, unchecked((ushort)actor.X)); Put16(data, o + 4, unchecked((ushort)actor.Y)); Put16(data, o + 6, unchecked((ushort)actor.Z)); Put16(data, o + 8, unchecked((ushort)actor.RotationX)); Put16(data, o + 10, unchecked((ushort)actor.RotationY)); Put16(data, o + 12, unchecked((ushort)actor.RotationZ)); Put16(data, o + 14, actor.Variable);
    }

    private static void WriteGeometryVertex(byte[] data, int o, RomGeometryVertex vertex)
    {
        Put16(data, o, unchecked((ushort)vertex.X)); Put16(data, o + 2, unchecked((ushort)vertex.Y)); Put16(data, o + 4, unchecked((ushort)vertex.Z)); Put16(data, o + 8, unchecked((ushort)vertex.S)); Put16(data, o + 10, unchecked((ushort)vertex.T)); data[o + 12] = vertex.R; data[o + 13] = vertex.G; data[o + 14] = vertex.B; data[o + 15] = vertex.A;
    }

    private static int Align(int value, int alignment) => checked((value + alignment - 1) / alignment * alignment);

    private static ushort U16(byte[] b, int o) => (ushort)(b[o] << 8 | b[o + 1]);
    private static uint U32(byte[] b, int o) => (uint)b[o] << 24 | (uint)b[o + 1] << 16 | (uint)b[o + 2] << 8 | b[o + 3];
    private static void Put16(byte[] b, int o, ushort v) { b[o] = (byte)(v >> 8); b[o + 1] = (byte)v; }
    private static void Put32(byte[] b, int o, uint v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }
    private static void PutRgb(byte[] b, int o, uint rgb) { b[o] = (byte)(rgb >> 16); b[o + 1] = (byte)(rgb >> 8); b[o + 2] = (byte)rgb; }
    private static void Put64(byte[] b, int o, ulong v) { for (int i = 7; i >= 0; i--) { b[o + i] = (byte)v; v >>= 8; } }
}
