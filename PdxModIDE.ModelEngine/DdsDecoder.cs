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

        int width = (int)header.Width;
        int height = (int)header.Height;

        int headerSize = Marshal.SizeOf<DdsHeader>();
        int pixelDataOffset = headerSize;

        if ((header.PixelFormat.Flags & DDPF_FOURCC) != 0 && header.PixelFormat.FourCC == FOURCC_DX10)
        {
            pixelDataOffset += 20;
            uint dxgiFormat = BitConverter.ToUInt32(data, headerSize);
            return DecodeDx10(data, pixelDataOffset, width, height, dxgiFormat);
        }

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

    private static DdsImage DecodeDx10(byte[] data, int offset, int width, int height, uint dxgiFormat)
    {
        switch (dxgiFormat)
        {
            case 71: // BC1_UNORM
                return DecodeDxt1(data, offset, width, height);
            case 72: // BC2_UNORM
                return DecodeDxt3(data, offset, width, height);
            case 73: // BC3_UNORM
                return DecodeDxt5(data, offset, width, height);
            case 74: // BC4_UNORM
                return DecodeBc4(data, offset, width, height);
            case 76: // BC5_UNORM
                return DecodeBc5(data, offset, width, height);
            case 98: // BC7_UNORM
            case 99: // BC7_UNORM_SRGB
                return DecodeBc7(data, offset, width, height);
            case 95: // BC6H_UF16
            case 96: // BC6H_SF16
                return DecodeBc6h(data, offset, width, height);
            default:
                throw new NotSupportedException($"Unsupported DX10 format: {dxgiFormat}");
        }
    }

    private static DdsImage DecodeBc4(byte[] data, int offset, int width, int height)
    {
        int blockCountX = (width + 3) / 4;
        int blockCountY = (height + 3) / 4;
        byte[] pixels = new byte[width * height * 4];

        for (int by = 0; by < blockCountY; by++)
        {
            for (int bx = 0; bx < blockCountX; bx++)
            {
                int blockOffset = offset + (by * blockCountX + bx) * 8;
                if (blockOffset + 8 > data.Length) continue;

                byte r0 = data[blockOffset];
                byte r1 = data[blockOffset + 1];
                ulong bits = 0;
                for (int i = 0; i < 6; i++)
                    bits |= (ulong)data[blockOffset + 2 + i] << (i * 8);

                byte[] reds = new byte[8];
                reds[0] = r0;
                reds[1] = r1;
                if (r0 > r1)
                {
                    for (int i = 1; i < 7; i++)
                        reds[i + 1] = (byte)(((7 - i) * r0 + i * r1) / 7);
                }
                else
                {
                    for (int i = 1; i < 5; i++)
                        reds[i + 1] = (byte)(((5 - i) * r0 + i * r1) / 5);
                    reds[6] = 0;
                    reds[7] = 255;
                }

                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        int px = bx * 4 + x;
                        int py = by * 4 + y;
                        if (px >= width || py >= height) continue;
                        int idx = (int)((bits >> (3 * (y * 4 + x))) & 7);
                        byte v = reds[idx];
                        int pi = (py * width + px) * 4;
                        pixels[pi] = v; pixels[pi + 1] = v; pixels[pi + 2] = v; pixels[pi + 3] = 255;
                    }
                }
            }
        }

        return new DdsImage { Width = width, Height = height, Data = pixels, HasAlpha = false };
    }

    private static DdsImage DecodeBc5(byte[] data, int offset, int width, int height)
    {
        int blockCountX = (width + 3) / 4;
        int blockCountY = (height + 3) / 4;
        byte[] pixels = new byte[width * height * 4];

        for (int by = 0; by < blockCountY; by++)
        {
            for (int bx = 0; bx < blockCountX; bx++)
            {
                int blockOffset = offset + (by * blockCountX + bx) * 16;
                if (blockOffset + 16 > data.Length) continue;

                byte r0 = data[blockOffset];
                byte r1 = data[blockOffset + 1];
                ulong rb = 0;
                for (int i = 0; i < 6; i++)
                    rb |= (ulong)data[blockOffset + 2 + i] << (i * 8);
                byte g0 = data[blockOffset + 8];
                byte g1 = data[blockOffset + 9];
                ulong gb = 0;
                for (int i = 0; i < 6; i++)
                    gb |= (ulong)data[blockOffset + 10 + i] << (i * 8);

                byte[] reds = BuildBc4Lut(r0, r1);
                byte[] greens = BuildBc4Lut(g0, g1);

                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        int px = bx * 4 + x;
                        int py = by * 4 + y;
                        if (px >= width || py >= height) continue;
                        int ri = (int)((rb >> (3 * (y * 4 + x))) & 7);
                        int gi = (int)((gb >> (3 * (y * 4 + x))) & 7);
                        int pi = (py * width + px) * 4;
                        pixels[pi] = reds[ri];
                        pixels[pi + 1] = greens[gi];
                        pixels[pi + 2] = 255;
                        pixels[pi + 3] = 255;
                    }
                }
            }
        }

        return new DdsImage { Width = width, Height = height, Data = pixels, HasAlpha = false };
    }

    private static byte[] BuildBc4Lut(byte v0, byte v1)
    {
        byte[] l = new byte[8];
        l[0] = v0;
        l[1] = v1;
        if (v0 > v1)
        {
            for (int i = 1; i < 7; i++)
                l[i + 1] = (byte)(((7 - i) * v0 + i * v1) / 7);
        }
        else
        {
            for (int i = 1; i < 5; i++)
                l[i + 1] = (byte)(((5 - i) * v0 + i * v1) / 5);
            l[6] = 0;
            l[7] = 255;
        }
        return l;
    }

    private static DdsImage DecodeBc6h(byte[] data, int offset, int width, int height)
        => throw new NotSupportedException("BC6H texture format is not supported for viewport rendering");

        private static DdsImage DecodeBc7(byte[] data, int offset, int width, int height)
    {
        int blockCountX = (width + 3) / 4;
        int blockCountY = (height + 3) / 4;
        byte[] pixels = new byte[width * height * 4];

        for (int by = 0; by < blockCountY; by++)
        {
            for (int bx = 0; bx < blockCountX; bx++)
            {
                int blockOffset = offset + (by * blockCountX + bx) * 16;
                if (blockOffset + 16 > data.Length) continue;
                DecodeBc7Block(data, blockOffset, bx, by, width, height, pixels);
            }
        }

        return new DdsImage { Width = width, Height = height, Data = pixels, HasAlpha = false };
    }

    private static void DecodeBc7Block(byte[] data, int blockOffset, int bx, int by, int width, int height, byte[] pixels)
    {
        var bs = new Bc7BitStream(BitConverter.ToUInt64(data, blockOffset), BitConverter.ToUInt64(data, blockOffset + 8));

        int mode;
        for (mode = 0; mode < 8 && bs.ReadBit() == 0; mode++) { }

        int[] blockPixels = new int[16]; // packed 0xAABBGGRR
        if (mode >= 8)
        {
            for (int i = 0; i < 16; i++) blockPixels[i] = 0;
        }
        else
        {
            DecodeBc7Mode(bs, mode, blockPixels);
        }

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                int px = bx * 4 + x;
                int py = by * 4 + y;
                if (px >= width || py >= height) continue;
                int p = blockPixels[y * 4 + x];
                int pi = (py * width + px) * 4;
                pixels[pi] = (byte)(p & 0xFF);
                pixels[pi + 1] = (byte)((p >> 8) & 0xFF);
                pixels[pi + 2] = (byte)((p >> 16) & 0xFF);
                pixels[pi + 3] = (byte)((p >> 24) & 0xFF);
            }
        }
    }

    private static void DecodeBc7Mode(Bc7BitStream bs, int mode, int[] outPixels)
    {
        int j, k;
        int numPartitions = 1;
        int partition = 0;
        int rotation = 0;
        int indexSelectionBit = 0;

        if (mode == 0 || mode == 1 || mode == 2 || mode == 3 || mode == 7)
        {
            numPartitions = (mode == 0 || mode == 2) ? 3 : 2;
            partition = bs.ReadBits(mode == 0 ? 4 : 6);
        }

        int numEndpoints = numPartitions * 2;

        if (mode == 4 || mode == 5)
        {
            rotation = bs.ReadBits(2);
            if (mode == 4) indexSelectionBit = bs.ReadBit();
        }

        // Extract endpoints
        int[,] endpoints = new int[6, 4];
        for (j = 0; j < 3; j++)
            for (int e = 0; e < numEndpoints; e++)
                endpoints[e, j] = bs.ReadBits(Bc7ActualBitsCount0[mode]);
        if (Bc7ActualBitsCount1[mode] > 0)
            for (int e = 0; e < numEndpoints; e++)
                endpoints[e, 3] = bs.ReadBits(Bc7ActualBitsCount1[mode]);

        // P-bits
        if (mode == 0 || mode == 1 || mode == 3 || mode == 6 || mode == 7)
        {
            for (int e = 0; e < numEndpoints; e++)
                for (j = 0; j < 4; j++)
                    endpoints[e, j] <<= 1;

            if (mode == 1)
            {
                int i0 = bs.ReadBit();
                int i1 = bs.ReadBit();
                for (k = 0; k < 3; k++)
                {
                    endpoints[0, k] |= i0; endpoints[1, k] |= i0;
                    endpoints[2, k] |= i1; endpoints[3, k] |= i1;
                }
            }
            else if ((Bc7ModeHasPBits & (1 << mode)) != 0)
            {
                for (int e = 0; e < numEndpoints; e++)
                {
                    int pbit = bs.ReadBit();
                    for (k = 0; k < 4; k++) endpoints[e, k] |= pbit;
                }
            }
        }

        for (int e = 0; e < numEndpoints; e++)
        {
            j = Bc7ActualBits[0][mode] + ((Bc7ModeHasPBits >> mode) & 1);
            for (k = 0; k < 3; k++)
            {
                endpoints[e, k] = endpoints[e, k] << (8 - j);
                endpoints[e, k] |= endpoints[e, k] >> j;
            }
            j = Bc7ActualBits[1][mode] + ((Bc7ModeHasPBits >> mode) & 1);
            endpoints[e, 3] = endpoints[e, 3] << (8 - j);
            endpoints[e, 3] |= endpoints[e, 3] >> j;
        }

        if (Bc7ActualBits[1][mode] == 0)
            for (int e = 0; e < numEndpoints; e++)
                endpoints[e, 3] = 0xFF;

        int indexBits = (mode == 0 || mode == 1) ? 3 : (mode == 6 ? 4 : 2);
        int indexBits2 = mode == 4 ? 3 : (mode == 5 ? 2 : 0);
        int[] weights = indexBits == 2 ? AWeight2 : (indexBits == 3 ? AWeight3 : AWeight4);
        int[] weights2 = indexBits2 == 2 ? AWeight2 : AWeight3;

        int[,] indices = new int[4, 4];

        // Pass #1: color indices
        for (int i = 0; i < 4; i++)
        {
            for (int c = 0; c < 4; c++)
            {
                int partitionSet = numPartitions == 1
                    ? ((i | c) == 0 ? 128 : 0)
                    : (numPartitions == 2 ? Partition2Subset : Partition3Subset)[partition * 16 + i * 4 + c];

                indexBits = (mode == 0 || mode == 1) ? 3 : (mode == 6 ? 4 : 2);
                if ((partitionSet & 0x80) != 0) indexBits--;

                indices[i, c] = bs.ReadBits(indexBits);
            }
        }

        // Pass #2: alpha indices, interpolate, rotate
        for (int i = 0; i < 4; i++)
        {
            for (int c = 0; c < 4; c++)
            {
                int partitionSet = numPartitions == 1
                    ? 0
                    : (numPartitions == 2 ? Partition2Subset : Partition3Subset)[partition * 16 + i * 4 + c] & 0x03;

                int index = indices[i, c];
                int r, g, b, a;

                if (indexBits2 == 0)
                {
                    r = Interpolate(endpoints[partitionSet * 2, 0], endpoints[partitionSet * 2 + 1, 0], weights, index);
                    g = Interpolate(endpoints[partitionSet * 2, 1], endpoints[partitionSet * 2 + 1, 1], weights, index);
                    b = Interpolate(endpoints[partitionSet * 2, 2], endpoints[partitionSet * 2 + 1, 2], weights, index);
                    a = Interpolate(endpoints[partitionSet * 2, 3], endpoints[partitionSet * 2 + 1, 3], weights, index);
                }
                else
                {
                    int index2 = bs.ReadBits((i | c) != 0 ? indexBits2 : indexBits2 - 1);
                    if (indexSelectionBit == 0)
                    {
                        r = Interpolate(endpoints[partitionSet * 2, 0], endpoints[partitionSet * 2 + 1, 0], weights, index);
                        g = Interpolate(endpoints[partitionSet * 2, 1], endpoints[partitionSet * 2 + 1, 1], weights, index);
                        b = Interpolate(endpoints[partitionSet * 2, 2], endpoints[partitionSet * 2 + 1, 2], weights, index);
                        a = Interpolate(endpoints[partitionSet * 2, 3], endpoints[partitionSet * 2 + 1, 3], weights2, index2);
                    }
                    else
                    {
                        r = Interpolate(endpoints[partitionSet * 2, 0], endpoints[partitionSet * 2 + 1, 0], weights2, index2);
                        g = Interpolate(endpoints[partitionSet * 2, 1], endpoints[partitionSet * 2 + 1, 1], weights2, index2);
                        b = Interpolate(endpoints[partitionSet * 2, 2], endpoints[partitionSet * 2 + 1, 2], weights2, index2);
                        a = Interpolate(endpoints[partitionSet * 2, 3], endpoints[partitionSet * 2 + 1, 3], weights, index);
                    }
                }

                switch (rotation)
                {
                    case 1: Swap(ref a, ref r); break;
                    case 2: Swap(ref a, ref g); break;
                    case 3: Swap(ref a, ref b); break;
                }

                outPixels[i * 4 + c] = (a << 24) | (b << 16) | (g << 8) | r;
            }
        }
    }

    private static int Interpolate(int a, int b, int[] weights, int index)
        => (a * (64 - weights[index]) + b * weights[index] + 32) >> 6;

    private static void Swap(ref int a, ref int b)
    {
        (a, b) = (b, a);
    }

    private static readonly int[][] Bc7ActualBits = new[]
    {
        new[] { 4, 6, 5, 7, 5, 7, 7, 5 },
        new[] { 0, 0, 0, 0, 6, 8, 7, 5 }
    };
    private const int BC7_MODE_HAS_PBITS = 0b11001011;
    private static readonly int[] Bc7ActualBitsCount0 = Bc7ActualBits[0];
    private static readonly int[] Bc7ActualBitsCount1 = Bc7ActualBits[1];
    private static int Bc7ModeHasPBits => BC7_MODE_HAS_PBITS;

    private static readonly int[] AWeight2 = { 0, 21, 43, 64 };
    private static readonly int[] AWeight3 = { 0, 9, 18, 27, 37, 46, 55, 64 };
    private static readonly int[] AWeight4 = { 0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64 };

    // Partition tables: flat arrays, 64 partitions x 16 texels; MSB set on fix-up indices
    private static readonly int[] Partition2Subset =
    {
        // 0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15
        128,  0,  1,  1,  0,  0,  1,  1,  0,  0,  1,  1,  0,  0,  1,129,
        128,  0,  0,  1,  0,  0,  0,  1,  0,  0,  0,  1,  0,  0,  0,129,
        128,  1,  1,  1,  0,  1,  1,  1,  0,  1,  1,  1,  0,  1,  1,129,
        128,  0,  0,  1,  0,  0,  1,  1,  0,  0,  1,  1,  0,  1,  1,129,
        128,  0,  0,  0,  0,  0,  0,  1,  0,  0,  0,  1,  0,  0,  1,129,
        128,  0,  1,  1,  0,  1,  1,  1,  0,  1,  1,  1,  1,  1,  1,129,
        128,  0,  0,  1,  0,  0,  1,  1,  0,  1,  1,  1,  1,  1,  1,129,
        128,  0,  0,  0,  0,  0,  0,  1,  0,  0,  1,  1,  0,  1,  1,129,
        128,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  1,  0,  0,  1,129,
        128,  0,  1,  1,  0,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,129,
        128,  0,  0,  0,  0,  0,  0,  1,  0,  1,  1,  1,  1,  1,  1,129,
        128,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  1,  0,  1,  1,129,
        128,  0,  0,  1,  0,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,129,
        128,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,  1,  1,  1,  1,129,
        128,  0,  0,  0,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,  1,129,
        128,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  1,  1,  1,129,
        128,  0,  0,  0,  1,  0,  0,  0,  1,  1,  1,  0,  1,  1,  1,129,
        128,  1,129,  1,  0,  0,  0,  1,  0,  0,  0,  0,  0,  0,  0,  0,
        128,  0,  0,  0,  0,  0,  0,  0,129,  0,  0,  0,  1,  1,  1,  0,
        128,  1,129,  1,  0,  0,  1,  1,  0,  0,  0,  1,  0,  0,  0,  0,
        128,  0,129,  1,  0,  0,  0,  1,  0,  0,  0,  0,  0,  0,  0,  0,
        128,  0,  0,  0,  1,  0,  0,  0,129,  1,  0,  0,  1,  1,  1,  0,
        128,  0,  0,  0,  0,  0,  0,  0,129,  0,  0,  0,  1,  1,  0,  0,
        128,  1,  1,  1,  0,  0,  1,  1,  0,  0,  1,  1,  0,  0,  0,129,
        128,  0,129,  1,  0,  0,  0,  1,  0,  0,  0,  1,  0,  0,  0,  0,
        128,  0,  0,  0,  1,  0,  0,  0,129,  0,  0,  0,  1,  1,  0,  0,
        128,  1,129,  0,  0,  1,  1,  0,  0,  1,  1,  0,  0,  1,  1,  0,
        128,  0,129,  1,  0,  1,  1,  0,  0,  1,  1,  0,  1,  1,  0,  0,
        128,  0,  0,  1,  0,  1,  1,  1,129,  1,  1,  0,  1,  0,  0,  0,
        128,  0,  0,  0,  1,  1,  1,  1,129,  1,  1,  1,  0,  0,  0,  0,
        128,  1,129,  1,  0,  0,  0,  1,  1,  0,  0,  0,  1,  1,  1,  0,
        128,  0,129,  1,  1,  0,  0,  1,  1,  0,  0,  1,  1,  1,  0,  0,
        128,  1,  0,  1,  0,  1,  0,  1,  0,  1,  0,  1,  0,  1,  0,129,
        128,  0,  0,  0,  1,  1,  1,  1,  0,  0,  0,  0,  1,  1,  1,129,
        128,  1,  0,  1,  1,  0,129,  0,  0,  1,  0,  1,  1,  0,  1,  0,
        128,  0,  1,  1,  0,  0,  1,  1,129,  1,  0,  0,  1,  1,  0,  0,
        128,  0,129,  1,  1,  1,  0,  0,  0,  0,  1,  1,  1,  1,  0,  0,
        128,  1,  0,  1,  0,  1,  0,  1,129,  0,  1,  0,  1,  0,  1,  0,
        128,  1,  1,  0,  1,  0,  0,  1,  0,  1,  1,  0,  1,  0,  0,129,
        128,  1,  0,  1,  1,  0,  1,  0,  1,  0,  1,  0,  0,  1,  0,129,
        128,  1,129,  1,  0,  0,  1,  1,  1,  1,  0,  0,  1,  1,  1,  0,
        128,  0,  0,  1,  0,  0,  1,  1,129,  1,  0,  0,  1,  0,  0,  0,
        128,  0,129,  1,  0,  0,  1,  0,  0,  1,  0,  0,  1,  1,  0,  0,
        128,  0,129,  1,  1,  0,  1,  1,  1,  1,  0,  1,  1,  1,  0,  0,
        128,  1,129,  0,  1,  0,  0,  1,  1,  0,  0,  1,  0,  1,  1,  0,
        128,  0,  1,  1,  1,  1,  0,  0,  1,  1,  0,  0,  0,  0,  1,129,
        128,  1,  1,  0,  0,  1,  1,  0,  1,  0,  0,  1,  1,  0,  0,129,
        128,  0,  0,  0,  0,  1,129,  0,  0,  1,  1,  0,  0,  0,  0,  0,
        128,  1,  0,  0,  1,  1,129,  0,  0,  1,  0,  0,  0,  0,  0,  0,
        128,  0,129,  0,  0,  1,  1,  1,  0,  0,  1,  0,  0,  0,  0,  0,
        128,  0,  0,  0,  0,  0,129,  0,  0,  1,  1,  1,  0,  0,  1,  0,
        128,  0,  0,  0,  0,  1,  0,  0,129,  1,  1,  0,  0,  1,  0,  0,
        128,  1,  1,  0,  1,  1,  0,  0,  1,  0,  0,  1,  0,  0,  1,129,
        128,  0,  1,  1,  0,  1,  1,  0,  1,  1,  0,  0,  1,  0,  0,129,
        128,  1,129,  0,  0,  0,  1,  1,  1,  0,  0,  1,  1,  1,  0,  0,
        128,  0,129,  1,  1,  0,  0,  1,  1,  1,  0,  0,  0,  1,  1,  0,
        128,  1,  1,  0,  1,  1,  0,  0,  1,  1,  0,  0,  1,  0,  0,129,
        128,  1,  1,  0,  0,  0,  1,  1,  0,  0,  1,  1,  1,  0,  0,129,
        128,  1,  1,  1,  1,  1,  1,  0,  1,  0,  0,  0,  0,  0,  0,129,
        128,  0,  0,  1,  1,  0,  0,  0,  1,  1,  1,  0,  0,  1,  1,129,
        128,  0,  0,  0,  1,  1,  1,  1,  0,  0,  1,  1,  0,  0,  1,129,
        128,  0,129,  1,  0,  0,  1,  1,  1,  1,  1,  1,  0,  0,  0,  0,
        128,  0,129,  0,  0,  0,  1,  0,  1,  1,  1,  0,  1,  1,  1,  0,
        128,  1,  0,  0,  0,  1,  0,  0,  0,  1,  1,  1,  0,  1,  1,129
    };

    private static readonly int[] Partition3Subset =
    {
        //  0   1   2   3   4   5   6   7    8   9   10  11  12  13  14  15
        128,  0,  1,129,  0,  0,  1,  1,  0,  2,  2,  1,  2,  2,  2,130,
        128,  0,  0,129,  0,  0,  1,  1,130,  2,  1,  1,  2,  2,  2,  1,
        128,  0,  0,  0,  2,  0,  0,  1,130,  2,  1,  1,  2,  2,  1,129,
        128,  2,  2,130,  0,  0,  2,  2,  0,  0,  1,  1,  0,  1,  1,129,
        128,  0,  0,  0,  0,  0,  0,  0,129,  1,  2,  2,  1,  1,  2,130,
        128,  0,  1,129,  0,  0,  1,  1,  0,  0,  2,  2,  0,  0,  2,130,
        128,  0,  2,130,  0,  0,  2,  2,  1,  1,  1,  1,  1,  1,  1,129,
        128,  0,  1,  1,  0,  0,  1,  1,130,  2,  1,  1,  2,  2,  1,129,
        128,  0,  0,  0,  0,  0,  0,  0,129,  1,  1,  1,  2,  2,  2,130,
        128,  0,  0,  0,  1,  1,  1,  1,129,  1,  1,  1,  2,  2,  2,130,
        128,  0,  0,  0,  1,  1,129,  1,  2,  2,  2,  2,  2,  2,  2,130,
        128,  0,  1,  2,  0,  0,129,  2,  0,  0,  1,  2,  0,  0,  1,130,
        128,  1,  1,  2,  0,  1,129,  2,  0,  1,  1,  2,  0,  1,  1,130,
        128,  1,  2,  2,  0,129,  2,  2,  0,  1,  2,  2,  0,  1,  2,130,
        128,  0,  1,129,  0,  1,  1,  2,  1,  1,  2,  2,  1,  2,  2,130,
        128,  0,  1,129,  2,  0,  0,  1,130,  2,  0,  0,  2,  2,  2,  0,
        128,  0,  0,129,  0,  0,  1,  1,  0,  1,  1,  2,  1,  1,  2,130,
        128,  1,  1,129,  0,  0,  1,  1,130,  0,  0,  1,  2,  2,  0,  0,
        128,  0,  0,  0,  1,  1,  2,  2,129,  1,  2,  2,  1,  1,  2,130,
        128,  0,  2,130,  0,  0,  2,  2,  0,  0,  2,  2,  1,  1,  1,129,
        128,  1,  1,129,  0,  1,  1,  1,  0,  2,  2,  2,  0,  2,  2,130,
        128,  0,  0,129,  0,  0,  0,  1,130,  2,  2,  1,  2,  2,  2,  1,
        128,  0,  0,  0,  0,  0,129,  1,  0,  1,  2,  2,  0,  1,  2,130,
        128,  0,  0,  0,  1,  1,  0,  0,130,  2,129,  0,  2,  2,  1,  0,
        128,  1,  2,130,  0,129,  2,  2,  0,  0,  1,  1,  0,  0,  0,  0,
        128,  0,  1,  2,  0,  0,  1,  2,129,  1,  2,  2,  2,  2,  2,130,
        128,  1,  1,  0,  1,  2,130,  1,129,  2,  2,  1,  0,  1,  1,  0,
        128,  0,  0,  0,  0,  1,129,  0,  1,  2,130,  1,  1,  2,  2,  1,
        128,  0,  2,  2,  1,  1,  0,  2,129,  1,  0,  2,  0,  0,  2,130,
        128,  1,  1,  0,  0,129,  1,  0,  2,  0,  0,  2,  2,  2,  2,130,
        128,  0,  1,  1,  0,  1,  2,  2,  0,  1,130,  2,  0,  0,  1,129,
        128,  0,  0,  0,  2,  0,  0,  0,130,  2,  1,  1,  2,  2,  2,129,
        128,  0,  0,  0,  0,  0,  0,  2,129,  1,  2,  2,  1,  2,  2,130,
        128,  2,  2,130,  0,  0,  2,  2,  0,  0,  1,  2,  0,  0,  1,129,
        128,  0,  1,129,  0,  0,  1,  2,  0,  0,  2,  2,  0,  2,  2,130,
        128,  1,  2,  0,  0,129,  2,  0,  0,  1,130,  0,  0,  1,  2,  0,
        128,  0,  0,  0,  1,  1,129,  1,  2,  2,130,  2,  0,  0,  0,  0,
        128,  1,  2,  0,  1,  2,  0,  1,130,  0,129,  2,  0,  1,  2,  0,
        128,  1,  2,  0,  2,  0,  1,  2,129,130,  0,  1,  0,  1,  2,  0,
        128,  0,  1,  1,  2,  2,  0,  0,  1,  1,130,  2,  0,  0,  1,129,
        128,  0,  1,  1,  1,  1,130,  2,  2,  2,  0,  0,  0,  0,  1,129,
        128,  1,  0,129,  0,  1,  0,  1,  2,  2,  2,  2,  2,  2,  2,130,
        128,  0,  0,  0,  0,  0,  0,  0,130,  1,  2,  1,  2,  1,  2,129,
        128,  0,  2,  2,  1,129,  2,  2,  0,  0,  2,  2,  1,  1,  2,130,
        128,  0,  2,130,  0,  0,  1,  1,  0,  0,  2,  2,  0,  0,  1,129,
        128,  2,  2,  0,  1,  2,130,  1,  0,  2,  2,  0,  1,  2,  2,129,
        128,  1,  0,  1,  2,  2,130,  2,  2,  2,  2,  2,  0,  1,  0,129,
        128,  0,  0,  0,  2,  1,  2,  1,130,  1,  2,  1,  2,  1,  2,129,
        128,  1,  0,129,  0,  1,  0,  1,  0,  1,  0,  1,  2,  2,  2,130,
        128,  2,  2,130,  0,  1,  1,  1,  0,  2,  2,  2,  0,  1,  1,129,
        128,  0,  0,  2,  1,129,  1,  2,  0,  0,  0,  2,  1,  1,  1,130,
        128,  0,  0,  0,  2,129,  1,  2,  2,  1,  1,  2,  2,  1,  1,130,
        128,  2,  2,  2,  0,129,  1,  1,  0,  1,  1,  1,  0,  2,  2,130,
        128,  0,  0,  2,  1,  1,  1,  2,129,  1,  1,  2,  0,  0,  0,130,
        128,  1,  1,  0,  0,129,  1,  0,  0,  1,  1,  0,  2,  2,  2,130,
        128,  0,  0,  0,  0,  0,  0,  0,  2,  1,129,  2,  2,  1,  1,130,
        128,  1,  1,  0,  0,129,  1,  0,  2,  2,  2,  2,  2,  2,  2,130,
        128,  0,  2,  2,  0,  0,  1,  1,  0,  0,129,  1,  0,  0,  2,130,
        128,  0,  2,  2,  1,  1,  2,  2,129,  1,  2,  2,  0,  0,  2,130,
        128,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  2,129,  1,130,
        128,  0,  0,130,  0,  0,  0,  1,  0,  0,  0,  2,  0,  0,  0,129,
        128,  2,  2,  2,  1,  2,  2,  2,  0,  2,  2,  2,129,  2,  2,130,
        128,  1,  0,129,  2,  2,  2,  2,  2,  2,  2,  2,  2,  2,  2,130,
        128,  1,  1,129,  2,  0,  1,  1,130,  2,  0,  1,  2,  2,  2,  0
    };

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

internal sealed class Bc7BitStream
{
    private ulong _low;
    private ulong _high;

    public Bc7BitStream(ulong low, ulong high)
    {
        _low = low;
        _high = high;
    }

    public int ReadBit() => ReadBits(1);

    public int ReadBits(int numBits)
    {
        int mask = (1 << numBits) - 1;
        int bits = (int)(_low & (uint)mask);

        _low >>= numBits;
        _low |= (_high & (uint)mask) << (64 - numBits);
        _high >>= numBits;

        return bits;
    }
}

public sealed class DdsImage
{
    public int Width { get; init; }
    public int Height { get; init; }
    public byte[] Data { get; init; } = [];
    public bool HasAlpha { get; init; }
}