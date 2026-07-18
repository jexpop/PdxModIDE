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
        private TitleHistoryLoader? _titleHistoryBase;
        private TitleHistoryLoader? _titleHistoryMod;
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
            TitleModePanel.Visibility = Visibility.Collapsed;
        }

        public string Mode { get; set; } = "base";

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            IsVisibleChanged += OnIsVisibleChanged;
            UpdateOffsetLabel();
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
            if (e.PropertyName == nameof(MainViewModel.CurrentProfile) || e.PropertyName == nameof(MainViewModel.YearOffset))
                UpdateOffsetLabel();
        }

        private void UpdateOffsetLabel()
        {
            if (OffsetLabel == null) return;
            var profile = ViewModel?.CurrentProfile;
            if (profile == null)
            {
                OffsetLabel.Content = "Fecha Mod: -";
                return;
            }
            if (int.TryParse(YearBox.Text, out int year))
                OffsetLabel.Content = $"Fecha Mod: {year + profile.YearOffset}";
            else
                OffsetLabel.Content = "Fecha Mod: -";
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
            DoLoad(profile.GameRoot, profile.ModRoot);
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

        private void DoLoad(string gameRoot, string modRoot)
        {
            try
            {
                string? provincesPng = FindFileBase("map_data/provinces.png");
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

                _titleHistoryBase = new TitleHistoryLoader();
                int baseCount = _titleHistoryBase.LoadAll(gameRoot);

                int modCount = 0;
                if (!string.IsNullOrEmpty(modRoot) && Directory.Exists(modRoot))
                {
                    _titleHistoryMod = new TitleHistoryLoader();
                    modCount = _titleHistoryMod.LoadAll(modRoot, overwriteDuplicates: true);
                }
                else
                {
                    _titleHistoryMod = null;
                }

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
                        ApplySourceStructure();
                        if (HasActiveSource())
                            ReapplyActiveMode();
                        QueueRender();
                    }
                }), System.Windows.Threading.DispatcherPriority.Render);
                StatusLabel.Content = $"{loader.ProvincesById.Count} prov, {baseCount} títulos base, {modCount} títulos mod";
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

        private bool HasActiveSource() =>
            BaseSourceCheck?.IsChecked == true || ModSourceCheck?.IsChecked == true;

        private void UpdateTitleModeVisibility()
        {
            bool visible = HasActiveSource();
            TitleModePanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ApplySourceStructure()
        {
            if (_mapLoader == null) return;
            if (ModSourceCheck?.IsChecked == true)
            {
                var modRoot = ViewModel?.CurrentProfile?.ModRoot;
                if (!string.IsNullOrEmpty(modRoot))
                    _mapLoader.LoadModLandedTitles(modRoot);
            }
            else
            {
                _mapLoader.ResetToBase();
            }
        }

        private void EnsureAtLeastOneMode()
        {
            if (!HasActiveSource()) return;
            if (HolderModeCheck.IsChecked == true ||
                CountyModeCheck.IsChecked == true ||
                DuchyModeCheck.IsChecked == true ||
                KingdomModeCheck.IsChecked == true ||
                EmpireModeCheck.IsChecked == true)
                return;

            HolderModeCheck.IsChecked = true;
        }

        private void SourceModeChanged(object sender, RoutedEventArgs e)
        {
            UpdateTitleModeVisibility();
            ApplySourceStructure();

            if (HasActiveSource())
            {
                if (HolderModeCheck.IsChecked != true &&
                    CountyModeCheck.IsChecked != true &&
                    DuchyModeCheck.IsChecked != true &&
                    KingdomModeCheck.IsChecked != true &&
                    EmpireModeCheck.IsChecked != true)
                    HolderModeCheck.IsChecked = true;
            }

            if (!_mapLoaded || _renderer == null) return;
            ReapplyActiveMode();
        }

        private void ReapplyActiveMode()
        {
            if (HolderModeCheck?.IsChecked == true) ApplyHolderMode();
            else if (CountyModeCheck?.IsChecked == true) ApplyCountyMode();
            else if (DuchyModeCheck?.IsChecked == true) ApplyDuchyMode();
            else if (KingdomModeCheck?.IsChecked == true) ApplyKingdomMode();
            else if (EmpireModeCheck?.IsChecked == true) ApplyEmpireMode();
            else
            {
                _renderer!.SetHolderMode(false, null, null);
                _cachedWidth = -1;
                QueueRender();
            }
        }

        private void HolderModeChanged(object sender, RoutedEventArgs e)
        {
            if (!_mapLoaded || _renderer == null || _mapLoader == null)
                return;

            if (HolderModeCheck.IsChecked == true)
            {
                CountyModeCheck.IsChecked = false;
                DuchyModeCheck.IsChecked = false;
                KingdomModeCheck.IsChecked = false;
                EmpireModeCheck.IsChecked = false;
                ApplyHolderMode();
            }
            else
            {
                EnsureAtLeastOneMode();
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
                HolderModeCheck.IsChecked = false;
                DuchyModeCheck.IsChecked = false;
                KingdomModeCheck.IsChecked = false;
                EmpireModeCheck.IsChecked = false;
                ApplyCountyMode();
            }
            else
            {
                EnsureAtLeastOneMode();
                _renderer.SetHolderMode(false, null, null);
                _cachedWidth = -1;
                QueueRender();
            }
        }

        private void DuchyModeChanged(object sender, RoutedEventArgs e)
        {
            if (!_mapLoaded || _renderer == null || _mapLoader == null)
                return;

            if (DuchyModeCheck.IsChecked == true)
            {
                HolderModeCheck.IsChecked = false;
                CountyModeCheck.IsChecked = false;
                KingdomModeCheck.IsChecked = false;
                EmpireModeCheck.IsChecked = false;
                ApplyDuchyMode();
            }
            else
            {
                EnsureAtLeastOneMode();
                _renderer.SetHolderMode(false, null, null);
                _cachedWidth = -1;
                QueueRender();
            }
        }

        private void KingdomModeChanged(object sender, RoutedEventArgs e)
        {
            if (!_mapLoaded || _renderer == null || _mapLoader == null)
                return;

            if (KingdomModeCheck.IsChecked == true)
            {
                HolderModeCheck.IsChecked = false;
                CountyModeCheck.IsChecked = false;
                DuchyModeCheck.IsChecked = false;
                EmpireModeCheck.IsChecked = false;
                ApplyKingdomMode();
            }
            else
            {
                EnsureAtLeastOneMode();
                _renderer.SetHolderMode(false, null, null);
                _cachedWidth = -1;
                QueueRender();
            }
        }

        private void EmpireModeChanged(object sender, RoutedEventArgs e)
        {
            if (!_mapLoaded || _renderer == null || _mapLoader == null)
                return;

            if (EmpireModeCheck.IsChecked == true)
            {
                HolderModeCheck.IsChecked = false;
                CountyModeCheck.IsChecked = false;
                DuchyModeCheck.IsChecked = false;
                KingdomModeCheck.IsChecked = false;
                ApplyEmpireMode();
            }
            else
            {
                EnsureAtLeastOneMode();
                _renderer.SetHolderMode(false, null, null);
                _cachedWidth = -1;
                QueueRender();
            }
        }

        private void YearBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateOffsetLabel();
            if (!_mapLoaded || _renderer == null) return;

            if (HolderModeCheck.IsChecked == true)
                ApplyHolderMode();
            else if (CountyModeCheck.IsChecked == true)
                ApplyCountyMode();
            else if (DuchyModeCheck.IsChecked == true)
                ApplyDuchyMode();
            else if (KingdomModeCheck.IsChecked == true)
                ApplyKingdomMode();
            else if (EmpireModeCheck.IsChecked == true)
                ApplyEmpireMode();
        }

        private void ApplyHolderMode()
        {
            if (!int.TryParse(YearBox.Text, out int year)) return;

            if (!HasActiveSource())
            {
                _renderer!.SetHolderMode(false, null, null);
                StatusLabel.Content = "Activa \"Base\" y/o \"Mod\" para ver datos de titulares";
                _cachedWidth = -1;
                QueueRender();
                return;
            }

            bool useBase = BaseSourceCheck?.IsChecked == true;
            bool useMod = ModSourceCheck?.IsChecked == true;
            int offset = ViewModel?.CurrentProfile?.YearOffset ?? 0;

            int? baseYear = useBase ? year : (int?)null;
            int? modYear = useMod ? year + offset : (int?)null;

            var holderLut = _mapLoader!.BuildCombinedHolderLut(
                baseYear, useBase ? _titleHistoryBase : null,
                modYear, useMod ? _titleHistoryMod : null,
                out var indexToHolder);
            var palette = MapLoader.BuildHolderPalette(indexToHolder);
            _renderer!.SetHolderMode(true, holderLut, palette);

            string fuente = useBase && useMod ? "Mod+Base" : useMod ? "Mod" : "Base";
            StatusLabel.Content = $"Modo Titular [{fuente}] — año {year} — {indexToHolder.Count} titulares";
            _cachedWidth = -1;
            QueueRender();
        }

        private void ApplyCountyMode()
        {
            if (!HasActiveSource())
            {
                _renderer!.SetHolderMode(false, null, null);
                StatusLabel.Content = "Activa \"Base\" y/o \"Mod\" para ver datos de condados";
                _cachedWidth = -1;
                QueueRender();
                return;
            }
            var countyLut = _mapLoader!.BuildCountyLut(out var indexToCounty);
            var palette = MapLoader.BuildCountyPalette(indexToCounty);
            _renderer!.SetHolderMode(true, countyLut, palette);
            StatusLabel.Content = $"Modo Condados — {indexToCounty.Count} condados";
            _cachedWidth = -1;
            QueueRender();
        }

        private void ApplyDuchyMode()
        {
            if (!HasActiveSource())
            {
                _renderer!.SetHolderMode(false, null, null);
                StatusLabel.Content = "Activa \"Base\" y/o \"Mod\" para ver datos de ducados";
                _cachedWidth = -1;
                QueueRender();
                return;
            }
            var duchyLut = _mapLoader!.BuildDuchyLut(out var indexToDuchy);
            var palette = MapLoader.BuildDuchyPalette(indexToDuchy);
            _renderer!.SetHolderMode(true, duchyLut, palette);
            StatusLabel.Content = $"Modo Ducados — {indexToDuchy.Count} ducados";
            _cachedWidth = -1;
            QueueRender();
        }

        private void ApplyKingdomMode()
        {
            if (!HasActiveSource())
            {
                _renderer!.SetHolderMode(false, null, null);
                StatusLabel.Content = "Activa \"Base\" y/o \"Mod\" para ver datos de reinos";
                _cachedWidth = -1;
                QueueRender();
                return;
            }
            var kingdomLut = _mapLoader!.BuildKingdomLut(out var indexToKingdom);
            var palette = MapLoader.BuildKingdomPalette(indexToKingdom);
            _renderer!.SetHolderMode(true, kingdomLut, palette);
            StatusLabel.Content = $"Modo Reinos — {indexToKingdom.Count} reinos";
            _cachedWidth = -1;
            QueueRender();
        }

        private void ApplyEmpireMode()
        {
            if (!HasActiveSource())
            {
                _renderer!.SetHolderMode(false, null, null);
                StatusLabel.Content = "Activa \"Base\" y/o \"Mod\" para ver datos de imperios";
                _cachedWidth = -1;
                QueueRender();
                return;
            }
            var empireLut = _mapLoader!.BuildEmpireLut(out var indexToEmpire);
            var palette = MapLoader.BuildEmpirePalette(indexToEmpire);
            _renderer!.SetHolderMode(true, empireLut, palette);
            StatusLabel.Content = $"Modo Imperios — {indexToEmpire.Count} imperios";
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

                if (county != "-" && int.TryParse(YearBox.Text, out int year))
                {
                    bool useBase = BaseSourceCheck?.IsChecked == true;
                    bool useMod = ModSourceCheck?.IsChecked == true;
                    int offset = ViewModel?.CurrentProfile?.YearOffset ?? 0;

                    string? holder = null, liege = null, fuente = null;

                    if (useMod && _titleHistoryMod != null && _titleHistoryMod.AllTitles.TryGetValue(county, out var modHist))
                    {
                        holder = TitleHistoryLoader.GetHolderAtYear(modHist, year + offset);
                        liege = TitleHistoryLoader.GetLiegeAtYear(modHist, year + offset);
                        if (holder != null) fuente = "Mod";
                    }
                    if (holder == null && useBase && _titleHistoryBase != null && _titleHistoryBase.AllTitles.TryGetValue(county, out var baseHist))
                    {
                        holder = TitleHistoryLoader.GetHolderAtYear(baseHist, year);
                        liege = TitleHistoryLoader.GetLiegeAtYear(baseHist, year);
                        if (holder != null) fuente = "Base";
                    }

                    string sufijo = fuente != null ? $" [{fuente}]" : "";
                    LabelHolder.Content = $"Holder en {year}{sufijo}: {holder ?? "(sin datos)"}";
                    LabelLiege.Content = $"Liege en {year}{sufijo}: {liege ?? "(sin datos)"}";
                }
                else if (county != "-")
                {
                    LabelHolder.Content = "Holder: (año inválido)";
                    LabelLiege.Content = "Liege: (año inválido)";
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
