using System.Text.Json;

namespace Archarina64.Core;

public sealed record RomActorEdit(int SceneId, int RoomId, int ActorIndex, RomActor Actor);
public sealed record RomActorListEdit(int SceneId, int RoomId, IReadOnlyList<RomActor> Actors);
public sealed record RomRoomOrderEdit(int SceneId, IReadOnlyList<int> Order);
public sealed record RomAlternateRoomOrderEdit(int SceneId, int HeaderIndex, IReadOnlyList<int> Order);
public sealed record RomEntranceEdit(int Index, byte SceneId, byte SpawnId);
public sealed record RomPathEdit(int SceneId, int PathId, int PointIndex, RomPathPoint Point);
public sealed record RomPathListEdit(int SceneId, IReadOnlyList<RomPath> Paths);
public sealed record RomExitEdit(int SceneId, int ExitIndex, ushort Raw);
public sealed record RomWaterboxEdit(int SceneId, int WaterboxIndex, RomWaterbox Waterbox);
public sealed record RomEnvironmentEdit(int SceneId, int EnvironmentIndex, RomEnvironment Environment);
public sealed record RomCollisionVertexEdit(int SceneId, int VertexIndex, RomCollisionVertex Vertex);
public sealed record RomCollisionTriangleEdit(int SceneId, int TriangleIndex, RomCollisionTriangle Triangle);
public sealed record RomCollisionSurfaceEdit(int SceneId, int SurfaceIndex, ulong SurfaceType);
public sealed record RomCollisionBoundsEdit(int SceneId, short MinX, short MinY, short MinZ, short MaxX, short MaxY, short MaxZ);
public sealed record RomCollisionTopologyEdit(int SceneId, IReadOnlyList<RomCollisionVertex> Vertices, IReadOnlyList<RomCollisionTriangle> Triangles, IReadOnlyList<ulong> SurfaceTypes);
public sealed record RomGeometryVertexEdit(int SceneId, int RoomId, int VertexIndex, int SourceOffset, RomGeometryVertex Vertex);
public sealed record RomGeometryTriangleEdit(int SceneId, int RoomId, int TriangleIndex, int SourceOffset, uint Prefix, byte SlotA, byte SlotB, byte SlotC, RomGeometryTriangle Triangle);
public sealed record RomGeometryCommandEdit(int SceneId, int RoomId, int SourceOffset, uint Word0, uint Word1);
public sealed record RomTextureEdit(int SceneId, int RoomId, int SourceOffset, byte Segment, int Address, string Format, string Size, int Width, int Height, byte[] Rgba, byte[] PaletteRgba = null, int PaletteBase = 0, int OriginalWidth = 0, int OriginalHeight = 0);
public sealed record RomGeometryRebuildEdit(int SceneId, int RoomId, int MeshEntryOffset, IReadOnlyList<RomGeometryVertex> Vertices, IReadOnlyList<RomGeometryTriangle> Triangles);
public sealed record RomSpawnEdit(int SceneId, int SpawnIndex, RomSpawnPoint Spawn);
public sealed record RomTransitionEdit(int SceneId, int TransitionIndex, RomTransition Transition);
public sealed record RomObjectEdit(int SceneId, int RoomId, int ObjectIndex, ushort ObjectId);
public sealed record RomObjectListEdit(int SceneId, int RoomId, IReadOnlyList<ushort> ObjectIds);
public sealed record RomRoomTopologyEdit(int SceneId, int RoomId, int SourceRoomId = -1, bool Delete = false);
public sealed record RomAlternateRoomTopologyEdit(int SceneId, int HeaderIndex, int RoomId, int SourceRoomId = -1, bool Delete = false);
public sealed record RomSceneTopologyEdit(int SceneId, int SourceSceneId = -1, bool Delete = false, IReadOnlyList<int> Slots = null, IReadOnlyList<int> Order = null);
public sealed record RomRoomSettingsEdit(int SceneId, int RoomId, RomRoomSettings Settings);
public sealed record RomRoomExitEdit(int SceneId, int RoomId, int ExitIndex, ushort Raw);
public sealed record RomCameraEdit(int SceneId, int CameraIndex, RomCamera Camera);
public sealed record RomSceneSettingsEdit(int SceneId, RomSceneSettings Settings);
public sealed record RomAlternateHeaderEdit(int SceneId, int HeaderIndex, uint Pointer, bool Delete = false);
public sealed record RomAlternateCommandEdit(int SceneId, int HeaderIndex, int CommandIndex, RomHeaderCommand Command, bool Insert = false, bool Delete = false);
public sealed record RomSceneCommandEdit(int SceneId, int CommandIndex, RomHeaderCommand Command, bool Insert = false, bool Delete = false);
public sealed record RomAlternateSceneSettingsEdit(int SceneId, int HeaderIndex, RomSceneSettings Settings);
public sealed record RomAlternateActorEdit(int SceneId, int HeaderIndex, int RoomId, int ActorIndex, RomActor Actor);
public sealed record RomAlternateActorListEdit(int SceneId, int HeaderIndex, int RoomId, IReadOnlyList<RomActor> Actors);
public sealed record RomAlternateObjectEdit(int SceneId, int HeaderIndex, int RoomId, int ObjectIndex, ushort ObjectId);
public sealed record RomAlternateObjectListEdit(int SceneId, int HeaderIndex, int RoomId, IReadOnlyList<ushort> ObjectIds);
public sealed record RomAlternateRoomSettingsEdit(int SceneId, int HeaderIndex, int RoomId, RomRoomSettings Settings);
public sealed record RomAlternateRoomExitEdit(int SceneId, int HeaderIndex, int RoomId, int ExitIndex, ushort Raw);
public sealed record RomAlternateGeometryVertexEdit(int SceneId, int HeaderIndex, int RoomId, int VertexIndex, int SourceOffset, RomGeometryVertex Vertex);
public sealed record RomAlternateGeometryTriangleEdit(int SceneId, int HeaderIndex, int RoomId, int TriangleIndex, int SourceOffset, uint Prefix, byte SlotA, byte SlotB, byte SlotC, RomGeometryTriangle Triangle);
public sealed record RomAlternateCollisionVertexEdit(int SceneId, int HeaderIndex, int VertexIndex, RomCollisionVertex Vertex);
public sealed record RomAlternateCollisionTriangleEdit(int SceneId, int HeaderIndex, int TriangleIndex, RomCollisionTriangle Triangle);
public sealed record RomAlternateCollisionSurfaceEdit(int SceneId, int HeaderIndex, int SurfaceIndex, ulong SurfaceType);
public sealed record RomAlternateCollisionBoundsEdit(int SceneId, int HeaderIndex, short MinX, short MinY, short MinZ, short MaxX, short MaxY, short MaxZ);
public sealed record RomAlternateCollisionTopologyEdit(int SceneId, int HeaderIndex, IReadOnlyList<RomCollisionVertex> Vertices, IReadOnlyList<RomCollisionTriangle> Triangles, IReadOnlyList<ulong> SurfaceTypes);
public sealed record RomAlternateCameraEdit(int SceneId, int HeaderIndex, int CameraIndex, RomCamera Camera);
public sealed record RomAlternateWaterboxEdit(int SceneId, int HeaderIndex, int WaterboxIndex, RomWaterbox Waterbox);
public sealed record RomWorkspacePatch(string RomFingerprint, IReadOnlyList<RomActorEdit> Actors, IReadOnlyList<RomEntranceEdit> Entrances = null, IReadOnlyList<RomPathEdit> Paths = null, IReadOnlyList<RomExitEdit> Exits = null, IReadOnlyList<RomWaterboxEdit> Waterboxes = null, IReadOnlyList<RomEnvironmentEdit> Environments = null, IReadOnlyList<RomCollisionVertexEdit> CollisionVertices = null, IReadOnlyList<RomCollisionTriangleEdit> CollisionTriangles = null, IReadOnlyList<RomCollisionSurfaceEdit> CollisionSurfaces = null, IReadOnlyList<RomCollisionBoundsEdit> CollisionBounds = null, IReadOnlyList<RomGeometryVertexEdit> GeometryVertices = null, IReadOnlyList<RomGeometryTriangleEdit> GeometryTriangles = null, IReadOnlyList<RomSpawnEdit> Spawns = null, IReadOnlyList<RomTransitionEdit> Transitions = null, IReadOnlyList<RomObjectEdit> Objects = null, IReadOnlyList<RomObjectListEdit> ObjectLists = null, IReadOnlyList<RomActorListEdit> ActorLists = null, IReadOnlyList<RomRoomOrderEdit> RoomOrders = null, IReadOnlyList<RomRoomSettingsEdit> RoomSettings = null, IReadOnlyList<RomRoomExitEdit> RoomExits = null, IReadOnlyList<RomCameraEdit> Cameras = null, IReadOnlyList<RomSceneSettingsEdit> SceneSettings = null, IReadOnlyList<RomSceneCommandEdit> SceneCommands = null, IReadOnlyList<RomAlternateHeaderEdit> AlternateHeaders = null, IReadOnlyList<RomAlternateCommandEdit> AlternateCommands = null, IReadOnlyList<RomAlternateSceneSettingsEdit> AlternateSceneSettings = null, IReadOnlyList<RomAlternateActorEdit> AlternateActors = null, IReadOnlyList<RomAlternateActorListEdit> AlternateActorLists = null, IReadOnlyList<RomAlternateObjectEdit> AlternateObjects = null, IReadOnlyList<RomAlternateObjectListEdit> AlternateObjectLists = null, IReadOnlyList<RomAlternateRoomSettingsEdit> AlternateRoomSettings = null, IReadOnlyList<RomAlternateRoomExitEdit> AlternateRoomExits = null, IReadOnlyList<RomAlternateGeometryVertexEdit> AlternateGeometryVertices = null, IReadOnlyList<RomAlternateGeometryTriangleEdit> AlternateGeometryTriangles = null, IReadOnlyList<RomAlternateCollisionVertexEdit> AlternateCollisionVertices = null, IReadOnlyList<RomAlternateCollisionTriangleEdit> AlternateCollisionTriangles = null, IReadOnlyList<RomAlternateCollisionSurfaceEdit> AlternateCollisionSurfaces = null, IReadOnlyList<RomAlternateCollisionBoundsEdit> AlternateCollisionBounds = null, IReadOnlyList<RomAlternateCameraEdit> AlternateCameras = null, IReadOnlyList<RomAlternateWaterboxEdit> AlternateWaterboxes = null, IReadOnlyList<RomGeometryRebuildEdit> GeometryRebuilds = null, IReadOnlyList<RomCollisionTopologyEdit> CollisionTopologies = null, IReadOnlyList<RomAlternateCollisionTopologyEdit> AlternateCollisionTopologies = null, IReadOnlyList<RomPathListEdit> PathLists = null, IReadOnlyList<RomGeometryCommandEdit> GeometryCommands = null, IReadOnlyList<RomTextureEdit> Textures = null, IReadOnlyList<RomSceneTopologyEdit> SceneTopology = null, IReadOnlyList<RomRoomTopologyEdit> RoomTopology = null, IReadOnlyList<RomAlternateRoomTopologyEdit> AlternateRoomTopology = null, IReadOnlyList<RomAlternateRoomOrderEdit> AlternateRoomOrders = null);

