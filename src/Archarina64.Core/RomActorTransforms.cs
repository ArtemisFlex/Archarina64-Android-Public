using System.Numerics;

namespace Archarina64.Core;

public static class RomActorTransforms
{
    public static IReadOnlyList<RomActor> Apply(IReadOnlyList<RomActor> actors, IReadOnlyCollection<int> indexes, int moveX = 0, int moveY = 0, int moveZ = 0, int rotateX = 0, int rotateY = 0, int rotateZ = 0, float scale = 1f)
    {
        if (actors is null) throw new ArgumentNullException(nameof(actors));
        if (indexes is null || indexes.Count == 0) throw new InvalidDataException("Actor selection cannot be empty.");
        if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0) throw new InvalidDataException("Actor scale must be a finite value greater than zero.");
        int[] selected = indexes.Distinct().ToArray();
        if (selected.Any(index => index < 0 || index >= actors.Count)) throw new InvalidDataException("Actor selection contains an index outside this room.");

        double centerX = selected.Average(index => actors[index].X), centerY = selected.Average(index => actors[index].Y), centerZ = selected.Average(index => actors[index].Z);
        Vector3 radians = new(rotateX * (MathF.Tau / 65536f), rotateY * (MathF.Tau / 65536f), rotateZ * (MathF.Tau / 65536f));
        var result = actors.ToArray();
        foreach (int index in selected)
        {
            var actor = actors[index];
            Vector3 offset = new((float)((actor.X - centerX) * scale), (float)((actor.Y - centerY) * scale), (float)((actor.Z - centerZ) * scale));
            offset = RotateX(offset, radians.X); offset = RotateY(offset, radians.Y); offset = RotateZ(offset, radians.Z);
            int x = ClampCoordinate((int)Math.Round(centerX + offset.X + moveX));
            int y = ClampCoordinate((int)Math.Round(centerY + offset.Y + moveY));
            int z = ClampCoordinate((int)Math.Round(centerZ + offset.Z + moveZ));
            result[index] = actor with { X = (short)x, Y = (short)y, Z = (short)z, RotationX = AddBinang(actor.RotationX, rotateX), RotationY = AddBinang(actor.RotationY, rotateY), RotationZ = AddBinang(actor.RotationZ, rotateZ) };
        }
        return result;
    }

    public static int BinangFromDegrees(float degrees)
    {
        if (float.IsNaN(degrees) || float.IsInfinity(degrees)) throw new InvalidDataException("Rotation must be finite.");
        return (int)MathF.Round(degrees * (65536f / 360f));
    }

    private static int ClampCoordinate(int value) => Math.Clamp(value, short.MinValue, short.MaxValue);
    private static short AddBinang(short current, int delta) => unchecked((short)(current + delta));
    private static Vector3 RotateX(Vector3 value, float angle) => new(value.X, value.Y * MathF.Cos(angle) - value.Z * MathF.Sin(angle), value.Y * MathF.Sin(angle) + value.Z * MathF.Cos(angle));
    private static Vector3 RotateY(Vector3 value, float angle) => new(value.X * MathF.Cos(angle) + value.Z * MathF.Sin(angle), value.Y, -value.X * MathF.Sin(angle) + value.Z * MathF.Cos(angle));
    private static Vector3 RotateZ(Vector3 value, float angle) => new(value.X * MathF.Cos(angle) - value.Y * MathF.Sin(angle), value.X * MathF.Sin(angle) + value.Y * MathF.Cos(angle), value.Z);
}
