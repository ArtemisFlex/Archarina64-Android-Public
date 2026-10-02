using System.Numerics;

namespace Archarina64.Core;

public sealed record RomProjectedActor(float ScreenX, float ScreenY, float Depth, bool Visible);

/// <summary>Small camera-space projection helper for native actor layout previews.</summary>
public static class RomCameraProjection
{
    public static RomProjectedActor Project(RomCamera camera, RomActor actor, float aspect = 1f) => Project(camera, new Vector3(actor.X, actor.Y, actor.Z), aspect);

    public static RomProjectedActor Project(RomCamera camera, RomCollisionVertex vertex, float aspect = 1f) => Project(camera, new Vector3(vertex.X, vertex.Y, vertex.Z), aspect);

    public static RomProjectedActor Project(RomCamera camera, Vector3 world, float aspect = 1f)
    {
        var view = WorldToCamera(camera, world);
        float depth = -view.Z; float fov = FieldOfView(camera); float scale = MathF.Tan(fov * MathF.PI / 360f);
        if (depth <= 0.001f) return new RomProjectedActor(0, 0, depth, false);
        float screenX = view.X / (depth * scale * MathF.Max(aspect, 0.01f)); float screenY = view.Y / (depth * scale);
        return new RomProjectedActor(screenX, screenY, depth, MathF.Abs(screenX) <= 1f && MathF.Abs(screenY) <= 1f);
    }

    public static Vector3 UnprojectAtDepth(RomCamera camera, float screenX, float screenY, float depth, float aspect = 1f)
    {
        float scale = MathF.Tan(FieldOfView(camera) * MathF.PI / 360f); var view = new Vector3(screenX * depth * scale * MathF.Max(aspect, 0.01f), screenY * depth * scale, -depth);
        return CameraToWorld(camera, view);
    }

    private static float FieldOfView(RomCamera camera) => Math.Clamp(camera.Fov <= 0 ? 45f : camera.Fov, 10f, 170f);

    private static Vector3 WorldToCamera(RomCamera camera, Vector3 world)
    {
        var value = world - new Vector3(camera.X, camera.Y, camera.Z); value = RotateZ(value, -Angle(camera.RotationZ)); value = RotateX(value, -Angle(camera.RotationX)); return RotateY(value, -Angle(camera.RotationY));
    }

    private static Vector3 CameraToWorld(RomCamera camera, Vector3 view)
    {
        var value = RotateY(view, Angle(camera.RotationY)); value = RotateX(value, Angle(camera.RotationX)); value = RotateZ(value, Angle(camera.RotationZ)); return value + new Vector3(camera.X, camera.Y, camera.Z);
    }

    private static float Angle(short value) => value * (2f * MathF.PI / 65536f);
    private static Vector3 RotateX(Vector3 value, float angle) => new(value.X, value.Y * MathF.Cos(angle) - value.Z * MathF.Sin(angle), value.Y * MathF.Sin(angle) + value.Z * MathF.Cos(angle));
    private static Vector3 RotateY(Vector3 value, float angle) => new(value.X * MathF.Cos(angle) + value.Z * MathF.Sin(angle), value.Y, -value.X * MathF.Sin(angle) + value.Z * MathF.Cos(angle));
    private static Vector3 RotateZ(Vector3 value, float angle) => new(value.X * MathF.Cos(angle) - value.Y * MathF.Sin(angle), value.X * MathF.Sin(angle) + value.Y * MathF.Cos(angle), value.Z);
}
