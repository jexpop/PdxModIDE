using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;
using PdxModIDE.MapEngine;

namespace PdxModIDE.Rendering
{
    public class MapRenderer : IDisposable
    {
        private SKBitmap? _provincesBitmap;
        private SKImage? _provincesImage;
        private SKImage? _lutImage;
        private SKImage? _paletteImage;
        private byte[]? _holderLutCpu;
        private byte[]? _paletteCpu;
        private MapLoader? _mapLoader;
        private SKRuntimeEffect? _fullEffect;
        private SKRuntimeEffectUniforms? _lastUniforms;
        private SKRuntimeEffectChildren? _lastChildren;
        private int _lastHighlight = -2;
        private bool _holderMode;
        private bool _allowShaderOverlay = true;
        private int _lastHolderYear = -1;

        private float _zoom = 1.0f;
        private float _offsetX;
        private float _offsetY;
        private readonly HashSet<int> _highlightProvinceIds = new HashSet<int>();
        private int _highlightVersion;

        public int Width => _mapLoader?.MapWidth ?? 0;
        public int Height => _mapLoader?.MapHeight ?? 0;
        public float Zoom => _zoom;
        public float OffsetX => _offsetX;
        public float OffsetY => _offsetY;

        public int HighlightProvinceId
        {
            get
            {
                foreach (var id in _highlightProvinceIds)
                    return id;
                return -1;
            }
            set
            {
                _highlightProvinceIds.Clear();
                if (value >= 0)
                    _highlightProvinceIds.Add(value);
                _highlightVersion++;
            }
        }

        public HashSet<int> HighlightProvinceIds => _highlightProvinceIds;
        public int HighlightVersion => _highlightVersion;

        public void SetHighlightProvinces(HashSet<int> ids)
        {
            _highlightProvinceIds.Clear();
            foreach (var id in ids)
                _highlightProvinceIds.Add(id);
            _highlightVersion++;
        }

        private const string ShaderSrc = @"
uniform shader provinces;
uniform shader lut;
uniform shader holderLut;
uniform shader palette;
uniform float3 highlightColor;
uniform float mode;

half4 main(float2 coord) {
    half4 provColor = half4(provinces.eval(coord + 0.5));
    float r = provColor.r * 255.0;
    float g = provColor.g * 255.0;
    float b = provColor.b * 255.0;

    float idx = r * 65536.0 + g * 256.0 + b;
    float lutX = mod(idx, 4096.0);
    float lutY = floor(idx / 4096.0);
    float lutVal = float(lut.eval(float2(lutX + 0.5, lutY + 0.5)).r) * 255.0;

    half4 color;
    if (mode > 0.5) {
        float holderIdx = float(holderLut.eval(float2(lutX + 0.5, lutY + 0.5)).r) * 255.0;
        if (holderIdx < 0.5) {
            // Sin datos de titular/mod/base: tierra gris, mar azul (igual criterio que el mapa por defecto)
            if (lutVal < 0.5) color = half4(120.0 / 255.0, 120.0 / 255.0, 120.0 / 255.0, 1);
            else if (lutVal < 3.5) color = half4(80.0 / 255.0, 120.0 / 255.0, 255.0 / 255.0, 1);
            else if (lutVal < 4.5) color = half4(90.0 / 255.0, 90.0 / 255.0, 90.0 / 255.0, 1);
            else color = half4(40.0 / 255.0, 40.0 / 255.0, 40.0 / 255.0, 1);
        } else {
            color = palette.eval(float2(holderIdx + 0.5, 0.5));
        }
    } else {
        if (lutVal < 0.5) color = half4(235.0 / 255.0, 180.0 / 255.0, 60.0 / 255.0, 1);
        else if (lutVal < 1.5) color = half4(80.0 / 255.0, 120.0 / 255.0, 255.0 / 255.0, 1);
        else if (lutVal < 2.5) color = half4(60.0 / 255.0, 100.0 / 255.0, 230.0 / 255.0, 1);
        else if (lutVal < 3.5) color = half4(100.0 / 255.0, 150.0 / 255.0, 255.0 / 255.0, 1);
        else if (lutVal < 4.5) color = half4(120.0 / 255.0, 120.0 / 255.0, 120.0 / 255.0, 1);
        else color = half4(0, 0, 0, 1);
    }

    half4 leftP = half4(provinces.eval(coord + 0.5 - float2(1.0, 0)));
    if (abs(provColor.r - leftP.r) > 0.001 ||
        abs(provColor.g - leftP.g) > 0.001 ||
        abs(provColor.b - leftP.b) > 0.001)
        return half4(0.1, 0.1, 0.1, 1);

    half4 topP = half4(provinces.eval(coord + 0.5 - float2(0, 1.0)));
    if (abs(provColor.r - topP.r) > 0.001 ||
        abs(provColor.g - topP.g) > 0.001 ||
        abs(provColor.b - topP.b) > 0.001)
        return half4(0.1, 0.1, 0.1, 1);

    if (highlightColor.x >= 0.0) {
        if (abs(r - highlightColor.x) < 0.5 &&
            abs(g - highlightColor.y) < 0.5 &&
            abs(b - highlightColor.z) < 0.5)
            return half4(1, 1, 0, 1);
    }

    return color;
}
";

