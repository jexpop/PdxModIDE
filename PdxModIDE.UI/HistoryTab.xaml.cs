using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
        private CharacterHistoryLoader? _characterHistoryBase;
        private CharacterHistoryLoader? _characterHistoryMod;
        private DynastyLoader? _dynastyBase;
        private DynastyLoader? _dynastyMod;
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
        private readonly HashSet<int> _selectedProvinceIds = new HashSet<int>();
        private bool _editMode;

        private Dictionary<int, ProvincePixelInfo>? _provincePixelInfo;
        private List<TitleLabelInfo>? _titleLabels;
        private Dictionary<int, string>? _provinceToTitle;
        private byte[]? _currentHolderLut;
        private Dictionary<int, string>? _currentIndexToHolder;

        private class ProvincePixelInfo
        {
            public float CenterX;
            public float CenterY;
            public int PixelCount;
            public int MinX;
            public int MaxX;
            public int MinY;
            public int MaxY;
            public double SumXX;
            public double SumYY;
            public double SumXY;
        }

        private class TitleLabelInfo
        {
            public string TitleKey { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public float CenterX;
            public float CenterY;
            public int PixelCount;
            public int MinX;
            public int MaxX;
            public int MinY;
            public int MaxY;
            public float RotationDeg;
        }

        public HistoryTab()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            MapImage.SizeChanged += OnMapSizeChanged;
            TitleModePanel.Visibility = Visibility.Collapsed;
            UpdateEditModeState();
        }

        public string Mode { get; set; } = "base";

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            IsVisibleChanged += OnIsVisibleChanged;
            UpdateOffsetLabel();
            UpdateShowNamesCheck();
        }

        private void UpdateShowNamesCheck()
        {
            if (ShowNamesCheck != null && ViewModel?.CurrentProfile != null)
                ShowNamesCheck.IsChecked = ViewModel.CurrentProfile.ShowTitleNames;
        }

        private void UpdateEditModeState()
        {
            bool modActive = ModSourceCheck?.IsChecked == true;

            if (!modActive && _editMode)
            {
                _editMode = false;
                ModeToggleButton.SetResourceReference(System.Windows.Controls.ContentControl.ContentProperty, "HistoryTab_ModeView");
                ModeToggleButton.SetResourceReference(System.Windows.FrameworkElement.ToolTipProperty, "HistoryTab_ModeViewTooltip");
            }

            ModeToggleButton.IsEnabled = modActive;

            if (_editMode)
            {
                if (BaseSourceCheck?.IsChecked == true && ModSourceCheck?.IsChecked == true)
                    BaseSourceCheck.IsChecked = false;
                BaseSourceCheck.Visibility = Visibility.Collapsed;
                ModSourceCheck.Visibility = Visibility.Collapsed;
                TitleModePanel.Visibility = Visibility.Visible;
                HolderModeCheck.Visibility = Visibility.Collapsed;
                CountyModeCheck.Visibility = Visibility.Collapsed;
                DuchyModeCheck.Visibility = Visibility.Collapsed;
                KingdomModeCheck.Visibility = Visibility.Collapsed;
                EmpireModeCheck.Visibility = Visibility.Collapsed;
                ShowNamesCheck.Visibility = Visibility.Visible;
            }
            else
            {
                BaseSourceCheck.Visibility = Visibility.Visible;
                ModSourceCheck.Visibility = Visibility.Visible;
                TitleModePanel.Visibility = HasActiveSource() ? Visibility.Visible : Visibility.Collapsed;
                HolderModeCheck.Visibility = Visibility.Visible;
                CountyModeCheck.Visibility = Visibility.Visible;
                DuchyModeCheck.Visibility = Visibility.Visible;
                KingdomModeCheck.Visibility = Visibility.Visible;
                EmpireModeCheck.Visibility = Visibility.Visible;
                ShowNamesCheck.Visibility = Visibility.Visible;
            }
        }

        private void ModeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _editMode = !_editMode;
            if (_editMode)
            {
                ModeToggleButton.SetResourceReference(System.Windows.Controls.ContentControl.ContentProperty, "HistoryTab_ModeEdit");
                ModeToggleButton.SetResourceReference(System.Windows.FrameworkElement.ToolTipProperty, "HistoryTab_ModeEditTooltip");
            }
            else
            {
                ModeToggleButton.SetResourceReference(System.Windows.Controls.ContentControl.ContentProperty, "HistoryTab_ModeView");
                ModeToggleButton.SetResourceReference(System.Windows.FrameworkElement.ToolTipProperty, "HistoryTab_ModeViewTooltip");
            }
            UpdateEditModeState();
            if (!_editMode && (HolderModeCheck.IsChecked == true || CountyModeCheck.IsChecked == true ||
                     DuchyModeCheck.IsChecked == true || KingdomModeCheck.IsChecked == true ||
                     EmpireModeCheck.IsChecked == true))
            {
                ReapplyActiveMode();
            }
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
            {
                _mapLoaded = false;
                UpdateShowNamesCheck();
            }
            if (e.PropertyName == nameof(MainViewModel.CurrentProfile) || e.PropertyName == nameof(MainViewModel.YearOffset))
                UpdateOffsetLabel();
            if (e.PropertyName == nameof(MainViewModel.Language))
            {
                if (_selectedProvinceIds.Count > 0)
                    UpdateSelectionInfo();
                if (_mapLoaded && HasActiveSource())
                {
                    BuildTitleLabels();
                    InvalidateRender();
                }
            }
        }

        private void UpdateOffsetLabel()
        {
            if (OffsetLabel == null) return;
            var profile = ViewModel?.CurrentProfile;
            if (profile == null)
            {
                OffsetLabel.Content = TryFindResource("HistoryTab_OffsetMod") ?? "Mod Date: -";
                return;
            }
            if (int.TryParse(YearBox.Text, out int year))
                OffsetLabel.Content = $"Mod Date: {year + profile.YearOffset}";
            else
                OffsetLabel.Content = TryFindResource("HistoryTab_OffsetMod") ?? "Mod Date: -";
        }

        private void TryAutoLoad()
        {
            if (_mapLoaded) return;
            var profile = ViewModel?.CurrentProfile;
            if (profile == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(profile.GameRoot) || !Directory.Exists(profile.GameRoot))
            {
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

                _characterHistoryBase = new CharacterHistoryLoader();
                int baseCharCount = _characterHistoryBase.LoadAll(gameRoot);

                _dynastyBase = new DynastyLoader();
                int baseDynCount = _dynastyBase.LoadAll(gameRoot);

                int modCount = 0;
                int modCharCount = 0;
                int modDynCount = 0;
                if (!string.IsNullOrEmpty(modRoot) && Directory.Exists(modRoot))
                {
                    _titleHistoryMod = new TitleHistoryLoader();
                    modCount = _titleHistoryMod.LoadAll(modRoot, overwriteDuplicates: true);

                    _characterHistoryMod = new CharacterHistoryLoader();
                    modCharCount = _characterHistoryMod.LoadAll(modRoot, overwriteDuplicates: true);

                    _dynastyMod = new DynastyLoader();
                    modDynCount = _dynastyMod.LoadAll(modRoot, overwriteDuplicates: true);

                    loader.LoadModLocalization(modRoot);
                }
                else
                {
                    _titleHistoryMod = null;
                    _characterHistoryMod = null;
                    _dynastyMod = null;
                }

                _mapLoader = loader;

                var renderer = new MapRenderer();
                if (!renderer.Load(loader))
                {
                    return;
                }

                _renderer = renderer;
                _mapLoaded = true;
                BuildProvinceCentroids();

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading map: {ex.Message}");
            }
        }

        private int _renderVersion;
        private int _cachedRenderVersion = -1;

        private void QueueRender()
        {
            if (_renderPending) return;
            _renderPending = true;
            Dispatcher.BeginInvoke(new Action(RenderNow), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void InvalidateRender()
        {
            _renderVersion++;
            _cachedWidth = -1;
            QueueRender();
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
                _cachedRenderVersion != _renderVersion ||
                _cachedWidth != vw ||
                _cachedHeight != vh ||
                _cachedHighlight != _renderer.HighlightVersion ||
                _cachedZoom != _renderer.Zoom ||
                _cachedOffX != _renderer.OffsetX ||
                _cachedOffY != _renderer.OffsetY;

            if (needsRender)
            {
                _renderCache?.Dispose();
                _renderCache = _renderer.RenderToBitmap(vw, vh);
                _cachedWidth = vw;
                _cachedHeight = vh;
                _cachedHighlight = _renderer.HighlightVersion;
                _cachedZoom = _renderer.Zoom;
                _cachedOffX = _renderer.OffsetX;
                _cachedOffY = _renderer.OffsetY;
                _cachedRenderVersion = _renderVersion;
            }

            if (ShowNamesCheck?.IsChecked == true && _titleLabels?.Count > 0 && _renderCache != null)
                DrawLabels(_renderCache);

            if (_renderCache == null) return;
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
                        if (_editMode)
                        {
                            if (province.Type != "land")
                            {
                                _selectedProvinceIds.Clear();
                                _selectedProvinceIds.Add(provinceId);
                            }
                            else
                            {
                                _selectedProvinceIds.RemoveWhere(id =>
                                {
                                    var p = _mapLoader.GetProvinceFromId(id);
                                    return p != null && p.Type != "land";
                                });
                                if (!_selectedProvinceIds.Remove(provinceId))
                                    _selectedProvinceIds.Add(provinceId);
                            }

                            if (_selectedProvinceIds.Count == 0)
                            {
                                InfoPanel.Visibility = Visibility.Collapsed;
                                InfoPlaceholder.Visibility = Visibility.Visible;
                            }
                            else if (_selectedProvinceIds.Count == 1)
                            {
                                InfoPlaceholder.Visibility = Visibility.Collapsed;
                                UpdateProvinceInfo(_selectedProvinceIds.First());
                            }
                            else
                            {
                                InfoPlaceholder.Visibility = Visibility.Collapsed;
                                UpdateMultiProvinceInfo(_selectedProvinceIds);
                            }

                            _renderer.SetHighlightProvinces(_selectedProvinceIds);
                            QueueRender();
                        }
                        else
                        {
                            InfoPlaceholder.Visibility = Visibility.Collapsed;
                            UpdateProvinceInfo(provinceId);
                        }
                        return;
                    }
                }

                _selectedProvinceIds.Clear();
                _renderer.SetHighlightProvinces(_selectedProvinceIds);
                InfoPanel.Visibility = Visibility.Collapsed;
                InfoPlaceholder.Visibility = Visibility.Visible;
                QueueRender();
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
            bool visible = HasActiveSource() && !_editMode;
            TitleModePanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ApplySourceStructure()
        {
            if (_mapLoader == null) return;
            if (ModSourceCheck?.IsChecked == true)
            {
                var modRoot = ViewModel?.CurrentProfile?.ModRoot;
                if (!string.IsNullOrEmpty(modRoot))
                {
                    _mapLoader.LoadModLandedTitles(modRoot);
                    _mapLoader.LoadModLocalization(modRoot);
                }
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
            UpdateEditModeState();
            UpdateTitleModeVisibility();
            ApplySourceStructure();

            if (InfoPanel.Visibility == Visibility.Visible && _selectedProvinceIds.Count > 0)
            {
                bool allLand = _selectedProvinceIds.All(id =>
                {
                    var p = _mapLoader?.GetProvinceFromId(id);
                    return p != null && p.Type == "land";
                });
                TitleGroup.Visibility = HasActiveSource() && allLand ? Visibility.Visible : Visibility.Collapsed;
                if (_selectedProvinceIds.Count > 1)
                    UpdateMultiProvinceInfo(_selectedProvinceIds);
                else
                    UpdateProvinceInfo(_selectedProvinceIds.First());
            }

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
            _titleLabels = null;
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
                _titleLabels = null;
                InvalidateRender();
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
                _titleLabels = null;
                InvalidateRender();
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
                _titleLabels = null;
                InvalidateRender();
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
                _titleLabels = null;
                InvalidateRender();
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
                _titleLabels = null;
                InvalidateRender();
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
                InvalidateRender();
                _titleLabels = null;
            }
        }

        private void ShowNamesChanged(object sender, RoutedEventArgs e)
        {
            if (!_mapLoaded || _renderer == null) return;
            SaveShowNamesSetting();
            InvalidateRender();
        }

        private void SaveShowNamesSetting()
        {
            if (ViewModel?.CurrentProfile != null)
            {
                ViewModel.CurrentProfile.ShowTitleNames = ShowNamesCheck.IsChecked == true;
                ViewModel.ProjectService.UpdateProfile(ViewModel.CurrentProfile);
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
                InvalidateRender();
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
            _currentHolderLut = holderLut;
            _currentIndexToHolder = indexToHolder;

            BuildTitleLabels();
            InvalidateRender();
            UpdateSelectionInfo();
        }
        
        private void ApplyCountyMode()
        {
            if (!HasActiveSource())
            {
                _renderer!.SetHolderMode(false, null, null);
                InvalidateRender();
                return;
            }
            var countyLut = _mapLoader!.BuildCountyLut(out var indexToCounty);
            var palette = MapLoader.BuildCountyPalette(indexToCounty);
            _renderer!.SetHolderMode(true, countyLut, palette);
            BuildTitleLabels();
            InvalidateRender();
            UpdateSelectionInfo();
        }

        private void ApplyDuchyMode()
        {
            if (!HasActiveSource())
            {
                _renderer!.SetHolderMode(false, null, null);
                InvalidateRender();
                return;
            }
            var duchyLut = _mapLoader!.BuildDuchyLut(out var indexToDuchy);
            var palette = MapLoader.BuildDuchyPalette(indexToDuchy);
            _renderer!.SetHolderMode(true, duchyLut, palette);
            BuildTitleLabels();
            InvalidateRender();
            UpdateSelectionInfo();
        }

        private void ApplyKingdomMode()
        {
            if (!HasActiveSource())
            {
                _renderer!.SetHolderMode(false, null, null);
                InvalidateRender();
                return;
            }
            var kingdomLut = _mapLoader!.BuildKingdomLut(out var indexToKingdom);
            var palette = MapLoader.BuildKingdomPalette(indexToKingdom);
            _renderer!.SetHolderMode(true, kingdomLut, palette);
            BuildTitleLabels();
            InvalidateRender();
            UpdateSelectionInfo();
        }

        private void ApplyEmpireMode()
        {
            if (!HasActiveSource())
            {
                _renderer!.SetHolderMode(false, null, null);
                InvalidateRender();
                return;
            }
            var empireLut = _mapLoader!.BuildEmpireLut(out var indexToEmpire);
            var palette = MapLoader.BuildEmpirePalette(indexToEmpire);
            _renderer!.SetHolderMode(true, empireLut, palette);
            BuildTitleLabels();
            InvalidateRender();
            UpdateSelectionInfo();
        }

        private void UpdateSelectionInfo()
        {
            if (_selectedProvinceIds.Count == 0)
            {
                InfoPanel.Visibility = Visibility.Collapsed;
                InfoPlaceholder.Visibility = Visibility.Visible;
            }
            else if (_selectedProvinceIds.Count == 1)
                UpdateProvinceInfo(_selectedProvinceIds.First());
            else
                UpdateMultiProvinceInfo(_selectedProvinceIds);
        }

        private string GetCommonValue(HashSet<int> ids, Func<int, string> getValue)
        {
            string? first = null;
            foreach (var id in ids)
            {
                string val = getValue(id);
                if (first == null)
                    first = val;
                else if (first != val)
                    return "(Multiple)";
            }
            return first ?? "-";
        }

        private void UpdateMultiProvinceInfo(HashSet<int> provinceIds)
        {
            try
            {
                if (_mapLoader == null || provinceIds.Count < 2) return;

                InfoPanel.Visibility = Visibility.Visible;
                InfoPlaceholder.Visibility = Visibility.Collapsed;

                TextIdValue.Text = string.Join(", ", provinceIds.OrderBy(id => id));

                TextNameValue.Text = GetCommonValue(provinceIds, pid =>
                {
                    var p = _mapLoader.GetProvinceFromId(pid);
                    return p != null ? GetLocalizedTitleName(p.Name) : "-";
                });

                TextColorValue.Text = GetCommonValue(provinceIds, pid =>
                {
                    var p = _mapLoader.GetProvinceFromId(pid);
                    return p != null ? $"({p.R},{p.G},{p.B})" : "-";
                });

                TextTypeValue.Text = GetCommonValue(provinceIds, pid =>
                {
                    var p = _mapLoader.GetProvinceFromId(pid);
                    return p != null ? TranslateTerrainType(p.Type) : "-";
                });

                string commonBarony = GetCommonValue(provinceIds, pid =>
                {
                    string bk = _mapLoader.GetTitleFromProvinceId(pid) ?? "-";
                    return bk != "-" ? GetLocalizedTitleName(bk) : "-";
                });
                TextBaronyValue.Text = commonBarony;

                string commonCounty = GetCommonValue(provinceIds, pid =>
                {
                    string bk = _mapLoader.GetTitleFromProvinceId(pid) ?? "-";
                    if (bk == "-") return "-";
                    string ck = _mapLoader.GetCountyFromBarony(bk) ?? "-";
                    return ck != "-" ? GetLocalizedTitleName(ck) : "-";
                });
                TextCountyValue.Text = commonCounty;

                string commonDuchy = GetCommonValue(provinceIds, pid =>
                {
                    string bk = _mapLoader.GetTitleFromProvinceId(pid) ?? "-";
                    if (bk == "-") return "-";
                    string ck = _mapLoader.GetCountyFromBarony(bk) ?? "-";
                    if (ck == "-") return "-";
                    return _mapLoader.CountyToDuchy.TryGetValue(ck, out var dk) ? GetLocalizedTitleName(dk) : "-";
                });
                TextDuchyValue.Text = commonDuchy;

                string commonKingdom = GetCommonValue(provinceIds, pid =>
                {
                    string bk = _mapLoader.GetTitleFromProvinceId(pid) ?? "-";
                    if (bk == "-") return "-";
                    string ck = _mapLoader.GetCountyFromBarony(bk) ?? "-";
                    if (ck == "-") return "-";
                    if (!_mapLoader.CountyToDuchy.TryGetValue(ck, out var dk)) return "-";
                    return _mapLoader.DuchyToKingdom.TryGetValue(dk, out var kk) ? GetLocalizedTitleName(kk) : "-";
                });
                TextKingdomValue.Text = commonKingdom;

                string commonEmpire = GetCommonValue(provinceIds, pid =>
                {
                    string bk = _mapLoader.GetTitleFromProvinceId(pid) ?? "-";
                    if (bk == "-") return "-";
                    string ck = _mapLoader.GetCountyFromBarony(bk) ?? "-";
                    if (ck == "-") return "-";
                    if (!_mapLoader.CountyToDuchy.TryGetValue(ck, out var dk)) return "-";
                    if (!_mapLoader.DuchyToKingdom.TryGetValue(dk, out var kk)) return "-";
                    return _mapLoader.KingdomToEmpire.TryGetValue(kk, out var ek) ? GetLocalizedTitleName(ek) : "-";
                });
                TextEmpireValue.Text = commonEmpire;

                TextBaronyLabel.Content = Res("HistoryTab_BaronyLabel");
                TextCountyLabel.Content = Res("HistoryTab_CountyLabel");
                TextDuchyLabel.Content = Res("HistoryTab_DuchyLabel");
                TextKingdomLabel.Content = Res("HistoryTab_KingdomLabel");
                TextEmpireLabel.Content = Res("HistoryTab_EmpireLabel");

                bool allLand = provinceIds.All(id =>
                {
                    var p = _mapLoader.GetProvinceFromId(id);
                    return p != null && p.Type == "land";
                });
                TitleGroup.Visibility = HasActiveSource() && allLand ? Visibility.Visible : Visibility.Collapsed;

                TextHolderLabel.Content = Res("HistoryTab_HolderLabel");
                TextHolderValue.Text = "(multi-select)";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating multi-province info: {ex.Message}");
            }
        }

        private void UpdateProvinceInfo(int provinceId)
        {
            try
            {
                if (_mapLoader == null) return;

                var province = _mapLoader.GetProvinceFromId(provinceId);
                if (province == null) return;

                _selectedProvinceIds.Clear();
                _selectedProvinceIds.Add(provinceId);
                InfoPanel.Visibility = Visibility.Visible;
                InfoPlaceholder.Visibility = Visibility.Collapsed;
                TitleGroup.Visibility = HasActiveSource() && province.Type == "land" ? Visibility.Visible : Visibility.Collapsed;

                TextIdValue.Text = province.Id.ToString();
                TextNameValue.Text = GetLocalizedTitleName(province.Name);
                TextColorValue.Text = $"({province.R},{province.G},{province.B})";
                TextTypeValue.Text = TranslateTerrainType(province.Type);

                string baronyKey = _mapLoader.GetTitleFromProvinceId(provinceId) ?? "-";
                string baronyName = baronyKey != "-" ? GetLocalizedTitleName(baronyKey) : "-";
                TextBaronyValue.Text = baronyName;

                string countyKey = "-";
                string countyName = "-";
                if (baronyKey != "-")
                {
                    countyKey = _mapLoader.GetCountyFromBarony(baronyKey) ?? "-";
                    countyName = countyKey != "-" ? GetLocalizedTitleName(countyKey) : "-";
                }
                TextCountyValue.Text = countyName;

                string duchyKey = "-";
                string duchyName = "-";
                if (countyKey != "-" && _mapLoader.CountyToDuchy.TryGetValue(countyKey, out var dk))
                {
                    duchyKey = dk;
                    duchyName = GetLocalizedTitleName(duchyKey);
                }
                TextDuchyValue.Text = duchyName;

                string kingdomKey = "-";
                string kingdomName = "-";
                if (duchyKey != "-" && _mapLoader.DuchyToKingdom.TryGetValue(duchyKey, out var kk))
                {
                    kingdomKey = kk;
                    kingdomName = GetLocalizedTitleName(kingdomKey);
                }
                TextKingdomValue.Text = kingdomName;

                string empireKey = "-";
                string empireName = "-";
                if (kingdomKey != "-" && _mapLoader.KingdomToEmpire.TryGetValue(kingdomKey, out var ek))
                {
                    empireKey = ek;
                    empireName = GetLocalizedTitleName(empireKey);
                }
                TextEmpireValue.Text = empireName;

                TextBaronyLabel.Content = Res("HistoryTab_BaronyLabel");
                TextCountyLabel.Content = Res("HistoryTab_CountyLabel");
                TextDuchyLabel.Content = Res("HistoryTab_DuchyLabel");
                TextKingdomLabel.Content = Res("HistoryTab_KingdomLabel");
                TextEmpireLabel.Content = Res("HistoryTab_EmpireLabel");

                if (countyKey != "-" && int.TryParse(YearBox.Text, out int year))
                {
                    bool useBase = BaseSourceCheck?.IsChecked == true;
                    bool useMod = ModSourceCheck?.IsChecked == true;
                    int offset = ViewModel?.CurrentProfile?.YearOffset ?? 0;

                    string? targetTitleKey = null;
                    string holderLevelResKey = "HistoryTab_CountyMode";

                    if (DuchyModeCheck?.IsChecked == true)
                    {
                        targetTitleKey = duchyKey != "-" ? duchyKey : null;
                        holderLevelResKey = "HistoryTab_DuchyMode";
                    }
                    else if (KingdomModeCheck?.IsChecked == true)
                    {
                        targetTitleKey = kingdomKey != "-" ? kingdomKey : null;
                        holderLevelResKey = "HistoryTab_KingdomMode";
                    }
                    else if (EmpireModeCheck?.IsChecked == true)
                    {
                        targetTitleKey = empireKey != "-" ? empireKey : null;
                        holderLevelResKey = "HistoryTab_EmpireMode";
                    }
                    else
                    {
                        targetTitleKey = countyKey != "-" ? countyKey : null;
                        holderLevelResKey = "HistoryTab_CountyMode";
                    }

                    string? holder = null;
                    string? fuente = null;

                    if (targetTitleKey != null)
                    {
                        if (useMod && _titleHistoryMod != null &&
                            _titleHistoryMod.AllTitles.TryGetValue(targetTitleKey, out var modHist))
                        {
                            holder = TitleHistoryLoader.GetHolderAtYear(modHist, year + offset);
                            if (holder != null) fuente = "Mod";
                        }
                        if (holder == null && useBase && _titleHistoryBase != null &&
                            _titleHistoryBase.AllTitles.TryGetValue(targetTitleKey, out var baseHist))
                        {
                            holder = TitleHistoryLoader.GetHolderAtYear(baseHist, year);
                            if (holder != null) fuente = "Base";
                        }
                    }

                    string holderLabelText = Res("HistoryTab_HolderLabel");
                    string levelName = Res(holderLevelResKey);
                    TextHolderLabel.Content = $"{holderLabelText} ({levelName})";

                    string sufijo = fuente != null ? $" [{fuente}]" : "";
                    if (holder != null)
                    {
                        string holderName = GetCharacterName(holder);
                        TextHolderValue.Text = $"{holderName}{sufijo}";
                    }
                    else
                    {
                        TextHolderValue.Text = $"(no data){sufijo}";
                    }

                }
                else if (countyKey != "-")
                {
                    TextHolderLabel.Content = Res("HistoryTab_HolderLabel");
                    TextHolderValue.Text = "(invalid year)";
                }
                else
                {
                    TextHolderLabel.Content = Res("HistoryTab_HolderLabel");
                    TextHolderValue.Text = "(no data)";
                }

                if (_renderer != null)
                {
                    _renderer.SetHighlightProvinces(_selectedProvinceIds);
                    QueueRender();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating province info: {ex.Message}");
            }
        }

        private string TranslateTerrainType(string? type)
        {
            return type switch
            {
                "land" => Res("MapTerrain_Land"),
                "sea" => Res("MapTerrain_Sea"),
                "lake" => Res("MapTerrain_Lake"),
                "river" => Res("MapTerrain_River"),
                "impassable" => Res("MapTerrain_Impassable"),
                "unknown" => Res("MapTerrain_Unknown"),
                _ => type ?? "?"
            };
        }

        private static string Res(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string ?? key;
        }

        private void BuildProvinceCentroids()
        {
            _provincePixelInfo = null;
            if (_mapLoader?.ProvinceIdMap == null) return;

            var landTypes = new HashSet<string> { "land", "unknown" };

            var sumX = new Dictionary<int, double>();
            var sumY = new Dictionary<int, double>();
            var sumXX = new Dictionary<int, double>();
            var sumYY = new Dictionary<int, double>();
            var sumXY = new Dictionary<int, double>();
            var count = new Dictionary<int, int>();
            var minX = new Dictionary<int, int>();
            var maxX = new Dictionary<int, int>();
            var minY = new Dictionary<int, int>();
            var maxY = new Dictionary<int, int>();

            int w = _mapLoader.MapWidth;
            int h = _mapLoader.MapHeight;
            int[] idMap = _mapLoader.ProvinceIdMap;

            for (int y = 0; y < h; y++)
            {
                int rowOff = y * w;
                for (int x = 0; x < w; x++)
                {
                    int pid = idMap[rowOff + x];
                    if (pid <= 0) continue;

                    var prov = _mapLoader.GetProvinceFromId(pid);
                    if (prov == null || !landTypes.Contains(prov.Type))
                        continue;

                    if (!sumX.ContainsKey(pid))
                    {
                        sumX[pid] = 0;
                        sumY[pid] = 0;
                        sumXX[pid] = 0;
                        sumYY[pid] = 0;
                        sumXY[pid] = 0;
                        count[pid] = 0;
                        minX[pid] = x;
                        maxX[pid] = x;
                        minY[pid] = y;
                        maxY[pid] = y;
                    }
                    else
                    {
                        if (x < minX[pid]) minX[pid] = x;
                        if (x > maxX[pid]) maxX[pid] = x;
                        if (y < minY[pid]) minY[pid] = y;
                        if (y > maxY[pid]) maxY[pid] = y;
                    }
                    sumX[pid] += x;
                    sumY[pid] += y;
                    sumXX[pid] += (double)x * x;
                    sumYY[pid] += (double)y * y;
                    sumXY[pid] += (double)x * y;
                    count[pid]++;
                }
            }

            _provincePixelInfo = new Dictionary<int, ProvincePixelInfo>();
            foreach (var pid in sumX.Keys)
            {
                _provincePixelInfo[pid] = new ProvincePixelInfo
                {
                    CenterX = (float)(sumX[pid] / count[pid]),
                    CenterY = (float)(sumY[pid] / count[pid]),
                    PixelCount = count[pid],
                    MinX = minX[pid],
                    MaxX = maxX[pid],
                    MinY = minY[pid],
                    MaxY = maxY[pid],
                    SumXX = sumXX[pid],
                    SumYY = sumYY[pid],
                    SumXY = sumXY[pid]
                };
            }
        }

        private void BuildTitleLabels()
        {
            _titleLabels = null;
            if (_mapLoader == null || _provincePixelInfo == null) return;

            Func<int, string?> getTitleForProvince;

            if (CountyModeCheck?.IsChecked == true)
            {
                getTitleForProvince = pid =>
                {
                    var barony = _mapLoader.GetTitleFromProvinceId(pid);
                    return barony != null ? _mapLoader.GetCountyFromBarony(barony) : null;
                };
            }
            else if (DuchyModeCheck?.IsChecked == true)
            {
                getTitleForProvince = pid =>
                {
                    var barony = _mapLoader.GetTitleFromProvinceId(pid);
                    if (barony == null) return null;
                    var county = _mapLoader.GetCountyFromBarony(barony);
                    if (county == null) return null;
                    return _mapLoader.CountyToDuchy.TryGetValue(county, out var d) ? d : null;
                };
            }
            else if (KingdomModeCheck?.IsChecked == true)
            {
                getTitleForProvince = pid =>
                {
                    var barony = _mapLoader.GetTitleFromProvinceId(pid);
                    if (barony == null) return null;
                    var county = _mapLoader.GetCountyFromBarony(barony);
                    if (county == null) return null;
                    if (!_mapLoader.CountyToDuchy.TryGetValue(county, out var duchy)) return null;
                    return _mapLoader.DuchyToKingdom.TryGetValue(duchy, out var k) ? k : null;
                };
            }
            else if (EmpireModeCheck?.IsChecked == true)
            {
                getTitleForProvince = pid =>
                {
                    var barony = _mapLoader.GetTitleFromProvinceId(pid);
                    if (barony == null) return null;
                    var county = _mapLoader.GetCountyFromBarony(barony);
                    if (county == null) return null;
                    if (!_mapLoader.CountyToDuchy.TryGetValue(county, out var duchy)) return null;
                    if (!_mapLoader.DuchyToKingdom.TryGetValue(duchy, out var kingdom)) return null;
                    return _mapLoader.KingdomToEmpire.TryGetValue(kingdom, out var e) ? e : null;
                };
            }
            else if (HolderModeCheck?.IsChecked == true)
            {
                bool useBase = BaseSourceCheck?.IsChecked == true;
                bool useMod = ModSourceCheck?.IsChecked == true;
                int offset = ViewModel?.CurrentProfile?.YearOffset ?? 0;
                int.TryParse(YearBox.Text, out int year);
                int? baseYear = useBase ? year : (int?)null;
                int? modYear = useMod ? year + offset : (int?)null;

                getTitleForProvince = pid =>
                {
                    var barony = _mapLoader.GetTitleFromProvinceId(pid);
                    if (barony == null) return null;
                    var county = _mapLoader.GetCountyFromBarony(barony);
                    if (county == null) return null;

                    string? holder = null;
                    if (modYear.HasValue && _titleHistoryMod != null &&
                        _titleHistoryMod.AllTitles.TryGetValue(county, out var modHist))
                        holder = TitleHistoryLoader.GetHolderAtYear(modHist, modYear.Value);
                    if (holder == null && baseYear.HasValue && _titleHistoryBase != null &&
                        _titleHistoryBase.AllTitles.TryGetValue(county, out var baseHist))
                        holder = TitleHistoryLoader.GetHolderAtYear(baseHist, baseYear.Value);
                    return holder;
                };
            }
            else
            {
                return;
            }

            _provinceToTitle = new Dictionary<int, string>();
            var groups = new Dictionary<string, (double sumX, double sumY, double sumXX, double sumYY, double sumXY, int count, int minX, int maxX, int minY, int maxY)>();
            foreach (var (pid, info) in _provincePixelInfo)
            {
                var title = getTitleForProvince(pid);
                if (title != null)
                    _provinceToTitle[pid] = title;
                else
                    continue;

                if (!groups.TryGetValue(title, out var g))
                {
                    g = (info.CenterX * info.PixelCount,
                         info.CenterY * info.PixelCount,
                         info.SumXX, info.SumYY, info.SumXY,
                         info.PixelCount,
                         info.MinX, info.MaxX, info.MinY, info.MaxY);
                }
                else
                {
                    if (info.MinX < g.minX) g.minX = info.MinX;
                    if (info.MaxX > g.maxX) g.maxX = info.MaxX;
                    if (info.MinY < g.minY) g.minY = info.MinY;
                    if (info.MaxY > g.maxY) g.maxY = info.MaxY;
                    g.sumX += info.CenterX * info.PixelCount;
                    g.sumY += info.CenterY * info.PixelCount;
                    g.sumXX += info.SumXX;
                    g.sumYY += info.SumYY;
                    g.sumXY += info.SumXY;
                    g.count += info.PixelCount;
                }
                groups[title] = g;
            }

            _titleLabels = new List<TitleLabelInfo>();
            foreach (var (title, (sx, sy, sxx, syy, sxy, cnt, mnX, mxX, mnY, mxY)) in groups)
            {
                float cx = cnt > 0 ? (float)(sx / cnt) : (mnX + mxX) / 2f;
                float cy = cnt > 0 ? (float)(sy / cnt) : (mnY + mxY) / 2f;

                float rot = 0;
                if (cnt > 1)
                {
                    double meanX = sx / cnt;
                    double meanY = sy / cnt;
                    double covXX = sxx / cnt - meanX * meanX;
                    double covYY = syy / cnt - meanY * meanY;
                    double covXY = sxy / cnt - meanX * meanY;

                    double trace = covXX + covYY;
                    double det = covXX * covYY - covXY * covXY;
                    double disc = trace * trace / 4 - det;
                    if (disc > 0)
                    {
                        double lambda1 = trace / 2 + Math.Sqrt(disc);
                        double lambda2 = trace / 2 - Math.Sqrt(disc);
                        double ratio = lambda1 > 0 ? lambda2 / lambda1 : 0;

                        if (ratio < 0.65)
                        {
                            double angle = Math.Atan2(2 * covXY, covXX - covYY) / 2;
                            rot = (float)(angle * 180 / Math.PI);
                            if (rot > 90) rot -= 180;
                            else if (rot < -90) rot += 180;
                            rot = Math.Clamp(rot, -90f, 90f);
                        }
                    }
                }

                string displayName = HolderModeCheck?.IsChecked == true
                    ? GetCharacterName(title)
                    : GetLocalizedTitleName(title);
                _titleLabels.Add(new TitleLabelInfo
                {
                    TitleKey = title,
                    DisplayName = displayName,
                    CenterX = cx,
                    CenterY = cy,
                    PixelCount = cnt,
                    MinX = mnX,
                    MaxX = mxX,
                    MinY = mnY,
                    MaxY = mxY,
                    RotationDeg = rot
                });
            }
        }

        private string GetLocalizedTitleName(string titleKey)
        {
            if (_mapLoader == null) return titleKey;
            return _mapLoader.GetDisplayName(titleKey, GetAppLanguage());
        }

        private string GetCharacterName(string characterId)
        {
            string? name = null;
            string? dynastyId = null;

            if (_characterHistoryMod != null)
            {
                name = _characterHistoryMod.GetCharacterName(characterId);
                dynastyId = _characterHistoryMod.GetCharacterDynasty(characterId);
            }
            if (name == null && _characterHistoryBase != null)
            {
                name = _characterHistoryBase.GetCharacterName(characterId);
                dynastyId ??= _characterHistoryBase.GetCharacterDynasty(characterId);
            }

            if (name == null)
            {
                string localized = GetLocalizedTitleName(characterId);
                if (localized != characterId) name = localized;
                else
                {
                    localized = GetLocalizedTitleName("char_" + characterId);
                    if (localized != "char_" + characterId) name = localized;
                    else
                    {
                        localized = GetLocalizedTitleName("character_" + characterId);
                        if (localized != "character_" + characterId) name = localized;
                    }
                }
            }

            if (name == null)
                return characterId;

            if (!string.IsNullOrEmpty(dynastyId))
            {
                string dynastyName = GetDynastyDisplayName(dynastyId);
                if (!string.IsNullOrEmpty(dynastyName))
                    return $"{name} {dynastyName}";
            }

            return name;
        }

        private string GetDynastyDisplayName(string dynastyId)
        {
            string? rawName = null;

            if (_dynastyMod != null)
                rawName = _dynastyMod.GetDynastyName(dynastyId);
            if (rawName == null && _dynastyBase != null)
                rawName = _dynastyBase.GetDynastyName(dynastyId);

            if (rawName == null)
                return "";

            string localized = GetLocalizedTitleName(rawName);
            if (localized != rawName)
                return localized;

            return rawName;
        }

        private string GetAppLanguage()
        {
            return ViewModel?.Language ?? "en";
        }

        private void DrawLabels(SKBitmap bitmap)
        {
            if (_renderer == null || _titleLabels == null) return;

            float zoom = _renderer.Zoom;
            float offX = _renderer.OffsetX;
            float offY = _renderer.OffsetY;
            int bmpW = bitmap.Width;
            int bmpH = bitmap.Height;

            using var canvas = new SKCanvas(bitmap);

            using var typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

            var drawnRects = new List<SKRect>();

            foreach (var label in _titleLabels.OrderByDescending(l => l.PixelCount))
            {
                float cx = label.CenterX * zoom + offX;
                float cy = label.CenterY * zoom + offY;

                if (cx < 0 || cx > bmpW || cy < 0 || cy > bmpH)
                    continue;

                float sx1 = label.MinX * zoom + offX;
                float sx2 = label.MaxX * zoom + offX;
                float sy1 = label.MinY * zoom + offY;
                float sy2 = label.MaxY * zoom + offY;

                float boxW = sx2 - sx1;
                float boxH = sy2 - sy1;

                if (boxW < 30 || boxH < 20) continue;

                float maxTextWidth = boxW * 0.85f;
                float fontSize = Math.Clamp(boxW * 0.14f, 8f, boxW * 0.3f);
                using var font = new SKFont(typeface, fontSize);
                float textWidth = font.MeasureText(label.DisplayName);
                if (textWidth > maxTextWidth)
                {
                    fontSize *= maxTextWidth / textWidth;
                    font.Size = fontSize;
                    textWidth = font.MeasureText(label.DisplayName);
                }
                float textHeight = fontSize;

                float padX = fontSize * 0.35f;
                float padY = fontSize * 0.2f;
                float bgW = textWidth + padX * 2;
                float bgH = textHeight + padY * 2;

                float boxCx = sx1 + boxW / 2;
                float boxCy = sy1 + boxH / 2;

                var candidates = new List<(float x, float y, bool canRotate)>();
                candidates.Add((cx, cy, true));

                float minDim = Math.Min(boxW, boxH);
                if (minDim > bgW * 1.5f && minDim > bgH * 1.5f)
                {
                    float qx = boxW * 0.25f;
                    float qy = boxH * 0.25f;
                    candidates.Add((boxCx, sy1 + qy, false));
                    candidates.Add((boxCx, sy2 - qy, false));
                    candidates.Add((sx1 + qx, boxCy, false));
                    candidates.Add((sx2 - qx, boxCy, false));
                    candidates.Add((sx1 + qx, sy1 + qy, false));
                    candidates.Add((sx2 - qx, sy1 + qy, false));
                    candidates.Add((sx1 + qx, sy2 - qy, false));
                    candidates.Add((sx2 - qx, sy2 - qy, false));
                }

                (float px, float py, bool doRotate) = (cx, cy, true);
                bool placed = false;

                foreach (var (candX, candY, canRot) in candidates)
                {
                    float bgX = candX - bgW / 2;
                    float bgY = candY - textHeight * 0.5f - padY;

                    bgX = Math.Max(1, bgX);
                    bgY = Math.Max(1, bgY);
                    if (bgX + bgW > bmpW - 1) bgX = bmpW - bgW - 1;
                    if (bgY + bgH > bmpH - 1) bgY = bmpH - bgH - 1;

                    bool onOtherLand = false;
                    if (_mapLoader != null && _mapLoader.ProvinceIdMap != null && _provinceToTitle != null)
                    {
                        int mapW = _mapLoader.MapWidth;
                        int mapH = _mapLoader.MapHeight;
                        int[] idMap = _mapLoader.ProvinceIdMap;

                        int sampleCols = Math.Min(Math.Max(3, (int)(bgW / Math.Max(1f, fontSize * 0.4f))), 6);
                        int sampleRows = Math.Min(Math.Max(2, (int)(bgH / Math.Max(1f, fontSize * 0.4f))), 4);

                        float rotRad = (canRot && Math.Abs(label.RotationDeg) > 1f) ? label.RotationDeg * MathF.PI / 180f : 0f;
                        float cosR = MathF.Cos(rotRad);
                        float sinR = MathF.Sin(rotRad);
                        bool useRot = Math.Abs(rotRad) > 0.01f;

                        for (int sy = 0; sy < sampleRows && !onOtherLand; sy++)
                        {
                            for (int sx = 0; sx < sampleCols && !onOtherLand; sx++)
                            {
                                float localX = (sx + 0.5f) / sampleCols * bgW - bgW / 2;
                                float localY = (sy + 0.5f) / sampleRows * bgH - bgH / 2;

                                float screenX = candX + localX;
                                float screenY = candY + localY;
                                if (useRot)
                                {
                                    screenX = candX + localX * cosR - localY * sinR;
                                    screenY = candY + localX * sinR + localY * cosR;
                                }

                                float mapXf = (screenX - offX) / zoom;
                                float mapYf = (screenY - offY) / zoom;
                                int mapX = (int)Math.Round(mapXf);
                                int mapY = (int)Math.Round(mapYf);

                                if (mapX < 0 || mapX >= mapW || mapY < 0 || mapY >= mapH)
                                    continue;

                                int pid = idMap[mapY * mapW + mapX];
                                if (pid <= 0) continue;

                                if (_provinceToTitle.TryGetValue(pid, out var pixelTitle))
                                {
                                    if (pixelTitle != label.TitleKey)
                                    {
                                        var pType = _mapLoader.GetProvinceFromId(pid)?.Type;
                                        if (pType == "land") onOtherLand = true;
                                    }
                                }
                                else
                                {
                                    var pType = _mapLoader.GetProvinceFromId(pid)?.Type;
                                    if (pType == "land") onOtherLand = true;
                                }
                            }
                        }
                    }

                    if (onOtherLand) continue;

                    bool overlaps = false;
                    foreach (var r in drawnRects)
                    {
                        if (bgX < r.Right + 4 && bgX + bgW > r.Left - 4 &&
                            bgY < r.Bottom + 4 && bgY + bgH > r.Top - 4)
                        { overlaps = true; break; }
                    }
                    if (!overlaps)
                    {
                        (px, py, doRotate) = (candX, candY, canRot);
                        placed = true;
                        drawnRects.Add(new SKRect(bgX, bgY, bgX + bgW, bgY + bgH));
                        break;
                    }
                }

                if (!placed) continue;

                canvas.Save();
                canvas.Translate(px, py);
                if (doRotate && Math.Abs(label.RotationDeg) > 1f)
                    canvas.RotateDegrees(label.RotationDeg);

                using var outlinePaint = new SKPaint
                {
                    Color = new SKColor(255, 255, 255, 200),
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = Math.Max(1.5f, fontSize / 10f)
                };

                canvas.DrawText(label.DisplayName, 0, textHeight * 0.35f, SKTextAlign.Center, font, outlinePaint);

                using var fillPaint = new SKPaint
                {
                    Color = new SKColor(102, 102, 102),
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };

                for (int i = 0; i < 3; i++)
                    canvas.DrawText(label.DisplayName, 0, textHeight * 0.35f, SKTextAlign.Center, font, fillPaint);
                canvas.Restore();
            }
        }
    }
}
