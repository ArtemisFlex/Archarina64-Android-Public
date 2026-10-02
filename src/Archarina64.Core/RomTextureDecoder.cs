namespace Archarina64.Core;

public sealed record RomTextureAsset(
    int SourceOffset,
    string Format,
    string Size,
    int Width,
    int Height,
    byte Segment,
    int Address,
    byte[] Rgba,
    string Error = null,
    byte[] PaletteRgba = null,
    int PaletteBase = 0,
    int SourceWidth = 0,
    int SourceHeight = 0)
{
    public bool IsDecoded => Rgba is { Length: > 0 } && string.IsNullOrWhiteSpace(Error);
}

/// <summary>Bounded native OoT texture extraction for the formats used by F3DEX display lists.</summary>
public static class RomTextureDecoder
{
    private const int MaxPixels = 1_048_576;

    public static IReadOnlyList<RomTextureAsset> ExtractRoomTextures(byte[] rom, RomRoom room)
    {
        if (rom is null || room is null || room.Geometry?.DisplayListCommands is not { } commands) return [];
        if (room.Start >= room.End || room.End > rom.Length) return [];
        byte[] decodedRoom = N64Compression.Decode(rom.AsSpan(checked((int)room.Start), checked((int)(room.End - room.Start))));
        var assets = new List<RomTextureAsset>(); var paletteImage = (RomTextureCommandInfo)null; byte[] palette = null;
        for (int index = 0; index < commands.Count; index++)
        {
            if (!RomTextureCommandDecoder.TryDecode(commands[index], out var decoded)) continue;
            if (decoded.Kind == "G_SETTIMG")
            {
                if (decoded.FormatName == "RGBA" && decoded.SizeName == "16b" && decoded.Width == 1) paletteImage = decoded;
                if (decoded.FormatName == "CI")
                {
                    var size = FindTextureSize(commands, index + 1, decoded.Width); int paletteBase = FindPaletteBase(commands, index + 1, decoded.Size);
                    assets.Add(DecodeAsset(decodedRoom, decoded, size.Width, size.Height, palette, paletteBase));
                }
                else if (!(decoded.FormatName == "RGBA" && decoded.SizeName == "16b" && decoded.Width == 1))
                {
                    var size = FindTextureSize(commands, index + 1, decoded.Width); assets.Add(DecodeAsset(decodedRoom, decoded, size.Width, size.Height, null, 0));
                }
            }
            else if (decoded.Kind == "G_LOADTLUT" && paletteImage is not null && paletteImage.Segment == 3)
            {
                int count = Math.Clamp(decoded.Dxt + 1, 1, 256); int bytes = checked(count * 2);
                if (paletteImage.Address >= 0 && paletteImage.Address <= decodedRoom.Length - bytes)
                {
                    palette = DecodePixels(decodedRoom.AsSpan(paletteImage.Address, bytes), "RGBA", "16b", count, 1);
                }
            }
        }
        return assets;
    }

    private static int FindPaletteBase(IReadOnlyList<RomDisplayListCommand> commands, int startIndex, int size)
    {
        if (size != 0) return 0;
        for (int index = Math.Max(0, startIndex); index < commands.Count; index++)
        {
            if (commands[index].Operation == 0xFD) break;
            if (commands[index].Operation == 0xF5) return (int)((commands[index].Word1 >> 20) & 0xF) * 16;
        }
        return 0;
    }

    public static (int Width, int Height) FindTextureSize(IReadOnlyList<RomDisplayListCommand> commands, int startIndex, int fallbackWidth)
    {
        for (int index = Math.Max(0, startIndex); index < commands.Count; index++)
        {
            if (commands[index].Operation == 0xFD) break;
            if (!RomTextureCommandDecoder.TryDecode(commands[index], out var tile) || tile.Kind != "G_SETTILESIZE") continue;
            int width = Math.Max(1, (tile.Lrs - tile.Uls) / 4 + 1);
            int height = Math.Max(1, (tile.Lrt - tile.Ult) / 4 + 1);
            return (width, height);
        }
        return (Math.Max(1, fallbackWidth), 0);
    }

