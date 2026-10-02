namespace Archarina64.Core;

public static class N64Compression
{
    public static byte[] EncodeYaz0(ReadOnlySpan<byte> input)
    {
        if (input.Length > RomDecoder.MaxRomBytes) throw new InvalidDataException("Yaz0 input exceeds the supported limit.");
        using var output = new MemoryStream(); output.Write("Yaz0"u8); WriteU32(output, checked((uint)input.Length)); output.Write(new byte[8]);
        int source = 0;
        while (source < input.Length)
        {
            long codePosition = output.Position; output.WriteByte(0); byte code = 0;
            for (int bit = 7; bit >= 0 && source < input.Length; bit--)
            {
                int bestDistance = 0, bestLength = 0, windowStart = Math.Max(0, source - 0x1000);
                for (int candidate = source - 1; candidate >= windowStart; candidate--)
                {
                    int length = 0; while (length < 0x111 && source + length < input.Length && input[candidate + length] == input[source + length] && candidate + length < source) length++;
                    if (length >= 3 && length > bestLength) { bestLength = length; bestDistance = source - candidate; if (length == 0x111) break; }
                }
                if (bestLength >= 3)
                {
                    int lengthNibble = bestLength >= 0x12 ? 0 : bestLength - 2; output.WriteByte((byte)((lengthNibble << 4) | ((bestDistance - 1) >> 8))); output.WriteByte((byte)((bestDistance - 1) & 0xFF));
                    if (lengthNibble == 0) output.WriteByte((byte)(bestLength - 0x12)); source += bestLength;
                }
                else { code |= (byte)(1 << bit); output.WriteByte(input[source++]); }
            }
            long end = output.Position; output.Position = codePosition; output.WriteByte(code); output.Position = end;
        }
        return output.ToArray();
    }

    public static byte[] EncodeMio0(ReadOnlySpan<byte> input)
    {
        if (input.Length > RomDecoder.MaxRomBytes) throw new InvalidDataException("MIO0 input exceeds the supported limit.");
        int layoutBytes = checked(((input.Length + 31) / 32) * 4), compOffset = 16 + layoutBytes, rawOffset = compOffset;
        using var output = new MemoryStream(); output.Write("MIO0"u8); WriteU32(output, checked((uint)input.Length)); WriteU32(output, checked((uint)compOffset)); WriteU32(output, checked((uint)rawOffset));
        for (int block = 0; block < layoutBytes / 4; block++)
        {
            int remaining = input.Length - block * 32; uint bits = remaining >= 32 ? 0xFFFFFFFFu : remaining <= 0 ? 0u : uint.MaxValue << (32 - remaining);
            WriteU32(output, bits);
        }
        output.Write(input);
        return output.ToArray();
    }

    public static byte[] Decode(ReadOnlySpan<byte> input)
    {
        if (input.Length < 4) throw new InvalidDataException("Compressed N64 file is truncated.");
        uint magic = (uint)input[0] << 24 | (uint)input[1] << 16 | (uint)input[2] << 8 | input[3];
        return magic switch
        {
            0x59617A30 => DecodeYaz0(input), // Yaz0
            0x4D494F30 => DecodeMio0(input), // MIO0
            _ => input.ToArray()
        };
    }

    private static byte[] DecodeYaz0(ReadOnlySpan<byte> input)
    {
        if (input.Length < 16) throw new InvalidDataException("Yaz0 header is truncated.");
        int outputSize = checked((int)U32(input, 4));
        if (outputSize < 0 || outputSize > RomDecoder.MaxRomBytes) throw new InvalidDataException("Yaz0 output exceeds the supported limit.");
        var output = new byte[outputSize]; int source = 16, destination = 0, mask = 0; byte code = 0;
        while (destination < outputSize)
        {
            if (mask == 0) { if (source >= input.Length) throw new InvalidDataException("Yaz0 code stream is truncated."); code = input[source++]; mask = 0x80; }
            if ((code & mask) != 0)
            {
                if (source >= input.Length) throw new InvalidDataException("Yaz0 literal stream is truncated.");
                output[destination++] = input[source++];
            }
            else
            {
                if (source + 1 >= input.Length) throw new InvalidDataException("Yaz0 back-reference is truncated.");
                int first = input[source++], second = input[source++], distance = ((first & 0xF) << 8 | second) + 1, length = first >> 4;
                if (length == 0) { if (source >= input.Length) throw new InvalidDataException("Yaz0 long length is truncated."); length = input[source++] + 0x12; } else length += 2;
                if (distance > destination) throw new InvalidDataException("Yaz0 back-reference points before output.");
                for (int i = 0; i < length && destination < outputSize; i++) output[destination] = output[destination++ - distance];
            }
            mask >>= 1;
        }
        return output;
    }

    private static byte[] DecodeMio0(ReadOnlySpan<byte> input)
    {
        if (input.Length < 16) throw new InvalidDataException("MIO0 header is truncated.");
        int outputSize = checked((int)U32(input, 4)), comp = checked((int)U32(input, 8)), raw = checked((int)U32(input, 12));
        if (outputSize < 0 || outputSize > RomDecoder.MaxRomBytes || comp < 16 || raw < 16 || comp > input.Length || raw > input.Length) throw new InvalidDataException("MIO0 header is invalid.");
        var output = new byte[outputSize]; int destination = 0, bitOffset = 16, compOffset = comp, rawOffset = raw;
        while (destination < outputSize)
        {
            if (bitOffset + 4 > input.Length) throw new InvalidDataException("MIO0 layout stream is truncated.");
            uint bits = U32(input, bitOffset); bitOffset += 4;
            for (int bit = 31; bit >= 0 && destination < outputSize; bit--)
            {
                if (((bits >> bit) & 1) != 0)
                {
                    if (rawOffset >= input.Length) throw new InvalidDataException("MIO0 raw stream is truncated.");
                    output[destination++] = input[rawOffset++];
                }
                else
                {
                    if (compOffset + 1 >= input.Length) throw new InvalidDataException("MIO0 back-reference is truncated.");
                    int first = input[compOffset++], second = input[compOffset++], distance = ((first & 0xF) << 8 | second) + 1, length = (first >> 4) + 3;
                    if (distance > destination) throw new InvalidDataException("MIO0 back-reference points before output.");
                    for (int i = 0; i < length && destination < outputSize; i++) output[destination] = output[destination++ - distance];
                }
            }
        }
        return output;
    }

    private static uint U32(ReadOnlySpan<byte> data, int offset) => (uint)data[offset] << 24 | (uint)data[offset + 1] << 16 | (uint)data[offset + 2] << 8 | data[offset + 3];
    private static void WriteU32(Stream output, uint value) { output.WriteByte((byte)(value >> 24)); output.WriteByte((byte)(value >> 16)); output.WriteByte((byte)(value >> 8)); output.WriteByte((byte)value); }
}
