using System.Text;

namespace Dial.Sharp.Examples.SpeechToText;

internal static class WavFileWriter
{
    public static void WritePcm16(Stream stream, ReadOnlySpan<byte> pcm, int sampleRate, short channels = 1)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (pcm.Length % (channels * 2) != 0)
        {
            throw new ArgumentException("PCM16 buffer length must align to sample frame size.", nameof(pcm));
        }

        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

        const short bitsPerSample = 16;
        var blockAlign = (short)(channels * (bitsPerSample / 8));
        var byteRate = sampleRate * blockAlign;
        var dataSize = pcm.Length;

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);
        writer.Write(pcm);
    }
}
