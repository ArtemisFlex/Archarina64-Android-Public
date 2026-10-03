using System.Numerics;
using System.Text.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.IO.Compression;
using Archarina64.Core;
using SharpOcarina;

int passed = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); }
void Test(string name, Action run) { run(); passed++; Console.WriteLine("PASS " + name); }
void Reject(Action run) { try { run(); } catch (Exception e) when (e is InvalidDataException or XmlException or InvalidOperationException or JsonException) { return; } throw new Exception("Invalid input was accepted."); }

Test("Native OoT ROM decoder reads scene rooms actors and collision metadata", () => {
    byte[] rom = new byte[0xB72000];
    Put32(rom, 0, 0x80371240);
    int table = 0xB71440, scene = 0x1000, roomTable = 0x1108, room = 0x1200, actors = 0x1220;
    Put32(rom, table, (uint)scene); Put32(rom, table + 4, 0x1200);
    Put32(rom, table + 20, 0x2000); Put32(rom, table + 24, 0x2080);
    rom[scene] = 0x02; Put32(rom, scene + 4, 0x02000040); rom[scene + 8] = 0x04; rom[scene + 9] = 1; Put32(rom, scene + 12, 0x02000108); rom[scene + 16] = 0x00; rom[scene + 17] = 1; Put32(rom, scene + 20, 0x020000B0); rom[scene + 24] = 0x0D; rom[scene + 25] = 1; Put32(rom, scene + 28, 0x020000C0); rom[scene + 32] = 0x13; rom[scene + 33] = 1; Put32(rom, scene + 36, 0x020000F0); rom[scene + 40] = 0x0F; rom[scene + 41] = 1; Put32(rom, scene + 44, 0x020000D8); rom[scene + 48] = 0x14;
    Put32(rom, roomTable, (uint)room); Put32(rom, roomTable + 4, 0x1300);
    rom[room] = 0x01; rom[room + 1] = 1; Put32(rom, room + 4, 0x03000020); rom[room + 8] = 0x0B; rom[room + 9] = 1; Put32(rom, room + 12, 0x03000030); rom[room + 16] = 0x0A; Put32(rom, room + 20, 0x03000040); rom[room + 24] = 0x14;
    Put16(rom, actors, 0x123); Put16(rom, actors + 2, unchecked((ushort)-10)); Put16(rom, actors + 4, 20); Put16(rom, actors + 6, 30); Put16(rom, actors + 14, 7); Put16(rom, room + 0x30, 0x444);
    int collision = scene + 0x40; Put16(rom, collision + 12, 1); Put32(rom, collision + 16, 0x02000070); Put16(rom, collision + 20, 1); Put32(rom, collision + 24, 0x02000080); Put32(rom, collision + 28, 0x020000A0); Put32(rom, collision + 32, 0); Put16(rom, collision + 36, 1); Put32(rom, collision + 40, 0x020000F8);
    Put16(rom, scene + 0x70, 1); Put16(rom, scene + 0x72, 2); Put16(rom, scene + 0x74, 3);
    Put16(rom, scene + 0x80, 0); Put16(rom, scene + 0x82, 0); Put16(rom, scene + 0x84, 0); Put16(rom, scene + 0x86, 0); Put16(rom, scene + 0x88, 0); Put16(rom, scene + 0x8A, 0); Put16(rom, scene + 0x8C, 0); Put16(rom, scene + 0x8E, 0);
    Put16(rom, scene + 0xB0, 0x456); Put16(rom, scene + 0xB2, 11); Put16(rom, scene + 0xB4, 22); Put16(rom, scene + 0xB6, 33); Put16(rom, scene + 0xBE, 9);
    rom[scene + 0xC0] = 2; Put32(rom, scene + 0xC4, 0x020000C8); Put16(rom, scene + 0xC8, 1); Put16(rom, scene + 0xCA, 2); Put16(rom, scene + 0xCC, 3); Put16(rom, scene + 0xCE, 4); Put16(rom, scene + 0xD0, 5); Put16(rom, scene + 0xD2, 6);
    Put16(rom, scene + 0xF0, 0x0421);
    Put16(rom, scene + 0xF8, 100); Put16(rom, scene + 0xFA, 200); Put16(rom, scene + 0xFC, 300); Put16(rom, scene + 0xFE, 40); Put16(rom, scene + 0x100, 50); Put16(rom, scene + 0x102, 0xBEEF); Put32(rom, scene + 0x104, 0x01234567);
    for (int i = 0; i < 18; i++) rom[scene + 0xD8 + i] = (byte)(i + 1); Put16(rom, scene + 0xEA, 0x8A14); Put16(rom, scene + 0xEC, 0x1234);
    int mesh = room + 0x40; rom[mesh] = 0; rom[mesh + 1] = 1; Put32(rom, mesh + 4, 0x03000050); Put32(rom, mesh + 8, 0);
    Put32(rom, room + 0x50, 0x03000060); Put32(rom, room + 0x54, 0); Put32(rom, room + 0x60, 0x01003000); Put32(rom, room + 0x64, 0x03000080); Put32(rom, room + 0x68, 0xBF000000); Put32(rom, room + 0x6C, 0x00000408); Put32(rom, room + 0x70, 0xB8000000); Put32(rom, room + 0x74, 0);
    for (int i = 0; i < 3; i++) { int v = room + 0x80 + i * 16; Put16(rom, v, (ushort)(i * 10)); Put16(rom, v + 2, 0); Put16(rom, v + 4, (ushort)(i == 2 ? 10 : 0)); Put16(rom, v + 8, 0); Put16(rom, v + 10, 0); rom[v + 12] = 255; rom[v + 13] = 200; rom[v + 14] = 100; rom[v + 15] = 255; }
    rom[scene + 48] = 0x0E; rom[scene + 49] = 1; Put32(rom, scene + 52, 0x02000118); rom[scene + 56] = 0x14;
    rom[scene + 0x118] = 1; rom[scene + 0x119] = 0; rom[scene + 0x11A] = 2; rom[scene + 0x11B] = 3; Put16(rom, scene + 0x11C, 4); Put16(rom, scene + 0x11E, 5); Put16(rom, scene + 0x120, 6); Put16(rom, scene + 0x122, 7); Put16(rom, scene + 0x124, 8);
    var decoded = RomDecoder.Read(rom);
    var firstScene = decoded.Scenes[0]; var firstRoom = firstScene.Rooms.FirstOrDefault() ?? throw new Exception($"No room decoded: {string.Join(" | ", firstScene.Diagnostics)}");
    Check(decoded.Profile.Id == "oot-1.0-ntsc", "Profile mismatch."); Check(firstScene.Rooms.Count == 1 && firstScene.SpawnPoints.Count == 1 && firstScene.SpawnPoints[0].Number == 0x456, $"Scene rooms/spawn mismatch: rooms={firstScene.Rooms.Count}, spawns={firstScene.SpawnPoints.Count}, number={firstScene.SpawnPoints.FirstOrDefault()?.Number:X4}."); Check(firstScene.Paths is { Count: 1 } && firstScene.Paths[0].Points.Count == 2, "Path mismatch."); Check(firstScene.Exits is { Count: 1 } && firstScene.Exits[0].RoomId == 2 && firstScene.Exits[0].SpawnId == 33, "Exit mismatch."); Check(firstScene.Environments is { Count: 1 } && firstScene.Environments[0].Ambient == 0x010203 && firstScene.Environments[0].DrawDistance == 0x1234, "Environment mismatch."); Check(firstScene.HasCollision && firstScene.Collision is { Vertices.Count: 1, Triangles.Count: 1, Waterboxes.Count: 1 } && firstScene.Collision.Waterboxes[0].Properties == 0x01234567, "Collision/waterbox mismatch."); Check(firstRoom.Geometry is { Vertices.Count: 3, Triangles.Count: 1 } && firstRoom.Actors[0].Number == 0x123 && firstRoom.Actors[0].X == -10, "Room geometry/actor mismatch.");
    var nativeWorkspace = new RomWorkspace(decoded); nativeWorkspace.EditRoomActor(0, 0, 0, firstRoom.Actors[0] with { X = 77 }); nativeWorkspace.EditEntrance(0, 7, 8); nativeWorkspace.EditSpawn(0, 0, firstScene.SpawnPoints[0] with { X = 321 }); nativeWorkspace.EditPathPoint(0, 0, 1, new RomPathPoint(40, 50, 60)); nativeWorkspace.EditExit(0, 0, 0x0842); nativeWorkspace.EditWaterbox(0, 0, firstScene.Collision.Waterboxes[0] with { Y = 999 }); nativeWorkspace.EditEnvironment(0, 0, firstScene.Environments[0] with { Ambient = 0xAABBCC, DrawDistance = 0x4321 }); nativeWorkspace.EditCollisionVertex(0, 0, firstScene.Collision.Vertices[0] with { X = 123 }); nativeWorkspace.EditCollisionTriangle(0, 0, firstScene.Collision.Triangles[0] with { NormalX = -123 }); nativeWorkspace.EditCollisionSurface(0, 0, 0x1122334455667788UL); byte[] patched = nativeWorkspace.ApplyToRom(rom);
    Check((short)(patched[actors + 2] << 8 | patched[actors + 3]) == 77 && patched[0xB6FBF0] == 7 && patched[0xB6FBF1] == 8 && (short)(patched[scene + 0xB2] << 8 | patched[scene + 0xB3]) == 321 && (short)(patched[scene + 0xCE] << 8 | patched[scene + 0xCF]) == 40 && (short)(patched[scene + 0xD0] << 8 | patched[scene + 0xD1]) == 50 && (short)(patched[scene + 0xD2] << 8 | patched[scene + 0xD3]) == 60 && patched[scene + 0xF0] == 0x08 && patched[scene + 0xF1] == 0x42 && (short)(patched[scene + 0xFA] << 8 | patched[scene + 0xFB]) == 999 && patched[scene + 0xD8] == 0xAA && patched[scene + 0xD9] == 0xBB && patched[scene + 0xDA] == 0xCC && patched[scene + 0xEC] == 0x43 && patched[scene + 0xED] == 0x21 && (short)(patched[scene + 0x70] << 8 | patched[scene + 0x71]) == 123 && (short)(patched[scene + 0x88] << 8 | patched[scene + 0x89]) == -123 && patched[scene + 0xA0] == 0x11 && patched[scene + 0xA7] == 0x88, "Uncompressed native ROM patch did not write actor, entrance, spawn, path, exit, environment, waterbox, collision triangle and surface data.");
});

Test("Expanded hack scene table is selected over coincidental ROM pointers", () => {
    byte[] rom = new byte[0xB75000]; Put32(rom, 0, 0x80371240);
    int table = 0xB71450; Put32(rom, table, 0x1000); Put32(rom, table + 4, 0x1100); Put32(rom, table + 20, 0x2000); Put32(rom, table + 24, 0x2100);
    rom[0x1000] = 0x15; rom[0x1008] = 0x04; rom[0x1009] = 0; Put32(rom, 0x100C, 0x02000020); rom[0x1010] = 0x14;
    var decoded = RomDecoder.Read(rom);
    Check(decoded.Profile.Id == "oot-expanded-hack" && decoded.Scenes.Count == 2, "The shifted scene table was not selected.");
    Array.Clear(rom, table, 40);
    Put32(rom, 0xB73C40, 0x3000); Put32(rom, 0xB73C44, 0x3100);
    Put32(rom, 0xB73C54, 0x4000); Put32(rom, 0xB73C58, 0x4100);
    Reject(() => RomDecoder.Read(rom));
});

Test("Native transition edits preserve room and camera routing", () => {
    var transition = new RomTransition(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
    var scene = new RomScene(0, "Test", 0, 1, [], false, [], null, [], [transition], [], [], []);
    var document = new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []);
    var workspace = new RomWorkspace(document); workspace.EditTransition(0, 0, transition with { BackCamera = 99, X = 123 });
    Check(workspace.Document.Scenes[0].Transitions![0].BackCamera == 99 && workspace.TransitionEdits.Count == 1, "Native transition edit was not staged.");
});

Test("Native room object edits are staged with object IDs", () => {
    var room = new RomRoom(0, 0, 40, [], 2, false, false, [], null, [0x1234, 0x2345]); var scene = new RomScene(0, "Scene", 0, 40, [room], false, []); var document = new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []);
    var workspace = new RomWorkspace(document); workspace.EditRoomObject(0, 0, 1, 0x4567);
    Check(workspace.Document.Scenes[0].Rooms[0].Objects![1] == 0x4567 && workspace.ObjectEdits.Count == 1 && workspace.WritePatch().Contains("ObjectId"), "Native room object edit was not staged.");
});

Test("Native room settings edits are staged", () => {
    var room = new RomRoom(0, 0, 40, [], 0, false, false, [], null, [], new RomRoomSettings(1, 2, 3, 4, 5, 6)); var scene = new RomScene(0, "Scene", 0, 40, [room], false, []); var document = new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []);
    var workspace = new RomWorkspace(document); workspace.EditRoomSettings(0, 0, new RomRoomSettings(9, 8, 7, 6, 5, 4));
    Check(workspace.Document.Scenes[0].Rooms[0].Settings!.Behavior == 9 && workspace.RoomSettingsEdits.Count == 1 && workspace.WritePatch().Contains("RoomSettings"), "Native room settings edit was not staged.");
});

Test("Native room exit edits are staged", () => {
    var room = new RomRoom(0, 0, 40, [], 0, false, false, [], null, [], null, [0x0123]); var scene = new RomScene(0, "Scene", 0, 40, [room], false, []); var document = new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []);
    var workspace = new RomWorkspace(document); workspace.EditRoomExit(0, 0, 0, 0x0456);
    Check(workspace.Document.Scenes[0].Rooms[0].Exits![0] == 0x0456 && workspace.RoomExitEdits.Count == 1 && workspace.WritePatch().Contains("RoomExits"), "Native room exit edit was not staged.");
});