/// <summary>Native in-memory editing session for decoded ROM data. It is deliberately separate from XML tile packages.</summary>
public sealed class RomWorkspace
{
    private readonly List<RomActorEdit> edits = new();
    private readonly List<RomEntranceEdit> entranceEdits = new();
    private readonly List<RomPathEdit> pathEdits = new();
    private readonly List<RomPathListEdit> pathListEdits = new();
    private readonly List<RomExitEdit> exitEdits = new();
    private readonly List<RomWaterboxEdit> waterboxEdits = new();
    private readonly List<RomEnvironmentEdit> environmentEdits = new();
    private readonly List<RomCollisionVertexEdit> collisionVertexEdits = new();
    private readonly List<RomCollisionTriangleEdit> collisionTriangleEdits = new();
    private readonly List<RomCollisionSurfaceEdit> collisionSurfaceEdits = new(); private readonly List<RomCollisionBoundsEdit> collisionBoundsEdits = new();
    private readonly List<RomCollisionTopologyEdit> collisionTopologyEdits = new();
    private readonly List<RomGeometryVertexEdit> geometryVertexEdits = new();
    private readonly List<RomGeometryTriangleEdit> geometryTriangleEdits = new();
    private readonly List<RomGeometryCommandEdit> geometryCommandEdits = new();
    private readonly List<RomTextureEdit> textureEdits = new();
    private readonly List<RomGeometryRebuildEdit> geometryRebuildEdits = new();
    private readonly List<RomSpawnEdit> spawnEdits = new();
    private readonly List<RomTransitionEdit> transitionEdits = new();
    private readonly List<RomObjectEdit> objectEdits = new();
    private readonly List<RomObjectListEdit> objectListEdits = new();
    private readonly List<RomActorListEdit> actorListEdits = new();
    private readonly List<RomRoomOrderEdit> roomOrderEdits = new();
    private readonly List<RomRoomTopologyEdit> roomTopologyEdits = new();
    private readonly List<RomSceneTopologyEdit> sceneTopologyEdits = new();
    private readonly List<RomAlternateRoomOrderEdit> alternateRoomOrderEdits = new();
    private readonly List<RomAlternateRoomTopologyEdit> alternateRoomTopologyEdits = new();
    private bool topologyDirty;
    private readonly List<RomRoomSettingsEdit> roomSettingsEdits = new();
    private readonly List<RomRoomExitEdit> roomExitEdits = new();
    private readonly List<RomCameraEdit> cameraEdits = new();
    private readonly List<RomSceneSettingsEdit> sceneSettingsEdits = new(); private readonly List<RomSceneCommandEdit> sceneCommandEdits = new();
    private readonly List<RomAlternateHeaderEdit> alternateHeaderEdits = new(); private readonly List<RomAlternateCommandEdit> alternateCommandEdits = new();
    private readonly List<RomAlternateSceneSettingsEdit> alternateSceneSettingsEdits = new();
    private readonly List<RomAlternateActorEdit> alternateActorEdits = new();
    private readonly List<RomAlternateActorListEdit> alternateActorListEdits = new();
    private readonly List<RomAlternateObjectEdit> alternateObjectEdits = new(); private readonly List<RomAlternateRoomSettingsEdit> alternateRoomSettingsEdits = new(); private readonly List<RomAlternateRoomExitEdit> alternateRoomExitEdits = new(); private readonly List<RomAlternateGeometryVertexEdit> alternateGeometryVertexEdits = new(); private readonly List<RomAlternateGeometryTriangleEdit> alternateGeometryTriangleEdits = new(); private readonly List<RomAlternateCollisionVertexEdit> alternateCollisionVertexEdits = new(); private readonly List<RomAlternateCollisionTriangleEdit> alternateCollisionTriangleEdits = new(); private readonly List<RomAlternateCollisionSurfaceEdit> alternateCollisionSurfaceEdits = new(); private readonly List<RomAlternateCollisionBoundsEdit> alternateCollisionBoundsEdits = new(); private readonly List<RomAlternateCollisionTopologyEdit> alternateCollisionTopologyEdits = new(); private readonly List<RomAlternateCameraEdit> alternateCameraEdits = new(); private readonly List<RomAlternateWaterboxEdit> alternateWaterboxEdits = new();
    private readonly List<RomAlternateObjectListEdit> alternateObjectListEdits = new();
    public RomDocument Document { get; private set; }
    public IReadOnlyList<RomActorEdit> ActorEdits => edits;
    public IReadOnlyList<RomEntranceEdit> EntranceEdits => entranceEdits;
    public IReadOnlyList<RomPathEdit> PathEdits => pathEdits;
    public IReadOnlyList<RomPathListEdit> PathListEdits => pathListEdits;
    public IReadOnlyList<RomExitEdit> ExitEdits => exitEdits;
    public IReadOnlyList<RomWaterboxEdit> WaterboxEdits => waterboxEdits;
    public IReadOnlyList<RomEnvironmentEdit> EnvironmentEdits => environmentEdits;
    public IReadOnlyList<RomCollisionVertexEdit> CollisionVertexEdits => collisionVertexEdits;
    public IReadOnlyList<RomCollisionTriangleEdit> CollisionTriangleEdits => collisionTriangleEdits;
    public IReadOnlyList<RomCollisionSurfaceEdit> CollisionSurfaceEdits => collisionSurfaceEdits;
    public IReadOnlyList<RomCollisionBoundsEdit> CollisionBoundsEdits => collisionBoundsEdits;
    public IReadOnlyList<RomCollisionTopologyEdit> CollisionTopologyEdits => collisionTopologyEdits;
    public IReadOnlyList<RomGeometryVertexEdit> GeometryVertexEdits => geometryVertexEdits;
    public IReadOnlyList<RomGeometryTriangleEdit> GeometryTriangleEdits => geometryTriangleEdits;
    public IReadOnlyList<RomGeometryCommandEdit> GeometryCommandEdits => geometryCommandEdits;
    public IReadOnlyList<RomTextureEdit> TextureEdits => textureEdits;
    public IReadOnlyList<RomGeometryRebuildEdit> GeometryRebuildEdits => geometryRebuildEdits;
    public IReadOnlyList<RomSpawnEdit> SpawnEdits => spawnEdits;
    public IReadOnlyList<RomTransitionEdit> TransitionEdits => transitionEdits;
    public IReadOnlyList<RomObjectEdit> ObjectEdits => objectEdits;
    public IReadOnlyList<RomObjectListEdit> ObjectListEdits => objectListEdits;
    public IReadOnlyList<RomActorListEdit> ActorListEdits => actorListEdits;
    public IReadOnlyList<RomRoomOrderEdit> RoomOrderEdits => roomOrderEdits;
    public IReadOnlyList<RomRoomTopologyEdit> RoomTopologyEdits => roomTopologyEdits;
    public IReadOnlyList<RomSceneTopologyEdit> SceneTopologyEdits => sceneTopologyEdits;
    public IReadOnlyList<RomAlternateRoomOrderEdit> AlternateRoomOrderEdits => alternateRoomOrderEdits;
    public IReadOnlyList<RomAlternateRoomTopologyEdit> AlternateRoomTopologyEdits => alternateRoomTopologyEdits;
    public bool TopologyDirty => topologyDirty;
    public IReadOnlyList<RomRoomSettingsEdit> RoomSettingsEdits => roomSettingsEdits;
    public IReadOnlyList<RomRoomExitEdit> RoomExitEdits => roomExitEdits;
    public IReadOnlyList<RomCameraEdit> CameraEdits => cameraEdits;
    public IReadOnlyList<RomSceneSettingsEdit> SceneSettingsEdits => sceneSettingsEdits;
    public IReadOnlyList<RomSceneCommandEdit> SceneCommandEdits => sceneCommandEdits;
    public IReadOnlyList<RomAlternateHeaderEdit> AlternateHeaderEdits => alternateHeaderEdits;
    public IReadOnlyList<RomAlternateCommandEdit> AlternateCommandEdits => alternateCommandEdits;
    public IReadOnlyList<RomAlternateSceneSettingsEdit> AlternateSceneSettingsEdits => alternateSceneSettingsEdits;
    public IReadOnlyList<RomAlternateActorEdit> AlternateActorEdits => alternateActorEdits;
    public IReadOnlyList<RomAlternateActorListEdit> AlternateActorListEdits => alternateActorListEdits;
    public IReadOnlyList<RomAlternateObjectEdit> AlternateObjectEdits => alternateObjectEdits;
    public IReadOnlyList<RomAlternateObjectListEdit> AlternateObjectListEdits => alternateObjectListEdits;
    public IReadOnlyList<RomAlternateRoomSettingsEdit> AlternateRoomSettingsEdits => alternateRoomSettingsEdits;
    public IReadOnlyList<RomAlternateRoomExitEdit> AlternateRoomExitEdits => alternateRoomExitEdits;
    public IReadOnlyList<RomAlternateGeometryVertexEdit> AlternateGeometryVertexEdits => alternateGeometryVertexEdits;
    public IReadOnlyList<RomAlternateGeometryTriangleEdit> AlternateGeometryTriangleEdits => alternateGeometryTriangleEdits;
    public IReadOnlyList<RomAlternateCollisionVertexEdit> AlternateCollisionVertexEdits => alternateCollisionVertexEdits;
    public IReadOnlyList<RomAlternateCollisionTriangleEdit> AlternateCollisionTriangleEdits => alternateCollisionTriangleEdits;
    public IReadOnlyList<RomAlternateCollisionSurfaceEdit> AlternateCollisionSurfaceEdits => alternateCollisionSurfaceEdits;
    public IReadOnlyList<RomAlternateCollisionBoundsEdit> AlternateCollisionBoundsEdits => alternateCollisionBoundsEdits;
    public IReadOnlyList<RomAlternateCollisionTopologyEdit> AlternateCollisionTopologyEdits => alternateCollisionTopologyEdits;
    public IReadOnlyList<RomAlternateCameraEdit> AlternateCameraEdits => alternateCameraEdits;
    public IReadOnlyList<RomAlternateWaterboxEdit> AlternateWaterboxEdits => alternateWaterboxEdits;

    public RomWorkspace(RomDocument document) => Document = document ?? throw new ArgumentNullException(nameof(document));

    public RomScene CloneScene(int sourceSceneId, int targetSceneId)
    {
        if (targetSceneId < 0 || targetSceneId >= Document.Profile.SceneCount) throw new InvalidDataException("Target scene index is outside the profile.");
        var source = Document.Scenes.FirstOrDefault(s => s.Id == sourceSceneId) ?? throw new InvalidDataException("Source scene is not loaded.");
        if (Document.Scenes.Any(s => s.Id == targetSceneId)) throw new InvalidDataException("Target scene slot is already occupied.");
        var clone = source with { Id = targetSceneId, Start = 0, End = 0, Name = $"{source.Name} Clone", Diagnostics = [.. source.Diagnostics, "Cloned in Android scene workspace; native scene-table allocation will occur on export."] };
        var scenes = Document.Scenes.Concat([clone]).OrderBy(s => s.Id).ToArray(); Document = Document with { Scenes = scenes };
        sceneTopologyEdits.RemoveAll(e => e.SceneId == targetSceneId); sceneTopologyEdits.Add(new RomSceneTopologyEdit(targetSceneId, sourceSceneId)); return clone;
    }

    public void DeleteScene(int sceneId)
    {
        if (sceneId < 0 || sceneId >= Document.Profile.SceneCount) throw new InvalidDataException("Scene index is outside the profile.");
        if (!Document.Scenes.Any(s => s.Id == sceneId)) throw new InvalidDataException("Scene is not loaded.");
        Document = Document with { Scenes = Document.Scenes.Where(s => s.Id != sceneId).ToArray() };
        bool canceledClone = sceneTopologyEdits.RemoveAll(e => e.SceneId == sceneId && !e.Delete) > 0; if (!canceledClone) { sceneTopologyEdits.RemoveAll(e => e.SceneId == sceneId); sceneTopologyEdits.Add(new RomSceneTopologyEdit(sceneId, Delete: true)); }
    }

    public IReadOnlyList<RomScene> ReorderScenes(IReadOnlyList<int> order)
    {
        var slots = Document.Scenes.Select(s => s.Id).OrderBy(id => id).ToArray(); if (order is null || order.Count != slots.Length || order.Distinct().Count() != slots.Length || order.Any(id => !slots.Contains(id))) throw new InvalidDataException("Scene order must be a complete permutation of the loaded scene slots.");
        var sceneMap = order.Select((sourceId, index) => (Source: sourceId, Target: slots[index])).ToDictionary(item => item.Source, item => item.Target);
        var originalEntrances = (Document.Entrances ?? []).ToArray(); var reordered = order.Select((sourceId, index) => Document.Scenes.First(s => s.Id == sourceId) with { Id = slots[index] }).OrderBy(s => s.Id).ToArray();
        var entrances = originalEntrances.Select(entry => sceneMap.TryGetValue(entry.SceneId, out int target) ? entry with { SceneId = checked((byte)target) } : entry).ToArray(); Document = Document with { Scenes = reordered, Entrances = entrances };
        foreach (var entry in entrances) { var original = originalEntrances.FirstOrDefault(item => item.Index == entry.Index); if (original is not null && original.SceneId == entry.SceneId) continue; entranceEdits.RemoveAll(edit => edit.Index == entry.Index); entranceEdits.Add(new RomEntranceEdit(entry.Index, entry.SceneId, entry.SpawnId)); }
        sceneTopologyEdits.RemoveAll(e => e.Slots is not null); sceneTopologyEdits.Add(new RomSceneTopologyEdit(-1, Slots: slots, Order: order.ToArray())); return reordered;
    }