    public static byte[] DecodePixels(ReadOnlySpan<byte> source, string format, string size, int width, int height)
        => DecodePixels(source, format, size, width, height, null, 0);

    public static byte[] DecodePixels(ReadOnlySpan<byte> source, string format, string size, int width, int height, byte[] paletteRgba, int paletteBase = 0)
    {
        if (width <= 0 || height <= 0 || (long)width * height > MaxPixels) throw new InvalidDataException("Texture dimensions exceed the supported native preview limit.");
        int bits = size switch { "4b" => 4, "8b" => 8, "16b" => 16, "32b" => 32, _ => throw new InvalidDataException($"Unsupported native texture size {size}.") };
        int sourceBytes = checked((width * height * bits + 7) / 8);
        if (source.Length < sourceBytes) throw new InvalidDataException("Native texture data is outside the room bounds.");
        if (format == "YUV" && bits == 16) { if ((width & 1) != 0) throw new InvalidDataException("Native YUV16 textures require an even width."); return DecodeYuv(source[..sourceBytes], width, height); }
        byte[] rgba = new byte[checked(width * height * 4)];
        for (int pixel = 0; pixel < width * height; pixel++)
        {
            int value = bits switch
            {
                4 => (source[pixel / 2] >> ((pixel & 1) == 0 ? 4 : 0)) & 0xF,
                8 => source[pixel],
                16 => (source[pixel * 2] << 8) | source[pixel * 2 + 1],
                _ => 0
            };
            var color = DecodePixel(value, source, pixel, format, bits, paletteRgba, paletteBase);
            int output = pixel * 4; rgba[output] = color.R; rgba[output + 1] = color.G; rgba[output + 2] = color.B; rgba[output + 3] = color.A;
        }
        return rgba;
    }

    public static byte[] EncodePixels(ReadOnlySpan<byte> rgba, string format, string size, int width, int height)
        => EncodePixels(rgba, format, size, width, height, null, 0);

    public static byte[] EncodePixels(ReadOnlySpan<byte> rgba, string format, string size, int width, int height, byte[] paletteRgba, int paletteBase = 0)
    {
        if (width <= 0 || height <= 0 || (long)width * height > MaxPixels || rgba.Length < width * height * 4) throw new InvalidDataException("Texture dimensions or pixels are outside the supported native preview limit.");
        int bits = size switch { "4b" => 4, "8b" => 8, "16b" => 16, "32b" => 32, _ => throw new InvalidDataException($"Unsupported native texture size {size}.") };
        if (format == "YUV" && bits == 16) { if ((width & 1) != 0) throw new InvalidDataException("Native YUV16 textures require an even width."); return EncodeYuv(rgba, width, height); }
        byte[] output = new byte[checked((width * height * bits + 7) / 8)];
        for (int pixel = 0; pixel < width * height; pixel++)
        {
            int source = pixel * 4; byte red = rgba[source], green = rgba[source + 1], blue = rgba[source + 2], alpha = rgba[source + 3];
            int gray = (red * 30 + green * 59 + blue * 11 + 50) / 100;
            int value = format switch
            {
                "RGBA" when bits == 16 => ((red * 31 + 127) / 255 << 11) | ((green * 31 + 127) / 255 << 6) | ((blue * 31 + 127) / 255 << 1) | (alpha >= 128 ? 1 : 0),
                "RGBA" when bits == 32 => -1,
                "IA" when bits == 4 => (((gray * 7 + 127) / 255) << 1) | (alpha >= 128 ? 1 : 0),
                "IA" when bits == 8 => ((gray * 15 + 127) / 255 << 4) | (alpha * 15 + 127) / 255,
                "IA" when bits == 16 => -1,
                "I" when bits == 4 => (gray * 15 + 127) / 255,
                "I" when bits == 8 => gray,
                "CI" when bits is 4 or 8 && paletteRgba is { Length: > 0 } => FindPaletteIndex(rgba, source, paletteRgba, paletteBase, bits),
                _ => throw new InvalidDataException($"Native texture format {format} {bits}b is not supported for replacement.")
            };
            if (bits == 4)
            {
                if ((pixel & 1) == 0) output[pixel / 2] = (byte)(value << 4); else output[pixel / 2] |= (byte)value;
            }
            else if (bits == 8) output[pixel] = (byte)value;
            else if (bits == 16)
            {
                if (value >= 0) { output[pixel * 2] = (byte)(value >> 8); output[pixel * 2 + 1] = (byte)value; }
                else if (format == "IA") { output[pixel * 2] = (byte)gray; output[pixel * 2 + 1] = alpha; }
            }
            else { output[pixel * 4] = red; output[pixel * 4 + 1] = green; output[pixel * 4 + 2] = blue; output[pixel * 4 + 3] = alpha; }
        }
        return output;
    }