        public bool Load(MapLoader loader)
        {
            _mapLoader = loader;

            if (loader.ProvincesPngPath == null || !File.Exists(loader.ProvincesPngPath))
                return false;

            _provincesBitmap = LoadBitmap(loader.ProvincesPngPath);
            if (_provincesBitmap == null)
                return false;

            _provincesImage = SKImage.FromBitmap(_provincesBitmap);
            _lutImage = BuildLutImage(loader.Lut, out _);
            if (_lutImage == null)
                return false;

            InitShader();
            BuildShaderCache();
            return true;
        }

        private static SKBitmap? LoadBitmap(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var codec = SKCodec.Create(stream);
                var info = codec.Info;
                var bitmap = new SKBitmap(info.Width, info.Height);
                codec.GetPixels(bitmap.Info, bitmap.GetPixels());
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private static SKImage? BuildLutImage(byte[]? lut, out SKBitmap? backingBitmap)
        {
            backingBitmap = null;
            if (lut == null || lut.Length != 16_777_216) return null;

            var info = new SKImageInfo(4096, 4096, SKColorType.Rgba8888, SKAlphaType.Opaque);
            var bitmap = new SKBitmap(info);
            IntPtr ptr = bitmap.GetPixels();
            byte[] rgba = new byte[4096 * 4096 * 4];

            for (int i = 0; i < 16_777_216; i++)
            {
                int off = i * 4;
                rgba[off] = lut[i];
                rgba[off + 1] = lut[i];
                rgba[off + 2] = lut[i];
                rgba[off + 3] = 255;
            }

            Marshal.Copy(rgba, 0, ptr, rgba.Length);
            backingBitmap = bitmap;
            return SKImage.FromBitmap(bitmap);
        }

        private void InitShader()
        {
            _fullEffect = SKRuntimeEffect.CreateShader(ShaderSrc, out var errors);
            if (_fullEffect == null)
                throw new InvalidOperationException($"Shader compilation failed: {errors}");
        }

        private void BuildShaderCache()
        {
            if (_fullEffect == null || _provincesImage == null || _lutImage == null) return;

            var nearest = new SKSamplingOptions(SKFilterMode.Nearest);
            _lastChildren = new SKRuntimeEffectChildren(_fullEffect);
            _lastChildren["provinces"] = SKShader.CreateImage(_provincesImage, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, nearest);
            _lastChildren["lut"] = SKShader.CreateImage(_lutImage, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, nearest);

            _lastChildren["holderLut"] = SKShader.CreateColor(SKColors.Black);
            _lastChildren["palette"] = SKShader.CreateColor(SKColors.Black);

            _lastUniforms = new SKRuntimeEffectUniforms(_fullEffect);
            _lastUniforms["mode"] = 0f;
        }

        public void SetHolderMode(bool enabled, byte[]? holderLutData, SKImage? palette)
        {
            _holderMode = enabled;

            if (holderLutData != null)
                _holderLutCpu = holderLutData;

            if (palette != null)
            {
                _paletteImage?.Dispose();
                _paletteImage = palette;
                // Decode palette to CPU array (RGBA order, 4 bytes per entry, 256 entries)
                _paletteCpu = new byte[256 * 4];
                using var tmpBmp = SKBitmap.FromImage(_paletteImage);
                for (int i = 0; i < 256; i++)
                {
                    var c = tmpBmp.GetPixel(i, 0);
                    int off = i * 4;
                    _paletteCpu[off] = c.Red;
                    _paletteCpu[off + 1] = c.Green;
                    _paletteCpu[off + 2] = c.Blue;
                    _paletteCpu[off + 3] = c.Alpha;
                }
            }
        }

        public void Render(SKCanvas canvas, int width, int height)
        {
            if (_fullEffect == null || _lastChildren == null || _lastUniforms == null)
                return;

            canvas.Clear(SKColors.Black);
            canvas.Save();
            canvas.Translate(_offsetX, _offsetY);
            canvas.Scale(_zoom);

            if (_lastHighlight != _highlightVersion)
            {
                _lastHighlight = _highlightVersion;
                if (_highlightProvinceIds.Count == 1)
                {
                    int singleId = HighlightProvinceId;
                    if (singleId >= 0 && _mapLoader?.ProvinceIdToPacked.TryGetValue(singleId, out var packed) == true)
                        _lastUniforms["highlightColor"] = new SKPoint3((packed >> 16) & 0xFF, (packed >> 8) & 0xFF, packed & 0xFF);
                    else
                        _lastUniforms["highlightColor"] = new SKPoint3(-1, -1, -1);
                }
                else
                    _lastUniforms["highlightColor"] = new SKPoint3(-1, -1, -1);
            }

            _lastUniforms["mode"] = _holderMode && _allowShaderOverlay ? 1f : 0f;

            using var shader = _fullEffect.ToShader(_lastUniforms, _lastChildren);
            using var paint = new SKPaint { Shader = shader };

            float left = Math.Max(0, -_offsetX / _zoom);
            float top = Math.Max(0, -_offsetY / _zoom);
            float right = Math.Min(_provincesBitmap!.Width, (width - _offsetX) / _zoom);
            float bottom = Math.Min(_provincesBitmap.Height, (height - _offsetY) / _zoom);

            if (left < right && top < bottom)
                canvas.DrawRect(left, top, right - left, bottom - top, paint);
            canvas.Restore();
        }

        public SKBitmap RenderToBitmap(int viewWidth, int viewHeight)
        {
            var info = new SKImageInfo(viewWidth, viewHeight);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;

            // Always render terrain (mode=0) via shader — this gives us borders and terrain
            _allowShaderOverlay = false;
            Render(canvas, viewWidth, viewHeight);
            _allowShaderOverlay = true;

            using var terrainImage = surface.Snapshot();
            var terrainBmp = SKBitmap.FromImage(terrainImage);

            // CPU overlay: for each output pixel, lookup province color → holderIdx → palette
            if (_holderMode && _holderLutCpu != null && _paletteCpu != null && _provincesBitmap != null)
            {
                int w = terrainBmp.Width;
                int h = terrainBmp.Height;

                // Read provinces bitmap pixel data via GetPixels
                int pw = _provincesBitmap.Width;
                int ph = _provincesBitmap.Height;
                int provRowBytes = _provincesBitmap.RowBytes;
                IntPtr provBase = _provincesBitmap.GetPixels();
                int provBpp = _provincesBitmap.BytesPerPixel;

                // Read terrain bitmap via GetPixels
                int outRowBytes = terrainBmp.RowBytes;
                IntPtr outBase = terrainBmp.GetPixels();
                int outBpp = terrainBmp.BytesPerPixel;

                // Determine byte offsets based on provinces bitmap format
                int prB, prG, prR;
                if (_provincesBitmap.ColorType == SKColorType.Rgba8888)
                {
                    prR = 0; prG = 1; prB = 2;
                }
                else
                {
                    // Default: BGRA8888 (Windows)
                    prR = 2; prG = 1; prB = 0;
                }

                // Determine output byte offsets: Skia on Windows uses BGRA
                int rOff = 2;
                int gOff = 1;
                int bOff = 0;

                byte[] outRow = new byte[outRowBytes];

                for (int y = 0; y < h; y++)
                {
                    // Read output row
                    Marshal.Copy(outBase + y * outRowBytes, outRow, 0, outRowBytes);

                    for (int x = 0; x < w; x++)
                    {
                        int po = x * outBpp;

                        // Check if border (dark gray ~RGB 25,25,25) or highlight (yellow)
                        byte pr = outRow[po + rOff];
                        byte pg = outRow[po + gOff];
                        byte pb = outRow[po + bOff];
                        if (pr < 30 && pg < 30 && pb < 30 && pr > 20)
                            continue;
                        if (pr == 255 && pg == 255 && pb == 0)
                            continue;

                        // Map output pixel to provinces image coordinates
                        int provX = (int)((x - _offsetX) / _zoom + 0.5f);
                        int provY = (int)((y - _offsetY) / _zoom + 0.5f);

                        if (provX < 0 || provX >= pw || provY < 0 || provY >= ph)
                            continue;

                        // Read province pixel at (provX, provY)
                        IntPtr provPixelPtr = provBase + provY * provRowBytes + provX * provBpp;
                        byte provB = Marshal.ReadByte(provPixelPtr + prB);
                        byte provG = Marshal.ReadByte(provPixelPtr + prG);
                        byte provR = Marshal.ReadByte(provPixelPtr + prR);

                        int idx = (provR << 16) | (provG << 8) | provB;
                        byte holderIdx = _holderLutCpu[idx];
                        if (holderIdx > 0)
                        {
                            int palOff = holderIdx * 4;
                            outRow[po + rOff] = _paletteCpu[palOff];
                            outRow[po + gOff] = _paletteCpu[palOff + 1];
                            outRow[po + bOff] = _paletteCpu[palOff + 2];
                        }
                    }

                    // Write modified row back
                    Marshal.Copy(outRow, 0, outBase + y * outRowBytes, outRowBytes);
                }
            }

            if (_highlightProvinceIds.Count > 1 && _mapLoader?.ProvinceIdMap != null && _provincesBitmap != null)
            {
                int[] idMap = _mapLoader.ProvinceIdMap;
                int mapW = _mapLoader.MapWidth;

                int outRowBytes = terrainBmp.RowBytes;
                IntPtr outBase = terrainBmp.GetPixels();
                int outBpp = terrainBmp.BytesPerPixel;

                int rOff = 2, gOff = 1, bOff = 0;

                int w = terrainBmp.Width;
                int h = terrainBmp.Height;
                byte[] outRow = new byte[outRowBytes];

                for (int y = 0; y < h; y++)
                {
                    Marshal.Copy(outBase + y * outRowBytes, outRow, 0, outRowBytes);

                    for (int x = 0; x < w; x++)
                    {
                        int po = x * outBpp;

                        byte pr = outRow[po + rOff];
                        byte pg = outRow[po + gOff];
                        byte pb = outRow[po + bOff];
                        if (pr < 30 && pg < 30 && pb < 30 && pr > 20)
                            continue;
                        if (pr == 255 && pg == 255 && pb == 0)
                            continue;

                        int provX = (int)((x - _offsetX) / _zoom + 0.5f);
                        int provY = (int)((y - _offsetY) / _zoom + 0.5f);

                        if (provX < 0 || provX >= _provincesBitmap.Width || provY < 0 || provY >= _provincesBitmap.Height)
                            continue;

                        int pid = idMap[provY * mapW + provX];
                        if (pid > 0 && _highlightProvinceIds.Contains(pid))
                        {
                            outRow[po + rOff] = 255;
                            outRow[po + gOff] = 255;
                            outRow[po + bOff] = 0;
                        }
                    }

                    Marshal.Copy(outRow, 0, outBase + y * outRowBytes, outRowBytes);
                }
            }

            return terrainBmp;
        }

        public int GetProvinceAt(int x, int y)
        {
            if (_mapLoader?.ProvinceIdMap == null) return -1;
            int mx = (int)((x - _offsetX) / _zoom);
            int my = (int)((y - _offsetY) / _zoom);
            if (mx < 0 || my < 0 || mx >= Width || my >= Height)
                return -1;
            return _mapLoader.ProvinceIdMap[my * Width + mx];
        }

        public void Pan(float deltaX, float deltaY)
        {
            _offsetX += deltaX;
            _offsetY += deltaY;
        }

        public void ZoomTo(float factor, float centerX, float centerY)
        {
            float oldZoom = _zoom;
            _zoom = Math.Clamp(_zoom * factor, 0.1f, 10f);
            float actualFactor = _zoom / oldZoom;
            _offsetX = centerX - actualFactor * (centerX - _offsetX);
            _offsetY = centerY - actualFactor * (centerY - _offsetY);
        }

        public void ResetView()
        {
            _zoom = 1.0f;
            _offsetX = 0;
            _offsetY = 0;
        }

        public void Dispose()
        {
            _provincesBitmap?.Dispose();
            _provincesImage?.Dispose();
            _lutImage?.Dispose();
            _paletteImage?.Dispose();
            _fullEffect?.Dispose();
        }
    }
}
