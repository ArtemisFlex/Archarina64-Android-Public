namespace Archarina64.Core;

public sealed record RomTextureCommandInfo(
    int SourceOffset,
    byte Operation,
    string Kind,
    int Format,
    int Size,
    int Width,
    byte Segment,
    int Address,
    int Uls,
    int Ult,
    int Lrs,
    int Lrt,
    int Dxt)
{
    public string FormatName => Format switch { 0 => "RGBA", 1 => "YUV", 2 => "CI", 3 => "IA", 4 => "I", _ => $"FMT{Format}" };
    public string SizeName => Size switch { 0 => "4b", 1 => "8b", 2 => "16b", 3 => "32b", _ => $"SIZE{Size}" };
}

public static class RomTextureCommandDecoder
{
    public static bool TryDecode(RomDisplayListCommand command, out RomTextureCommandInfo info)
    {
        byte op = command.Operation; uint w0 = command.Word0, w1 = command.Word1;
        if (op == 0xFD)
        {
            info = new RomTextureCommandInfo(command.SourceOffset, op, "G_SETTIMG", (int)((w0 >> 21) & 7), (int)((w0 >> 19) & 3), (int)(w0 & 0xFFF) + 1, (byte)(w1 >> 24), (int)(w1 & 0x00FFFFFF), 0, 0, 0, 0, 0);
            return true;
        }
        if (op == 0xF5)
        {
            info = new RomTextureCommandInfo(command.SourceOffset, op, "G_SETTILE", (int)((w0 >> 21) & 7), (int)((w0 >> 19) & 3), 0, 0, 0, (int)((w0 >> 12) & 0xFFF), (int)(w0 & 0xFFF), (int)((w1 >> 12) & 0xFFF), (int)(w1 & 0xFFF), 0);
            return true;
        }
        if (op is 0xF2 or 0xF3 or 0xF4)
        {
            info = new RomTextureCommandInfo(command.SourceOffset, op, op == 0xF2 ? "G_SETTILESIZE" : op == 0xF3 ? "G_LOADBLOCK" : "G_LOADTILE", 0, 0, 0, 0, 0, (int)((w0 >> 12) & 0xFFF), (int)(w0 & 0xFFF), (int)((w1 >> 12) & 0xFFF), (int)(w1 & 0xFFF), op == 0xF3 ? (int)(w1 & 0xFFF) : 0);
            return true;
        }
        if (op == 0xF0)
        {
            info = new RomTextureCommandInfo(command.SourceOffset, op, "G_LOADTLUT", 0, 2, 0, 0, 0, 0, 0, 0, 0, (int)((w1 >> 14) & 0x3FF));
            return true;
        }
        info = null!;
        return false;
    }
}