Test("Native collision camera edits are staged", () => {
    var collision = new RomCollisionData(0, 0, 0, 1, 1, 1, [], [], [], [], [new RomCamera(3, 1, 2, 3, 4, 5, 6, 45, 7, 8)]); var scene = new RomScene(0, "Scene", 0, 40, [], true, [], collision); var document = new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []);
    var workspace = new RomWorkspace(document); workspace.EditCamera(0, 0, new RomCamera(4, 9, 8, 7, 6, 5, 4, 60, 3, 2));
    Check(workspace.Document.Scenes[0].Collision!.Cameras![0].Fov == 60 && workspace.CameraEdits.Count == 1 && workspace.WritePatch().Contains("CameraIndex"), "Native collision camera edit was not staged.");
});
Test("Native camera page paths are staged and retained", () => {
    var path = new[] { new RomCameraPathPoint(1, 2, 3), new RomCameraPathPoint(4, 5, 6) }; var camera = new RomCamera(0x1E, 0, 0, 0, 0, 0, 0, 0, 0, 0, (short)path.Length, path);
    var collision = new RomCollisionData(0, 0, 0, 1, 1, 1, [], [], [], [], [camera]); var scene = new RomScene(0, "Scene", 0, 40, [], true, [], collision); var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    var updated = workspace.EditCamera(0, 0, camera with { PathPoints = [new RomCameraPathPoint(10, 20, 30), new RomCameraPathPoint(40, 50, 60)] });
    Check(updated.DataCount == 2 && updated.PathPoints![1].Z == 60 && workspace.CameraEdits.Count == 1, "Native camera page path edits were not staged.");
});
Test("Native camera projection maps actors and supports depth-preserving drag conversion", () => {
    var camera = new RomCamera(1, 0, 0, 0, 0, 0, 0, 90, 0, 0); var actor = new RomActor(1, 0, 0, -100, 0, 0, 0, 0);
    var projected = RomCameraProjection.Project(camera, actor); Check(projected.Visible && MathF.Abs(projected.ScreenX) < 0.001f && MathF.Abs(projected.ScreenY) < 0.001f && MathF.Abs(projected.Depth - 100) < 0.01f, "Camera projection did not map a centered actor.");
    var world = RomCameraProjection.UnprojectAtDepth(camera, 0.5f, 0, projected.Depth); Check(MathF.Abs(world.X - 50) < 0.1f && MathF.Abs(world.Z + 100) < 0.1f, "Camera unprojection did not preserve actor depth.");
});

Test("Native scene settings edits write camera movement and world map", () => {
    byte[] rom = new byte[0x240]; int sceneTable = 0x100, sceneStart = 0x140, sceneEnd = 0x160;
    Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x19; rom[sceneStart + 1] = 1; rom[sceneStart + 7] = 2; rom[sceneStart + 8] = 0x14;
    using var sha = System.Security.Cryptography.SHA256.Create(); string fingerprint = Convert.ToHexString(sha.ComputeHash(rom)).ToLowerInvariant();
    var settings = new RomSceneSettings(1, 2); var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, [], false, [], Settings: settings);
    var document = new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []);
    var workspace = new RomWorkspace(document); workspace.EditSceneSettings(0, new RomSceneSettings(7, 9)); byte[] patched = workspace.ApplyToRom(rom);
    Check(patched[sceneStart + 1] == 7 && patched[sceneStart + 7] == 9 && workspace.SceneSettingsEdits.Count == 1 && workspace.WritePatch().Contains("SceneSettings"), "Native scene settings did not write camera movement and world map.");
});
Test("Native scene command edits are staged", () => {
    var commands = new[] { new RomHeaderCommand(0x19, 1, 0x02000020), new RomHeaderCommand(0x14, 0, 0) }; var scene = new RomScene(0, "Scene", 0, 40, [], false, [], Commands: commands); var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 20), [scene], []));
    workspace.EditSceneCommand(0, 0, new RomHeaderCommand(0x02, 2, 0x02000080));
    Check(workspace.Document.Scenes[0].Commands![0].Command == 0x02 && workspace.SceneCommandEdits.Count == 1 && workspace.WritePatch().Contains("SceneCommands"), "Scene command edit was not staged.");
});
Test("Native scene command insertion and final deletion are staged safely", () => {
    var commands = new[] { new RomHeaderCommand(0x19, 1, 0x02000020) }; var scene = new RomScene(0, "Scene", 0, 40, [], false, [], Commands: commands); var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 20), [scene], []));
    workspace.InsertSceneCommand(0, new RomHeaderCommand(0x02, 0, 0x02000080)); workspace.DeleteSceneCommand(0, 1);
    Check(workspace.Document.Scenes[0].Commands!.Count == 1 && workspace.SceneCommandEdits[^1].Delete, "Scene command insertion/deletion was not staged.");
});
Test("Native scene command insertion supports middle indexes", () => {
    var commands = new[] { new RomHeaderCommand(0x19, 1, 0x20), new RomHeaderCommand(0x02, 0, 0x80) }; var scene = new RomScene(0, "Scene", 0, 40, [], false, [], Commands: commands); var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 20), [scene], []));
    workspace.InsertSceneCommandAt(0, 1, new RomHeaderCommand(0x04, 2, 0x90));
    Check(workspace.Document.Scenes[0].Commands![1].Command == 0x04 && workspace.Document.Scenes[0].Commands[2].Command == 0x02 && workspace.SceneCommandEdits[0].Insert, "Middle scene command insertion was not staged.");
});

Test("Native scene clone and delete use fixed scene-table slots", () => {
    byte[] rom = new byte[0x800]; int sceneTable = 0x100, sceneStart = 0x200, sceneEnd = 0x220; Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd); rom[sceneStart] = 0x14; rom[sceneStart + 1] = 0x5A;
    string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant(); var source = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, [], false, []); var document = new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 40)), [source], []); var workspace = new RomWorkspace(document);
    var clone = workspace.CloneScene(0, 1); Check(clone.Id == 1 && workspace.SceneTopologyEdits.Count == 1 && workspace.WritePatch().Contains("SceneTopology"), "Native scene clone was not staged.");
    byte[] cloned = workspace.ApplyToRom(rom); uint cloneStart = Read32(cloned, sceneTable + 20), cloneEnd = Read32(cloned, sceneTable + 24); Check(cloneStart != 0 && cloneEnd > cloneStart && cloned[(int)cloneStart + 1] == 0x5A, "Native scene clone did not allocate and copy the scene blob.");
    workspace.DeleteScene(1); byte[] canceled = workspace.ApplyToRom(rom); Check(Read32(canceled, sceneTable + 20) == 0 && Read32(canceled, sceneTable + 24) == 0 && workspace.SceneTopologyEdits.Count == 0, "Canceling a cloned scene did not restore the empty scene-table slot.");
    var deleteWorkspace = new RomWorkspace(document); deleteWorkspace.DeleteScene(0); byte[] deleted = deleteWorkspace.ApplyToRom(rom); Check(Read32(deleted, sceneTable) == 0 && Read32(deleted, sceneTable + 4) == 0 && deleteWorkspace.SceneTopologyEdits.Count == 1, "Native scene deletion did not clear the occupied scene-table slot.");
});

Test("Native scene-table order preserves empty slots", () => {
    byte[] rom = new byte[0x800]; int sceneTable = 0x100, entranceTable = 0x180, firstStart = 0x200, secondStart = 0x240; Put32(rom, sceneTable, (uint)firstStart); Put32(rom, sceneTable + 4, (uint)(firstStart + 0x20)); Put32(rom, sceneTable + 40, (uint)secondStart); Put32(rom, sceneTable + 44, (uint)(secondStart + 0x20)); rom[firstStart] = 0x14; rom[firstStart + 1] = 0x11; rom[secondStart] = 0x14; rom[secondStart + 1] = 0x22; rom[entranceTable] = 0; rom[entranceTable + 1] = 1; rom[entranceTable + 4] = 2; rom[entranceTable + 5] = 2;
    string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant(); var scenes = new[] { new RomScene(0, "First", (uint)firstStart, (uint)(firstStart + 0x20), [], false, []), new RomScene(2, "Second", (uint)secondStart, (uint)(secondStart + 0x20), [], false, []) }; var document = new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 60), (uint)entranceTable, (uint)(entranceTable + 8)), scenes, [], [new RomEntrance(0, 0, 1), new RomEntrance(1, 2, 2)]); var workspace = new RomWorkspace(document);
    var reordered = workspace.ReorderScenes([2, 0]); byte[] patched = workspace.ApplyToRom(rom); Check(reordered[0].Name == "Second" && reordered[1].Name == "First" && workspace.SceneTopologyEdits.Any(e => e.Order is not null) && workspace.EntranceEdits.Count == 2 && Read32(patched, sceneTable) == (uint)secondStart && Read32(patched, sceneTable + 40) == (uint)firstStart && Read32(patched, sceneTable + 20) == 0 && patched[entranceTable] == 2 && patched[entranceTable + 4] == 0, "Native scene-table reorder did not swap loaded scenes, preserve the empty slot, and remap entrances.");
});

Test("Native room-table order remaps transitions and exits", () => {
    byte[] rom = new byte[0x400]; int sceneTable = 0x100, sceneStart = 0x180, sceneEnd = 0x1C0, firstRoom = 0x300, secondRoom = 0x340;
    Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x04; rom[sceneStart + 1] = 2; Put32(rom, sceneStart + 4, 0x02000010); rom[sceneStart + 8] = 0x0E; rom[sceneStart + 9] = 1; Put32(rom, sceneStart + 12, 0x02000028); rom[sceneStart + 0x20] = 0x14;
    Put32(rom, sceneStart + 0x10, (uint)firstRoom); Put32(rom, sceneStart + 0x14, (uint)(firstRoom + 0x20)); Put32(rom, sceneStart + 0x18, (uint)secondRoom); Put32(rom, sceneStart + 0x1C, (uint)(secondRoom + 0x20));
    rom[firstRoom] = 0x08; rom[firstRoom + 1] = 1; Put32(rom, firstRoom + 4, 0x03000010); rom[firstRoom + 8] = 0x14; Put16(rom, firstRoom + 0x10, 0x0203);
    rom[secondRoom] = 0x08; rom[secondRoom + 1] = 1; Put32(rom, secondRoom + 4, 0x03000010); rom[secondRoom + 8] = 0x14; Put16(rom, secondRoom + 0x10, 0x0004);
    string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant(); var rooms = new[] { new RomRoom(0, (uint)firstRoom, (uint)(firstRoom + 0x20), [], 0, false, false, [], null, [], null, [0x0203]), new RomRoom(1, (uint)secondRoom, (uint)(secondRoom + 0x20), [], 0, false, false, [], null, [], null, [0x0004]) }; var transition = new RomTransition(0, 1, 1, 2, 3, 4, 5, 6, 7, 8); var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, rooms, false, [], Transitions: [transition]); var workspace = new RomWorkspace(new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []));
    var reordered = workspace.ReorderRooms(0, [1, 0]); byte[] patched = workspace.ApplyToRom(rom); Check(reordered[0].Exits![0] == 0x0204 && reordered[1].Exits![0] == 0x0003 && reordered[0].Id == 0 && workspace.TransitionEdits.Count == 1 && workspace.RoomExitEdits.Count == 2 && Read32(patched, sceneStart + 0x10) == (uint)secondRoom && Read32(patched, sceneStart + 0x18) == (uint)firstRoom && Read16(patched, secondRoom + 0x10) == 0x0204 && Read16(patched, firstRoom + 0x10) == 0x0003 && patched[sceneStart + 0x28] == 1 && patched[sceneStart + 0x29] == 1 && patched[sceneStart + 0x2A] == 0 && patched[sceneStart + 0x2B] == 2, $"Native room-table reorder did not preserve room content, cameras, transitions, and exits: doc={reordered[0].Exits![0]:X4}/{reordered[1].Exits![0]:X4}, table={Read32(patched, sceneStart + 0x10):X8}/{Read32(patched, sceneStart + 0x18):X8}, exits={Read16(patched, secondRoom + 0x10):X4}/{Read16(patched, firstRoom + 0x10):X4}, transition={patched[sceneStart + 0x28]:X2}/{patched[sceneStart + 0x29]:X2}/{patched[sceneStart + 0x2A]:X2}/{patched[sceneStart + 0x2B]:X2}");
    workspace.ReorderRooms(0, [1, 0]); byte[] restored = workspace.ApplyToRom(rom);
    Check(workspace.RoomOrderEdits[0].Order.SequenceEqual([0, 1]) && Read32(restored, sceneStart + 0x10) == (uint)firstRoom && Read32(restored, sceneStart + 0x18) == (uint)secondRoom && Read16(restored, firstRoom + 0x10) == 0x0203 && restored[sceneStart + 0x29] == 1, "Repeated room reorders did not compose back to the original native layout.");
});

Test("Native alternate headers clone and delete through the scene table", () => {
    byte[] rom = new byte[0x260]; int sceneTable = 0x100, sceneStart = 0x140, sceneEnd = 0x180, headerTable = sceneStart + 0x20;
    Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x18; Put32(rom, sceneStart + 4, 0x02000020); rom[sceneStart + 8] = 0x14; rom[sceneStart + 0x10] = 0x19; rom[sceneStart + 0x17] = 2; rom[sceneStart + 0x18] = 0x14; Put32(rom, headerTable, 0x02000010); Put32(rom, headerTable + 4, 0);
    using var sha = System.Security.Cryptography.SHA256.Create(); string fingerprint = Convert.ToHexString(sha.ComputeHash(rom)).ToLowerInvariant();
    var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, [], false, [], AlternateHeaders: [new RomAlternateHeader(1, 0x02000010, false, Settings: new RomSceneSettings(1, 2))]);
    var document = new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []); var workspace = new RomWorkspace(document);
    var clone = workspace.CloneAlternateHeader(0); Check(clone.Index == 2 && clone.IsClone && workspace.AlternateHeaderEdits.Count == 1, "Alternate header clone was not staged."); byte[] cloned = workspace.ApplyToRom(rom); Check((uint)(cloned[headerTable + 4] << 24 | cloned[headerTable + 5] << 16 | cloned[headerTable + 6] << 8 | cloned[headerTable + 7]) == 0x02000000, "Alternate header clone did not write a base pointer.");
    workspace.EditAlternateSceneSettings(0, 1, new RomSceneSettings(7, 9)); byte[] settingsPatched = workspace.ApplyToRom(rom); Check(settingsPatched[sceneStart + 0x11] == 7 && settingsPatched[sceneStart + 0x17] == 9 && workspace.AlternateSceneSettingsEdits.Count == 1, "Alternate scene settings did not write independently.");
    workspace.DeleteAlternateHeader(0, 1); byte[] deleted = workspace.ApplyToRom(rom); Check(deleted[headerTable] == 0 && deleted[headerTable + 1] == 0 && workspace.AlternateHeaderEdits.Count == 2, "Alternate header deletion did not write an empty table slot.");
});

