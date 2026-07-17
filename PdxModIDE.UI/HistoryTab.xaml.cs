using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;
using PdxModIDE.MapEngine;
using PdxModIDE.Rendering;
using PdxModIDE.UI.ViewModels;

namespace PdxModIDE.UI
{
    public partial class HistoryTab : System.Windows.Controls.UserControl
    {
        private MapLoader? _mapLoader;
        private TitleHistoryLoader? _titleHistory;
        private MapRenderer? _renderer;
        private WriteableBitmap? _writeableBmp;
        private bool _isDragging;
        private bool _mapLoaded;
        private System.Windows.Point _lastMousePos;
        private SKBitmap? _renderCache;
        private int _cachedWidth;
        private int _cachedHeight;
        private int _cachedHighlight = -2;
        private float _cachedZoom;
        private float _cachedOffX;
        private float _cachedOffY;
        private bool _renderPending;

        public HistoryTab()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            MapImage.SizeChanged += OnMapSizeChanged;
        }

        public string Mode { get; set; } = "base";

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            IsVisibleChanged += OnIsVisibleChanged;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue && !_mapLoaded)
                TryAutoLoad();
        }

        private void OnMapSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_mapLoaded && _renderer != null)
                QueueRender();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentProfile))
                _mapLoaded = false;
        }

        private void TryAutoLoad()
        {
            if (_mapLoaded) return;
            var profile = ViewModel?.CurrentProfile;
            if (profile == null)
            {
                StatusLabel.Content = "Sin perfil seleccionado";
                return;
            }
            if (string.IsNullOrEmpty(profile.GameRoot) || !Directory.Exists(profile.GameRoot))
            {
                StatusLabel.Content = "Configura un perfil con GameRoot válido";
                return;
            }
            DoLoad(profile.GameRoot, Mode == "mod" ? profile.ModRoot : profile.GameRoot);
        }

        private string? FindFileBase(string relativePath)
        {
            var profile = ViewModel?.CurrentProfile;
            if (profile == null) return null;
            string full = Path.Combine(profile.GameRoot, relativePath);
            return File.Exists(full) ? full : null;
        }

        private string? FindFileMod(string relativePath)
        {
            var profile = ViewModel?.CurrentProfile;
            if (profile == null) return null;
            string modPath = Path.Combine(profile.ModRoot, relativePath);
            if (File.Exists(modPath)) return modPath;
            string gamePath = Path.Combine(profile.GameRoot, relativePath);
            return File.Exists(gamePath) ? gamePath : null;
        }

        private void DoLoad(string gameRoot, string historyRoot)
        {
            try
            {
                string? provincesPng = Mode == "mod" ? FindFileMod("map_data/provinces.png") : FindFileBase("map_data/provinces.png");
                if (provincesPng == null)
                {
                    StatusLabel.Content = "No se encontró provinces.png";
                    return;
                }

                _renderer?.Dispose();
                _renderer = null;
                _mapLoader = null;
                _renderCache?.Dispose();
                _renderCache = null;

                var loader = new MapLoader(gameRoot);
                loader.LoadAll();

                _titleHistory = new TitleHistoryLoader();
                int count = _titleHistory.LoadAll(historyRoot);
                _mapLoader = loader;

                var renderer = new MapRenderer();
                if (!renderer.Load(loader))
                {
                    StatusLabel.Content = "Error al cargar el renderizador";
                    return;
                }

                _renderer = renderer;
                _mapLoaded = true;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_mapLoaded && _renderer != null)
                    {
                        ResetView();
                        QueueRender();
                    }
                }), System.Windows.Threading.DispatcherPriority.Render);
                StatusLabel.Content = Mode == "mod"
                    ? $"Vista: Mod | {loader.ProvincesById.Count} prov, {count} títulos"
                    : $"Vista: Juego Base | {loader.ProvincesById.Count} prov, {count} títulos";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error: {ex.Message}";
                File.AppendAllText("logs/crash.log", $"[{DateTime.Now:HH:mm:ss}] HistoryTab.DoLoad: {ex.Message}\n");
            }
        }

        private void QueueRender()
        {
            if (_renderPending) return;
            _renderPending = true;
            Dispatcher.BeginInvoke(new Action(RenderNow), System.Windows.Threading.DispatcherPriority.Render);
        }

        private (int w, int h) GetViewportPx()
        {
            double dpiScale = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
                dpiScale = source.CompositionTarget.TransformToDevice.M11;
            int vw = Math.Max(1, (int)((ActualWidth - 195) * dpiScale));
            int vh = Math.Max(1, (int)((ActualHeight - 55) * dpiScale));
            return (vw, vh);
        }

        private void RenderNow()
        {
            _renderPending = false;
            if (_renderer == null || !_mapLoaded) return;

            var (vw, vh) = GetViewportPx();

            bool needsRender = _renderCache == null ||
                _cachedWidth != vw ||
                _cachedHeight != vh ||
                _cachedHighlight != _renderer.HighlightProvinceId ||
                _cachedZoom != _renderer.Zoom ||
                _cachedOffX != _renderer.OffsetX ||
                _cachedOffY != _renderer.OffsetY;

            if (needsRender)
            {
                _renderCache?.Dispose();
                _renderCache = _renderer.RenderToBitmap(vw, vh);
                _cachedWidth = vw;
                _cachedHeight = vh;
                _cachedHighlight = _renderer.HighlightProvinceId;
                _cachedZoom = _renderer.Zoom;
                _cachedOffX = _renderer.OffsetX;
                _cachedOffY = _renderer.OffsetY;
            }

            int w = _renderCache.Width;
            int h = _renderCache.Height;

            if (_writeableBmp == null || _writeableBmp.PixelWidth != w || _writeableBmp.PixelHeight != h)
            {
                _writeableBmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
                MapImage.Source = _writeableBmp;
            }

            int stride = w * 4;
            _writeableBmp.WritePixels(
                new Int32Rect(0, 0, w, h),
                _renderCache.GetPixels(),
                stride * h,
                stride);
        }

        private void ResetView()
        {
            if (_renderer == null) return;
            _renderer.ResetView();

            var (vw, vh) = GetViewportPx();

            float zoom = Math.Min(vw / (float)_renderer.Width, vh / (float)_renderer.Height);
            _renderer.ZoomTo(zoom, 0, 0);
            _renderer.Pan((vw - _renderer.Width * zoom) / 2, (vh - _renderer.Height * zoom) / 2);
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e) => ZoomAtCenter(1.15);
        private void ZoomOut_Click(object sender, RoutedEventArgs e) => ZoomAtCenter(0.87);

        private void ZoomAtCenter(double factor)
        {
            if (_renderer == null) return;

            double dpiScale = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
                dpiScale = source.CompositionTarget.TransformToDevice.M11;

            float cx = (float)(MapImage.ActualWidth * dpiScale / 2);
            float cy = (float)(MapImage.ActualHeight * dpiScale / 2);
            _renderer.ZoomTo((float)factor, cx, cy);
            QueueRender();
        }

        private void FitToWindow_Click(object sender, RoutedEventArgs e)
        {
            ResetView();
            QueueRender();
        }

        private void MapImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_renderer == null || !_mapLoaded) return;

            if (e.ChangedButton == MouseButton.Right)
            {
                _isDragging = true;
                _lastMousePos = e.GetPosition(MapImage);
                MapImage.Cursor = System.Windows.Input.Cursors.Hand;
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                var pos = e.GetPosition(MapImage);
                int x = (int)(pos.X * (MapImage.ActualWidth > 0 ? _cachedWidth / MapImage.ActualWidth : 1));
                int y = (int)(pos.Y * (MapImage.ActualHeight > 0 ? _cachedHeight / MapImage.ActualHeight : 1));

                int provinceId = _renderer.GetProvinceAt(x, y);
                if (provinceId > 0 && _mapLoader != null)
                {
                    var province = _mapLoader.GetProvinceFromId(provinceId);
                    if (province != null)
                    {
                        string barony = _mapLoader.GetTitleFromProvinceId(province.Id) ?? "-";
                        string county = barony != "-" ? (_mapLoader.GetCountyFromBarony(barony) ?? "-") : "-";
                        StatusLabel.Content = $"Prov: {province.Id} | {province.Name} | Tipo: {province.Type ?? "?"} | Bar: {barony} | Cond: {county}";
                        UpdateProvinceInfo(provinceId);
                        return;
                    }
                }

                _renderer.HighlightProvinceId = -1;
                _cachedHighlight = _renderer.HighlightProvinceId - 1;
                QueueRender();
                StatusLabel.Content = $"Coords: {x}, {y} - SIN PROVINCIA";
            }
        }

        private void MapImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            MapImage.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private void MapImage_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _isDragging = false;
            MapImage.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private void MapImage_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDragging) return;

            var currentPos = e.GetPosition(MapImage);
            double dx = currentPos.X - _lastMousePos.X;
            double dy = currentPos.Y - _lastMousePos.Y;

            double dpiScale = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
                dpiScale = source.CompositionTarget.TransformToDevice.M11;

            _renderer?.Pan((float)(dx * dpiScale), (float)(dy * dpiScale));
            _lastMousePos = currentPos;
            QueueRender();
        }

        private void MapImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double factor = e.Delta > 0 ? 1.15 : 0.87;
            var pos = e.GetPosition(MapImage);

            double dpiScale = 1.0;
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget != null)
                dpiScale = source.CompositionTarget.TransformToDevice.M11;

            _renderer?.ZoomTo((float)factor, (float)(pos.X * dpiScale), (float)(pos.Y * dpiScale));
            QueueRender();
            e.Handled = true;
        }

        private void HolderModeChanged(object sender, RoutedEventArgs e)
        {
            if (!_mapLoaded || _renderer == null || _mapLoader == null || _titleHistory == null)
                return;

            if (HolderModeCheck.IsChecked == true)
            {
                CountyModeCheck.IsChecked = false; // Mutually exclusive
                ApplyHolderMode();
            }
            else
            {
                _renderer.SetHolderMode(false, null, null);
                _cachedWidth = -1;
                QueueRender();
            }
        }

        private void CountyModeChanged(object sender, RoutedEventArgs e)
        {
            if (!_mapLoaded || _renderer == null || _mapLoader == null)
                return;

            if (CountyModeCheck.IsChecked == true)
            {
                HolderModeCheck.IsChecked = false; // Mutually exclusive
                ApplyCountyMode();
            }
            else
            {
                _renderer.SetHolderMode(false, null, null);
                _cachedWidth = -1;
                QueueRender();
            }
        }

        private void YearBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_mapLoaded || _renderer == null) return;

            if (HolderModeCheck.IsChecked == true)
                ApplyHolderMode();
            else if (CountyModeCheck.IsChecked == true)
                ApplyCountyMode();
        }

        private void ApplyHolderMode()
        {
            if (!int.TryParse(YearBox.Text, out int year)) return;
            var holderLut = _mapLoader!.BuildHolderLut(year, _titleHistory!, out var indexToHolder);
            var palette = MapLoader.BuildHolderPalette(indexToHolder);
            _renderer!.SetHolderMode(true, holderLut, palette);
            StatusLabel.Content = $"Modo Titular — año {year} — {indexToHolder.Count} titulares";
            _cachedWidth = -1;
            QueueRender();
        }

        private void ApplyCountyMode()
        {
            var countyLut = _mapLoader!.BuildCountyLut(out var indexToCounty);
            var palette = MapLoader.BuildCountyPalette(indexToCounty);
            _renderer!.SetHolderMode(true, countyLut, palette);
            StatusLabel.Content = $"Modo Condados — {indexToCounty.Count} condados";
            _cachedWidth = -1;
            QueueRender();
        }

        private void UpdateProvinceInfo(int provinceId)
        {
            try
            {
                if (_mapLoader == null) return;

                var province = _mapLoader.GetProvinceFromId(provinceId);
                if (province == null) return;

                LabelId.Content = $"ID: {province.Id}";
                LabelName.Content = $"Nombre: {province.Name}";
                LabelColor.Content = $"Color: ({province.R},{province.G},{province.B})";
                LabelType.Content = $"Tipo: {province.Type ?? "?"}";

                string barony = _mapLoader.GetTitleFromProvinceId(provinceId) ?? "-";
                LabelBarony.Content = $"Baronía: {barony}";

                string county = "-";
                if (barony != "-")
                    county = _mapLoader.GetCountyFromBarony(barony) ?? "-";
                LabelCounty.Content = $"Condado: {county}";

                if (_titleHistory != null && county != "-" && _titleHistory.AllTitles.TryGetValue(county, out var history))
                {
                    if (int.TryParse(YearBox.Text, out int year))
                    {
                        LabelHolder.Content = $"Holder en {year}: {TitleHistoryLoader.GetHolderAtYear(history, year) ?? "(sin datos)"}";
                        LabelLiege.Content = $"Liege en {year}: {TitleHistoryLoader.GetLiegeAtYear(history, year) ?? "(sin datos)"}";
                    }
                    else
                    {
                        LabelHolder.Content = "Holder: (año inválido)";
                        LabelLiege.Content = "Liege: (año inválido)";
                    }
                }
                else
                {
                    LabelHolder.Content = "Holder: (sin datos)";
                    LabelLiege.Content = "Liege: (sin datos)";
                }

                if (_renderer != null)
                {
                    _renderer.HighlightProvinceId = provinceId;
                    _cachedHighlight = _renderer.HighlightProvinceId - 1;
                    QueueRender();
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Error: {ex.Message}";
                File.AppendAllText("logs/crash.log", $"[{DateTime.Now:HH:mm:ss}] UpdateProvinceInfo: {ex.Message}\n");
            }
        }
    }
}
