using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PdxModIDE.ModelEngine;

public static class DdsDecoder
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct DdsHeader
    {
        public uint Magic;
        public uint Size;
        public uint Flags;
        public uint Height;
        public uint Width;
        public uint PitchOrLinearSize;
        public uint Depth;
        public uint MipMapCount;
        public uint Reserved1;
        public uint Reserved2;
        public uint Reserved3;
        public uint Reserved4;
        public uint Reserved5;
        public uint Reserved6;
        public uint Reserved7;
        public uint Reserved8;
        public uint Reserved9;
        public uint Reserved10;
        public uint Reserved11;
        public PixelFormat PixelFormat;
        public uint Caps;
        public uint Caps2;
        public uint Caps3;
        public uint Caps4;
        public uint Reserved12;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PixelFormat
    {
        public uint Size;
        public uint Flags;
        public uint FourCC;
        public uint RgbBitCount;
        public uint RBitMask;
        public uint GBitMask;
        public uint BBitMask;
        public uint ABitMask;
    }

    private const uint DDS_MAGIC = 0x20534444;
    private const uint DDPF_FOURCC = 0x4;
    private const uint DDPF_RGB = 0x40;
    private const uint DDPF_ALPHAPIXELS = 0x1;
    private const uint FOURCC_DXT1 = 0x31545844;
    private const uint FOURCC_DXT3 = 0x33545844;
    private const uint FOURCC_DXT5 = 0x35545844;
    private const uint FOURCC_DX10 = 0x30315844;

    public static DdsImage Decode(string filepath)
    {
        byte[] data = File.ReadAllBytes(filepath);
        return Decode(data);
    }

    public static DdsImage Decode(byte[] data)
    {
        if (data.Length < Marshal.SizeOf<DdsHeader>())
            throw new InvalidDataException("File too small to be a DDS");

        var header = ByteArrayToStructure<DdsHeader>(data, 0);

        if (header.Magic != DDS_MAGIC)
            throw new InvalidDataException("Invalid DDS magic number");

        int headerSize = Marshal.SizeOf<DdsHeader>();
        int pixelDataOffset = headerSize;

        if ((header.PixelFormat.Flags & DDPF_FOURCC) != 0 && header.PixelFormat.FourCC == FOURCC_DX10)
        {
            pixelDataOffset += 20;
        }

        int width = (int)header.Width;
        int height = (int)header.Height;

        if ((header.PixelFormat.Flags & DDPF_FOURCC) != 0)
        {
            uint fourcc = header.PixelFormat.FourCC;
            if (fourcc == FOURCC_DXT1)
                return DecodeDxt1(data, pixelDataOffset, width, height);
            if (fourcc == FOURCC_DXT3)
                return DecodeDxt3(data, pixelDataOffset, width, height);
            if (fourcc == FOURCC_DXT5)
                return DecodeDxt5(data, pixelDataOffset, width, height);
            throw new NotSupportedException($"Unsupported DDS FourCC: {fourcc:X8}");
        }
        else if ((header.PixelFormat.Flags & DDPF_RGB) != 0)
        {
            return DecodeRgb(data, pixelDataOffset, width, height, header.PixelFormat);
        }

        throw new NotSupportedException("Unsupported DDS format");
    }

    private static DdsImage DecodeDxt1(byte[] data, int offset, int width, int height)
    {
        int blockCountX = (width + 3) / 4;
        int blockCountY = (height + 3) / 4;
        int blockSize = 8;
        byte[] pixels = new byte[width * height * 4];

        for (int by = 0; by < blockCountY; by++)
        {
            for (int bx = 0; bx < blockCountX; bx++)
            {
                int blockOffset = offset + (by * blockCountX + bx) * blockSize;
                if (blockOffset + blockSize > data.Length) continue;

                ushort color0 = BitConverter.ToUInt16(data, blockOffset);
                ushort color1 = BitConverter.ToUInt16(data, blockOffset + 2);
                uint bits = BitConverter.ToUInt32(data, blockOffset + 4);

                DecodeDxtBlock(color0, color1, bits, false, width, height, bx, by, pixels);
            }
        }

        return new DdsImage { Width = width, Height = height, Data = pixels, HasAlpha = false };
    }

    private static DdsImage DecodeDxt3(byte[] data, int offset, int width, int height)
    {
        int blockCountX = (width + 3) / 4;
        int blockCountY = (height + 3) / 4;
        int blockSize = 16;
        byte[] pixels = new byte[width * height * 4];

        for (int by = 0; by < blockCountY; by++)
        {
            for (int bx = 0; bx < blockCountX; bx++)
            {
                int blockOffset = offset + (by * blockCountX + bx) * blockSize;
                if (blockOffset + blockSize > data.Length) continue;

                ulong alphaBits = BitConverter.ToUInt64(data, blockOffset);
                ushort color0 = BitConverter.ToUInt16(data, blockOffset + 8);
                ushort color1 = BitConverter.ToUInt16(data, blockOffset + 10);
                uint colorBits = BitConverter.ToUInt32(data, blockOffset + 12);

                DecodeDxtBlock(color0, color1, colorBits, true, width, height, bx, by, pixels, alphaBits);
            }
        }

        return new DdsImage { Width = width, Height = height, Data = pixels, HasAlpha = true };
    }

    private static DdsImage DecodeDxt5(byte[] data, int offset, int width, int height)
    {
        int blockCountX = (width + 3) / 4;
        int blockCountY = (height + 3) / 4;
        int blockSize = 16;
        byte[] pixels = new byte[width * height * 4];

        for (int by = 0; by < blockCountY; by++)
        {
            for (int bx = 0; bx < blockCountX; bx++)
            {
                int blockOffset = offset + (by * blockCountX + bx) * blockSize;
                if (blockOffset + blockSize > data.Length) continue;

                byte alpha0 = data[blockOffset];
                byte alpha1 = data[blockOffset + 1];
                ulong alphaBits = 0;
                for (int i = 0; i < 6; i++)
                    alphaBits |= (ulong)data[blockOffset + 2 + i] << (i * 8);

                ushort color0 = BitConverter.ToUInt16(data, blockOffset + 8);
                ushort color1 = BitConverter.ToUInt16(data, blockOffset + 10);
                uint colorBits = BitConverter.ToUInt32(data, blockOffset + 12);

                DecodeDxt5Block(color0, color1, colorBits, alpha0, alpha1, alphaBits, width, height, bx, by, pixels);
            }
        }

        return new DdsImage { Width = width, Height = height, Data = pixels, HasAlpha = true };
    }

    private static void DecodeDxtBlock(ushort color0, ushort color1, uint bits, bool hasExplicitAlpha, int width, int height, int bx, int by, byte[] pixels, ulong explicitAlpha = 0)
    {
        int r0 = (color0 >> 11) & 0x1F;
        int g0 = (color0 >> 5) & 0x3F;
        int b0 = color0 & 0x1F;

        int r1 = (color1 >> 11) & 0x1F;
        int g1 = (color1 >> 5) & 0x3F;
        int b1 = color1 & 0x1F;

        r0 = (r0 << 3) | (r0 >> 2);
        g0 = (g0 << 2) | (g0 >> 4);
        b0 = (b0 << 3) | (b0 >> 2);

        r1 = (r1 << 3) | (r1 >> 2);
        g1 = (g1 << 2) | (g1 >> 4);
        b1 = (b1 << 3) | (b1 >> 2);

        int[] colors = new int[4];
        colors[0] = (r0 << 16) | (g0 << 8) | b0;
        colors[1] = (r1 << 16) | (g1 << 8) | b1;
        colors[2] = (((2 * r0 + r1) / 3) << 16) | (((2 * g0 + g1) / 3) << 8) | ((2 * b0 + b1) / 3);
        colors[3] = (((2 * r1 + r0) / 3) << 16) | (((2 * g1 + g0) / 3) << 8) | ((2 * b1 + b0) / 3);

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                int px = bx * 4 + x;
                int py = by * 4 + y;
                if (px >= width || py >= height) continue;

                int index = (int)((bits >> (2 * (y * 4 + x))) & 3);
                int color = colors[index];
                int alpha = 255;

                if (hasExplicitAlpha)
                {
                    alpha = (int)(explicitAlpha >> (4 * (y * 4 + x))) & 0xF;
                    alpha = (alpha << 4) | alpha;
                }
                else if (index == 3 && color0 <= color1)
                {
                    alpha = 0;
                }

                int pixelIndex = (py * width + px) * 4;
                pixels[pixelIndex] = (byte)(color & 0xFF);
                pixels[pixelIndex + 1] = (byte)((color >> 8) & 0xFF);
                pixels[pixelIndex + 2] = (byte)((color >> 16) & 0xFF);
                pixels[pixelIndex + 3] = (byte)alpha;
            }
        }
    }

    private static void DecodeDxt5Block(ushort color0, ushort color1, uint colorBits, byte alpha0, byte alpha1, ulong alphaBits, int width, int height, int bx, int by, byte[] pixels)
    {
        int r0 = (color0 >> 11) & 0x1F;
        int g0 = (color0 >> 5) & 0x3F;
        int b0 = color0 & 0x1F;

        int r1 = (color1 >> 11) & 0x1F;
        int g1 = (color1 >> 5) & 0x3F;
        int b1 = color1 & 0x1F;

        r0 = (r0 << 3) | (r0 >> 2);
        g0 = (g0 << 2) | (g0 >> 4);
        b0 = (b0 << 3) | (b0 >> 2);

        r1 = (r1 << 3) | (r1 >> 2);
        g1 = (g1 << 2) | (g1 >> 4);
        b1 = (b1 << 3) | (b1 >> 2);

        int[] colors = new int[4];
        colors[0] = (r0 << 16) | (g0 << 8) | b0;
        colors[1] = (r1 << 16) | (g1 << 8) | b1;
        colors[2] = (((2 * r0 + r1) / 3) << 16) | (((2 * g0 + g1) / 3) << 8) | ((2 * b0 + b1) / 3);
        colors[3] = (((2 * r1 + r0) / 3) << 16) | (((2 * g1 + g0) / 3) << 8) | ((2 * b1 + b0) / 3);

        byte[] alphas = new byte[8];
        alphas[0] = alpha0;
        alphas[1] = alpha1;
        if (alpha0 > alpha1)
        {
            for (int i = 1; i < 7; i++)
                alphas[i + 1] = (byte)(((7 - i) * alpha0 + i * alpha1) / 7);
        }
        else
        {
            for (int i = 1; i < 5; i++)
                alphas[i + 1] = (byte)(((5 - i) * alpha0 + i * alpha1) / 5);
            alphas[6] = 0;
            alphas[7] = 255;
        }

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                int px = bx * 4 + x;
                int py = by * 4 + y;
                if (px >= width || py >= height) continue;

                int colorIndex = (int)((colorBits >> (2 * (y * 4 + x))) & 3);
                int color = colors[colorIndex];

                int alphaIndex = (int)((alphaBits >> (3 * (y * 4 + x))) & 7);
                int alpha = alphas[alphaIndex];

                int pixelIndex = (py * width + px) * 4;
                pixels[pixelIndex] = (byte)(color & 0xFF);
                pixels[pixelIndex + 1] = (byte)((color >> 8) & 0xFF);
                pixels[pixelIndex + 2] = (byte)((color >> 16) & 0xFF);
                pixels[pixelIndex + 3] = (byte)alpha;
            }
        }
    }

    private static DdsImage DecodeRgb(byte[] data, int offset, int width, int height, PixelFormat pf)
    {
        int bpp = (int)pf.RgbBitCount / 8;
        int stride = width * bpp;
        byte[] pixels = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            int rowOffset = offset + y * stride;
            for (int x = 0; x < width; x++)
            {
                int pixelOffset = rowOffset + x * bpp;
                if (pixelOffset + bpp > data.Length) continue;

                byte r = 0, g = 0, b = 0, a = 255;
                if (bpp == 4)
                {
                    b = data[pixelOffset];
                    g = data[pixelOffset + 1];
                    r = data[pixelOffset + 2];
                    a = data[pixelOffset + 3];
                }
                else if (bpp == 3)
                {
                    b = data[pixelOffset];
                    g = data[pixelOffset + 1];
                    r = data[pixelOffset + 2];
                }

                int pixelIndex = (y * width + x) * 4;
                pixels[pixelIndex] = r;
                pixels[pixelIndex + 1] = g;
                pixels[pixelIndex + 2] = b;
                pixels[pixelIndex + 3] = a;
            }
        }

        return new DdsImage { Width = width, Height = height, Data = pixels, HasAlpha = (pf.Flags & DDPF_ALPHAPIXELS) != 0 };
    }

    private static T ByteArrayToStructure<T>(byte[] bytes, int offset) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(bytes, offset, ptr, size);
            return Marshal.PtrToStructure<T>(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}

public sealed class DdsImage
{
    public int Width { get; init; }
    public int Height { get; init; }
    public byte[] Data { get; init; } = [];
    public bool HasAlpha { get; init; }
}