Test("Native alternate header room actors are staged independently", () => {
    var actor = new RomActor(0x123, 1, 2, 3, 4, 5, 6, 7); var geometry = new RomRoomGeometry([new RomGeometryVertex(1, 2, 3, 0, 0, 255, 255, 255, 255)], [], 1, [16]); var room = new RomRoom(0, 0, 40, [actor], 1, false, false, [], geometry, [0x1234], new RomRoomSettings(1, 2, 3, 4, 5, 6), [0x0102]); var header = new RomAlternateHeader(1, 0x02000010, false, Rooms: [room]); var scene = new RomScene(0, "Scene", 0, 40, [], false, [], AlternateHeaders: [header]); var document = new RomDocument("test", new RomProfile("test", "Test", 0, 20), [scene], []); var workspace = new RomWorkspace(document); workspace.EditAlternateRoomActor(0, 1, 0, 0, actor with { X = 77 }); workspace.EditAlternateRoomObject(0, 1, 0, 0, 0x4567); workspace.EditAlternateRoomSettings(0, 1, 0, new RomRoomSettings(9, 8, 7, 6, 5, 4)); workspace.EditAlternateRoomExit(0, 1, 0, 0, 0x0456); workspace.EditAlternateGeometryVertex(0, 1, 0, 0, geometry.Vertices[0] with { X = 99 });
    Check(workspace.Document.Scenes[0].AlternateHeaders![0].Rooms![0].Actors[0].X == 77 && workspace.Document.Scenes[0].AlternateHeaders![0].Rooms![0].Objects![0] == 0x4567 && workspace.Document.Scenes[0].AlternateHeaders![0].Rooms![0].Settings!.Behavior == 9 && workspace.Document.Scenes[0].AlternateHeaders![0].Rooms![0].Exits![0] == 0x0456 && workspace.Document.Scenes[0].AlternateHeaders![0].Rooms![0].Geometry!.Vertices[0].X == 99 && workspace.AlternateActorEdits.Count == 1 && workspace.AlternateGeometryVertexEdits.Count == 1 && workspace.WritePatch().Contains("AlternateGeometryVertices"), "Alternate-header room edits were not staged independently.");
});
Test("Native alternate room cloning appends a copied room and relocates its room table", () => {
    byte[] rom = new byte[0x600]; int sceneTable = 0x100, sceneStart = 0x140, sceneEnd = 0x1C0, header = sceneStart + 0x40, roomTable = sceneStart + 0x50, roomStart = 0x280, roomEnd = 0x2C0; Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x18; Put32(rom, sceneStart + 4, 0x02000020); rom[sceneStart + 8] = 0x14; Put32(rom, sceneStart + 0x20, 0x02000040); rom[header] = 0x04; rom[header + 1] = 1; Put32(rom, header + 4, 0x02000050); rom[header + 8] = 0x14; Put32(rom, roomTable, (uint)roomStart); Put32(rom, roomTable + 4, (uint)roomEnd); rom[roomStart] = 0x14; rom[roomStart + 1] = 0x5A;
    string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant(); var room = new RomRoom(0, (uint)roomStart, (uint)roomEnd, [], 0, false, false, []); var alternate = new RomAlternateHeader(1, 0x02000040, false, Rooms: [room]); var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, [], false, [], AlternateHeaders: [alternate]); var workspace = new RomWorkspace(new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []));
    var clone = workspace.CloneAlternateRoom(0, 1, 0); byte[] patched = workspace.ApplyToRom(rom); int relocatedScene = checked((int)Read32(patched, sceneTable)); byte[] decodedScene = N64Compression.Decode(patched.AsSpan(relocatedScene, checked((int)Read32(patched, sceneTable + 4) - relocatedScene)).ToArray()); int relocatedHeaderTable = checked((int)(Read32(decodedScene, 4) & 0x00FFFFFF)); int relocatedHeader = checked((int)(Read32(decodedScene, relocatedHeaderTable) & 0x00FFFFFF)); int relocatedRoomTable = checked((int)(Read32(decodedScene, relocatedHeader + 4) & 0x00FFFFFF)); uint cloneStart = Read32(decodedScene, relocatedRoomTable + 8); uint cloneEnd = Read32(decodedScene, relocatedRoomTable + 12);
    Check(clone.Id == 1 && decodedScene[relocatedHeader + 1] == 2 && cloneStart != roomStart && cloneEnd > cloneStart && cloneStart < (uint)patched.Length - 1 && patched[(int)cloneStart] == 0x14 && patched[(int)cloneStart + 1] == 0x5A, "Native alternate room clone did not append a copied room and relocate its room table.");
});
Test("Native alternate room deletion shrinks its room table", () => {
    byte[] rom = new byte[0x600]; int sceneTable = 0x100, sceneStart = 0x140, sceneEnd = 0x1C0, header = sceneStart + 0x40, roomTable = sceneStart + 0x50, firstRoom = 0x280, secondRoom = 0x2C0; Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x18; Put32(rom, sceneStart + 4, 0x02000020); rom[sceneStart + 8] = 0x14; Put32(rom, sceneStart + 0x20, 0x02000040); rom[header] = 0x04; rom[header + 1] = 2; Put32(rom, header + 4, 0x02000050); rom[header + 8] = 0x14; Put32(rom, roomTable, (uint)firstRoom); Put32(rom, roomTable + 4, (uint)(firstRoom + 0x20)); Put32(rom, roomTable + 8, (uint)secondRoom); Put32(rom, roomTable + 12, (uint)(secondRoom + 0x20)); rom[firstRoom] = 0x14; rom[firstRoom + 1] = 0x11; rom[secondRoom] = 0x14; rom[secondRoom + 1] = 0x22;
    string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant(); var rooms = new[] { new RomRoom(0, (uint)firstRoom, (uint)(firstRoom + 0x20), [], 0, false, false, []), new RomRoom(1, (uint)secondRoom, (uint)(secondRoom + 0x20), [], 0, false, false, []) }; var alternate = new RomAlternateHeader(1, 0x02000040, false, Rooms: rooms); var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, [], false, [], AlternateHeaders: [alternate]); var workspace = new RomWorkspace(new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []));
    workspace.DeleteAlternateRoom(0, 1, 0); byte[] patched = workspace.ApplyToRom(rom); int relocatedScene = checked((int)Read32(patched, sceneTable)); byte[] decodedScene = N64Compression.Decode(patched.AsSpan(relocatedScene, checked((int)Read32(patched, sceneTable + 4) - relocatedScene)).ToArray()); int relocatedHeaderTable = checked((int)(Read32(decodedScene, 4) & 0x00FFFFFF)); int relocatedHeader = checked((int)(Read32(decodedScene, relocatedHeaderTable) & 0x00FFFFFF)); int relocatedRoomTable = checked((int)(Read32(decodedScene, relocatedHeader + 4) & 0x00FFFFFF));
    Check(decodedScene[relocatedHeader + 1] == 1 && Read32(decodedScene, relocatedRoomTable) == secondRoom && patched[secondRoom + 1] == 0x22, "Native alternate room deletion did not shrink the room table while preserving the remaining room.");
});
Test("Native alternate room reordering permutes the alternate room table", () => {
    byte[] rom = new byte[0x600]; int sceneTable = 0x100, sceneStart = 0x140, sceneEnd = 0x1C0, header = sceneStart + 0x40, roomTable = sceneStart + 0x50, firstRoom = 0x280, secondRoom = 0x2C0; Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x18; Put32(rom, sceneStart + 4, 0x02000020); rom[sceneStart + 8] = 0x14; Put32(rom, sceneStart + 0x20, 0x02000040); rom[header] = 0x04; rom[header + 1] = 2; Put32(rom, header + 4, 0x02000050); rom[header + 8] = 0x14; Put32(rom, roomTable, (uint)firstRoom); Put32(rom, roomTable + 4, (uint)(firstRoom + 0x20)); Put32(rom, roomTable + 8, (uint)secondRoom); Put32(rom, roomTable + 12, (uint)(secondRoom + 0x20)); rom[firstRoom] = 0x14; rom[firstRoom + 1] = 0x11; rom[secondRoom] = 0x14; rom[secondRoom + 1] = 0x22;
    string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant(); var rooms = new[] { new RomRoom(0, (uint)firstRoom, (uint)(firstRoom + 0x20), [], 0, false, false, []), new RomRoom(1, (uint)secondRoom, (uint)(secondRoom + 0x20), [], 0, false, false, []) }; var alternate = new RomAlternateHeader(1, 0x02000040, false, Rooms: rooms); var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, [], false, [], AlternateHeaders: [alternate]); var workspace = new RomWorkspace(new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []));
    var reordered = workspace.ReorderAlternateRooms(0, 1, [1, 0]); byte[] patched = workspace.ApplyToRom(rom); byte[] decodedScene = N64Compression.Decode(patched.AsSpan(sceneStart, sceneEnd - sceneStart).ToArray()); int headerTable = checked((int)(Read32(decodedScene, 4) & 0x00FFFFFF)); int relocatedHeader = checked((int)(Read32(decodedScene, headerTable) & 0x00FFFFFF)); int table = checked((int)(Read32(decodedScene, relocatedHeader + 4) & 0x00FFFFFF));
    Check(reordered[0].Id == 0 && reordered[0].Start == secondRoom && workspace.AlternateRoomOrderEdits.Count == 1 && Read32(decodedScene, table) == secondRoom && Read32(decodedScene, table + 8) == firstRoom, "Native alternate room reorder did not permute the alternate room table.");
});

Test("Native alternate room object list insertion and removal round-trip", () => {
    byte[] rom = new byte[0x1000]; int sceneTable = 0x100, sceneStart = 0x140, sceneEnd = 0x1C0, header = sceneStart + 0x40, roomTable = sceneStart + 0x50, roomStart = 0x300, roomEnd = 0x380;
    Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x18; Put32(rom, sceneStart + 4, 0x02000020); rom[sceneStart + 8] = 0x14; Put32(rom, sceneStart + 0x20, 0x02000040);
    rom[header] = 0x04; rom[header + 1] = 1; Put32(rom, header + 4, 0x02000050); rom[header + 8] = 0x14;
    Put32(rom, roomTable, (uint)roomStart); Put32(rom, roomTable + 4, (uint)roomEnd);
    rom[roomStart] = 0x0B; rom[roomStart + 1] = 1; Put32(rom, roomStart + 4, 0x03000020); rom[roomStart + 8] = 0x14; Put16(rom, roomStart + 0x20, 0x1234);
    string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant(); var room = new RomRoom(0, (uint)roomStart, (uint)roomEnd, [], 1, false, false, [], Objects: [0x1234]); var alternate = new RomAlternateHeader(1, 0x02000040, false, Rooms: [room]); var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, [], false, [], AlternateHeaders: [alternate]); var workspace = new RomWorkspace(new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []));
    workspace.EditAlternateRoomObjects(0, 1, 0, [0x1234, 0x2345, 0x3456, 0x4567]); Check(workspace.AlternateObjectListEdits.Count == 1 && workspace.WritePatch().Contains("AlternateObjectLists"), "Alternate object list insertion was not staged.");
    byte[] patched = workspace.ApplyToRom(rom); int patchedSceneStart = checked((int)Read32(patched, sceneTable)); int patchedSceneEnd = checked((int)Read32(patched, sceneTable + 4)); byte[] decodedScene = N64Compression.Decode(patched.AsSpan(patchedSceneStart, patchedSceneEnd - patchedSceneStart).ToArray()); int decodedHeader = checked((int)(Read32(decodedScene, 0x20) & 0x00FFFFFF)); int decodedRoomTable = checked((int)(Read32(decodedScene, decodedHeader + 4) & 0x00FFFFFF)); uint patchedRoomStart = Read32(decodedScene, decodedRoomTable); uint patchedRoomEnd = Read32(decodedScene, decodedRoomTable + 4); byte[] decodedRoom = N64Compression.Decode(patched.AsSpan(checked((int)patchedRoomStart), checked((int)(patchedRoomEnd - patchedRoomStart))).ToArray());
    Check(decodedRoom[1] == 4 && Read16(decodedRoom, 0x20) == 0x1234 && Read16(decodedRoom, 0x22) == 0x2345 && Read16(decodedRoom, 0x26) == 0x4567, "Alternate object list insertion did not round-trip through native export.");
    workspace.EditAlternateRoomObjects(0, 1, 0, [0xCAFE]); Check(workspace.Document.Scenes[0].AlternateHeaders![0].Rooms![0].ObjectCount == 1 && workspace.AlternateObjectListEdits.Count == 1, "Alternate object list removal did not update the workspace model.");
});

Test("Native alternate room actor list insertion and removal round-trip", () => {
    byte[] rom = new byte[0x1000]; int sceneTable = 0x100, sceneStart = 0x140, sceneEnd = 0x1C0, header = sceneStart + 0x40, roomTable = sceneStart + 0x50, roomStart = 0x300, roomEnd = 0x380;
    Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd); rom[sceneStart] = 0x18; Put32(rom, sceneStart + 4, 0x02000020); rom[sceneStart + 8] = 0x14; Put32(rom, sceneStart + 0x20, 0x02000040); rom[header] = 0x04; rom[header + 1] = 1; Put32(rom, header + 4, 0x02000050); rom[header + 8] = 0x14; Put32(rom, roomTable, (uint)roomStart); Put32(rom, roomTable + 4, (uint)roomEnd);
    rom[roomStart] = 0x01; rom[roomStart + 1] = 1; Put32(rom, roomStart + 4, 0x03000020); rom[roomStart + 8] = 0x14; Put16(rom, roomStart + 0x20, 0x0123); Put16(rom, roomStart + 0x22, unchecked((ushort)-10));
    string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant(); var actor = new RomActor(0x0123, -10, 2, 3, 4, 5, 6, 7); var room = new RomRoom(0, (uint)roomStart, (uint)roomEnd, [actor], 0, false, false, []); var alternate = new RomAlternateHeader(1, 0x02000040, false, Rooms: [room]); var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, [], false, [], AlternateHeaders: [alternate]); var workspace = new RomWorkspace(new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []));
    var second = actor with { Number = 0x0456, X = 77, Variable = 0x88 }; workspace.EditAlternateRoomActors(0, 1, 0, [actor, second]); Check(workspace.AlternateActorListEdits.Count == 1 && workspace.WritePatch().Contains("AlternateActorLists"), "Alternate actor list insertion was not staged.");
    byte[] patched = workspace.ApplyToRom(rom); int patchedSceneStart = checked((int)Read32(patched, sceneTable)); int patchedSceneEnd = checked((int)Read32(patched, sceneTable + 4)); byte[] decodedScene = N64Compression.Decode(patched.AsSpan(patchedSceneStart, patchedSceneEnd - patchedSceneStart).ToArray()); int decodedHeader = checked((int)(Read32(decodedScene, 0x20) & 0x00FFFFFF)); int decodedRoomTable = checked((int)(Read32(decodedScene, decodedHeader + 4) & 0x00FFFFFF)); uint patchedRoomStart = Read32(decodedScene, decodedRoomTable); uint patchedRoomEnd = Read32(decodedScene, decodedRoomTable + 4); byte[] decodedRoom = N64Compression.Decode(patched.AsSpan(checked((int)patchedRoomStart), checked((int)(patchedRoomEnd - patchedRoomStart))).ToArray());
    Check(decodedRoom[1] == 2 && Read16(decodedRoom, 0x20) == 0x0123 && Read16(decodedRoom, 0x30) == 0x0456 && Read16(decodedRoom, 0x3E) == 0x0088, "Alternate actor list insertion did not round-trip through native export.");
    workspace.EditAlternateRoomActors(0, 1, 0, []); Check(workspace.Document.Scenes[0].AlternateHeaders![0].Rooms![0].Actors.Count == 0 && workspace.AlternateActorListEdits.Count == 1, "Alternate actor list removal did not update the workspace model.");
});