    public RomActor EditRoomActor(int sceneId, int roomId, int actorIndex, RomActor actor)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        var room = scene.Rooms[roomId]; if (actorIndex < 0 || actorIndex >= room.Actors.Count) throw new InvalidDataException("Actor is not loaded.");
        var actors = room.Actors.ToArray(); actors[actorIndex] = actor;
        var rooms = scene.Rooms.ToArray(); rooms[roomId] = room with { Actors = actors };
        var scenes = Document.Scenes.ToArray(); scenes[Document.Scenes.ToList().IndexOf(scene)] = scene with { Rooms = rooms };
        Document = Document with { Scenes = scenes };
        edits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId && e.ActorIndex == actorIndex);
        edits.Add(new RomActorEdit(sceneId, roomId, actorIndex, actor));
        return actor;
    }

    public IReadOnlyList<ushort> EditRoomObjects(int sceneId, int roomId, IReadOnlyList<ushort> objectIds)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        if (objectIds.Count > byte.MaxValue) throw new InvalidDataException("A room cannot contain more than 255 objects.");
        var room = scene.Rooms[roomId]; var updated = objectIds.ToArray(); var rooms = scene.Rooms.ToArray(); rooms[roomId] = room with { Objects = updated, ObjectCount = updated.Length };
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Rooms = rooms }; Document = Document with { Scenes = scenes };
        objectListEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId); objectListEdits.Add(new RomObjectListEdit(sceneId, roomId, updated));
        return updated;
    }

    public IReadOnlyList<RomActor> EditRoomActors(int sceneId, int roomId, IReadOnlyList<RomActor> actors)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        if (actors.Count > byte.MaxValue) throw new InvalidDataException("A room cannot contain more than 255 actors.");
        var updated = actors.ToArray(); var rooms = scene.Rooms.ToArray(); rooms[roomId] = scene.Rooms[roomId] with { Actors = updated };
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Rooms = rooms }; Document = Document with { Scenes = scenes };
        actorListEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId); actorListEdits.Add(new RomActorListEdit(sceneId, roomId, updated)); return updated;
    }

    public IReadOnlyList<RomRoom> ReorderRooms(int sceneId, IReadOnlyList<int> order)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        if (order.Count != scene.Rooms.Count || order.Distinct().Count() != order.Count || order.Any(index => index < 0 || index >= scene.Rooms.Count)) throw new InvalidDataException("Room order must be a complete permutation.");
        EnsureRoomTopologyCanShift(sceneId, allowRoomOrder: true);
        var previousOrder = roomOrderEdits.FirstOrDefault(edit => edit.SceneId == sceneId)?.Order;
        var exportOrder = previousOrder is null ? order.ToArray() : order.Select(index => previousOrder[index]).ToArray();
        var roomMap = order.Select((source, target) => (source, target)).ToDictionary(item => item.source, item => item.target);
        var rooms = order.Select((source, target) => scene.Rooms[source] with { Id = target }).ToArray();
        var updated = RemapRoomReferences(scene, rooms, roomMap); ReplaceScene(updated);
        roomOrderEdits.RemoveAll(e => e.SceneId == sceneId); roomOrderEdits.Add(new RomRoomOrderEdit(sceneId, exportOrder)); return updated.Rooms;
    }

    public IReadOnlyList<RomRoom> ReorderAlternateRooms(int sceneId, int headerIndex, IReadOnlyList<int> order)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded."); var header = (scene.AlternateHeaders ?? []).FirstOrDefault(h => h.Index == headerIndex) ?? throw new InvalidDataException("Alternate header is not loaded."); var rooms = header.Rooms ?? throw new InvalidDataException("Alternate rooms are not loaded.");
        if (order.Count != rooms.Count || order.Distinct().Count() != order.Count || order.Any(index => index < 0 || index >= rooms.Count)) throw new InvalidDataException("Alternate room order must be a complete permutation.");
        var reordered = order.Select(index => rooms[index] with { Id = order.ToList().IndexOf(index) }).ToArray(); var headers = scene.AlternateHeaders.ToArray(); headers[Array.IndexOf(headers, header)] = header with { Rooms = reordered }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateRoomOrderEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex); alternateRoomOrderEdits.Add(new RomAlternateRoomOrderEdit(sceneId, headerIndex, order.ToArray())); return reordered;
    }

    public RomRoom CloneRoom(int sceneId, int roomId)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        return InsertRoomClone(sceneId, roomId, scene.Rooms.Count);
    }

    public RomRoom InsertRoomClone(int sceneId, int sourceRoomId, int insertIndex)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        if (sourceRoomId < 0 || sourceRoomId >= scene.Rooms.Count) throw new InvalidDataException("Source room is not loaded.");
        if (insertIndex < 0 || insertIndex > scene.Rooms.Count || scene.Rooms.Count >= byte.MaxValue) throw new InvalidDataException("Room insertion index or count is outside the native range.");
        EnsureRoomTopologyCanShift(sceneId);
        var roomMap = Enumerable.Range(0, scene.Rooms.Count).ToDictionary(index => index, index => index < insertIndex ? index : index + 1);
        var source = scene.Rooms[sourceRoomId];
        var clone = source with { Id = insertIndex, Start = 0, End = 0, Diagnostics = [.. source.Diagnostics, "Cloned in Android scene workspace; native room allocation will occur on export."] };
        var rooms = scene.Rooms.Select((room, index) => room with { Id = roomMap[index] }).ToList(); rooms.Insert(insertIndex, clone);
        ReplaceScene(RemapRoomReferences(scene, rooms, roomMap));
        roomTopologyEdits.Add(new RomRoomTopologyEdit(sceneId, insertIndex, sourceRoomId));
        return Document.Scenes.First(s => s.Id == sceneId).Rooms[insertIndex];
    }

    public void DeleteRoom(int sceneId, int roomId)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        if (scene.Rooms.Count <= 1) throw new InvalidDataException("A scene must retain at least one room.");
        if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        EnsureRoomTopologyCanShift(sceneId);
        var roomMap = Enumerable.Range(0, scene.Rooms.Count).Where(index => index != roomId).ToDictionary(index => index, index => index < roomId ? index : index - 1);
        var rooms = scene.Rooms.Where((_, index) => index != roomId).Select((room, index) => room with { Id = index }).ToArray();
        ReplaceScene(RemapRoomReferences(scene, rooms, roomMap));
        roomTopologyEdits.Add(new RomRoomTopologyEdit(sceneId, roomId, Delete: true));
    }

    private RomScene RemapRoomReferences(RomScene scene, IReadOnlyList<RomRoom> rooms, IReadOnlyDictionary<int, int> roomMap)
    {
        int RemapTarget(int target)
        {
            if (roomMap.TryGetValue(target, out int mapped)) return mapped;
            if (target < scene.Rooms.Count) throw new InvalidDataException($"Room {target:D2} is still referenced by a transition or surviving room exit. Change those references before deleting it.");
            return target;
        }
        ushort RemapExit(ushort raw)
        {
            int mapped = RemapTarget((raw >> 9) & 0x3F);
            if (mapped > 0x3F) throw new InvalidDataException("A room exit cannot target a room above 63.");
            return checked((ushort)((raw & 0x81FF) | (mapped << 9)));
        }
        var remappedRooms = rooms.Select(room => room with { Exits = room.Exits is null ? null : room.Exits.Select(RemapExit).ToArray() }).ToArray();
        var transitions = (scene.Transitions ?? []).Select(transition => transition with { FrontRoom = checked((byte)RemapTarget(transition.FrontRoom)), BackRoom = checked((byte)RemapTarget(transition.BackRoom)) }).ToArray();
        transitionEdits.RemoveAll(edit => edit.SceneId == scene.Id);
        foreach (var (transition, index) in transitions.Select((value, index) => (value, index))) transitionEdits.Add(new RomTransitionEdit(scene.Id, index, transition));
        roomExitEdits.RemoveAll(edit => edit.SceneId == scene.Id);
        foreach (var room in remappedRooms) foreach (var (raw, index) in (room.Exits ?? []).Select((value, index) => (value, index))) roomExitEdits.Add(new RomRoomExitEdit(scene.Id, room.Id, index, raw));
        return scene with { Rooms = remappedRooms, Transitions = scene.Transitions is null ? null : transitions };
    }

    private void EnsureRoomTopologyCanShift(int sceneId, bool allowRoomOrder = false)
    {
        if ((!allowRoomOrder && roomOrderEdits.Any(edit => edit.SceneId == sceneId)) || edits.Any(edit => edit.SceneId == sceneId) || objectEdits.Any(edit => edit.SceneId == sceneId) || objectListEdits.Any(edit => edit.SceneId == sceneId) || actorListEdits.Any(edit => edit.SceneId == sceneId) || roomSettingsEdits.Any(edit => edit.SceneId == sceneId) || geometryVertexEdits.Any(edit => edit.SceneId == sceneId) || geometryTriangleEdits.Any(edit => edit.SceneId == sceneId) || geometryCommandEdits.Any(edit => edit.SceneId == sceneId) || geometryRebuildEdits.Any(edit => edit.SceneId == sceneId) || textureEdits.Any(edit => edit.SceneId == sceneId))
            throw new InvalidDataException("Export and reopen the ROM before changing room order or count after editing room contents.");
    }

    public RomRoomGeometry ReplaceRoomGeometry(int sceneId, int roomId, RomRoomGeometry geometry)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        if (geometry.Vertices.Count == 0 || geometry.Triangles.Count == 0) throw new InvalidDataException("Imported geometry must contain vertices and triangles.");
        var room = scene.Rooms[roomId];
        var existing = room.Geometry;
        bool writable = existing is not null
            && existing.Vertices.Count == geometry.Vertices.Count
            && existing.Triangles.Count == geometry.Triangles.Count
            && existing.VertexOffsets is { Count: > 0 } offsets && offsets.Count == geometry.Vertices.Count
            && existing.VertexSlots is { Count: > 0 } slots && slots.Count == geometry.Vertices.Count
            && existing.TriangleOffsets is { Count: > 0 } triangleOffsets && triangleOffsets.Count == geometry.Triangles.Count;
        if (writable)
        {
            for (int i = 0; i < geometry.Triangles.Count; i++)
            {
                var triangle = geometry.Triangles[i];
                if (triangle.A < 0 || triangle.A >= existing!.VertexSlots!.Count || triangle.B < 0 || triangle.B >= existing.VertexSlots.Count || triangle.C < 0 || triangle.C >= existing.VertexSlots.Count)
                {
                    writable = false;
                    break;
                }
            }
        }

        bool rebuildable = !writable && existing is not null
            && existing.MeshType == 0 && existing.MeshCount == 1 && existing.MeshTableA >= 0 && existing.MeshTableB == 0
            && existing.DisplayListOffsets is { Count: 1 } && geometry.Vertices.Count <= 32
            && geometry.Triangles.All(t => t.A >= 0 && t.A < geometry.Vertices.Count && t.B >= 0 && t.B < geometry.Vertices.Count && t.C >= 0 && t.C < geometry.Vertices.Count);
        var preservedGeometry = writable
            ? geometry with { DisplayLists = existing!.DisplayLists, VertexOffsets = existing.VertexOffsets, VertexSlots = existing.VertexSlots, TriangleOffsets = existing.TriangleOffsets, TrianglePrefixes = existing.TrianglePrefixes, MeshOffset = existing.MeshOffset, MeshType = existing.MeshType, MeshCount = existing.MeshCount, MeshTableA = existing.MeshTableA, MeshTableB = existing.MeshTableB, DisplayListOffsets = existing.DisplayListOffsets }
            : rebuildable
                ? geometry with { DisplayLists = existing!.DisplayLists, MeshOffset = existing.MeshOffset, MeshType = existing.MeshType, MeshCount = existing.MeshCount, MeshTableA = existing.MeshTableA, MeshTableB = existing.MeshTableB, DisplayListOffsets = existing.DisplayListOffsets }
                : geometry;
        var diagnostics = writable || rebuildable
            ? room.Diagnostics
            : [.. room.Diagnostics, "Custom geometry imported into Android workspace; native display-list allocation is pending."];
        var rooms = scene.Rooms.ToArray();
        rooms[roomId] = room with { Geometry = preservedGeometry, HasMesh = true, Diagnostics = diagnostics };
        var scenes = Document.Scenes.ToArray();
        scenes[Array.IndexOf(scenes, scene)] = scene with { Rooms = rooms };
        Document = Document with { Scenes = scenes };

        geometryVertexEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId);
        geometryTriangleEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId);
        if (!writable)
        {
            geometryRebuildEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId);
            if (rebuildable)
            {
                geometryRebuildEdits.Add(new RomGeometryRebuildEdit(sceneId, roomId, existing!.MeshTableA, geometry.Vertices.ToArray(), geometry.Triangles.ToArray()));
                return preservedGeometry;
            }
            topologyDirty = true;
            return preservedGeometry;
        }

        geometryRebuildEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId);
        for (int i = 0; i < geometry.Vertices.Count; i++)
            geometryVertexEdits.Add(new RomGeometryVertexEdit(sceneId, roomId, i, existing!.VertexOffsets![i], geometry.Vertices[i]));
        for (int i = 0; i < geometry.Triangles.Count; i++)
        {
            var triangle = geometry.Triangles[i];
            geometryTriangleEdits.Add(new RomGeometryTriangleEdit(sceneId, roomId, i, existing!.TriangleOffsets![i], existing.TrianglePrefixes is { Count: > 0 } prefixes && i < prefixes.Count ? prefixes[i] : 0, (byte)existing.VertexSlots![triangle.A], (byte)existing.VertexSlots[triangle.B], (byte)existing.VertexSlots[triangle.C], triangle));
        }
        return preservedGeometry;
    }

    public RomEntrance EditEntrance(int index, byte sceneId, byte spawnId)
    {
        var entry = Document.Entrances.FirstOrDefault(e => e.Index == index) ?? throw new InvalidDataException("Entrance is not loaded.");
        var entries = Document.Entrances.ToArray(); int position = Array.FindIndex(entries, e => e.Index == index); entries[position] = entry with { SceneId = sceneId, SpawnId = spawnId };
        Document = Document with { Entrances = entries };
        entranceEdits.RemoveAll(e => e.Index == index); entranceEdits.Add(new RomEntranceEdit(index, sceneId, spawnId)); return entries[position];
    }

    public RomPathPoint EditPathPoint(int sceneId, int pathId, int pointIndex, RomPathPoint point)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var path = scene.Paths?.FirstOrDefault(p => p.Id == pathId) ?? throw new InvalidDataException("Path is not loaded.");
        if (pointIndex < 0 || pointIndex >= path.Points.Count) throw new InvalidDataException("Path point is not loaded.");
        var points = path.Points.ToArray(); points[pointIndex] = point;
        var paths = scene.Paths.ToArray(); paths[Array.IndexOf(paths, path)] = path with { Points = points };
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Paths = paths };
        Document = Document with { Scenes = scenes };
        pathEdits.RemoveAll(e => e.SceneId == sceneId && e.PathId == pathId && e.PointIndex == pointIndex);
        if (pathListEdits.FirstOrDefault(e => e.SceneId == sceneId) is { } pathList)
        {
            pathListEdits.Remove(pathList);
            pathListEdits.Add(new RomPathListEdit(sceneId, paths));
        }
        else pathEdits.Add(new RomPathEdit(sceneId, pathId, pointIndex, point));
        return point;
    }

    public IReadOnlyList<RomPath> ReplacePaths(int sceneId, IReadOnlyList<RomPath> paths)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        if (paths is null || paths.Count > byte.MaxValue) throw new InvalidDataException("A scene cannot contain more than 255 paths.");
        var normalized = paths.Select((path, index) =>
        {
            if (path is null || path.Points is null || path.Points.Count > byte.MaxValue) throw new InvalidDataException("Path waypoint count is outside the native range.");
            return path with { Id = index, Points = path.Points.ToArray() };
        }).ToArray();
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Paths = normalized }; Document = Document with { Scenes = scenes };
        pathEdits.RemoveAll(e => e.SceneId == sceneId); pathListEdits.RemoveAll(e => e.SceneId == sceneId); pathListEdits.Add(new RomPathListEdit(sceneId, normalized));
        return normalized;
    }

    public RomExit EditExit(int sceneId, int exitIndex, ushort raw)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var exit = scene.Exits?.FirstOrDefault(e => e.Index == exitIndex) ?? throw new InvalidDataException("Exit is not loaded.");
        var exits = scene.Exits.ToArray(); exits[Array.IndexOf(exits, exit)] = exit with { Raw = raw };
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Exits = exits };
        Document = Document with { Scenes = scenes };
        exitEdits.RemoveAll(e => e.SceneId == sceneId && e.ExitIndex == exitIndex);
        exitEdits.Add(new RomExitEdit(sceneId, exitIndex, raw));
        return exit with { Raw = raw };
    }

    public RomWaterbox EditWaterbox(int sceneId, int waterboxIndex, RomWaterbox waterbox)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var waterboxes = scene.Collision?.Waterboxes ?? throw new InvalidDataException("Scene collision waterboxes are not loaded.");
        if (waterboxIndex < 0 || waterboxIndex >= waterboxes.Count) throw new InvalidDataException("Waterbox is not loaded.");
        var updated = waterboxes.ToArray(); updated[waterboxIndex] = waterbox;
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Collision = scene.Collision with { Waterboxes = updated } };
        Document = Document with { Scenes = scenes };
        waterboxEdits.RemoveAll(e => e.SceneId == sceneId && e.WaterboxIndex == waterboxIndex);
        waterboxEdits.Add(new RomWaterboxEdit(sceneId, waterboxIndex, waterbox));
        return waterbox;
    }

    public RomEnvironment EditEnvironment(int sceneId, int environmentIndex, RomEnvironment environment)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var environments = scene.Environments ?? throw new InvalidDataException("Scene environments are not loaded.");
        if (environmentIndex < 0 || environmentIndex >= environments.Count) throw new InvalidDataException("Environment is not loaded.");
        var updated = environments.ToArray(); updated[environmentIndex] = environment;
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Environments = updated };
        Document = Document with { Scenes = scenes };
        environmentEdits.RemoveAll(e => e.SceneId == sceneId && e.EnvironmentIndex == environmentIndex);
        environmentEdits.Add(new RomEnvironmentEdit(sceneId, environmentIndex, environment));
        return environment;
    }

    public RomCollisionVertex EditCollisionVertex(int sceneId, int vertexIndex, RomCollisionVertex vertex)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var vertices = scene.Collision?.Vertices ?? throw new InvalidDataException("Scene collision vertices are not loaded.");
        if (vertexIndex < 0 || vertexIndex >= vertices.Count) throw new InvalidDataException("Collision vertex is not loaded.");
        var updated = vertices.ToArray(); updated[vertexIndex] = vertex;
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Collision = scene.Collision with { Vertices = updated } };
        Document = Document with { Scenes = scenes };
        collisionVertexEdits.RemoveAll(e => e.SceneId == sceneId && e.VertexIndex == vertexIndex);
        collisionVertexEdits.Add(new RomCollisionVertexEdit(sceneId, vertexIndex, vertex));
        return vertex;
    }

    public RomCollisionTriangle EditCollisionTriangle(int sceneId, int triangleIndex, RomCollisionTriangle triangle)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var triangles = scene.Collision?.Triangles ?? throw new InvalidDataException("Scene collision triangles are not loaded.");
        if (triangleIndex < 0 || triangleIndex >= triangles.Count) throw new InvalidDataException("Collision triangle is not loaded.");
        var updated = triangles.ToArray(); updated[triangleIndex] = triangle;
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Collision = scene.Collision with { Triangles = updated } };
        Document = Document with { Scenes = scenes };
        collisionTriangleEdits.RemoveAll(e => e.SceneId == sceneId && e.TriangleIndex == triangleIndex);
        collisionTriangleEdits.Add(new RomCollisionTriangleEdit(sceneId, triangleIndex, triangle));
        return triangle;
    }

    public ulong EditCollisionSurface(int sceneId, int surfaceIndex, ulong surfaceType)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var surfaces = scene.Collision?.SurfaceTypes ?? throw new InvalidDataException("Scene collision surfaces are not loaded.");
        if (surfaceIndex < 0 || surfaceIndex >= surfaces.Count) throw new InvalidDataException("Collision surface is not loaded.");
        var updated = surfaces.ToArray(); updated[surfaceIndex] = surfaceType;
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Collision = scene.Collision with { SurfaceTypes = updated } };
        Document = Document with { Scenes = scenes };
        collisionSurfaceEdits.RemoveAll(e => e.SceneId == sceneId && e.SurfaceIndex == surfaceIndex);
        collisionSurfaceEdits.Add(new RomCollisionSurfaceEdit(sceneId, surfaceIndex, surfaceType));
        return surfaceType;
    }

    public RomCollisionData ReplaceCollisionTopology(int sceneId, IReadOnlyList<RomCollisionVertex> vertices, IReadOnlyList<RomCollisionTriangle> triangles, IReadOnlyList<ulong> surfaceTypes)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var collision = scene.Collision ?? throw new InvalidDataException("Scene collision is not loaded.");
        if (vertices is null || vertices.Count == 0 || vertices.Count > ushort.MaxValue) throw new InvalidDataException("Collision vertex count is outside the native range.");
        if (triangles is null || triangles.Count > ushort.MaxValue) throw new InvalidDataException("Collision triangle count is outside the native range.");
        if (surfaceTypes is null || surfaceTypes.Count != triangles.Count) throw new InvalidDataException("Collision surface count must match the triangle count.");
        if (triangles.Any(t => t.A >= vertices.Count || t.B >= vertices.Count || t.C >= vertices.Count)) throw new InvalidDataException("Collision triangle references a vertex outside the replacement topology.");
        var updated = collision with { Vertices = vertices.ToArray(), Triangles = triangles.ToArray(), SurfaceTypes = surfaceTypes.ToArray() };
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Collision = updated }; Document = Document with { Scenes = scenes };
        collisionVertexEdits.RemoveAll(e => e.SceneId == sceneId); collisionTriangleEdits.RemoveAll(e => e.SceneId == sceneId); collisionSurfaceEdits.RemoveAll(e => e.SceneId == sceneId); collisionTopologyEdits.RemoveAll(e => e.SceneId == sceneId);
        collisionTopologyEdits.Add(new RomCollisionTopologyEdit(sceneId, updated.Vertices, updated.Triangles, updated.SurfaceTypes));
        return updated;
    }

    public RomCollisionData RecalculateCollision(int sceneId)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var collision = scene.Collision ?? throw new InvalidDataException("Scene collision is not loaded.");
        if (collision.Vertices.Count == 0) throw new InvalidDataException("Collision bounds cannot be recalculated without vertices.");
        var triangles = collision.Triangles.Select(triangle => RecalculateCollisionTriangle(collision.Vertices, triangle)).ToArray();
        var updated = collision with { MinX = collision.Vertices.Min(v => v.X), MinY = collision.Vertices.Min(v => v.Y), MinZ = collision.Vertices.Min(v => v.Z), MaxX = collision.Vertices.Max(v => v.X), MaxY = collision.Vertices.Max(v => v.Y), MaxZ = collision.Vertices.Max(v => v.Z), Triangles = triangles };
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Collision = updated }; Document = Document with { Scenes = scenes };
        if (collisionTopologyEdits.Any(e => e.SceneId == sceneId)) { collisionTopologyEdits.RemoveAll(e => e.SceneId == sceneId); collisionTopologyEdits.Add(new RomCollisionTopologyEdit(sceneId, updated.Vertices, updated.Triangles, updated.SurfaceTypes)); }
        else { collisionTriangleEdits.RemoveAll(e => e.SceneId == sceneId); collisionTriangleEdits.AddRange(triangles.Select((triangle, index) => new RomCollisionTriangleEdit(sceneId, index, triangle))); }
        collisionBoundsEdits.RemoveAll(e => e.SceneId == sceneId); collisionBoundsEdits.Add(new RomCollisionBoundsEdit(sceneId, updated.MinX, updated.MinY, updated.MinZ, updated.MaxX, updated.MaxY, updated.MaxZ));
        return updated;
    }

    public RomCollisionData RecalculateAlternateCollision(int sceneId, int headerIndex)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var header = (scene.AlternateHeaders ?? []).FirstOrDefault(h => h.Index == headerIndex) ?? throw new InvalidDataException("Alternate header is not loaded.");
        var collision = header.Collision ?? throw new InvalidDataException("Alternate collision is not loaded.");
        if (collision.Vertices.Count == 0) throw new InvalidDataException("Alternate collision bounds cannot be recalculated without vertices.");
        var triangles = collision.Triangles.Select(triangle => RecalculateCollisionTriangle(collision.Vertices, triangle)).ToArray();
        var updated = collision with { MinX = collision.Vertices.Min(v => v.X), MinY = collision.Vertices.Min(v => v.Y), MinZ = collision.Vertices.Min(v => v.Z), MaxX = collision.Vertices.Max(v => v.X), MaxY = collision.Vertices.Max(v => v.Y), MaxZ = collision.Vertices.Max(v => v.Z), Triangles = triangles };
        var headers = scene.AlternateHeaders.ToArray(); headers[Array.IndexOf(headers, header)] = header with { Collision = updated }; ReplaceScene(scene with { AlternateHeaders = headers });
        if (alternateCollisionTopologyEdits.Any(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex)) { alternateCollisionTopologyEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex); alternateCollisionTopologyEdits.Add(new RomAlternateCollisionTopologyEdit(sceneId, headerIndex, updated.Vertices, updated.Triangles, updated.SurfaceTypes)); }
        else { alternateCollisionTriangleEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex); alternateCollisionTriangleEdits.AddRange(triangles.Select((triangle, index) => new RomAlternateCollisionTriangleEdit(sceneId, headerIndex, index, triangle))); }
        alternateCollisionBoundsEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex); alternateCollisionBoundsEdits.Add(new RomAlternateCollisionBoundsEdit(sceneId, headerIndex, updated.MinX, updated.MinY, updated.MinZ, updated.MaxX, updated.MaxY, updated.MaxZ));
        return updated;
    }

    public RomCollisionData EditCollisionBounds(int sceneId, RomCollisionData bounds) { var scene = Document.Scenes.First(s => s.Id == sceneId); var collision = scene.Collision ?? throw new InvalidDataException("Scene collision is not loaded."); var updated = collision with { MinX = bounds.MinX, MinY = bounds.MinY, MinZ = bounds.MinZ, MaxX = bounds.MaxX, MaxY = bounds.MaxY, MaxZ = bounds.MaxZ }; var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Collision = updated }; Document = Document with { Scenes = scenes }; collisionBoundsEdits.RemoveAll(e => e.SceneId == sceneId); collisionBoundsEdits.Add(new RomCollisionBoundsEdit(sceneId, bounds.MinX, bounds.MinY, bounds.MinZ, bounds.MaxX, bounds.MaxY, bounds.MaxZ)); return updated; }

    public RomGeometryVertex EditGeometryVertex(int sceneId, int roomId, int vertexIndex, RomGeometryVertex vertex)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        var room = scene.Rooms[roomId]; var geometry = room.Geometry ?? throw new InvalidDataException("Room geometry is not loaded.");
        if (vertexIndex < 0 || vertexIndex >= geometry.Vertices.Count || geometry.VertexOffsets is not { Count: > 0 } || vertexIndex >= geometry.VertexOffsets.Count) throw new InvalidDataException("Geometry vertex is not loaded.");
        var updated = geometry.Vertices.ToArray(); updated[vertexIndex] = vertex; var rooms = scene.Rooms.ToArray(); rooms[roomId] = room with { Geometry = geometry with { Vertices = updated } };
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Rooms = rooms }; Document = Document with { Scenes = scenes };
        geometryVertexEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId && e.VertexIndex == vertexIndex);
        geometryVertexEdits.Add(new RomGeometryVertexEdit(sceneId, roomId, vertexIndex, geometry.VertexOffsets[vertexIndex], vertex)); return vertex;
    }

    public RomGeometryTriangle EditGeometryTriangle(int sceneId, int roomId, int triangleIndex, RomGeometryTriangle triangle)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded."); if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        var geometry = scene.Rooms[roomId].Geometry ?? throw new InvalidDataException("Room geometry is not loaded.");
        if (triangleIndex < 0 || triangleIndex >= geometry.Triangles.Count || geometry.TriangleOffsets is not { Count: > 0 } || triangleIndex >= geometry.TriangleOffsets.Count || geometry.VertexSlots is not { Count: > 0 }) throw new InvalidDataException("Geometry triangle is not loaded.");
        if (triangle.A < 0 || triangle.A >= geometry.VertexSlots.Count || triangle.B < 0 || triangle.B >= geometry.VertexSlots.Count || triangle.C < 0 || triangle.C >= geometry.VertexSlots.Count) throw new InvalidDataException("Geometry triangle vertex index is outside the mesh.");
        var triangles = geometry.Triangles.ToArray(); triangles[triangleIndex] = triangle; var rooms = scene.Rooms.ToArray(); rooms[roomId] = scene.Rooms[roomId] with { Geometry = geometry with { Triangles = triangles } }; var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Rooms = rooms }; Document = Document with { Scenes = scenes };
        geometryTriangleEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId && e.TriangleIndex == triangleIndex); geometryTriangleEdits.Add(new RomGeometryTriangleEdit(sceneId, roomId, triangleIndex, geometry.TriangleOffsets[triangleIndex], geometry.TrianglePrefixes?[triangleIndex] ?? 0, (byte)geometry.VertexSlots[triangle.A], (byte)geometry.VertexSlots[triangle.B], (byte)geometry.VertexSlots[triangle.C], triangle)); return triangle;
    }

    public RomDisplayListCommand EditGeometryCommand(int sceneId, int roomId, int sourceOffset, uint word0, uint word1)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded."); if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        var room = scene.Rooms[roomId]; var geometry = room.Geometry ?? throw new InvalidDataException("Room geometry is not loaded."); var commands = (geometry.DisplayListCommands ?? []).ToArray(); int commandIndex = Array.FindIndex(commands, command => command.SourceOffset == sourceOffset); if (commandIndex < 0) throw new InvalidDataException("Display-list command is not loaded.");
        var updatedCommand = new RomDisplayListCommand(sourceOffset, (byte)(word0 >> 24), word0, word1); commands[commandIndex] = updatedCommand; var rooms = scene.Rooms.ToArray(); rooms[roomId] = room with { Geometry = geometry with { DisplayListCommands = commands } }; var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Rooms = rooms }; Document = Document with { Scenes = scenes };
        geometryCommandEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId && e.SourceOffset == sourceOffset); geometryCommandEdits.Add(new RomGeometryCommandEdit(sceneId, roomId, sourceOffset, word0, word1)); return updatedCommand;
    }

    public RomTextureAsset ReplaceRoomTexture(int sceneId, int roomId, RomTextureAsset asset)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded."); if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        if (asset is null || !asset.IsDecoded || asset.Segment != 3) throw new InvalidDataException("Only decoded room-local textures can be replaced.");
        var commands = scene.Rooms[roomId].Geometry?.DisplayListCommands ?? []; var command = commands.FirstOrDefault(item => item.SourceOffset == asset.SourceOffset);
        if (command is null || !RomTextureCommandDecoder.TryDecode(command, out var info) || info.Kind != "G_SETTIMG" || info.Segment != asset.Segment || info.Address != asset.Address) throw new InvalidDataException("Texture source command is not loaded or no longer matches the selected asset.");
        int originalWidth = asset.SourceWidth > 0 ? asset.SourceWidth : info.Width; int originalHeight = asset.SourceHeight > 0 ? asset.SourceHeight : asset.Height;
        if (!string.Equals(info.FormatName, asset.Format, StringComparison.Ordinal) || !string.Equals(info.SizeName, asset.Size, StringComparison.Ordinal) || info.Width != originalWidth || asset.Width <= 0 || asset.Height <= 0) throw new InvalidDataException("Texture format or dimensions changed since extraction.");
        _ = RomTextureDecoder.EncodePixels(asset.Rgba, asset.Format, asset.Size, asset.Width, asset.Height, asset.PaletteRgba, asset.PaletteBase);
        textureEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId && e.SourceOffset == asset.SourceOffset);
        textureEdits.Add(new RomTextureEdit(sceneId, roomId, asset.SourceOffset, asset.Segment, asset.Address, asset.Format, asset.Size, asset.Width, asset.Height, asset.Rgba.ToArray(), asset.PaletteRgba?.ToArray(), asset.PaletteBase, originalWidth, originalHeight));
        return asset;
    }

    public RomSpawnPoint EditSpawn(int sceneId, int spawnIndex, RomSpawnPoint spawn)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded."); var spawns = scene.SpawnPoints ?? throw new InvalidDataException("Scene spawns are not loaded.");
        if (spawnIndex < 0 || spawnIndex >= spawns.Count) throw new InvalidDataException("Spawn is not loaded."); var updated = spawns.ToArray(); updated[spawnIndex] = spawn; var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { SpawnPoints = updated }; Document = Document with { Scenes = scenes };
        spawnEdits.RemoveAll(e => e.SceneId == sceneId && e.SpawnIndex == spawnIndex); spawnEdits.Add(new RomSpawnEdit(sceneId, spawnIndex, spawn)); return spawn;
    }

    public RomTransition EditTransition(int sceneId, int transitionIndex, RomTransition transition)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var transitions = scene.Transitions ?? throw new InvalidDataException("Scene transitions are not loaded.");
        if (transitionIndex < 0 || transitionIndex >= transitions.Count) throw new InvalidDataException("Transition is not loaded.");
        var updated = transitions.ToArray(); updated[transitionIndex] = transition;
        var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Transitions = updated }; Document = Document with { Scenes = scenes };
        transitionEdits.RemoveAll(e => e.SceneId == sceneId && e.TransitionIndex == transitionIndex); transitionEdits.Add(new RomTransitionEdit(sceneId, transitionIndex, transition)); return transition;
    }

    public ushort EditRoomObject(int sceneId, int roomId, int objectIndex, ushort objectId)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        var room = scene.Rooms[roomId]; var objects = room.Objects ?? throw new InvalidDataException("Room objects are not loaded.");
        if (objectIndex < 0 || objectIndex >= objects.Count) throw new InvalidDataException("Room object is not loaded.");
        var updated = objects.ToArray(); updated[objectIndex] = objectId; var rooms = scene.Rooms.ToArray(); rooms[roomId] = room with { Objects = updated, ObjectCount = updated.Length }; var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Rooms = rooms }; Document = Document with { Scenes = scenes };
        objectEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId && e.ObjectIndex == objectIndex); objectEdits.Add(new RomObjectEdit(sceneId, roomId, objectIndex, objectId)); return objectId;
    }

    public RomRoomSettings EditRoomSettings(int sceneId, int roomId, RomRoomSettings settings)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded."); if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded.");
        var rooms = scene.Rooms.ToArray(); rooms[roomId] = rooms[roomId] with { Settings = settings }; var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Rooms = rooms }; Document = Document with { Scenes = scenes };
        roomSettingsEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId); roomSettingsEdits.Add(new RomRoomSettingsEdit(sceneId, roomId, settings)); return settings;
    }

    public ushort EditRoomExit(int sceneId, int roomId, int exitIndex, ushort raw)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded."); if (roomId < 0 || roomId >= scene.Rooms.Count) throw new InvalidDataException("Room is not loaded."); var room = scene.Rooms[roomId]; var exits = room.Exits ?? throw new InvalidDataException("Room exits are not loaded."); if (exitIndex < 0 || exitIndex >= exits.Count) throw new InvalidDataException("Room exit is not loaded."); var updated = exits.ToArray(); updated[exitIndex] = raw; var rooms = scene.Rooms.ToArray(); rooms[roomId] = room with { Exits = updated }; var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Rooms = rooms }; Document = Document with { Scenes = scenes }; roomExitEdits.RemoveAll(e => e.SceneId == sceneId && e.RoomId == roomId && e.ExitIndex == exitIndex); roomExitEdits.Add(new RomRoomExitEdit(sceneId, roomId, exitIndex, raw)); return raw;
    }

    public RomCamera EditCamera(int sceneId, int cameraIndex, RomCamera camera)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded."); var cameras = scene.Collision?.Cameras ?? throw new InvalidDataException("Scene cameras are not loaded."); if (cameraIndex < 0 || cameraIndex >= cameras.Count) throw new InvalidDataException("Camera is not loaded."); var updated = cameras.ToArray(); updated[cameraIndex] = camera; var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Collision = scene.Collision with { Cameras = updated } }; Document = Document with { Scenes = scenes }; cameraEdits.RemoveAll(e => e.SceneId == sceneId && e.CameraIndex == cameraIndex); cameraEdits.Add(new RomCameraEdit(sceneId, cameraIndex, camera)); return camera;
    }

    public RomSceneSettings EditSceneSettings(int sceneId, RomSceneSettings settings)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded."); var scenes = Document.Scenes.ToArray(); scenes[Array.IndexOf(scenes, scene)] = scene with { Settings = settings }; Document = Document with { Scenes = scenes }; sceneSettingsEdits.RemoveAll(e => e.SceneId == sceneId); sceneSettingsEdits.Add(new RomSceneSettingsEdit(sceneId, settings)); return settings;
    }

    public RomAlternateHeader CloneAlternateHeader(int sceneId)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var headers = (scene.AlternateHeaders ?? []).ToList(); int index = headers.Count == 0 ? 1 : headers.Max(h => h.Index) + 1;
        if (index > 32) throw new InvalidDataException("The scene already has the maximum supported alternate headers.");
        var header = new RomAlternateHeader(index, 0x02000000, true); headers.Add(header); ReplaceScene(scene with { AlternateHeaders = headers });
        alternateHeaderEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == index); alternateHeaderEdits.Add(new RomAlternateHeaderEdit(sceneId, index, 0x02000000)); return header;
    }

    public void DeleteAlternateHeader(int sceneId, int headerIndex)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var headers = (scene.AlternateHeaders ?? []).ToList(); int position = headers.FindIndex(h => h.Index == headerIndex); if (position < 0) throw new InvalidDataException("Alternate header is not loaded.");
        headers.RemoveAt(position); ReplaceScene(scene with { AlternateHeaders = headers });
        alternateHeaderEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex); alternateSceneSettingsEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex); alternateHeaderEdits.Add(new RomAlternateHeaderEdit(sceneId, headerIndex, 0, true));
    }

    public RomSceneSettings EditAlternateSceneSettings(int sceneId, int headerIndex, RomSceneSettings settings)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var headers = (scene.AlternateHeaders ?? []).ToList(); int position = headers.FindIndex(h => h.Index == headerIndex); if (position < 0) throw new InvalidDataException("Alternate header is not loaded.");
        headers[position] = headers[position] with { HasSettings = true, Settings = settings }; ReplaceScene(scene with { AlternateHeaders = headers });
        alternateSceneSettingsEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex); alternateSceneSettingsEdits.Add(new RomAlternateSceneSettingsEdit(sceneId, headerIndex, settings)); return settings;
    }

    public RomActor EditAlternateRoomActor(int sceneId, int headerIndex, int roomId, int actorIndex, RomActor actor)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded."); var header = (scene.AlternateHeaders ?? []).FirstOrDefault(h => h.Index == headerIndex) ?? throw new InvalidDataException("Alternate header is not loaded."); var rooms = (header.Rooms ?? []).ToArray(); if (roomId < 0 || roomId >= rooms.Length) throw new InvalidDataException("Alternate room is not loaded."); var actors = rooms[roomId].Actors.ToArray(); if (actorIndex < 0 || actorIndex >= actors.Length) throw new InvalidDataException("Alternate actor is not loaded."); actors[actorIndex] = actor; rooms[roomId] = rooms[roomId] with { Actors = actors }; var headers = (scene.AlternateHeaders ?? []).ToArray(); headers[Array.FindIndex(headers, h => h.Index == headerIndex)] = header with { Rooms = rooms }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateActorEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.RoomId == roomId && e.ActorIndex == actorIndex); alternateActorEdits.Add(new RomAlternateActorEdit(sceneId, headerIndex, roomId, actorIndex, actor)); return actor;
    }

    public IReadOnlyList<RomActor> EditAlternateRoomActors(int sceneId, int headerIndex, int roomId, IReadOnlyList<RomActor> actors)
    {
        if (actors is null || actors.Count > byte.MaxValue) throw new InvalidDataException("Alternate actor list count is outside the native range.");
        var room = GetAlternateRoom(sceneId, headerIndex, roomId); var updated = actors.ToArray(); ReplaceAlternateRoom(sceneId, headerIndex, roomId, room with { Actors = updated });
        alternateActorListEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.RoomId == roomId); alternateActorListEdits.Add(new RomAlternateActorListEdit(sceneId, headerIndex, roomId, updated));
        alternateActorEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.RoomId == roomId); return updated;
    }

    public RomRoom CloneAlternateRoom(int sceneId, int headerIndex, int roomId)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var header = (scene.AlternateHeaders ?? []).FirstOrDefault(h => h.Index == headerIndex) ?? throw new InvalidDataException("Alternate header is not loaded.");
        var rooms = (header.Rooms ?? []).ToList(); if (roomId < 0 || roomId >= rooms.Count) throw new InvalidDataException("Alternate room is not loaded.");
        var clone = rooms[roomId] with { Id = rooms.Count, Start = 0, End = 0, Diagnostics = [.. rooms[roomId].Diagnostics, "Cloned in Android alternate-header workspace; native allocation will occur on export."] };
        rooms.Add(clone); var headers = scene.AlternateHeaders.ToArray(); headers[Array.IndexOf(headers, header)] = header with { Rooms = rooms.ToArray() }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateRoomTopologyEdits.Add(new RomAlternateRoomTopologyEdit(sceneId, headerIndex, clone.Id, roomId)); return clone;
    }

    public void DeleteAlternateRoom(int sceneId, int headerIndex, int roomId)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var header = (scene.AlternateHeaders ?? []).FirstOrDefault(h => h.Index == headerIndex) ?? throw new InvalidDataException("Alternate header is not loaded.");
        var rooms = (header.Rooms ?? []).ToList(); if (rooms.Count <= 1) throw new InvalidDataException("An alternate header must retain at least one room."); if (roomId < 0 || roomId >= rooms.Count) throw new InvalidDataException("Alternate room is not loaded.");
        rooms.RemoveAt(roomId); for (int index = roomId; index < rooms.Count; index++) rooms[index] = rooms[index] with { Id = index }; var headers = scene.AlternateHeaders.ToArray(); headers[Array.IndexOf(headers, header)] = header with { Rooms = rooms.ToArray() }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateRoomTopologyEdits.Add(new RomAlternateRoomTopologyEdit(sceneId, headerIndex, roomId, Delete: true));
    }

    public ushort EditAlternateRoomObject(int sceneId, int headerIndex, int roomId, int objectIndex, ushort objectId) { var room = GetAlternateRoom(sceneId, headerIndex, roomId); var objects = (room.Objects ?? []).ToArray(); if (objectIndex < 0 || objectIndex >= objects.Length) throw new InvalidDataException("Alternate object is not loaded."); objects[objectIndex] = objectId; ReplaceAlternateRoom(sceneId, headerIndex, roomId, room with { Objects = objects }); alternateObjectEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.RoomId == roomId && e.ObjectIndex == objectIndex); alternateObjectEdits.Add(new RomAlternateObjectEdit(sceneId, headerIndex, roomId, objectIndex, objectId)); return objectId; }
    public IReadOnlyList<ushort> EditAlternateRoomObjects(int sceneId, int headerIndex, int roomId, IReadOnlyList<ushort> objectIds) { if (objectIds is null || objectIds.Count > byte.MaxValue) throw new InvalidDataException("Alternate object list count is outside the native range."); var room = GetAlternateRoom(sceneId, headerIndex, roomId); var updated = objectIds.ToArray(); ReplaceAlternateRoom(sceneId, headerIndex, roomId, room with { Objects = updated, ObjectCount = updated.Length }); alternateObjectListEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.RoomId == roomId); alternateObjectListEdits.Add(new RomAlternateObjectListEdit(sceneId, headerIndex, roomId, updated)); alternateObjectEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.RoomId == roomId); return updated; }
    public RomRoomSettings EditAlternateRoomSettings(int sceneId, int headerIndex, int roomId, RomRoomSettings settings) { GetAlternateRoom(sceneId, headerIndex, roomId); ReplaceAlternateRoom(sceneId, headerIndex, roomId, GetAlternateRoom(sceneId, headerIndex, roomId) with { Settings = settings }); alternateRoomSettingsEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.RoomId == roomId); alternateRoomSettingsEdits.Add(new RomAlternateRoomSettingsEdit(sceneId, headerIndex, roomId, settings)); return settings; }
    public ushort EditAlternateRoomExit(int sceneId, int headerIndex, int roomId, int exitIndex, ushort raw) { var room = GetAlternateRoom(sceneId, headerIndex, roomId); var exits = (room.Exits ?? []).ToArray(); if (exitIndex < 0 || exitIndex >= exits.Length) throw new InvalidDataException("Alternate room exit is not loaded."); exits[exitIndex] = raw; ReplaceAlternateRoom(sceneId, headerIndex, roomId, room with { Exits = exits }); alternateRoomExitEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.RoomId == roomId && e.ExitIndex == exitIndex); alternateRoomExitEdits.Add(new RomAlternateRoomExitEdit(sceneId, headerIndex, roomId, exitIndex, raw)); return raw; }
    public RomGeometryVertex EditAlternateGeometryVertex(int sceneId, int headerIndex, int roomId, int vertexIndex, RomGeometryVertex vertex) { var room = GetAlternateRoom(sceneId, headerIndex, roomId); var geometry = room.Geometry ?? throw new InvalidDataException("Alternate room geometry is not loaded."); if (vertexIndex < 0 || vertexIndex >= geometry.Vertices.Count) throw new InvalidDataException("Alternate geometry vertex is not loaded."); var vertices = geometry.Vertices.ToArray(); vertices[vertexIndex] = vertex; ReplaceAlternateRoom(sceneId, headerIndex, roomId, room with { Geometry = geometry with { Vertices = vertices } }); int source = geometry.VertexOffsets is { Count: > 0 } && vertexIndex < geometry.VertexOffsets.Count ? geometry.VertexOffsets[vertexIndex] : -1; if (source < 0) throw new InvalidDataException("Alternate geometry vertex has no source offset."); alternateGeometryVertexEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.RoomId == roomId && e.VertexIndex == vertexIndex); alternateGeometryVertexEdits.Add(new RomAlternateGeometryVertexEdit(sceneId, headerIndex, roomId, vertexIndex, source, vertex)); return vertex; }
    public RomGeometryTriangle EditAlternateGeometryTriangle(int sceneId, int headerIndex, int roomId, int triangleIndex, RomGeometryTriangle triangle) { var room = GetAlternateRoom(sceneId, headerIndex, roomId); var geometry = room.Geometry ?? throw new InvalidDataException("Alternate room geometry is not loaded."); if (triangleIndex < 0 || triangleIndex >= geometry.Triangles.Count) throw new InvalidDataException("Alternate geometry triangle is not loaded."); var triangles = geometry.Triangles.ToArray(); triangles[triangleIndex] = triangle; ReplaceAlternateRoom(sceneId, headerIndex, roomId, room with { Geometry = geometry with { Triangles = triangles } }); int source = geometry.TriangleOffsets is { Count: > 0 } && triangleIndex < geometry.TriangleOffsets.Count ? geometry.TriangleOffsets[triangleIndex] : -1; uint prefix = geometry.TrianglePrefixes is { Count: > 0 } && triangleIndex < geometry.TrianglePrefixes.Count ? geometry.TrianglePrefixes[triangleIndex] : 0; byte slotA = geometry.VertexSlots is { Count: > 0 } && triangle.A < geometry.VertexSlots.Count ? (byte)geometry.VertexSlots[triangle.A] : (byte)0; byte slotB = geometry.VertexSlots is { Count: > 0 } && triangle.B < geometry.VertexSlots.Count ? (byte)geometry.VertexSlots[triangle.B] : (byte)0; byte slotC = geometry.VertexSlots is { Count: > 0 } && triangle.C < geometry.VertexSlots.Count ? (byte)geometry.VertexSlots[triangle.C] : (byte)0; if (source < 0) throw new InvalidDataException("Alternate geometry triangle has no source offset."); alternateGeometryTriangleEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.RoomId == roomId && e.TriangleIndex == triangleIndex); alternateGeometryTriangleEdits.Add(new RomAlternateGeometryTriangleEdit(sceneId, headerIndex, roomId, triangleIndex, source, prefix, slotA, slotB, slotC, triangle)); return triangle; }
    public RomCollisionData ReplaceAlternateCollisionTopology(int sceneId, int headerIndex, IReadOnlyList<RomCollisionVertex> vertices, IReadOnlyList<RomCollisionTriangle> triangles, IReadOnlyList<ulong> surfaceTypes)
    {
        var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded.");
        var header = (scene.AlternateHeaders ?? []).FirstOrDefault(h => h.Index == headerIndex) ?? throw new InvalidDataException("Alternate header is not loaded.");
        var collision = header.Collision ?? throw new InvalidDataException("Alternate collision is not loaded.");
        if (vertices is null || vertices.Count == 0 || vertices.Count > ushort.MaxValue) throw new InvalidDataException("Alternate collision vertex count is outside the native range.");
        if (triangles is null || triangles.Count > ushort.MaxValue) throw new InvalidDataException("Alternate collision triangle count is outside the native range.");
        if (surfaceTypes is null || surfaceTypes.Count != triangles.Count) throw new InvalidDataException("Alternate collision surface count must match the triangle count.");
        if (triangles.Any(t => t.A >= vertices.Count || t.B >= vertices.Count || t.C >= vertices.Count)) throw new InvalidDataException("Alternate collision triangle references a vertex outside the replacement topology.");
        var updated = collision with { Vertices = vertices.ToArray(), Triangles = triangles.ToArray(), SurfaceTypes = surfaceTypes.ToArray() };
        var headers = scene.AlternateHeaders.ToArray(); headers[Array.IndexOf(headers, header)] = header with { Collision = updated }; ReplaceScene(scene with { AlternateHeaders = headers });
        alternateCollisionVertexEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex); alternateCollisionTriangleEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex); alternateCollisionSurfaceEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex); alternateCollisionTopologyEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex);
        alternateCollisionTopologyEdits.Add(new RomAlternateCollisionTopologyEdit(sceneId, headerIndex, updated.Vertices, updated.Triangles, updated.SurfaceTypes));
        return updated;
    }

    public RomCollisionVertex EditAlternateCollisionVertex(int sceneId, int headerIndex, int vertexIndex, RomCollisionVertex vertex) { var scene = Document.Scenes.First(s => s.Id == sceneId); var header = (scene.AlternateHeaders ?? []).FirstOrDefault(h => h.Index == headerIndex) ?? throw new InvalidDataException("Alternate header is not loaded."); var collision = header.Collision ?? throw new InvalidDataException("Alternate collision is not loaded."); var vertices = collision.Vertices.ToArray(); if (vertexIndex < 0 || vertexIndex >= vertices.Length) throw new InvalidDataException("Alternate collision vertex is not loaded."); vertices[vertexIndex] = vertex; var headers = scene.AlternateHeaders.ToArray(); headers[Array.FindIndex(headers, h => h.Index == headerIndex)] = header with { Collision = collision with { Vertices = vertices } }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateCollisionVertexEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.VertexIndex == vertexIndex); alternateCollisionVertexEdits.Add(new RomAlternateCollisionVertexEdit(sceneId, headerIndex, vertexIndex, vertex)); return vertex; }
    public RomCollisionTriangle EditAlternateCollisionTriangle(int sceneId, int headerIndex, int triangleIndex, RomCollisionTriangle triangle) { var scene = Document.Scenes.First(s => s.Id == sceneId); var header = (scene.AlternateHeaders ?? []).First(h => h.Index == headerIndex); var collision = header.Collision ?? throw new InvalidDataException("Alternate collision is not loaded."); var items = collision.Triangles.ToArray(); if (triangleIndex < 0 || triangleIndex >= items.Length) throw new InvalidDataException("Alternate collision triangle is not loaded."); items[triangleIndex] = triangle; var headers = scene.AlternateHeaders.ToArray(); headers[Array.FindIndex(headers, h => h.Index == headerIndex)] = header with { Collision = collision with { Triangles = items } }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateCollisionTriangleEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.TriangleIndex == triangleIndex); alternateCollisionTriangleEdits.Add(new RomAlternateCollisionTriangleEdit(sceneId, headerIndex, triangleIndex, triangle)); return triangle; }
    public ulong EditAlternateCollisionSurface(int sceneId, int headerIndex, int surfaceIndex, ulong surfaceType) { var scene = Document.Scenes.First(s => s.Id == sceneId); var header = (scene.AlternateHeaders ?? []).First(h => h.Index == headerIndex); var collision = header.Collision ?? throw new InvalidDataException("Alternate collision is not loaded."); var items = collision.SurfaceTypes.ToArray(); if (surfaceIndex < 0 || surfaceIndex >= items.Length) throw new InvalidDataException("Alternate collision surface is not loaded."); items[surfaceIndex] = surfaceType; var headers = scene.AlternateHeaders.ToArray(); headers[Array.FindIndex(headers, h => h.Index == headerIndex)] = header with { Collision = collision with { SurfaceTypes = items } }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateCollisionSurfaceEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.SurfaceIndex == surfaceIndex); alternateCollisionSurfaceEdits.Add(new RomAlternateCollisionSurfaceEdit(sceneId, headerIndex, surfaceIndex, surfaceType)); return surfaceType; }
    public RomCollisionData EditAlternateCollisionBounds(int sceneId, int headerIndex, RomCollisionData bounds) { var scene = Document.Scenes.First(s => s.Id == sceneId); var header = (scene.AlternateHeaders ?? []).First(h => h.Index == headerIndex); var collision = header.Collision ?? throw new InvalidDataException("Alternate collision is not loaded."); var updated = collision with { MinX = bounds.MinX, MinY = bounds.MinY, MinZ = bounds.MinZ, MaxX = bounds.MaxX, MaxY = bounds.MaxY, MaxZ = bounds.MaxZ }; var headers = scene.AlternateHeaders.ToArray(); headers[Array.FindIndex(headers, h => h.Index == headerIndex)] = header with { Collision = updated }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateCollisionBoundsEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex); alternateCollisionBoundsEdits.Add(new RomAlternateCollisionBoundsEdit(sceneId, headerIndex, bounds.MinX, bounds.MinY, bounds.MinZ, bounds.MaxX, bounds.MaxY, bounds.MaxZ)); return updated; }
    public RomHeaderCommand EditAlternateCommand(int sceneId, int headerIndex, int commandIndex, RomHeaderCommand command) { var scene = Document.Scenes.First(s => s.Id == sceneId); var header = (scene.AlternateHeaders ?? []).First(h => h.Index == headerIndex); var commands = (header.Commands ?? []).ToArray(); if (commandIndex < 0 || commandIndex >= commands.Length) throw new InvalidDataException("Alternate header command is not loaded."); commands[commandIndex] = command; var headers = scene.AlternateHeaders.ToArray(); headers[Array.FindIndex(headers, h => h.Index == headerIndex)] = header with { Commands = commands }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateCommandEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.CommandIndex == commandIndex); alternateCommandEdits.Add(new RomAlternateCommandEdit(sceneId, headerIndex, commandIndex, command)); return command; }
    public RomHeaderCommand EditSceneCommand(int sceneId, int commandIndex, RomHeaderCommand command) { var scene = Document.Scenes.First(s => s.Id == sceneId); var commands = (scene.Commands ?? []).ToArray(); if (commandIndex < 0 || commandIndex >= commands.Length) throw new InvalidDataException("Scene command is not loaded."); commands[commandIndex] = command; ReplaceScene(scene with { Commands = commands }); sceneCommandEdits.RemoveAll(e => e.SceneId == sceneId && e.CommandIndex == commandIndex); sceneCommandEdits.Add(new RomSceneCommandEdit(sceneId, commandIndex, command)); return command; }
    public RomHeaderCommand InsertSceneCommand(int sceneId, RomHeaderCommand command) { var scene = Document.Scenes.First(s => s.Id == sceneId); var commands = (scene.Commands ?? []).ToList(); commands.Add(command); ReplaceScene(scene with { Commands = commands }); sceneCommandEdits.RemoveAll(e => e.SceneId == sceneId && e.CommandIndex == commands.Count - 1); sceneCommandEdits.Add(new RomSceneCommandEdit(sceneId, commands.Count - 1, command, Insert: true)); return command; }
    public RomHeaderCommand InsertSceneCommandAt(int sceneId, int commandIndex, RomHeaderCommand command) { var scene = Document.Scenes.First(s => s.Id == sceneId); var commands = (scene.Commands ?? []).ToList(); if (commandIndex < 0 || commandIndex > commands.Count) throw new InvalidDataException("Scene command insertion index is outside the command list."); commands.Insert(commandIndex, command); ReplaceScene(scene with { Commands = commands }); sceneCommandEdits.RemoveAll(e => e.SceneId == sceneId && e.CommandIndex == commandIndex); sceneCommandEdits.Add(new RomSceneCommandEdit(sceneId, commandIndex, command, Insert: true)); return command; }
    public void DeleteSceneCommand(int sceneId, int commandIndex) { var scene = Document.Scenes.First(s => s.Id == sceneId); var commands = (scene.Commands ?? []).ToList(); if (commandIndex < 0 || commandIndex >= commands.Count || commandIndex != commands.Count - 1) throw new InvalidDataException("Only the final scene command can be deleted safely."); var command = commands[commandIndex]; commands.RemoveAt(commandIndex); ReplaceScene(scene with { Commands = commands }); sceneCommandEdits.RemoveAll(e => e.SceneId == sceneId && e.CommandIndex == commandIndex); sceneCommandEdits.Add(new RomSceneCommandEdit(sceneId, commandIndex, command, Delete: true)); }
    public RomHeaderCommand InsertAlternateCommand(int sceneId, int headerIndex, RomHeaderCommand command) { var scene = Document.Scenes.First(s => s.Id == sceneId); var header = (scene.AlternateHeaders ?? []).First(h => h.Index == headerIndex); var commands = (header.Commands ?? []).ToList(); commands.Add(command); var headers = scene.AlternateHeaders.ToArray(); headers[Array.FindIndex(headers, h => h.Index == headerIndex)] = header with { Commands = commands }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateCommandEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.CommandIndex == commands.Count - 1); alternateCommandEdits.Add(new RomAlternateCommandEdit(sceneId, headerIndex, commands.Count - 1, command, Insert: true)); return command; }
    public RomHeaderCommand InsertAlternateCommandAt(int sceneId, int headerIndex, int commandIndex, RomHeaderCommand command) { var scene = Document.Scenes.First(s => s.Id == sceneId); var header = (scene.AlternateHeaders ?? []).First(h => h.Index == headerIndex); var commands = (header.Commands ?? []).ToList(); if (commandIndex < 0 || commandIndex > commands.Count) throw new InvalidDataException("Alternate command insertion index is outside the command list."); commands.Insert(commandIndex, command); var headers = scene.AlternateHeaders.ToArray(); headers[Array.FindIndex(headers, h => h.Index == headerIndex)] = header with { Commands = commands }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateCommandEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.CommandIndex == commandIndex); alternateCommandEdits.Add(new RomAlternateCommandEdit(sceneId, headerIndex, commandIndex, command, Insert: true)); return command; }
    public void DeleteAlternateCommand(int sceneId, int headerIndex, int commandIndex) { var scene = Document.Scenes.First(s => s.Id == sceneId); var header = (scene.AlternateHeaders ?? []).First(h => h.Index == headerIndex); var commands = (header.Commands ?? []).ToList(); if (commandIndex < 0 || commandIndex >= commands.Count || commandIndex != commands.Count - 1) throw new InvalidDataException("Only the final alternate command can be deleted safely."); var command = commands[commandIndex]; commands.RemoveAt(commandIndex); var headers = scene.AlternateHeaders.ToArray(); headers[Array.FindIndex(headers, h => h.Index == headerIndex)] = header with { Commands = commands }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateCommandEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.CommandIndex == commandIndex); alternateCommandEdits.Add(new RomAlternateCommandEdit(sceneId, headerIndex, commandIndex, command, Delete: true)); }
    public RomCamera EditAlternateCamera(int sceneId, int headerIndex, int cameraIndex, RomCamera camera) { var scene = Document.Scenes.First(s => s.Id == sceneId); var header = scene.AlternateHeaders!.First(h => h.Index == headerIndex); var collision = header.Collision ?? throw new InvalidDataException("Alternate collision is not loaded."); var items = (collision.Cameras ?? []).ToArray(); if (cameraIndex < 0 || cameraIndex >= items.Length) throw new InvalidDataException("Alternate camera is not loaded."); items[cameraIndex] = camera; var headers = scene.AlternateHeaders.ToArray(); headers[Array.FindIndex(headers, h => h.Index == headerIndex)] = header with { Collision = collision with { Cameras = items } }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateCameraEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.CameraIndex == cameraIndex); alternateCameraEdits.Add(new RomAlternateCameraEdit(sceneId, headerIndex, cameraIndex, camera)); return camera; }
    public RomWaterbox EditAlternateWaterbox(int sceneId, int headerIndex, int waterboxIndex, RomWaterbox waterbox) { var scene = Document.Scenes.First(s => s.Id == sceneId); var header = scene.AlternateHeaders!.First(h => h.Index == headerIndex); var collision = header.Collision ?? throw new InvalidDataException("Alternate collision is not loaded."); var items = (collision.Waterboxes ?? []).ToArray(); if (waterboxIndex < 0 || waterboxIndex >= items.Length) throw new InvalidDataException("Alternate waterbox is not loaded."); items[waterboxIndex] = waterbox; var headers = scene.AlternateHeaders.ToArray(); headers[Array.FindIndex(headers, h => h.Index == headerIndex)] = header with { Collision = collision with { Waterboxes = items } }; ReplaceScene(scene with { AlternateHeaders = headers }); alternateWaterboxEdits.RemoveAll(e => e.SceneId == sceneId && e.HeaderIndex == headerIndex && e.WaterboxIndex == waterboxIndex); alternateWaterboxEdits.Add(new RomAlternateWaterboxEdit(sceneId, headerIndex, waterboxIndex, waterbox)); return waterbox; }
    private RomRoom GetAlternateRoom(int sceneId, int headerIndex, int roomId) { var scene = Document.Scenes.FirstOrDefault(s => s.Id == sceneId) ?? throw new InvalidDataException("Scene is not loaded."); var header = (scene.AlternateHeaders ?? []).FirstOrDefault(h => h.Index == headerIndex) ?? throw new InvalidDataException("Alternate header is not loaded."); var rooms = header.Rooms ?? throw new InvalidDataException("Alternate rooms are not loaded."); if (roomId < 0 || roomId >= rooms.Count) throw new InvalidDataException("Alternate room is not loaded."); return rooms[roomId]; }
    private void ReplaceAlternateRoom(int sceneId, int headerIndex, int roomId, RomRoom room) { var scene = Document.Scenes.First(s => s.Id == sceneId); var header = scene.AlternateHeaders!.First(h => h.Index == headerIndex); var rooms = header.Rooms!.ToArray(); rooms[roomId] = room; var headers = scene.AlternateHeaders.ToArray(); headers[Array.FindIndex(headers, h => h.Index == headerIndex)] = header with { Rooms = rooms }; ReplaceScene(scene with { AlternateHeaders = headers }); }

    private static RomCollisionTriangle RecalculateCollisionTriangle(IReadOnlyList<RomCollisionVertex> vertices, RomCollisionTriangle triangle)
    {
        if (triangle.A >= vertices.Count || triangle.B >= vertices.Count || triangle.C >= vertices.Count) throw new InvalidDataException("Collision triangle references a vertex outside the loaded topology.");
        var a = vertices[triangle.A]; var b = vertices[triangle.B]; var c = vertices[triangle.C];
        long abX = b.X - a.X, abY = b.Y - a.Y, abZ = b.Z - a.Z;
        long acX = c.X - a.X, acY = c.Y - a.Y, acZ = c.Z - a.Z;
        long normalX = abY * acZ - abZ * acY, normalY = abZ * acX - abX * acZ, normalZ = abX * acY - abY * acX;
        double length = Math.Sqrt((double)normalX * normalX + (double)normalY * normalY + (double)normalZ * normalZ);
        if (length < 0.0001) throw new InvalidDataException("Cannot recalculate a degenerate collision triangle.");
        short nx = (short)Math.Clamp((int)Math.Round(normalX / length * 32767.0), short.MinValue, short.MaxValue);
        short ny = (short)Math.Clamp((int)Math.Round(normalY / length * 32767.0), short.MinValue, short.MaxValue);
        short nz = (short)Math.Clamp((int)Math.Round(normalZ / length * 32767.0), short.MinValue, short.MaxValue);
        short distance = (short)Math.Clamp((int)Math.Round(-(nx * (double)a.X + ny * (double)a.Y + nz * (double)a.Z) / 32767.0), short.MinValue, short.MaxValue);
        return triangle with { NormalX = nx, NormalY = ny, NormalZ = nz, Distance = distance };
    }

    private void ReplaceScene(RomScene scene) { var scenes = Document.Scenes.ToArray(); scenes[Array.FindIndex(scenes, s => s.Id == scene.Id)] = scene; Document = Document with { Scenes = scenes }; }

    public string WritePatch() => JsonSerializer.Serialize(new RomWorkspacePatch(Document.Fingerprint, edits, entranceEdits, pathEdits, exitEdits, waterboxEdits, Environments: environmentEdits, CollisionVertices: collisionVertexEdits, CollisionTriangles: collisionTriangleEdits, CollisionSurfaces: collisionSurfaceEdits, CollisionBounds: collisionBoundsEdits, GeometryVertices: geometryVertexEdits, GeometryTriangles: geometryTriangleEdits, Spawns: spawnEdits, Transitions: transitionEdits, Objects: objectEdits, ObjectLists: objectListEdits, ActorLists: actorListEdits, RoomOrders: roomOrderEdits, RoomSettings: roomSettingsEdits, RoomExits: roomExitEdits, Cameras: cameraEdits, SceneSettings: sceneSettingsEdits, SceneCommands: sceneCommandEdits, AlternateHeaders: alternateHeaderEdits, AlternateCommands: alternateCommandEdits, AlternateSceneSettings: alternateSceneSettingsEdits, AlternateActors: alternateActorEdits, AlternateActorLists: alternateActorListEdits, AlternateObjects: alternateObjectEdits, AlternateObjectLists: alternateObjectListEdits, AlternateRoomSettings: alternateRoomSettingsEdits, AlternateRoomExits: alternateRoomExitEdits, AlternateGeometryVertices: alternateGeometryVertexEdits, AlternateGeometryTriangles: alternateGeometryTriangleEdits, AlternateCollisionVertices: alternateCollisionVertexEdits, AlternateCollisionTriangles: alternateCollisionTriangleEdits, AlternateCollisionSurfaces: alternateCollisionSurfaceEdits, AlternateCollisionBounds: alternateCollisionBoundsEdits, AlternateCameras: alternateCameraEdits, AlternateWaterboxes: alternateWaterboxEdits, GeometryRebuilds: geometryRebuildEdits, CollisionTopologies: collisionTopologyEdits, AlternateCollisionTopologies: alternateCollisionTopologyEdits, PathLists: pathListEdits, GeometryCommands: geometryCommandEdits, Textures: textureEdits, SceneTopology: sceneTopologyEdits, RoomTopology: roomTopologyEdits, AlternateRoomTopology: alternateRoomTopologyEdits, AlternateRoomOrders: alternateRoomOrderEdits), new JsonSerializerOptions { WriteIndented = true });
    public byte[] ApplyToRom(byte[] original) => RomBinaryPatcher.Apply(original, Document, new RomWorkspacePatch(Document.Fingerprint, edits, entranceEdits, pathEdits, exitEdits, waterboxEdits, environmentEdits, collisionVertexEdits, collisionTriangleEdits, collisionSurfaceEdits, collisionBoundsEdits, geometryVertexEdits, geometryTriangleEdits, spawnEdits, transitionEdits, objectEdits, ObjectLists: objectListEdits, ActorLists: actorListEdits, RoomOrders: roomOrderEdits, RoomSettings: roomSettingsEdits, RoomExits: roomExitEdits, Cameras: cameraEdits, SceneSettings: sceneSettingsEdits, SceneCommands: sceneCommandEdits, AlternateHeaders: alternateHeaderEdits, AlternateCommands: alternateCommandEdits, AlternateSceneSettings: alternateSceneSettingsEdits, AlternateActors: alternateActorEdits, AlternateActorLists: alternateActorListEdits, AlternateObjects: alternateObjectEdits, AlternateObjectLists: alternateObjectListEdits, AlternateRoomSettings: alternateRoomSettingsEdits, AlternateRoomExits: alternateRoomExitEdits, AlternateGeometryVertices: alternateGeometryVertexEdits, AlternateGeometryTriangles: alternateGeometryTriangleEdits, AlternateCollisionVertices: alternateCollisionVertexEdits, AlternateCollisionTriangles: alternateCollisionTriangleEdits, AlternateCollisionSurfaces: alternateCollisionSurfaceEdits, AlternateCollisionBounds: alternateCollisionBoundsEdits, AlternateCameras: alternateCameraEdits, AlternateWaterboxes: alternateWaterboxEdits, GeometryRebuilds: geometryRebuildEdits, CollisionTopologies: collisionTopologyEdits, AlternateCollisionTopologies: alternateCollisionTopologyEdits, PathLists: pathListEdits, GeometryCommands: geometryCommandEdits, Textures: textureEdits, SceneTopology: sceneTopologyEdits, RoomTopology: roomTopologyEdits, AlternateRoomTopology: alternateRoomTopologyEdits, AlternateRoomOrders: alternateRoomOrderEdits));
    public void ClearEdits() { edits.Clear(); entranceEdits.Clear(); pathEdits.Clear(); pathListEdits.Clear(); exitEdits.Clear(); waterboxEdits.Clear(); environmentEdits.Clear(); collisionVertexEdits.Clear(); collisionTriangleEdits.Clear(); collisionSurfaceEdits.Clear(); collisionBoundsEdits.Clear(); collisionTopologyEdits.Clear(); geometryVertexEdits.Clear(); geometryTriangleEdits.Clear(); geometryCommandEdits.Clear(); geometryRebuildEdits.Clear(); spawnEdits.Clear(); transitionEdits.Clear(); objectEdits.Clear(); objectListEdits.Clear(); actorListEdits.Clear(); roomOrderEdits.Clear(); sceneTopologyEdits.Clear(); roomTopologyEdits.Clear(); alternateRoomOrderEdits.Clear(); roomSettingsEdits.Clear(); roomExitEdits.Clear(); cameraEdits.Clear(); sceneSettingsEdits.Clear(); sceneCommandEdits.Clear(); alternateHeaderEdits.Clear(); alternateCommandEdits.Clear(); alternateSceneSettingsEdits.Clear(); alternateActorEdits.Clear(); alternateActorListEdits.Clear(); alternateObjectEdits.Clear(); alternateObjectListEdits.Clear(); alternateRoomSettingsEdits.Clear(); alternateRoomExitEdits.Clear(); alternateGeometryVertexEdits.Clear(); alternateGeometryTriangleEdits.Clear(); alternateCollisionVertexEdits.Clear(); alternateCollisionTriangleEdits.Clear(); alternateCollisionSurfaceEdits.Clear(); alternateCollisionBoundsEdits.Clear(); alternateCollisionTopologyEdits.Clear(); alternateCameraEdits.Clear(); alternateWaterboxEdits.Clear(); alternateRoomTopologyEdits.Clear(); }
}