    private static RomTextureAsset DecodeAsset(byte[] room, RomTextureCommandInfo image, int width, int height, byte[] palette, int paletteBase)
    {
        string format = image.FormatName, size = image.SizeName;
        if (height <= 0) return new RomTextureAsset(image.SourceOffset, format, size, width, 0, image.Segment, image.Address, [], "No G_SETTILESIZE height was found after G_SETTIMG.");
        if (image.Segment != 3) return new RomTextureAsset(image.SourceOffset, format, size, width, height, image.Segment, image.Address, [], $"Texture segment 0x{image.Segment:X2} is not a room-local segment.");
        try
        {
            int bits = image.Size switch { 0 => 4, 1 => 8, 2 => 16, 3 => 32, _ => 0 };
            int bytes = checked((width * height * bits + 7) / 8);
            if (image.Address < 0 || image.Address > room.Length - bytes) throw new InvalidDataException("Native texture data is outside the room bounds.");
            byte[] rgba = DecodePixels(room.AsSpan(image.Address, bytes), format, size, width, height, palette, paletteBase); return new RomTextureAsset(image.SourceOffset, format, size, width, height, image.Segment, image.Address, rgba, null, palette, paletteBase, image.Width, height);
        }
        catch (Exception error) when (error is InvalidDataException or OverflowException or ArgumentOutOfRangeException)
        {
            return new RomTextureAsset(image.SourceOffset, format, size, width, height, image.Segment, image.Address, [], error.Message);
        }
    }

    private static (byte R, byte G, byte B, byte A) DecodePixel(int value, ReadOnlySpan<byte> source, int pixel, string format, int bits, byte[] paletteRgba, int paletteBase)
    {
        return format switch
        {
            "RGBA" when bits == 16 => ((byte)(((value >> 11) & 0x1F) * 255 / 31), (byte)(((value >> 6) & 0x1F) * 255 / 31), (byte)(((value >> 1) & 0x1F) * 255 / 31), (byte)((value & 1) * 255)),
            "RGBA" when bits == 32 => (source[pixel * 4], source[pixel * 4 + 1], source[pixel * 4 + 2], source[pixel * 4 + 3]),
            "IA" when bits == 4 => ((byte)(((value >> 1) & 7) * 255 / 7), (byte)(((value >> 1) & 7) * 255 / 7), (byte)(((value >> 1) & 7) * 255 / 7), (byte)((value & 1) * 255)),
            "IA" when bits == 8 => ((byte)((value >> 4) * 17), (byte)((value >> 4) * 17), (byte)((value >> 4) * 17), (byte)((value & 0xF) * 17)),
            "IA" when bits == 16 => (source[pixel * 2], source[pixel * 2], source[pixel * 2], source[pixel * 2 + 1]),
            "I" when bits == 4 => ((byte)(value * 17), (byte)(value * 17), (byte)(value * 17), 255),
            "I" when bits == 8 => ((byte)value, (byte)value, (byte)value, 255),
            "CI" when bits is 4 or 8 && paletteRgba is { Length: > 0 } => PalettePixel(paletteRgba, paletteBase + (bits == 4 ? value : source[pixel])),
            _ => throw new InvalidDataException($"Native texture format {format} {bits}b is not supported for preview.")
        };
    }