Test("Native alternate header collision cameras and waterboxes are staged independently", () => {
    var camera = new RomCamera(3, 1, 2, 3, 4, 5, 6, 45, 7, 8);
    var waterbox = new RomWaterbox(10, 20, 30, 40, 50, 60, 0x12345678);
    var collision = new RomCollisionData(0, 0, 0, 100, 100, 100, [], [], [], [waterbox], [camera]);
    var header = new RomAlternateHeader(1, 0x02000010, false, HasCollision: true, Collision: collision);
    var scene = new RomScene(0, "Scene", 0, 40, [], false, [], AlternateHeaders: [header]);
    var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 20), [scene], []));
    workspace.EditAlternateCamera(0, 1, 0, camera with { Fov = 60 });
    workspace.EditAlternateWaterbox(0, 1, 0, waterbox with { Y = 99, Properties = 0xAABBCCDD });
    Check(workspace.Document.Scenes[0].AlternateHeaders![0].Collision!.Cameras![0].Fov == 60 && workspace.Document.Scenes[0].AlternateHeaders[0].Collision.Waterboxes![0].Y == 99 && workspace.AlternateCameraEdits.Count == 1 && workspace.AlternateWaterboxEdits.Count == 1 && workspace.WritePatch().Contains("AlternateCameras") && workspace.WritePatch().Contains("AlternateWaterboxes"), "Alternate-header collision camera/waterbox edits were not staged independently.");
    workspace.ClearEdits();
    Check(workspace.AlternateCameraEdits.Count == 0 && workspace.AlternateWaterboxEdits.Count == 0, "Alternate collision edits were not cleared.");
});

Test("Native alternate header collision bounds are staged", () => {
    var collision = new RomCollisionData(-10, -20, -30, 10, 20, 30, [], [], []);
    var header = new RomAlternateHeader(1, 0x02000010, false, HasCollision: true, Collision: collision);
    var scene = new RomScene(0, "Scene", 0, 40, [], false, [], AlternateHeaders: [header]);
    var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 20), [scene], []));
    workspace.EditAlternateCollisionBounds(0, 1, collision with { MinX = -100, MaxZ = 300 });
    Check(workspace.Document.Scenes[0].AlternateHeaders![0].Collision!.MinX == -100 && workspace.Document.Scenes[0].AlternateHeaders[0].Collision.MaxZ == 300 && workspace.AlternateCollisionBoundsEdits.Count == 1 && workspace.WritePatch().Contains("AlternateCollisionBounds"), "Alternate collision bounds were not staged.");
});

Test("Native alternate collision topology replacement relocates and round-trips", () => {
    byte[] rom = new byte[0xB72000]; Put32(rom, 0, 0x80371240); int table = 0xB71440, scene = 0x1000;
    Put32(rom, table, (uint)scene); Put32(rom, table + 4, 0x1200); Put32(rom, table + 20, 0x2000); Put32(rom, table + 24, 0x2080);
    rom[scene] = 0x18; Put32(rom, scene + 4, 0x02000020); rom[scene + 8] = 0x14; Put32(rom, scene + 0x20, 0x02000040); int header = scene + 0x40; rom[header] = 0x02; Put32(rom, header + 4, 0x02000080); rom[header + 8] = 0x14; int collision = scene + 0x80; Put16(rom, collision + 12, 1); Put32(rom, collision + 16, 0x020000C0); Put16(rom, collision + 20, 1); Put32(rom, collision + 24, 0x020000D0); Put32(rom, collision + 28, 0x020000F0);
    Put16(rom, scene + 0xC0, 1); Put16(rom, scene + 0xC2, 2); Put16(rom, scene + 0xC4, 3); Put16(rom, scene + 0xD0, 1); Put16(rom, scene + 0xD2, 0); Put16(rom, scene + 0xD4, 0); Put16(rom, scene + 0xD6, 0); Put64(rom, scene + 0xF0, 0x0102030405060708UL);
    var decoded = RomDecoder.Read(rom); var source = decoded.Scenes[0].AlternateHeaders![0].Collision!; var workspace = new RomWorkspace(decoded); var vertices = source.Vertices.Concat([new RomCollisionVertex(4, 5, 6)]).ToArray(); var triangles = source.Triangles.Concat([new RomCollisionTriangle(2, 0, 0, 1, 0, 1, 0, 0)]).ToArray(); var surfaces = source.SurfaceTypes.Concat([0x1112131415161718UL]).ToArray(); workspace.ReplaceAlternateCollisionTopology(0, 1, vertices, triangles, surfaces);
    Check(workspace.AlternateCollisionTopologyEdits.Count == 1 && workspace.WritePatch().Contains("AlternateCollisionTopologies"), "Alternate collision topology replacement was not staged.");
    var patched = workspace.ApplyToRom(rom); var collisionAfter = RomDecoder.Read(patched).Scenes[0].AlternateHeaders![0].Collision!;
    Check(collisionAfter.Vertices.Count == 2 && collisionAfter.Triangles.Count == 2 && collisionAfter.Vertices[1].Z == 6 && collisionAfter.SurfaceTypes[1] == 0x1112131415161718UL, "Alternate collision topology replacement did not round-trip.");
});

Test("Native alternate header command edits are staged", () => {
    var commands = new[] { new RomHeaderCommand(0x19, 1, 0x02000020), new RomHeaderCommand(0x14, 0, 0) };
    var header = new RomAlternateHeader(1, 0x02000010, false, Commands: commands);
    var scene = new RomScene(0, "Scene", 0, 40, [], false, [], AlternateHeaders: [header]);
    var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 20), [scene], []));
    workspace.EditAlternateCommand(0, 1, 0, new RomHeaderCommand(0x02, 3, 0x02000080));
    Check(workspace.Document.Scenes[0].AlternateHeaders![0].Commands![0].Command == 0x02 && workspace.AlternateCommandEdits.Count == 1 && workspace.WritePatch().Contains("AlternateCommands"), "Alternate command edit was not staged.");
});

Test("Native alternate header command insertion and final deletion are staged safely", () => {
    var commands = new[] { new RomHeaderCommand(0x19, 1, 0x02000020) };
    var header = new RomAlternateHeader(1, 0x02000010, false, Commands: commands);
    var scene = new RomScene(0, "Scene", 0, 40, [], false, [], AlternateHeaders: [header]);
    var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 20), [scene], []));
    workspace.InsertAlternateCommand(0, 1, new RomHeaderCommand(0x02, 0, 0x02000080));
    Check(workspace.Document.Scenes[0].AlternateHeaders![0].Commands!.Count == 2 && workspace.AlternateCommandEdits[^1].Insert, "Alternate command insertion was not staged.");
    workspace.DeleteAlternateCommand(0, 1, 1);
    Check(workspace.Document.Scenes[0].AlternateHeaders[0].Commands!.Count == 1 && workspace.AlternateCommandEdits[^1].Delete, "Final alternate command deletion was not staged.");
    workspace.InsertAlternateCommand(0, 1, new RomHeaderCommand(0x03, 0, 0x02000090));
    workspace.InsertAlternateCommand(0, 1, new RomHeaderCommand(0x04, 0, 0x020000A0));
    bool rejected = false; try { workspace.DeleteAlternateCommand(0, 1, 0); } catch (InvalidDataException) { rejected = true; }
    Check(rejected, "Unsafe middle alternate command deletion was accepted.");
});

Test("Native room geometry exports as a reusable SectionTile", () => {
    var geometry = new RomRoomGeometry([new RomGeometryVertex(1, 2, 3, 0, 0, 255, 0, 0, 255), new RomGeometryVertex(4, 5, 6, 0, 0, 0, 255, 0, 255), new RomGeometryVertex(7, 8, 9, 0, 0, 0, 0, 255, 255)], [new RomGeometryTriangle(0, 1, 2)], 1);
    var room = new RomRoom(0, 0, 100, [], 0, true, false, [], geometry); var scene = new RomScene(3, "Scene", 0, 100, [room], false, []); var document = new RomDocument("fingerprint", new RomProfile("test", "Test", 0, 40), [scene], []);
    string xml = RomTileExporter.Export(document, 3, 0); var preview = DesktopImport.ReadTile(xml);
    Check(preview.Name == "Scene 03 Room 00" && preview.Vertices.Length == 3 && preview.Indices.SequenceEqual([0, 1, 2]) && preview.UVs.Length == 3 && preview.FaceColors.Length == 1 && MathF.Abs(preview.FaceColors[0].X - 1f / 3f) < 0.01f && xml.Contains("fingerprint"), "Native room geometry did not export as a reusable tile with visual data.");
    var splitGeometry = new RomRoomGeometry([new RomGeometryVertex(0, 0, 0, 0, 0, 255, 0, 0, 255), new RomGeometryVertex(10, 0, 0, 0, 0, 255, 0, 0, 255), new RomGeometryVertex(0, 0, 10, 0, 0, 255, 0, 0, 255), new RomGeometryVertex(200, 0, 0, 0, 0, 255, 0, 0, 255), new RomGeometryVertex(210, 0, 0, 0, 0, 255, 0, 0, 255), new RomGeometryVertex(200, 0, 10, 0, 0, 255, 0, 0, 255)], [new RomGeometryTriangle(0, 1, 2), new RomGeometryTriangle(3, 4, 5)], 1);
    var splitScene = scene with { Rooms = [room with { Geometry = splitGeometry }] }; var splitDocument = document with { Scenes = [splitScene] }; var tiles = RomTileExporter.ExportTiles(splitDocument, 3, 0, 100);
    Check(tiles.Count == 2 && tiles.All(tile => DesktopImport.ReadTile(tile.Xml).Vertices.Length == 3), "Room geometry was not split into remapped tiles.");
});

Test("Native Yaz0 and MIO0 decoders expand bounded file blobs", () => {
    byte[] yaz = [ (byte)'Y', (byte)'a', (byte)'z', (byte)'0', 0, 0, 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0xE0, (byte)'a', (byte)'b', (byte)'c' ];
    Check(Encoding.ASCII.GetString(N64Compression.Decode(yaz)) == "abc", "Yaz0 literal decode failed.");
    byte[] mio = new byte[23]; mio[0] = (byte)'M'; mio[1] = (byte)'I'; mio[2] = (byte)'O'; mio[3] = (byte)'0'; Put32(mio, 4, 3); Put32(mio, 8, 20); Put32(mio, 12, 20); Put32(mio, 16, 0xE0000000); mio[20] = (byte)'a'; mio[21] = (byte)'b'; mio[22] = (byte)'c';
    Check(Encoding.ASCII.GetString(N64Compression.Decode(mio)) == "abc", "MIO0 literal decode failed.");
    byte[] repeated = Encoding.ASCII.GetBytes("abcabcabcabcabcabc"); Check(N64Compression.Decode(N64Compression.EncodeYaz0(repeated)).SequenceEqual(repeated), "Yaz0 encoder round-trip failed.");
    Check(N64Compression.Decode(N64Compression.EncodeMio0(repeated)).SequenceEqual(repeated), "MIO0 encoder round-trip failed.");
});

