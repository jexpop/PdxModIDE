using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PdxModIDE.UI.Translation
{
    public interface ITranslationProvider
    {
        string Id { get; }
        Task<(string? Text, bool Ok)> TranslateAsync(string text, string sourceCode, string targetCode);
    }

    public static class TranslationProviderConstants
    {
        public const string MyMemory = "mymemory";
        public const string LibreTranslate = "libretranslate";
        public const string Lingva = "lingva";
        public const string DeepL = "deepl";

        public const string DefaultLibreTranslateUrl = "https://libretranslate.pussthecat.org";
        public const string DefaultLingvaUrl = "https://lingva.ml";
    }

    public sealed class MyMemoryProvider : ITranslationProvider
    {
        private readonly HttpClient _http;
        public string Id => TranslationProviderConstants.MyMemory;

        public MyMemoryProvider(HttpClient http) => _http = http;

        public async Task<(string? Text, bool Ok)> TranslateAsync(string text, string sourceCode, string targetCode)
        {
            if (string.IsNullOrWhiteSpace(text)) return (text, true);
            try
            {
                string url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair={sourceCode}|{targetCode}&mt=1";
                using var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode) return (text, false);
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("responseStatus", out var status) && status.GetInt32() != 200)
                    return (text, false);
                if (doc.RootElement.TryGetProperty("responseData", out var rd)
                    && rd.TryGetProperty("translatedText", out var tt))
                {
                    var result = tt.GetString();
                    if (string.IsNullOrWhiteSpace(result)
                        || result.Contains("MYMEMORY", StringComparison.OrdinalIgnoreCase)
                        || result.Contains("QUOTA", StringComparison.OrdinalIgnoreCase))
                        return (text, false);
                    return (result, true);
                }
                return (text, false);
            }
            catch
            {
                return (text, false);
            }
        }
    }

    public sealed class LibreTranslateProvider : ITranslationProvider
    {
        private readonly HttpClient _http;
        private readonly string _base;
        public string Id => TranslationProviderConstants.LibreTranslate;

        public LibreTranslateProvider(HttpClient http, string baseUrl)
        {
            _http = http;
            _base = (baseUrl ?? "").Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(_base)) _base = TranslationProviderConstants.DefaultLibreTranslateUrl;
        }

        public async Task<(string? Text, bool Ok)> TranslateAsync(string text, string sourceCode, string targetCode)
        {
            if (string.IsNullOrWhiteSpace(text)) return (text, true);
            try
            {
                string src = Map(sourceCode);
                string tgt = Map(targetCode);
                var body = new { q = text, source = src, target = tgt, format = "text" };
                using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                using var response = await _http.PostAsync($"{_base}/translate", content);
                if (!response.IsSuccessStatusCode) return (text, false);
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("translatedText", out var tt))
                {
                    var result = tt.GetString();
                    if (string.IsNullOrWhiteSpace(result)) return (text, false);
                    return (result, true);
                }
                return (text, false);
            }
            catch
            {
                return (text, false);
            }
        }

        private static string Map(string code) => code.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ? "zh" : code.ToLowerInvariant();
    }

    public sealed class LingvaProvider : ITranslationProvider
    {
        private readonly HttpClient _http;
        private readonly string _base;
        public string Id => TranslationProviderConstants.Lingva;

        public LingvaProvider(HttpClient http, string baseUrl)
        {
            _http = http;
            _base = (baseUrl ?? "").Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(_base)) _base = TranslationProviderConstants.DefaultLingvaUrl;
        }

        public async Task<(string? Text, bool Ok)> TranslateAsync(string text, string sourceCode, string targetCode)
        {
            if (string.IsNullOrWhiteSpace(text)) return (text, true);
            try
            {
                string src = Map(sourceCode);
                string tgt = Map(targetCode);
                string url = $"{_base}/api/v1/{src}/{tgt}/{Uri.EscapeDataString(text)}";
                using var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode) return (text, false);
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("translation", out var tr))
                {
                    var result = tr.GetString();
                    if (string.IsNullOrWhiteSpace(result)) return (text, false);
                    return (result, true);
                }
                return (text, false);
            }
            catch
            {
                return (text, false);
            }
        }

        private static string Map(string code) => code.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ? "zh" : code.ToLowerInvariant();
    }

    public sealed class DeepLProvider : ITranslationProvider
    {
        private const string Endpoint = "https://api-free.deepl.com/v2/translate";
        private readonly HttpClient _http;
        private readonly string _key;
        public string Id => TranslationProviderConstants.DeepL;

        public DeepLProvider(HttpClient http, string key)
        {
            _http = http;
            _key = key ?? "";
        }

        public async Task<(string? Text, bool Ok)> TranslateAsync(string text, string sourceCode, string targetCode)
        {
            if (string.IsNullOrWhiteSpace(text)) return (text, true);
            if (string.IsNullOrWhiteSpace(_key)) return (text, false);
            try
            {
                var values = new Dictionary<string, string>
                {
                    ["text"] = text,
                    ["source_lang"] = Map(sourceCode),
                    ["target_lang"] = Map(targetCode)
                };
                using var content = new FormUrlEncodedContent(values);
                using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
                request.Headers.Add("Authorization", $"DeepL-Auth-Key {_key}");
                using var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode) return (text, false);
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("translations", out var arr) && arr.GetArrayLength() > 0)
                {
                    var result = arr[0].GetProperty("text").GetString();
                    if (string.IsNullOrWhiteSpace(result)) return (text, false);
                    return (result, true);
                }
                return (text, false);
            }
            catch
            {
                return (text, false);
            }
        }

        public static async Task<bool> ValidateKeyAsync(HttpClient http, string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            try
            {
                var values = new Dictionary<string, string> { ["text"] = "hello", ["target_lang"] = "ES" };
                using var content = new FormUrlEncodedContent(values);
                using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint) { Content = content };
                request.Headers.Add("Authorization", $"DeepL-Auth-Key {key}");
                using var response = await http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static string Map(string code) => code.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) ? "ZH" : code.ToUpperInvariant();
    }
}
