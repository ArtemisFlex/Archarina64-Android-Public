namespace Archarina64.Core;

/// <summary>Decoded OoT collision surface flags. The raw value remains available for unknown or game-specific bits.</summary>
public sealed record RomCollisionSurfaceType(
    ulong Raw,
    byte BgCameraIndex,
    byte ExitIndex,
    byte FloorType,
    byte Unknown18,
    byte WallType,
    byte FloorProperty,
    bool IsSoft,
    bool IsHorseBlocked,
    byte Material,
    byte FloorEffect,
    byte LightSetting,
    byte Echo,
    bool CanHookshot,
    byte ConveyorSpeed,
    byte ConveyorDirection,
    bool Unknown27)
{
    public static RomCollisionSurfaceType Decode(ulong raw)
    {
        uint type0 = (uint)(raw >> 32), type1 = (uint)raw;
        return new RomCollisionSurfaceType(raw,
            (byte)(type0 & 0xFF), (byte)((type0 >> 8) & 0x1F), (byte)((type0 >> 13) & 0x1F),
            (byte)((type0 >> 18) & 0x07), (byte)((type0 >> 21) & 0x1F), (byte)((type0 >> 26) & 0x0F),
            (type0 & (1u << 30)) != 0, (type0 & (1u << 31)) != 0,
            (byte)(type1 & 0x0F), (byte)((type1 >> 4) & 0x03), (byte)((type1 >> 6) & 0x1F),
            (byte)((type1 >> 11) & 0x3F), (type1 & (1u << 17)) != 0, (byte)((type1 >> 18) & 0x07),
            (byte)((type1 >> 21) & 0x3F), (type1 & (1u << 27)) != 0);
    }

    public ulong Encode()
    {
        uint type0 = (uint)(BgCameraIndex & 0xFF) | (uint)(ExitIndex & 0x1F) << 8 | (uint)(FloorType & 0x1F) << 13 |
            (uint)(Unknown18 & 0x07) << 18 | (uint)(WallType & 0x1F) << 21 | (uint)(FloorProperty & 0x0F) << 26 |
            (IsSoft ? 1u : 0u) << 30 | (IsHorseBlocked ? 1u : 0u) << 31;
        uint type1 = (uint)Raw & 0xF0000000u | (uint)(Material & 0x0F) | (uint)(FloorEffect & 0x03) << 4 | (uint)(LightSetting & 0x1F) << 6 |
            (uint)(Echo & 0x3F) << 11 | (CanHookshot ? 1u : 0u) << 17 | (uint)(ConveyorSpeed & 0x07) << 18 |
            (uint)(ConveyorDirection & 0x3F) << 21 | (Unknown27 ? 1u : 0u) << 27;
        return (ulong)type0 << 32 | type1;
    }

    public RomCollisionSurfaceType WithRaw(ulong raw) => Decode(raw);
}