    private static (byte R, byte G, byte B, byte A) PalettePixel(byte[] palette, int index)
    {
        int offset = checked(index * 4); if (offset < 0 || offset > palette.Length - 4) throw new InvalidDataException("CI texture palette index is outside the loaded palette.");
        return (palette[offset], palette[offset + 1], palette[offset + 2], palette[offset + 3]);
    }

    private static int FindPaletteIndex(ReadOnlySpan<byte> rgba, int source, byte[] palette, int paletteBase, int bits)
    {
        int count = bits == 4 ? 16 : 256; if (paletteBase < 0 || paletteBase > palette.Length / 4 - count) throw new InvalidDataException("CI texture palette range is outside the loaded palette.");
        int best = 0; long bestDistance = long.MaxValue;
        for (int index = 0; index < count; index++)
        {
            int offset = (paletteBase + index) * 4; long dr = rgba[source] - palette[offset], dg = rgba[source + 1] - palette[offset + 1], db = rgba[source + 2] - palette[offset + 2], da = rgba[source + 3] - palette[offset + 3]; long distance = dr * dr + dg * dg + db * db + da * da;
            if (distance < bestDistance) { bestDistance = distance; best = index; }
        }
        return best;
    }

    private static byte[] DecodeYuv(ReadOnlySpan<byte> source, int width, int height)
    {
        byte[] rgba = new byte[checked(width * height * 4)];
        for (int pixel = 0; pixel < width * height; pixel++)
        {
            int pair = (pixel / 2) * 4; int y = source[pair + ((pixel & 1) == 0 ? 0 : 2)]; int u = source[pair + 1] - 128; int v = source[pair + 3] - 128;
            int red = ClampByte(y + (int)Math.Round(1.402 * v)); int green = ClampByte(y - (int)Math.Round(0.344136 * u + 0.714136 * v)); int blue = ClampByte(y + (int)Math.Round(1.772 * u)); int output = pixel * 4;
            rgba[output] = (byte)red; rgba[output + 1] = (byte)green; rgba[output + 2] = (byte)blue; rgba[output + 3] = 255;
        }
        return rgba;
    }

    private static byte[] EncodeYuv(ReadOnlySpan<byte> rgba, int width, int height)
    {
        byte[] output = new byte[checked(width * height * 2)];
        for (int row = 0; row < height; row++)
            for (int x = 0; x < width; x += 2)
            {
                int first = (row * width + x) * 4; int second = (row * width + Math.Min(x + 1, width - 1)) * 4; int y0 = ClampByte((int)Math.Round(0.299 * rgba[first] + 0.587 * rgba[first + 1] + 0.114 * rgba[first + 2])); int y1 = ClampByte((int)Math.Round(0.299 * rgba[second] + 0.587 * rgba[second + 1] + 0.114 * rgba[second + 2])); int u0 = (int)Math.Round(-0.169 * rgba[first] - 0.331 * rgba[first + 1] + 0.5 * rgba[first + 2] + 128); int u1 = (int)Math.Round(-0.169 * rgba[second] - 0.331 * rgba[second + 1] + 0.5 * rgba[second + 2] + 128); int v0 = (int)Math.Round(0.5 * rgba[first] - 0.419 * rgba[first + 1] - 0.081 * rgba[first + 2] + 128); int v1 = (int)Math.Round(0.5 * rgba[second] - 0.419 * rgba[second + 1] - 0.081 * rgba[second + 2] + 128); int outputOffset = (row * width + x) * 2; output[outputOffset] = (byte)y0; output[outputOffset + 1] = (byte)ClampByte((u0 + u1) / 2); output[outputOffset + 2] = (byte)y1; output[outputOffset + 3] = (byte)ClampByte((v0 + v1) / 2);
            }
        return output;
    }

    private static int ClampByte(int value) => Math.Clamp(value, 0, 255);
}
