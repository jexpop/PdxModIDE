using System;
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
        private SKImage? _holderLutImage;
        private SKImage? _paletteImage;
        private MapLoader? _mapLoader;
        private SKRuntimeEffect? _fullEffect;
        private SKRuntimeEffectUniforms? _lastUniforms;
        private SKRuntimeEffectChildren? _lastChildren;
        private int _lastHighlight = -2;
        private bool _holderMode;
        private int _lastHolderYear = -1;

        private float _zoom = 1.0f;
        private float _offsetX;
        private float _offsetY;
        private int _highlightProvinceId = -1;

        public int Width => _mapLoader?.MapWidth ?? 0;
        public int Height => _mapLoader?.MapHeight ?? 0;
        public float Zoom => _zoom;
        public float OffsetX => _offsetX;
        public float OffsetY => _offsetY;
        public int HighlightProvinceId { get => _highlightProvinceId; set => _highlightProvinceId = value; }

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
        color = palette.eval(float2(holderIdx + 0.5, 0.5));
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
            _lutImage = BuildLutImage(loader.Lut);
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

        private static SKImage? BuildLutImage(byte[]? lut)
        {
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

            _lastChildren["holderLut"] = _holderLutImage != null
                ? SKShader.CreateImage(_holderLutImage, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, nearest)
                : SKShader.CreateColor(SKColors.Black);
            _lastChildren["palette"] = _paletteImage != null
                ? SKShader.CreateImage(_paletteImage, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, nearest)
                : SKShader.CreateColor(SKColors.Black);

            _lastUniforms = new SKRuntimeEffectUniforms(_fullEffect);
            _lastUniforms["mode"] = 0f;
        }

        public void SetHolderMode(bool enabled, byte[]? holderLutData, SKImage? palette)
        {
            _holderMode = enabled;

            if (holderLutData != null)
            {
                _holderLutImage?.Dispose();
                _holderLutImage = BuildLutImage(holderLutData);
            }

            if (palette != null)
            {
                _paletteImage?.Dispose();
                _paletteImage = palette;
            }

            if (_lastChildren != null)
            {
                var nearest = new SKSamplingOptions(SKFilterMode.Nearest);
                _lastChildren["holderLut"] = _holderLutImage != null
                    ? SKShader.CreateImage(_holderLutImage, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, nearest)
                    : SKShader.CreateColor(SKColors.Black);
                _lastChildren["palette"] = _paletteImage != null
                    ? SKShader.CreateImage(_paletteImage, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, nearest)
                    : SKShader.CreateColor(SKColors.Black);
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

            if (_highlightProvinceId != _lastHighlight)
            {
                _lastHighlight = _highlightProvinceId;
                if (_highlightProvinceId >= 0 && _mapLoader?.ProvinceIdToPacked.TryGetValue(_highlightProvinceId, out var packed) == true)
                    _lastUniforms["highlightColor"] = new SKPoint3((packed >> 16) & 0xFF, (packed >> 8) & 0xFF, packed & 0xFF);
                else
                    _lastUniforms["highlightColor"] = new SKPoint3(-1, -1, -1);
            }

            _lastUniforms["mode"] = _holderMode ? 1f : 0f;

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
            Render(surface.Canvas, viewWidth, viewHeight);
            using var image = surface.Snapshot();
            return SKBitmap.FromImage(image);
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
            _holderLutImage?.Dispose();
            _paletteImage?.Dispose();
            _fullEffect?.Dispose();
        }
    }
}