Test("SharpOcarina OoT actor metadata decodes variable masks and flag labels", () => {
    var database = ActorMetadataDatabase.LoadOoT();
    Check(database.TryDescribe(new RomActor(0x000A, 0, 0, 0, 0, 0, 3, 0x0012), out var chest) && chest.Name.Contains("Treasure Chest", StringComparison.OrdinalIgnoreCase), "OoT actor metadata was not loaded.");
    Check(chest.Properties.Any(p => p.Name.Contains("Switch Flag", StringComparison.OrdinalIgnoreCase) && p.Value == 3), "Actor switch flag mask was not decoded.");
});
Test("Native ROM workspace stages actor edits without changing source tile packages", () => {
    var actor = new RomActor(1, 1, 2, 3, 4, 5, 6, 7);
    var document = new RomDocument("test", new RomProfile("test", "Test", 0, 40), [new RomScene(0, "Scene", 0, 1, [new RomRoom(0, 0, 1, [actor], 0, false, false, [])], false, [])], []);
    var workspace = new RomWorkspace(document); workspace.EditRoomActor(0, 0, 0, actor with { X = 99, Variable = 42 });
    Check(workspace.Document.Scenes[0].Rooms[0].Actors[0].X == 99 && workspace.ActorEdits.Count == 1 && workspace.WritePatch().Contains("42"), "Native actor edit was not staged.");
});
Test("Native actor group transforms rotate, scale, translate and wrap BINANG values", () => {
    var actors = new[] { new RomActor(1, -10, 0, 0, short.MaxValue, 0, 0, 0), new RomActor(2, 10, 0, 0, 0, 0, 0, 0), new RomActor(3, 100, 100, 100, 0, 0, 0, 0) };
    var transformed = RomActorTransforms.Apply(actors, [0, 1], moveY: 5, rotateY: RomActorTransforms.BinangFromDegrees(90), scale: 2f);
    Check(transformed[0].X == 0 && transformed[0].Z == 20 && transformed[0].Y == 5 && transformed[0].RotationY == unchecked((short)0x4000), "Actor group transform did not rotate, scale, translate and wrap selected actors correctly.");
    Check(transformed[1].X == 0 && transformed[1].Z == -20 && transformed[2].X == 100 && transformed[2].Y == 100 && transformed[2].Z == 100, "Actor group transform changed unselected actors.");
    bool rejected = false; try { RomActorTransforms.Apply(actors, [0], scale: 0); } catch (InvalidDataException) { rejected = true; }
    Check(rejected, "Invalid actor group scale was accepted.");
});
Test("Native ROM workspace stages path waypoint edits", () => {
    var path = new RomPath(0, [new RomPathPoint(1, 2, 3), new RomPathPoint(4, 5, 6)]);
    var scene = new RomScene(0, "Scene", 0, 40, [], false, [], null, [], [], [path]);
    var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    workspace.EditPathPoint(0, 0, 1, new RomPathPoint(40, 50, 60));
    Check(workspace.Document.Scenes[0].Paths[0].Points[1].X == 40 && workspace.PathEdits.Count == 1 && workspace.WritePatch().Contains("PathId"), "Native path waypoint edit was not staged.");
    bool rejected = false; try { workspace.ApplyToRom(new byte[40]); } catch (InvalidDataException) { rejected = true; }
    Check(rejected, "Unsupported path ROM write-back was not rejected clearly.");
});
Test("Native path list insertion and waypoint deletion relocate and round-trip", () => {
    byte[] rom = new byte[0x400]; int sceneTable = 0x100, sceneStart = 0x140, sceneEnd = 0x180;
    Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd); rom[sceneStart] = 0x0D; rom[sceneStart + 1] = 1; Put32(rom, sceneStart + 4, 0x02000020); rom[sceneStart + 8] = 0x14;
    rom[sceneStart + 0x20] = 1; Put32(rom, sceneStart + 0x24, 0x02000030); Put16(rom, sceneStart + 0x30, 1); Put16(rom, sceneStart + 0x32, 2); Put16(rom, sceneStart + 0x34, 3);
    using var sha = System.Security.Cryptography.SHA256.Create(); string fingerprint = Convert.ToHexString(sha.ComputeHash(rom)).ToLowerInvariant(); var originalPath = new RomPath(0, [new RomPathPoint(1, 2, 3)]);
    var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, [], false, [], Paths: [originalPath]); var workspace = new RomWorkspace(new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []));
    workspace.ReplacePaths(0, [new RomPath(0, [new RomPathPoint(10, 20, 30), new RomPathPoint(40, 50, 60)]), new RomPath(1, [new RomPathPoint(70, 80, 90)])]);
    Check(workspace.PathListEdits.Count == 1 && workspace.WritePatch().Contains("PathLists"), "Native path list replacement was not staged.");
    byte[] patched = workspace.ApplyToRom(rom); int relocated = (int)Read32(patched, sceneTable); int command = relocated; int table = (int)(Read32(patched, command + 4) & 0x00FFFFFF); int firstPoints = (int)(Read32(patched, relocated + table + 4) & 0x00FFFFFF); int secondEntry = table + 8; int secondPoints = (int)(Read32(patched, relocated + secondEntry + 4) & 0x00FFFFFF);
    Check(relocated != sceneStart && patched[command + 1] == 2 && patched[relocated + table] == 2 && patched[relocated + secondEntry] == 1 && Read16(patched, relocated + firstPoints) == 10 && Read16(patched, relocated + secondPoints + 2) == 80, $"Native path list insertion did not relocate or write all waypoints: relocated=0x{relocated:X}, count={patched[command + 1]}, table=0x{table:X}, first={Read16(patched, relocated + firstPoints)}, secondY={Read16(patched, relocated + secondPoints + 2)}.");
});
Test("Native room cloning appends a copied room and relocates the room table", () => {
    byte[] rom = new byte[0x600]; int sceneTable = 0x100, sceneStart = 0x180, sceneEnd = 0x1C0, roomStart = 0x280, roomEnd = 0x2C0; Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x04; rom[sceneStart + 1] = 1; Put32(rom, sceneStart + 4, 0x02000010); Put32(rom, sceneStart + 0x10, (uint)roomStart); Put32(rom, sceneStart + 0x14, (uint)roomEnd); rom[sceneStart + 8] = 0x14; rom[roomStart] = 0x14; rom[roomStart + 1] = 0xA5;
    string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant(); var sourceRoom = new RomRoom(0, (uint)roomStart, (uint)roomEnd, [], 0, false, false, []); var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, [sourceRoom], false, []); var workspace = new RomWorkspace(new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []));
    var clone = workspace.CloneRoom(0, 0); byte[] patched = workspace.ApplyToRom(rom); int relocatedScene = checked((int)Read32(patched, sceneTable)); byte[] decodedScene = N64Compression.Decode(patched.AsSpan(relocatedScene, checked((int)Read32(patched, sceneTable + 4) - relocatedScene)).ToArray()); int table = checked((int)(Read32(decodedScene, 4) & 0x00FFFFFF)); uint cloneStart = Read32(decodedScene, table + 8); uint cloneEnd = Read32(decodedScene, table + 12);
    Check(clone.Id == 1 && decodedScene[1] == 2 && cloneStart != roomStart && cloneEnd > cloneStart && patched[cloneStart] == 0x14 && patched[cloneStart + 1] == 0xA5, "Native room clone did not append a copied room and relocate the room table.");
});
Test("Native room deletion shrinks the room table and preserves remaining entries", () => {
    byte[] rom = new byte[0x600]; int sceneTable = 0x100, sceneStart = 0x180, sceneEnd = 0x1C0, firstRoom = 0x280, secondRoom = 0x2C0; Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x04; rom[sceneStart + 1] = 2; Put32(rom, sceneStart + 4, 0x02000010); Put32(rom, sceneStart + 0x10, (uint)firstRoom); Put32(rom, sceneStart + 0x14, (uint)(firstRoom + 0x20)); Put32(rom, sceneStart + 0x18, (uint)secondRoom); Put32(rom, sceneStart + 0x1C, (uint)(secondRoom + 0x20)); rom[sceneStart + 8] = 0x14; rom[firstRoom] = 0x14; rom[firstRoom + 1] = 0x11; rom[secondRoom] = 0x14; rom[secondRoom + 1] = 0x22;
    string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant(); var rooms = new[] { new RomRoom(0, (uint)firstRoom, (uint)(firstRoom + 0x20), [], 0, false, false, []), new RomRoom(1, (uint)secondRoom, (uint)(secondRoom + 0x20), [], 0, false, false, []) }; var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, rooms, false, []); var workspace = new RomWorkspace(new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []));
    workspace.DeleteRoom(0, 0); byte[] patched = workspace.ApplyToRom(rom); int relocatedScene = checked((int)Read32(patched, sceneTable)); byte[] decodedScene = N64Compression.Decode(patched.AsSpan(relocatedScene, checked((int)Read32(patched, sceneTable + 4) - relocatedScene)).ToArray()); int table = checked((int)(Read32(decodedScene, 4) & 0x00FFFFFF));
    Check(decodedScene[1] == 1 && Read32(decodedScene, table) == secondRoom && patched[secondRoom + 1] == 0x22, "Native room deletion did not shrink the table while preserving the remaining room.");
});
Test("Native room clone inserts in the middle and remaps references", () => {
    byte[] rom = new byte[0x900]; int sceneTable = 0x100, sceneStart = 0x200, sceneEnd = 0x260, firstRoom = 0x300, secondRoom = 0x340;
    Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x04; rom[sceneStart + 1] = 2; Put32(rom, sceneStart + 4, 0x02000040); rom[sceneStart + 8] = 0x0E; rom[sceneStart + 9] = 1; Put32(rom, sceneStart + 12, 0x02000020); rom[sceneStart + 0x10] = 0x14;
    rom[sceneStart + 0x20] = 0; rom[sceneStart + 0x21] = 7; rom[sceneStart + 0x22] = 1; rom[sceneStart + 0x23] = 9;
    Put32(rom, sceneStart + 0x40, (uint)firstRoom); Put32(rom, sceneStart + 0x44, (uint)(firstRoom + 0x20)); Put32(rom, sceneStart + 0x48, (uint)secondRoom); Put32(rom, sceneStart + 0x4C, (uint)(secondRoom + 0x20));
    rom[firstRoom] = 0x08; rom[firstRoom + 1] = 1; Put32(rom, firstRoom + 4, 0x03000010); rom[firstRoom + 8] = 0x14; Put16(rom, firstRoom + 0x10, 0x0203);
    rom[secondRoom] = 0x08; rom[secondRoom + 1] = 1; Put32(rom, secondRoom + 4, 0x03000010); rom[secondRoom + 8] = 0x14; Put16(rom, secondRoom + 0x10, 0x0004);
    string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant();
    var rooms = new[] { new RomRoom(0, (uint)firstRoom, (uint)(firstRoom + 0x20), [], 0, false, false, [], Exits: [0x0203]), new RomRoom(1, (uint)secondRoom, (uint)(secondRoom + 0x20), [], 0, false, false, [], Exits: [0x0004]) };
    var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, rooms, false, [], Transitions: [new RomTransition(0, 7, 1, 9, 3, 4, 5, 6, 7, 8)]);
    var workspace = new RomWorkspace(new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []));
    var inserted = workspace.InsertRoomClone(0, 0, 1); byte[] patched = workspace.ApplyToRom(rom);
    int relocatedScene = checked((int)Read32(patched, sceneTable)); byte[] decoded = N64Compression.Decode(patched.AsSpan(relocatedScene, checked((int)Read32(patched, sceneTable + 4) - relocatedScene)).ToArray()); int table = checked((int)(Read32(decoded, 4) & 0x00FFFFFF)); uint cloneStart = Read32(decoded, table + 8);
    Check(inserted.Id == 1 && workspace.Document.Scenes[0].Rooms.Count == 3 && decoded[1] == 3 && Read32(decoded, table) == (uint)firstRoom && cloneStart != firstRoom && Read32(decoded, table + 16) == (uint)secondRoom && Read16(patched, firstRoom + 0x10) == 0x0403 && Read16(patched, checked((int)cloneStart + 0x10)) == 0x0403 && decoded[0x20] == 0 && decoded[0x21] == 7 && decoded[0x22] == 2 && decoded[0x23] == 9, "Middle room insertion did not relocate the table, copy room bytes, and preserve remapped routing and cameras.");
});
Test("Native room deletion remaps survivors and rejects live references", () => {
    byte[] rom = new byte[0x900]; int sceneTable = 0x100, sceneStart = 0x200, sceneEnd = 0x260, firstRoom = 0x300, middleRoom = 0x340, lastRoom = 0x380;
    Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x04; rom[sceneStart + 1] = 3; Put32(rom, sceneStart + 4, 0x02000040); rom[sceneStart + 8] = 0x0E; rom[sceneStart + 9] = 1; Put32(rom, sceneStart + 12, 0x02000020); rom[sceneStart + 0x10] = 0x14;
    rom[sceneStart + 0x20] = 0; rom[sceneStart + 0x21] = 7; rom[sceneStart + 0x22] = 2; rom[sceneStart + 0x23] = 9;
    uint[] starts = [(uint)firstRoom, (uint)middleRoom, (uint)lastRoom]; for (int i = 0; i < starts.Length; i++) { Put32(rom, sceneStart + 0x40 + i * 8, starts[i]); Put32(rom, sceneStart + 0x44 + i * 8, starts[i] + 0x20); rom[(int)starts[i]] = 0x08; rom[(int)starts[i] + 1] = 1; Put32(rom, (int)starts[i] + 4, 0x03000010); rom[(int)starts[i] + 8] = 0x14; Put16(rom, (int)starts[i] + 0x10, 0x0403); }
    string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant();
    var rooms = starts.Select((start, index) => new RomRoom(index, start, start + 0x20, [], 0, false, false, [], Exits: [0x0403])).ToArray();
    var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, rooms, false, [], Transitions: [new RomTransition(0, 7, 2, 9, 3, 4, 5, 6, 7, 8)]);
    var document = new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []);
    var workspace = new RomWorkspace(document); workspace.DeleteRoom(0, 1); byte[] patched = workspace.ApplyToRom(rom);
    int relocatedScene = checked((int)Read32(patched, sceneTable)); byte[] decoded = N64Compression.Decode(patched.AsSpan(relocatedScene, checked((int)Read32(patched, sceneTable + 4) - relocatedScene)).ToArray()); int table = checked((int)(Read32(decoded, 4) & 0x00FFFFFF));
    Check(decoded[1] == 2 && Read32(decoded, table) == (uint)firstRoom && Read32(decoded, table + 8) == (uint)lastRoom && Read16(patched, firstRoom + 0x10) == 0x0203 && Read16(patched, lastRoom + 0x10) == 0x0203 && decoded[0x21] == 7 && decoded[0x22] == 1 && decoded[0x23] == 9, "Room deletion did not preserve and remap surviving room references.");
    var referenced = new RomWorkspace(document); bool rejected = false; try { referenced.DeleteRoom(0, 2); } catch (InvalidDataException) { rejected = true; }
    Check(rejected && referenced.Document.Scenes[0].Rooms.Count == 3 && referenced.RoomTopologyEdits.Count == 0, "Deleting a room with live transition or exit references was accepted.");
});
Test("Native ROM workspace stages scene exit edits", () => {
    var scene = new RomScene(0, "Scene", 0, 40, [], false, [], null, [], [], [], [new RomExit(0, 0x0421)]);
    var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    workspace.EditExit(0, 0, 0x0842);
    Check(workspace.Document.Scenes[0].Exits[0].RoomId == 4 && workspace.Document.Scenes[0].Exits[0].SpawnId == 66 && workspace.ExitEdits.Count == 1 && workspace.WritePatch().Contains("ExitIndex"), "Native scene exit edit was not staged.");
});
Test("Native ROM workspace stages waterbox edits", () => {
    var waterbox = new RomWaterbox(1, 2, 3, 4, 5, 6, 7);
    var collision = new RomCollisionData(0, 0, 0, 10, 10, 10, [], [], [], [waterbox]);
    var scene = new RomScene(0, "Scene", 0, 40, [], true, [], collision);
    var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    var edited = workspace.EditWaterbox(0, 0, waterbox with { Y = 99, Properties = 42 });
    Check(edited.Y == 99 && workspace.Document.Scenes[0].Collision.Waterboxes[0].Properties == 42 && workspace.WaterboxEdits.Count == 1 && workspace.WritePatch().Contains("WaterboxIndex"), "Native waterbox edit was not staged.");
    bool rejected = false; try { workspace.ApplyToRom(new byte[40]); } catch (InvalidDataException) { rejected = true; }
    Check(rejected, "Unsupported waterbox ROM write-back was not rejected clearly.");
});
Test("Native ROM workspace stages environment edits", () => {
    var environment = new RomEnvironment(1, 2, 3, 4, 5, 6, 7, 8, 9);
    var scene = new RomScene(0, "Scene", 0, 40, [], false, [], null, [], [], [], [], [environment]);
    var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    var edited = workspace.EditEnvironment(0, 0, environment with { Ambient = 0xAABBCC, DrawDistance = 100 });
    Check(edited.Ambient == 0xAABBCC && workspace.Document.Scenes[0].Environments[0].DrawDistance == 100 && workspace.EnvironmentEdits.Count == 1 && workspace.WritePatch().Contains("EnvironmentIndex"), "Native environment edit was not staged.");
});
Test("Native ROM workspace stages collision vertex edits", () => {
    var vertex = new RomCollisionVertex(1, 2, 3);
    var collision = new RomCollisionData(0, 0, 0, 10, 10, 10, [vertex], [], []);
    var scene = new RomScene(0, "Scene", 0, 40, [], true, [], collision);
    var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    var edited = workspace.EditCollisionVertex(0, 0, vertex with { Z = 99 });
    Check(edited.Z == 99 && workspace.Document.Scenes[0].Collision.Vertices[0].Z == 99 && workspace.CollisionVertexEdits.Count == 1 && workspace.WritePatch().Contains("VertexIndex"), "Native collision vertex edit was not staged.");
});
Test("Native ROM workspace stages collision triangle and surface edits", () => {
    var triangle = new RomCollisionTriangle(1, 2, 3, 4, 5, 6, 7, 8); var collision = new RomCollisionData(0, 0, 0, 10, 10, 10, [], [triangle], [9UL]);
    var scene = new RomScene(0, "Scene", 0, 40, [], true, [], collision); var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    var edited = workspace.EditCollisionTriangle(0, 0, triangle with { Distance = -10 }); workspace.EditCollisionSurface(0, 0, 0x1122334455667788UL);
    Check(edited.Distance == -10 && workspace.Document.Scenes[0].Collision.Triangles[0].Distance == -10 && workspace.Document.Scenes[0].Collision.SurfaceTypes[0] == 0x1122334455667788UL && workspace.CollisionTriangleEdits.Count == 1 && workspace.CollisionSurfaceEdits.Count == 1, "Native collision triangle/surface edit was not staged.");
});
Test("OoT collision surface flags decode and encode without losing fields", () => {
    ulong raw = 0xC7ABCDEF8FABCDE1UL; var decoded = RomCollisionSurfaceType.Decode(raw);
    Check(decoded.BgCameraIndex == 0xEF && decoded.ExitIndex == 0x0D && decoded.FloorType == 30 && decoded.IsSoft && decoded.IsHorseBlocked && decoded.Material == 1 && decoded.CanHookshot && decoded.Encode() == raw, "Collision surface flags did not decode and encode losslessly.");
    var changed = decoded with { CanHookshot = false, ConveyorSpeed = 4, ConveyorDirection = 17 }; Check(changed.Encode() != raw && RomCollisionSurfaceType.Decode(changed.Encode()).ConveyorDirection == 17, "Collision surface flag edits did not round-trip.");
});
Test("Native ROM workspace stages collision bounds edits", () => {
    var collision = new RomCollisionData(-1, -2, -3, 1, 2, 3, [], [], []); var scene = new RomScene(0, "Scene", 0, 40, [], true, [], collision); var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    workspace.EditCollisionBounds(0, collision with { MinY = -20, MaxX = 30 });
    Check(workspace.Document.Scenes[0].Collision!.MinY == -20 && workspace.Document.Scenes[0].Collision.MaxX == 30 && workspace.CollisionBoundsEdits.Count == 1 && workspace.WritePatch().Contains("CollisionBounds"), "Native collision bounds edit was not staged.");
});
Test("Native collision normals and bounds recalculate", () => {
    var vertices = new[] { new RomCollisionVertex(0, 0, 0), new RomCollisionVertex(10, 0, 0), new RomCollisionVertex(0, 10, 0) };
    var collision = new RomCollisionData(99, 99, 99, -1, -1, -1, vertices, [new RomCollisionTriangle(7, 0, 1, 2, 0, 0, 0, 123)], [0x55UL]);
    var scene = new RomScene(0, "Scene", 0, 40, [], true, [], collision); var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    var updated = workspace.RecalculateCollision(0);
    Check(updated.MinX == 0 && updated.MinY == 0 && updated.MinZ == 0 && updated.MaxX == 10 && updated.MaxY == 10 && updated.MaxZ == 0 && updated.Triangles[0].NormalZ == 32767 && updated.Triangles[0].Distance == 0 && workspace.CollisionTriangleEdits.Count == 1 && workspace.CollisionBoundsEdits.Count == 1, "Native collision normals and bounds were not recalculated and staged.");
});
Test("Native alternate collision normals and bounds recalculate", () => {
    var vertices = new[] { new RomCollisionVertex(0, 0, 0), new RomCollisionVertex(10, 0, 0), new RomCollisionVertex(0, 10, 0) };
    var collision = new RomCollisionData(99, 99, 99, -1, -1, -1, vertices, [new RomCollisionTriangle(7, 0, 1, 2, 0, 0, 0, 123)], [0x55UL]);
    var header = new RomAlternateHeader(1, 0x02000010, false, HasCollision: true, Collision: collision); var scene = new RomScene(0, "Scene", 0, 40, [], false, [], AlternateHeaders: [header]);
    var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], [])); var updated = workspace.RecalculateAlternateCollision(0, 1);
    Check(updated.MinX == 0 && updated.MaxY == 10 && updated.Triangles[0].NormalZ == 32767 && updated.Triangles[0].Distance == 0 && workspace.AlternateCollisionTriangleEdits.Count == 1 && workspace.AlternateCollisionBoundsEdits.Count == 1, "Alternate collision normals and bounds were not recalculated and staged.");
});
Test("Native collision topology replacement relocates and round-trips", () => {
    byte[] rom = new byte[0xB72000]; Put32(rom, 0, 0x80371240); int table = 0xB71440, scene = 0x1000;
    Put32(rom, table, (uint)scene); Put32(rom, table + 4, 0x1100); Put32(rom, table + 20, 0x2000); Put32(rom, table + 24, 0x2080);
    rom[scene] = 0x02; Put32(rom, scene + 4, 0x02000040); rom[scene + 8] = 0x14; int collision = scene + 0x40; Put16(rom, collision + 12, 1); Put32(rom, collision + 16, 0x02000070); Put16(rom, collision + 20, 1); Put32(rom, collision + 24, 0x02000080); Put32(rom, collision + 28, 0x020000A0);
    Put16(rom, scene + 0x70, 1); Put16(rom, scene + 0x72, 2); Put16(rom, scene + 0x74, 3); Put16(rom, scene + 0x80, 1); Put16(rom, scene + 0x82, 0); Put16(rom, scene + 0x84, 0); Put16(rom, scene + 0x86, 0); Put64(rom, scene + 0xA0, 0x0102030405060708UL);
    var decoded = RomDecoder.Read(rom); var workspace = new RomWorkspace(decoded); var source = decoded.Scenes[0].Collision!; var vertices = source.Vertices.Concat([new RomCollisionVertex(4, 5, 6)]).ToArray(); var triangles = source.Triangles.Concat([new RomCollisionTriangle(2, 0, 0, 1, 0, 1, 0, 0)]).ToArray(); var surfaces = source.SurfaceTypes.Concat([0x1112131415161718UL]).ToArray(); workspace.ReplaceCollisionTopology(0, vertices, triangles, surfaces);
    Check(workspace.CollisionTopologyEdits.Count == 1 && workspace.WritePatch().Contains("CollisionTopologies"), "Native collision topology replacement was not staged.");
    var patched = workspace.ApplyToRom(rom); var collisionAfter = RomDecoder.Read(patched).Scenes[0].Collision!;
    Check(collisionAfter.Vertices.Count == 2 && collisionAfter.Triangles.Count == 2 && collisionAfter.Vertices[1].Z == 6 && collisionAfter.SurfaceTypes[1] == 0x1112131415161718UL, "Native collision topology replacement did not round-trip.");
});
Test("Native ROM workspace stages spawn edits", () => {
    var spawn = new RomSpawnPoint(1, 2, 3, 4, 5, 6, 7, 8); var scene = new RomScene(0, "Scene", 0, 40, [], false, [], null, [spawn]); var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], [])); var edited = workspace.EditSpawn(0, 0, spawn with { X = 99 }); Check(edited.X == 99 && workspace.Document.Scenes[0].SpawnPoints[0].X == 99 && workspace.SpawnEdits.Count == 1, "Native spawn edit was not staged.");
});
Test("Compressed scene fixed-size edits round-trip when the encoded scene fits", () => {
    byte[] source = new byte[0x180]; source[0] = 0x13; source[1] = 1; Put32(source, 4, 0x02000040); source[8] = 0x14; Put16(source, 0x40, 0x0421);
    byte[] stored = N64Compression.EncodeYaz0(source); byte[] rom = new byte[0xB72000]; Put32(rom, 0, 0x80371240); int table = 0xB71440, start = 0x1000, end = 0x1200; Put32(rom, table, (uint)start); Put32(rom, table + 4, (uint)end); Put32(rom, table + 20, 0x2000); Put32(rom, table + 24, 0x2080); Buffer.BlockCopy(stored, 0, rom, start, stored.Length);
    var decoded = RomDecoder.Read(rom); var workspace = new RomWorkspace(decoded); workspace.EditExit(0, 0, 0x0842); byte[] patched = workspace.ApplyToRom(rom); byte[] roundTrip = N64Compression.Decode(patched.AsSpan(start, end - start).ToArray());
    Check(roundTrip[0x40] == 0x08 && roundTrip[0x41] == 0x42, "Compressed scene fixed-size edit did not round-trip.");
});
Test("Oversized compressed scene edits relocate and update the scene table", () => {
    byte[] source = new byte[0x200]; source[0] = 0x0D; source[1] = 1; Put32(source, 4, 0x02000040); source[8] = 0x14; source[0x40] = 40; Put32(source, 0x44, 0x02000080);
    byte[] stored = N64Compression.EncodeYaz0(source); byte[] rom = new byte[0xB72000]; Put32(rom, 0, 0x80371240); int table = 0xB71440, start = 0x1000, end = start + stored.Length; Put32(rom, table, (uint)start); Put32(rom, table + 4, (uint)end); Put32(rom, table + 20, 0x2000); Put32(rom, table + 24, 0x2080); Buffer.BlockCopy(stored, 0, rom, start, stored.Length);
    var decoded = RomDecoder.Read(rom); var workspace = new RomWorkspace(decoded); for (int i = 0; i < 40; i++) workspace.EditPathPoint(0, 0, i, new RomPathPoint((short)(i * 137), (short)(-i * 83), (short)(i * 59)));
    byte[] patched = workspace.ApplyToRom(rom); uint relocatedStart = Read32(patched, table); uint relocatedEnd = Read32(patched, table + 4); Check(relocatedStart != start && relocatedEnd > relocatedStart, "Oversized compressed scene was not relocated.");
    var relocated = RomDecoder.Read(patched); Check(relocated.Scenes[0].Paths[0].Points[39].X == 39 * 137 && relocated.Scenes[0].Paths[0].Points[39].Y == -39 * 83, "Relocated scene data did not decode edited path points.");
});
Test("Oversized compressed scene command edits relocate and update the scene table", () => {
    byte[] source = new byte[0x200]; for (int i = 0; i < 20; i++) { source[i * 8] = 0x00; source[i * 8 + 1] = 1; } source[160] = 0x14;
    byte[] stored = N64Compression.EncodeYaz0(source); byte[] rom = new byte[0xB72000]; Put32(rom, 0, 0x80371240); int table = 0xB71440, start = 0x1000, end = start + stored.Length; Put32(rom, table, (uint)start); Put32(rom, table + 4, (uint)end); Put32(rom, table + 20, 0x2000); Put32(rom, table + 24, 0x2080); Buffer.BlockCopy(stored, 0, rom, start, stored.Length);
    var decoded = RomDecoder.Read(rom); var workspace = new RomWorkspace(decoded); for (int i = 0; i < 20; i++) workspace.EditSceneCommand(0, i, new RomHeaderCommand(0x13, 0xFE, 0xDEADBEEF - (uint)i));
    byte[] patched = workspace.ApplyToRom(rom); uint relocatedStart = Read32(patched, table); uint relocatedEnd = Read32(patched, table + 4); Check(relocatedStart != start && relocatedEnd > relocatedStart, "Oversized compressed scene command was not relocated.");
    var relocated = RomDecoder.Read(patched); Check(relocated.Scenes[0].Commands![0].Command == 0x13 && relocated.Scenes[0].Commands[0].Pointer == 0xDEADBEEF, "Relocated scene command did not decode edited bytes.");
});
Test("Oversized compressed alternate command edits relocate and update the scene table", () => {
    byte[] source = new byte[0x300]; source[0] = 0x18; Put32(source, 4, 0x02000020); Put32(source, 0x20, 0x02000040); for (int i = 0; i < 20; i++) source[0x40 + i * 8] = 0x00; source[0x140] = 0x14;
    byte[] stored = N64Compression.EncodeYaz0(source); byte[] rom = new byte[0xB72000]; Put32(rom, 0, 0x80371240); int table = 0xB71440, start = 0x1000, end = start + stored.Length; Put32(rom, table, (uint)start); Put32(rom, table + 4, (uint)end); Put32(rom, table + 20, 0x2000); Put32(rom, table + 24, 0x2080); Buffer.BlockCopy(stored, 0, rom, start, stored.Length);
    var decoded = RomDecoder.Read(rom); var workspace = new RomWorkspace(decoded); for (int i = 0; i < 20; i++) workspace.EditAlternateCommand(0, 1, i, new RomHeaderCommand(0x13, 0xFE, 0xCAFEBABE - (uint)i));
    byte[] patched = workspace.ApplyToRom(rom); uint relocatedStart = Read32(patched, table); uint relocatedEnd = Read32(patched, table + 4); Check(relocatedStart != start && relocatedEnd > relocatedStart, "Oversized compressed alternate command was not relocated.");
    var relocated = RomDecoder.Read(patched); Check(relocated.Scenes[0].AlternateHeaders![0].Commands![0].Pointer == 0xCAFEBABE, "Relocated alternate command did not decode edited bytes.");
});
Test("Native room geometry vertices retain source offsets for write-back", () => {
    var vertex = new RomGeometryVertex(1, 2, 3, 4, 5, 6, 7, 8, 9); var geometry = new RomRoomGeometry([vertex], [], 1, [32]); var room = new RomRoom(0, 0, 40, [], 0, true, false, [], geometry); var scene = new RomScene(0, "Scene", 0, 40, [room], false, []); var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    var edited = workspace.EditGeometryVertex(0, 0, 0, vertex with { X = 99, R = 200 });
    Check(edited.X == 99 && workspace.Document.Scenes[0].Rooms[0].Geometry.Vertices[0].R == 200 && workspace.GeometryVertexEdits.Count == 1 && workspace.WritePatch().Contains("SourceOffset"), "Native room geometry vertex edit was not staged with its source offset.");
});
Test("Native room geometry triangle edits retain display-list command offsets", () => {
    var geometry = new RomRoomGeometry([new RomGeometryVertex(1, 2, 3, 4, 5, 6, 7, 8, 9), new RomGeometryVertex(10, 11, 12, 13, 14, 15, 16, 17, 18), new RomGeometryVertex(19, 20, 21, 22, 23, 24, 25, 26, 27)], [new RomGeometryTriangle(0, 1, 2)], 1, [32, 48, 64], [0, 2, 4], [80], [0]);
    var scene = new RomScene(0, "Scene", 0, 40, [new RomRoom(0, 0, 40, [], 0, true, false, [], geometry)], false, []); var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    var edited = workspace.EditGeometryTriangle(0, 0, 0, new RomGeometryTriangle(2, 1, 0));
    Check(edited.A == 2 && workspace.GeometryTriangleEdits.Count == 1 && workspace.GeometryTriangleEdits[0].SourceOffset == 80 && workspace.WritePatch().Contains("SlotA"), "Native geometry triangle edit was not staged with its display-list offset.");
});
Test("Native display-list command words retain source offsets for write-back", () => {
    var command = new RomDisplayListCommand(96, 0xBF, 0xBF000000, 0x00000408); var geometry = new RomRoomGeometry([new RomGeometryVertex(1, 2, 3, 0, 0, 255, 255, 255, 255)], [], 1, DisplayListCommands: [command]);
    var scene = new RomScene(0, "Scene", 0, 40, [new RomRoom(0, 0, 40, [], 0, true, false, [], geometry)], false, []); var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    var edited = workspace.EditGeometryCommand(0, 0, 96, 0xBF000000, 0x00000000);
    Check(edited.Word1 == 0 && workspace.GeometryCommandEdits.Count == 1 && workspace.GeometryCommandEdits[0].SourceOffset == 96 && workspace.WritePatch().Contains("GeometryCommands"), "Native display-list command words were not staged with their source offset.");
});
Test("Native texture commands decode image and tile fields", () => {
    var image = new RomDisplayListCommand(64, 0xFD, (uint)(0xFD000000 | (3u << 21) | (2u << 19) | 31), 0x03001234); var tile = new RomDisplayListCommand(72, 0xF2, 0xF2001008, 0x00002028);
    Check(RomTextureCommandDecoder.TryDecode(image, out var imageInfo) && imageInfo.FormatName == "IA" && imageInfo.SizeName == "16b" && imageInfo.Width == 32 && imageInfo.Segment == 3 && imageInfo.Address == 0x1234, "G_SETTIMG fields did not decode correctly.");
    Check(RomTextureCommandDecoder.TryDecode(tile, out var tileInfo) && tileInfo.Kind == "G_SETTILESIZE" && tileInfo.Uls == 1 && tileInfo.Ult == 8 && tileInfo.Lrs == 2 && tileInfo.Lrt == 40, "G_SETTILESIZE fields did not decode correctly.");
});
Test("Native room textures extract RGBA16 pixels", () => {
    byte[] rom = new byte[0x140]; Put16(rom, 0x120, 0xF801); Put16(rom, 0x122, 0x07C1);
    var commands = new[]
    {
        new RomDisplayListCommand(0, 0xFD, (uint)(0xFD000000 | (2u << 19) | 1), 0x03000020),
        new RomDisplayListCommand(8, 0xF2, 0xF2000000, 0x00004000)
    };
    var geometry = new RomRoomGeometry([], [], 1, DisplayListCommands: commands); var room = new RomRoom(0, 0x100, 0x140, [], 0, true, false, [], geometry);
    var assets = RomTextureDecoder.ExtractRoomTextures(rom, room);
    Check(assets.Count == 1 && assets[0].IsDecoded && assets[0].Width == 2 && assets[0].Height == 1 && assets[0].Rgba.SequenceEqual(new byte[] { 255, 0, 0, 255, 0, 255, 0, 255 }), "RGBA16 room texture extraction did not decode the expected pixels.");
});
Test("Native room CI4 textures use loaded RGBA16 palettes", () => {
    byte[] rom = new byte[0x180]; Put16(rom, 0x120, 0xF801); Put16(rom, 0x122, 0x07C1); rom[0x160] = 0x10;
    var commands = new[]
    {
        new RomDisplayListCommand(0, 0xFD, (uint)(0xFD000000 | (2u << 19)), 0x03000020),
        new RomDisplayListCommand(8, 0xF0, 0xF0000000, 15u << 14),
        new RomDisplayListCommand(16, 0xFD, (uint)(0xFD000000 | (2u << 21) | 1), 0x03000060),
        new RomDisplayListCommand(24, 0xF5, 0xF5000000, 0x00000000),
        new RomDisplayListCommand(32, 0xF2, 0xF2000000, 0x00004000)
    };
    var geometry = new RomRoomGeometry([], [], 1, DisplayListCommands: commands); var room = new RomRoom(0, 0x100, 0x180, [], 0, true, false, [], geometry); var assets = RomTextureDecoder.ExtractRoomTextures(rom, room);
    Check(assets.Count == 1 && assets[0].IsDecoded && assets[0].Format == "CI" && assets[0].PaletteRgba?.Length == 64 && assets[0].Rgba.SequenceEqual(new byte[] { 0, 255, 0, 255, 255, 0, 0, 255 }), "CI4 palette extraction did not decode the indexed texture.");
});
Test("Native CI4 texture replacement quantizes against the loaded palette", () => {
    byte[] palette = new byte[64]; palette[0] = 255; palette[3] = 255; palette[4] = 0; palette[5] = 255; palette[7] = 255;
    byte[] encoded = RomTextureDecoder.EncodePixels([0, 255, 0, 255, 255, 0, 0, 255], "CI", "4b", 2, 1, palette);
    Check(encoded.SequenceEqual(new byte[] { 0x10 }), "CI4 replacement did not preserve palette indices.");
});
Test("Native YUV16 textures decode and encode as RGBA", () => {
    byte[] decoded = RomTextureDecoder.DecodePixels([100, 128, 200, 128], "YUV", "16b", 2, 1);
    Check(decoded.SequenceEqual(new byte[] { 100, 100, 100, 255, 200, 200, 200, 255 }), "YUV16 decoding did not preserve neutral luminance pairs.");
    byte[] encoded = RomTextureDecoder.EncodePixels(decoded, "YUV", "16b", 2, 1); Check(encoded[0] is >= 99 and <= 101 && encoded[2] is >= 199 and <= 201 && encoded[1] is >= 127 and <= 129 && encoded[3] is >= 127 and <= 129, "YUV16 encoding did not preserve neutral luminance pairs.");
});
Test("Native texture replacement encodes and writes fixed-size room pixels", () => {
    byte[] rom = new byte[0x400]; Put32(rom, 0, 0x80371240); int sceneTable = 0x100, sceneStart = 0x200, sceneEnd = 0x240, roomStart = 0x280, roomEnd = 0x2C0; Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x04; rom[sceneStart + 1] = 1; Put32(rom, sceneStart + 4, 0x02000010); Put32(rom, sceneStart + 0x10, (uint)roomStart); Put32(rom, sceneStart + 0x14, (uint)roomEnd); rom[sceneStart + 8] = 0x14;
    Put32(rom, roomStart, 0xFD100001); Put32(rom, roomStart + 4, 0x03000020); Put16(rom, roomStart + 0x20, 0xF801); Put16(rom, roomStart + 0x22, 0x07C1);
    var command = new RomDisplayListCommand(0, 0xFD, 0xFD100001, 0x03000020); var geometry = new RomRoomGeometry([], [], 1, DisplayListCommands: [command]); var room = new RomRoom(0, (uint)roomStart, (uint)roomEnd, [], 0, true, false, [], geometry); var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, [room], false, []); string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant(); var profile = new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)); var workspace = new RomWorkspace(new RomDocument(fingerprint, profile, [scene], []));
    var asset = new RomTextureAsset(0, "RGBA", "16b", 2, 1, 3, 0x20, [255, 0, 0, 255, 0, 255, 0, 255]); workspace.ReplaceRoomTexture(0, 0, asset); byte[] edited = workspace.ApplyToRom(rom); Check(workspace.TextureEdits.Count == 1 && edited[roomStart + 0x20] == 0xF8 && edited[roomStart + 0x22] == 0x07 && workspace.WritePatch().Contains("Textures"), "Native texture replacement was not staged and written back.");
});
Test("Native texture dimension changes update commands and relocate room data", () => {
    byte[] rom = new byte[0x500]; Put32(rom, 0, 0x80371240); int sceneTable = 0x100, sceneStart = 0x200, sceneEnd = 0x240, roomStart = 0x280, roomEnd = 0x2C0; Put32(rom, sceneTable, (uint)sceneStart); Put32(rom, sceneTable + 4, (uint)sceneEnd);
    rom[sceneStart] = 0x04; rom[sceneStart + 1] = 1; Put32(rom, sceneStart + 4, 0x02000010); Put32(rom, sceneStart + 0x10, (uint)roomStart); Put32(rom, sceneStart + 0x14, (uint)roomEnd); rom[sceneStart + 8] = 0x14;
    Put32(rom, roomStart, 0xFD100001); Put32(rom, roomStart + 4, 0x03000020); Put32(rom, roomStart + 8, 0xF2000000); Put32(rom, roomStart + 12, 0x00004000); Put32(rom, roomStart + 16, 0xB8000000); Put16(rom, roomStart + 0x20, 0xF801); Put16(rom, roomStart + 0x22, 0x07C1);
    var commands = new[] { new RomDisplayListCommand(0, 0xFD, 0xFD100001, 0x03000020), new RomDisplayListCommand(8, 0xF2, 0xF2000000, 0x00004000) }; var geometry = new RomRoomGeometry([], [], 1, DisplayListCommands: commands); var room = new RomRoom(0, (uint)roomStart, (uint)roomEnd, [], 0, true, false, [], geometry); var scene = new RomScene(0, "Scene", (uint)sceneStart, (uint)sceneEnd, [room], false, []); string fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rom)).ToLowerInvariant(); var workspace = new RomWorkspace(new RomDocument(fingerprint, new RomProfile("test", "Test", (uint)sceneTable, (uint)(sceneTable + 20)), [scene], []));
    var pixels = Enumerable.Repeat(new byte[] { 0, 0, 255, 255 }, 4).SelectMany(value => value).ToArray(); workspace.ReplaceRoomTexture(0, 0, new RomTextureAsset(0, "RGBA", "16b", 4, 1, 3, 0x20, pixels, SourceWidth: 2, SourceHeight: 1)); byte[] edited = workspace.ApplyToRom(rom); int relocated = checked((int)Read32(edited, sceneStart + 0x10)); Check(relocated != roomStart && (Read32(edited, relocated) & 0xFFF) == 3 && (Read32(edited, relocated + 4) & 0x00FFFFFF) == 0x40 && (Read32(edited, relocated + 12) >> 12) == 12, "Native texture dimension change did not update the display-list commands and relocate the room.");
});
Test("Same-topology custom geometry stages native vertex and triangle write-back", () => {
    var existing = new RomRoomGeometry(
        [new RomGeometryVertex(1, 2, 3, 4, 5, 6, 7, 8, 9), new RomGeometryVertex(10, 11, 12, 13, 14, 15, 16, 17, 18), new RomGeometryVertex(19, 20, 21, 22, 23, 24, 25, 26, 27)],
        [new RomGeometryTriangle(0, 1, 2)], 2, [32, 48, 64], [0, 2, 4], [80], [0xBF000000]);
    var imported = new RomRoomGeometry(
        [new RomGeometryVertex(101, 102, 103, 104, 105, 106, 107, 108, 109), new RomGeometryVertex(110, 111, 112, 113, 114, 115, 116, 117, 118), new RomGeometryVertex(119, 120, 121, 122, 123, 124, 125, 126, 127)],
        [new RomGeometryTriangle(2, 1, 0)], 1);
    var scene = new RomScene(0, "Scene", 0, 40, [new RomRoom(0, 0, 40, [], 0, true, false, [], existing)], false, []);
    var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    var replaced = workspace.ReplaceRoomGeometry(0, 0, imported);
    Check(!workspace.TopologyDirty && replaced.DisplayLists == 2 && replaced.VertexOffsets!.SequenceEqual([32, 48, 64]) && workspace.GeometryVertexEdits.Count == 3 && workspace.GeometryTriangleEdits.Count == 1 && workspace.GeometryTriangleEdits[0].SourceOffset == 80 && workspace.GeometryTriangleEdits[0].SlotA == 4, "Same-topology custom geometry did not stage native display-list write-back.");
});
Test("Topology-changing custom geometry remains export guarded", () => {
    var existing = new RomRoomGeometry([new RomGeometryVertex(1, 2, 3, 0, 0, 255, 255, 255, 255)], [new RomGeometryTriangle(0, 0, 0)], 1, [32], [0], [48], [0]);
    var imported = new RomRoomGeometry([new RomGeometryVertex(1, 2, 3, 0, 0, 255, 255, 255, 255), new RomGeometryVertex(4, 5, 6, 0, 0, 255, 255, 255, 255)], [new RomGeometryTriangle(0, 1, 0)], 1);
    var scene = new RomScene(0, "Scene", 0, 40, [new RomRoom(0, 0, 40, [], 0, true, false, [], existing)], false, []);
    var workspace = new RomWorkspace(new RomDocument("test", new RomProfile("test", "Test", 0, 40), [scene], []));
    workspace.ReplaceRoomGeometry(0, 0, imported);
    Check(workspace.TopologyDirty && workspace.GeometryVertexEdits.Count == 0 && workspace.GeometryTriangleEdits.Count == 0 && workspace.Document.Scenes[0].Rooms[0].Diagnostics.Any(d => d.Contains("allocation", StringComparison.OrdinalIgnoreCase)), "Topology-changing custom geometry was not guarded.");
});
Test("Native single-list geometry rebuild relocates and decodes", () => {
    byte[] rom = new byte[0xB72000]; Put32(rom, 0, 0x80371240); int table = 0xB71440, scene = 0x1000, room = 0x1200;
    Put32(rom, table, (uint)scene); Put32(rom, table + 4, 0x1100); Put32(rom, table + 20, 0x2000); Put32(rom, table + 24, 0x2080);
    rom[scene] = 0x04; rom[scene + 1] = 1; Put32(rom, scene + 4, 0x02000040); rom[scene + 8] = 0x14; Put32(rom, scene + 0x40, (uint)room); Put32(rom, scene + 0x44, 0x1300);
    rom[room] = 0x0A; Put32(rom, room + 4, 0x03000020); rom[room + 8] = 0x14; rom[room + 0x20] = 0; rom[room + 0x21] = 1; Put32(rom, room + 0x24, 0x03000030); Put32(rom, room + 0x28, 0);
    Put32(rom, room + 0x30, 0x03000040); Put32(rom, room + 0x34, 0); Put32(rom, room + 0x40, 0x01003000); Put32(rom, room + 0x44, 0x03000060); Put32(rom, room + 0x48, 0xBF000000); Put32(rom, room + 0x4C, 0x00000408); Put32(rom, room + 0x50, 0xB8000000);
    for (int i = 0; i < 3; i++) { int v = room + 0x60 + i * 16; Put16(rom, v, (ushort)(i * 10)); Put16(rom, v + 4, (ushort)(i == 2 ? 10 : 0)); rom[v + 12] = 255; rom[v + 15] = 255; }
    var decoded = RomDecoder.Read(rom); var workspace = new RomWorkspace(decoded); var imported = new RomRoomGeometry([new RomGeometryVertex(1, 2, 3, 0, 0, 255, 0, 0, 255), new RomGeometryVertex(4, 5, 6, 0, 0, 0, 255, 0, 255), new RomGeometryVertex(7, 8, 9, 0, 0, 0, 0, 255, 255), new RomGeometryVertex(10, 11, 12, 0, 0, 255, 255, 255, 255)], [new RomGeometryTriangle(0, 1, 2), new RomGeometryTriangle(1, 2, 3)], 1);
    workspace.ReplaceRoomGeometry(0, 0, imported); Check(!workspace.TopologyDirty && workspace.GeometryRebuildEdits.Count == 1, "Single-list geometry rebuild was not staged.");
    byte[] patched = workspace.ApplyToRom(rom); var rebuilt = RomDecoder.Read(patched).Scenes[0].Rooms[0].Geometry;
    Check(rebuilt is { Vertices.Count: 4, Triangles.Count: 2 } && rebuilt.Vertices[3].X == 10 && rebuilt.Triangles[1].C == 3, "Native display-list rebuild did not round-trip through the relocated room.");
});

void Put16(byte[] data, int offset, ushort value) { data[offset] = (byte)(value >> 8); data[offset + 1] = (byte)value; }
ushort Read16(byte[] data, int offset) => (ushort)(data[offset] << 8 | data[offset + 1]);
void Put32(byte[] data, int offset, uint value) { data[offset] = (byte)(value >> 24); data[offset + 1] = (byte)(value >> 16); data[offset + 2] = (byte)(value >> 8); data[offset + 3] = (byte)value; }
void Put64(byte[] data, int offset, ulong value) { for (int i = 7; i >= 0; i--) { data[offset + i] = (byte)value; value >>= 8; } }
uint Read32(byte[] data, int offset) => (uint)data[offset] << 24 | (uint)data[offset + 1] << 16 | (uint)data[offset + 2] << 8 | data[offset + 3];

string demo = DemoRoom.Create();
Test("Desktop tile mesh projection", () => {
    var tile = DesktopImport.ReadTile(demo);
    Check(tile.Vertices.Length == 7 && tile.Indices.Length == 18 && tile.FaceColors.Length == 6 && tile.Grid == 100, "Demo geometry changed.");
});
Test("Desktop material colors and texture references are carried into the portable preview", () => {
    string xml = XDocument.Parse(demo).ToString();
    var root = XDocument.Parse(xml).Root!;
    root.Element("Visual")!.Add(new XElement("Materials", new XElement("Material", new XElement("Name", "Demo"), new XElement("Kd", new XElement("float", "0.1"), new XElement("float", "0.2"), new XElement("float", "0.3")), new XElement("map_Kd", @"C:\textures\demo.png"))));
    foreach (var triangle in root.Descendants("Triangle")) triangle.AddFirst(new XElement("MaterialName", "Demo"));
    var tile = DesktopImport.ReadTile(root.ToString());
    Check(tile.FaceColors.All(c => c == new Vector3(0.1f, 0.2f, 0.3f)) && tile.TextureReferences.SequenceEqual(["demo.png"]), "Material data was not projected.");
});
Test("Tile bundle validates tile XML and embedded PNG paths", () => {
    using var stream = new MemoryStream();
    using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
    {
        using (var tile = new StreamWriter(archive.CreateEntry("tile.xml").Open())) tile.Write(demo);
        using (var png = archive.CreateEntry("textures/demo.png").Open()) png.Write(new byte[] { 1, 2, 3 });
    }
    stream.Position = 0;
    var bundle = DesktopImport.ReadBundle(stream);
    Check(bundle.Xml.Contains("Demo room") && bundle.Textures.Count == 1 && bundle.Textures.ContainsKey("demo.png"), "Bundle projection failed.");
    using var invalid = new MemoryStream();
    using (var archive = new ZipArchive(invalid, ZipArchiveMode.Create, leaveOpen: true)) using (var tile = new StreamWriter(archive.CreateEntry("tile.xml").Open())) tile.Write(demo);
    invalid.Position = 0; var noTextureBundle = DesktopImport.ReadBundle(invalid); Check(noTextureBundle.Textures.Count == 0, "Texture-free bundle rejected.");
});
Test("Gameplay actors, flags and logic links round-trip through visual edits", () => {
    var root = XDocument.Parse(demo).Root!;
    root.Add(new XElement("Gameplay", new XElement("Scene", 7), new XElement("Room", 2), new XElement("Name", "Demo gameplay"),
        new XElement("Actors", new XElement("RoomPackageActor", new XElement("Id", "switch"), new XElement("Role", "Switches"), new XElement("Name", "Floor switch"), new XElement("Number", 1), new XElement("Variable", 4), new XElement("Position", new XElement("X", 1), new XElement("Y", 2), new XElement("Z", 3)), new XElement("Rotation", new XElement("Y", 90)), new XElement("IsTransition", false), new XElement("Flags", new XElement("DungeonFlagReference", new XElement("Kind", "Switch Flag"), new XElement("Target", "Var"), new XElement("Value", 12))))),
        new XElement("Links", new XElement("LogicLink", new XElement("SourceId", "switch"), new XElement("TargetId", "door"), new XElement("Kind", "Opens"), new XElement("Parameter", "door-1")))));
    string source = root.ToString(); var summary = GameplayEditor.Read(source);
    Check(summary.Scene == 7 && summary.Room == 2 && summary.Actors.Count == 1 && summary.Actors[0].Flags[0].Value == 12 && summary.Links.Count == 1, "Gameplay summary was not read.");
    source = GameplayEditor.UpdateActor(source, "switch", 100, 200, 300, 180, 9, 42);
    source = GameplayEditor.AddLink(source, "switch", "door", "Activates", "test");
    var edited = GameplayEditor.Read(source);
    Check(edited.Actors[0].X == 100 && edited.Actors[0].RotationY == 180 && edited.Actors[0].Variable == 9 && edited.Actors[0].Flags[0].Value == 42 && edited.Links.Count == 2, "Gameplay edit was not retained.");
});
Test("Desktop positive and negative quarter-turn convention", () => {
    var point = new Vector3(1, 2, 3);
    Check(new LayoutTile { QuarterTurns = 1, X = 10 }.Transform(point) == new Vector3(13, 2, -1), "Desktop rotation convention mismatch.");
    Check(new LayoutTile { QuarterTurns = -1 }.Transform(point) == new Vector3(-3, 2, 1), "Negative turn mismatch.");
});
Test("Opaque gameplay, collision, socket and material XML survives layout round-trip", () => {
    var xml = XDocument.Parse(demo);
    xml.Root!.Add(new XElement("Gameplay", new XElement("UnknownFutureField", "retain")), new XElement("Collision", new XElement("Surfaces", "opaque")), new XElement("Sockets", "door"));
    xml.Root.Element("Visual")!.Add(new XElement("Materials", new XElement("Material", new XElement("map_Kd", @"C:\private\texture.png"))));
    string source = xml.ToString();
    var layout = new MobileLayout { Tiles = [new LayoutTile { SourceXml = source, QuarterTurns = 3, X = 123, Y = -45, Z = 600 }] };
    var restored = LayoutStorage.Read(LayoutStorage.Write(layout));
    Check(restored.Tiles[0].SourceXml == layout.Tiles[0].SourceXml && restored.Tiles[0].QuarterTurns == 3 && restored.Tiles[0].X == 123 && restored.Tiles[0].EmbeddedTextures.Count == 0, "Source XML or transform lost.");
});
Test("Independent tile placement and undo/redo", () => {
    var history = new LayoutHistory();
    history.Reset(new MobileLayout { Tiles = [new LayoutTile { SourceXml = demo }, new LayoutTile { SourceXml = demo, X = 400 }] });
    Check(!history.CanUndo && !history.CanRedo, "Restored baseline was treated as an edit.");
    history.Apply(history.Current with { Tiles = [history.Current.Tiles[0] with { QuarterTurns = 1 }, history.Current.Tiles[1]] });
    history.Undo(); Check(history.Current.Tiles[0].QuarterTurns == 0 && history.Current.Tiles[1].X == 400, "Undo changed independent placement.");
    history.Redo(); Check(history.Current.Tiles[0].QuarterTurns == 1, "Redo failed.");
    history.Undo(); history.Apply(history.Current with { Name = "Branch" }); Check(!history.CanRedo, "Redo branch retained.");
});
Test("Invalid triangle indices and nonfinite geometry rejected", () => {
    var xml = XDocument.Parse(demo); xml.Descendants("VertIndex").First().Elements().First().Value = "999999";
    Reject(() => DesktopImport.ReadTile(xml.ToString()));
    xml = XDocument.Parse(demo); xml.Descendants("X").First().Value = "NaN";
    Reject(() => DesktopImport.ReadTile(xml.ToString()));
});
Test("DTD, unrelated XML and future tile version rejected", () => {
    Reject(() => DesktopImport.ReadTile("<!DOCTYPE SectionTile [<!ENTITY x SYSTEM 'file:///private'>]>" + demo));
    Reject(() => DesktopImport.ReadTile("<Scene />"));
    Reject(() => DesktopImport.ReadTile(demo.Replace("<Version>1</Version>", "<Version>2</Version>")));
});
Test("Invalid layout version, null tile and invalid transform rejected", () => {
    Reject(() => LayoutStorage.Read("{\"Version\":2}"));
    Reject(() => LayoutStorage.Read("{\"Tiles\":[null]}"));
    Reject(() => LayoutStorage.Write(new MobileLayout { Tiles = [new LayoutTile { SourceXml = demo, X = float.NaN }] }));
    Reject(() => LayoutStorage.Write(new MobileLayout { Tiles = [new LayoutTile { SourceXml = demo, QuarterTurns = 4 }] }));
});
Test("Source catalog reader supports room blobs outside scene range", () => {
    byte[] rom = new byte[512];
    void U32(int at, uint v) { rom[at] = (byte)(v >> 24); rom[at+1] = (byte)(v >> 16); rom[at+2] = (byte)(v >> 8); rom[at+3] = (byte)v; }
    U32(0, 0x80371240); U32(32, 128); U32(36, 192);
    rom[128] = 4; rom[129] = 1; U32(132, 0x02000020); U32(160, 256); U32(164, 320);
    var catalog = SceneTileLibrary.Read(rom, 32, 52);
    Check(catalog.Rooms.Count == 1 && catalog.Rooms[0].Start == 256 && catalog.Diagnostics.Count == 0, "External room blob rejected.");
    using var writer = new StringWriter(); new XmlSerializer(typeof(SceneTileLibrary)).Serialize(writer, catalog);
    Check(DesktopImport.ReadCatalog(writer.ToString()).Rooms[0].ToString().Contains("Great Deku Tree"), "Scene-name catalog compatibility failed.");
    U32(164, 900); Check(SceneTileLibrary.Read(rom, 32, 52).Diagnostics.Count > 0, "Corrupt range not diagnosed.");
    Reject(() => SceneTileLibrary.Read(rom, 32, 51));
});
Test("Atomic save rejects invalid replacement without damaging prior file", () => {
    string folder = Path.Combine(Path.GetTempPath(), "archarina-core-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
    string path = Path.Combine(folder, "layout.json");
    try {
        var layout = new MobileLayout { Tiles = [new LayoutTile { SourceXml = demo }] };
        LayoutStorage.SaveAtomic(path, layout);
        Reject(() => LayoutStorage.SaveAtomic(path, layout with { Version = 99 }));
        Check(LayoutStorage.Read(File.ReadAllText(path)).Tiles.Length == 1, "Valid file overwritten.");
        LayoutStorage.SaveAtomic(path, layout with { Name = "Updated" });
        Check(LayoutStorage.Read(File.ReadAllText(path)).Name == "Updated", "Replacement failed.");
    } finally { File.Delete(path); if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp"); Directory.Delete(folder); }
});
Test("Bounded stream import rejects oversized input", () => {
    using var stream = new MemoryStream(new byte[LayoutStorage.MaxDocumentBytes + 1]);
    Reject(() => LayoutStorage.ReadBoundedAsync(stream).GetAwaiter().GetResult());
});
foreach (string path in args) Test("Local desktop tile read/round-trip", () => {
    if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    {
        var sceneLayout = LayoutStorage.Read(File.ReadAllText(path));
        Check(sceneLayout.Tiles.Length > 0 && sceneLayout.Tiles.All(tile => tile.SourceXml.Contains("<SectionTile")), "Scene layout did not retain room tile XML.");
        Console.WriteLine($"  {sceneLayout.Tiles.Length} room tiles assembled; source XML retained.");
        return;
    }
    string xml;
    if (path.EndsWith(".archtile", StringComparison.OrdinalIgnoreCase))
    {
        using var bundleStream = File.OpenRead(path);
        var bundle = DesktopImport.ReadBundle(bundleStream);
        xml = bundle.Xml;
        Check(bundle.Textures.Count > 0, "Local tile bundle did not carry textures.");
    }
    else xml = File.ReadAllText(path);
    var mesh = DesktopImport.ReadTile(xml);
    var layout = new MobileLayout { Tiles = [new LayoutTile { SourceXml = xml }] };
    Check(LayoutStorage.Read(LayoutStorage.Write(layout)).Tiles[0].SourceXml == xml, "Actual desktop XML changed.");
    Console.WriteLine($"  {mesh.Vertices.Length} vertices; {mesh.Indices.Length / 3} triangles; source retained.");
});
Console.WriteLine($"{passed} checks passed.");
