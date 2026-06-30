using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using ArcMapAssistant.Core.Detection;
using ArcMapAssistant.Core.Models;
using ArcMapAssistant.Core.Services;
using Microsoft.Win32;

namespace ArcMapAssistant.App;

public partial class MainWindow : Window
{
    private const int MaxPointBackups = 50;
    private const int WmHotkey = 0x0312;
    private const int ToggleOverlayHotkeyId = 1001;
    private const int ReloadConfigHotkeyId = 1002;
    private const int RecordPointHotkeyId = 1003;
    private const int CaptureScreenshotHotkeyId = 1004;
    private const int UndoDebugPointHotkeyId = 1005;

    private readonly string _configDirectory;
    private readonly OverlayWindow _overlayWindow = new();

    private HotkeyService? _hotkeyService;
    private HwndSource? _hwndSource;
    private AppSettings _settings = new();
    private DispatcherTimer? _debugCursorTimer;
    private string _pointsPath = string.Empty;
    private string _settingsPath = string.Empty;
    private string _mapsPath = string.Empty;
    private string _screenshotsDirectory = string.Empty;
    private string _backupsDirectory = string.Empty;
    private string _mapOpenTemplatePath = string.Empty;
    private IReadOnlyList<MapConfig> _mapConfigs = [];
    private bool _isLoadingSettings;
    private readonly ObservableCollection<MapPoint> _editablePoints = new();
    private readonly Stack<string> _debugPointUndoStack = new();
    private ICollectionView? _pointsView;
    private bool _isCapturingFullViewRect;
    private ScreenPoint? _fullViewTopLeft;

    public MainWindow()
    {
        InitializeComponent();
        _configDirectory = ResolveConfigDirectory();
        _pointsView = CollectionViewSource.GetDefaultView(_editablePoints);
        _pointsView.Filter = FilterPoint;
        PointsGrid.ItemsSource = _pointsView;
        BuildFilterPanels();
        BuildDebugPointSelectors();
        BuildListFilterControls();
        BuildBatchTypeControls();
    }

    protected override async void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);

        await ReloadConfigurationAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        _debugCursorTimer?.Stop();
        _hotkeyService?.Dispose();
        _hwndSource?.RemoveHook(WndProc);
        if (_overlayWindow.IsLoaded)
        {
            _overlayWindow.Close();
        }

        base.OnClosed(e);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey)
        {
            return IntPtr.Zero;
        }

        var id = wParam.ToInt32();

        if (id == ToggleOverlayHotkeyId)
        {
            _ = ToggleOverlayAsync();
            handled = true;
        }
        else if (id == ReloadConfigHotkeyId)
        {
            _ = ReloadConfigurationAsync();
            handled = true;
        }
        else if (id == RecordPointHotkeyId)
        {
            _ = RecordDebugPointAsync();
            handled = true;
        }
        else if (id == CaptureScreenshotHotkeyId)
        {
            _ = CaptureScreenshotAsync();
            handled = true;
        }
        else if (id == UndoDebugPointHotkeyId)
        {
            _ = UndoLastDebugPointAsync();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void RegisterHotkeys(IntPtr handle)
    {
        _hotkeyService?.Dispose();
        _hotkeyService = new HotkeyService(handle);

        var toggleRegistered = _hotkeyService.Register(ToggleOverlayHotkeyId, _settings.HotkeyToggleOverlay);
        var reloadRegistered = _hotkeyService.Register(ReloadConfigHotkeyId, _settings.HotkeyReloadConfig);
        var recordRegistered = _hotkeyService.Register(RecordPointHotkeyId, _settings.HotkeyRecordPoint);
        var screenshotRegistered = _hotkeyService.Register(CaptureScreenshotHotkeyId, _settings.HotkeyCaptureScreenshot);
        var undoRegistered = _hotkeyService.Register(UndoDebugPointHotkeyId, _settings.HotkeyUndoDebugPoint);

        if (!toggleRegistered || !reloadRegistered || !recordRegistered || !screenshotRegistered || !undoRegistered)
        {
            StatusText.Text += Environment.NewLine + "Warning: one or more hotkeys failed to register.";
        }
    }

    private async Task ReloadConfigurationAsync()
    {
        try
        {
            var settingsPath = Path.Combine(_configDirectory, "settings.json");
            var pointsPath = Path.Combine(_configDirectory, "points.json");
            var mapsPath = Path.Combine(_configDirectory, "maps.json");
            _settingsPath = settingsPath;
            _pointsPath = pointsPath;
            _mapsPath = mapsPath;
            _screenshotsDirectory = ResolveScreenshotsDirectory(_configDirectory);
            _backupsDirectory = ResolveBackupsDirectory(_configDirectory);
            _mapOpenTemplatePath = ResolveMapOpenTemplatePath(_configDirectory);

            var settingsService = new SettingsService(settingsPath);
            var pointRepository = new PointRepository(pointsPath);
            var mapConfigRepository = new MapConfigRepository(mapsPath);

            _settings = await settingsService.LoadAsync();
            var points = await pointRepository.LoadAsync();
            _mapConfigs = await mapConfigRepository.LoadAsync();

            ReplaceEditablePoints(points);
            ApplyCategoryCheckboxes();
            ApplyDebugPointSelection();
            RefreshPriorityFilterOptions();
            ApplyListFilters();
            _overlayWindow.ApplySettings(_settings);
            RefreshOverlayPoints();
            UpdateDebugCursorTimer();

            StatusText.Text = $"Loaded {points.Count} points from {pointsPath}. Coordinate mode={_settings.PointCoordinateMode}. Overlay is {(_overlayWindow.IsOverlayVisible ? "visible" : "hidden")}.";

            if (_hwndSource?.Handle is { } handle && handle != IntPtr.Zero)
            {
                RegisterHotkeys(handle);
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to reload config: {ex.Message}";
        }
    }

    private async Task ToggleOverlayAsync()
    {
        if (_overlayWindow.IsOverlayVisible)
        {
            _overlayWindow.HideOverlay();
            StatusText.Text = "Overlay is now hidden.";
            return;
        }

        var detectionResult = await DetectMapOpenAsync();
        UpdateDetectionLog(detectionResult);

        if (detectionResult.Success)
        {
            _overlayWindow.ShowOverlay();
            StatusText.Text = $"Map detected: {detectionResult.MapId}, confidence={detectionResult.Confidence:0.000}, threshold={detectionResult.Threshold:0.000}. Overlay is now visible.";
        }
        else
        {
            _overlayWindow.HideOverlay();
            StatusText.Text = $"Map not detected: confidence={detectionResult.Confidence:0.000}, threshold={detectionResult.Threshold:0.000}, message={detectionResult.Message}. Overlay remains hidden.";
        }
    }

    private async Task RecordDebugPointAsync()
    {
        if (!_settings.EnableDebugMode)
        {
            StatusText.Text = "Debug mode is disabled. Enable enableDebugMode before recording points.";
            return;
        }

        if (!GetCursorPos(out var cursorPosition))
        {
            StatusText.Text = "Failed to read current mouse position.";
            return;
        }

        var debugType = MarkerTaxonomy.NormalizeDebugSelection(_settings.DebugPointGroup, _settings.DebugPointType);
        _settings.DebugPointGroup = debugType.Group;
        _settings.DebugPointType = debugType.Type;
        _settings.DebugPointDisplayName = debugType.DisplayName;

        var now = DateTimeOffset.Now;
        var point = new MapPoint
        {
            Id = $"debug_{DateTime.Now:yyyyMMdd_HHmmss_fff}",
            Name = $"Debug Point {DateTime.Now:HH:mm:ss}",
            Group = debugType.Group,
            Type = debugType.Type,
            DisplayName = debugType.DisplayName,
            MapId = "manual_debug",
            X = cursorPosition.X,
            Y = cursorPosition.Y,
            Confidence = 1.0,
            Priority = 1,
            Enabled = true,
            Source = "debug_f10",
            CreatedAt = now,
            UpdatedAt = now,
            Note = "Recorded from debug cursor hotkey"
        };

        point = ConvertRecordedPointToCurrentCoordinateMode(point);

        try
        {
            _editablePoints.Add(point);
            _debugPointUndoStack.Push(point.Id);
            var coordinateText = point.MapX is not null && point.MapY is not null
                ? $"mapX={point.MapX:0}, mapY={point.MapY:0}"
                : $"X={point.X:0}, Y={point.Y:0}";
            await SavePointsAsync($"Recorded {point.Name} as {point.Group}/{point.Type} at {coordinateText}.");
            StatusText.Text = $"Recorded {point.Name} as {point.Group}/{point.Type} at {coordinateText}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to record debug point: {ex.Message}";
        }
    }

    private async Task CaptureScreenshotAsync()
    {
        var shouldRestoreOverlay = _overlayWindow.IsOverlayVisible;

        try
        {
            if (shouldRestoreOverlay)
            {
                _overlayWindow.HideOverlay();
                await Task.Delay(100);
            }

            var savedPath = await Task.Run(() =>
            {
                Directory.CreateDirectory(_screenshotsDirectory);

                var width = GetSystemMetrics(SystemMetricScreenWidth);
                var height = GetSystemMetrics(SystemMetricScreenHeight);
                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException("Primary screen size could not be read.");
                }

                var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                var path = Path.Combine(_screenshotsDirectory, fileName);

                using var bitmap = new Bitmap(width, height);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(width, height));
                bitmap.Save(path, ImageFormat.Png);

                return path;
            });

            StatusText.Text = $"Saved screenshot: {savedPath}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to capture screenshot: {ex.Message}";
        }
        finally
        {
            if (shouldRestoreOverlay)
            {
                _overlayWindow.ShowOverlay();
            }
        }
    }

    private async Task<DetectionResult> DetectMapOpenAsync()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"arc_map_detection_{Guid.NewGuid():N}.png");

        try
        {
            await Task.Run(() => CapturePrimaryScreenToPng(tempPath));

            var detector = new MapOpenDetector(_mapOpenTemplatePath, _settings.ConfidenceThreshold);
            return await Task.Run(() => detector.Detect(tempPath));
        }
        catch (Exception ex)
        {
            return new DetectionResult
            {
                Success = false,
                Matched = false,
                MapId = "buried_ruins",
                Confidence = 0,
                Threshold = _settings.ConfidenceThreshold,
                ElapsedMs = 0,
                TemplateName = Path.GetFileName(_mapOpenTemplatePath),
                Message = ex.Message
            };
        }
        finally
        {
            TryDeleteFile(tempPath);
        }
    }

    private void CapturePrimaryScreenToPng(string path)
    {
        var width = GetSystemMetrics(SystemMetricScreenWidth);
        var height = GetSystemMetrics(SystemMetricScreenHeight);
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("Primary screen size could not be read.");
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(width, height));
        bitmap.Save(path, ImageFormat.Png);
    }

    private void UpdateDetectionLog(DetectionResult result)
    {
        DetectionLogText.Text = $"Detection: confidence={result.Confidence:0.000} matched={result.Matched} elapsed={result.ElapsedMs}ms template={result.TemplateName}";
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup for temporary detection screenshots.
        }
    }

    private void UpdateDebugCursorTimer()
    {
        if (_settings.EnableDebugMode)
        {
            _debugCursorTimer ??= new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _debugCursorTimer.Tick -= DebugCursorTimer_Tick;
            _debugCursorTimer.Tick += DebugCursorTimer_Tick;
            _debugCursorTimer.Start();
        }
        else
        {
            _debugCursorTimer?.Stop();
        }
    }

    private void DebugCursorTimer_Tick(object? sender, EventArgs e)
    {
        if (GetCursorPos(out var cursorPosition))
        {
            _overlayWindow.UpdateDebugCoordinates(cursorPosition.X, cursorPosition.Y);
        }
    }

    private void ToggleOverlay_Click(object sender, RoutedEventArgs e)
    {
        _ = ToggleOverlayAsync();
    }

    private async void CategoryCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        await SaveCategorySettingsAsync();
    }

    private async void DebugPointSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        if (sender == DebugGroupComboBox)
        {
            var selectedGroup = DebugGroupComboBox.SelectedValue as string ?? _settings.DebugPointGroup;
            PopulateDebugTypeComboBox(selectedGroup, null);
        }

        await SaveDebugPointSelectionAsync();
    }

    private void StartFullViewRect_Click(object sender, RoutedEventArgs e)
    {
        _isCapturingFullViewRect = true;
        _fullViewTopLeft = null;
        StatusText.Text = "Full-view calibration started. Move mouse to the visible map top-left corner, then click Capture Corner.";
    }

    private async void CaptureFullViewCorner_Click(object sender, RoutedEventArgs e)
    {
        await CaptureFullViewCornerAsync();
    }

    private void CancelFullViewRect_Click(object sender, RoutedEventArgs e)
    {
        _isCapturingFullViewRect = false;
        _fullViewTopLeft = null;
        StatusText.Text = "Full-view calibration canceled.";
    }

    private async Task CaptureFullViewCornerAsync()
    {
        if (!_isCapturingFullViewRect)
        {
            StatusText.Text = "Click Start Rect before capturing full-view corners.";
            return;
        }

        if (!GetCursorPos(out var cursorPosition))
        {
            StatusText.Text = "Failed to read current mouse position.";
            return;
        }

        if (_fullViewTopLeft is null)
        {
            _fullViewTopLeft = cursorPosition;
            StatusText.Text = $"Captured full-view top-left: X={cursorPosition.X}, Y={cursorPosition.Y}. Move to bottom-right, then click Capture Corner.";
            return;
        }

        await SaveFullViewScreenRectAsync(_fullViewTopLeft.Value, cursorPosition);
        _isCapturingFullViewRect = false;
        _fullViewTopLeft = null;
    }

    private async Task SaveFullViewScreenRectAsync(ScreenPoint firstCorner, ScreenPoint secondCorner)
    {
        var left = Math.Min(firstCorner.X, secondCorner.X);
        var top = Math.Min(firstCorner.Y, secondCorner.Y);
        var width = Math.Abs(secondCorner.X - firstCorner.X);
        var height = Math.Abs(secondCorner.Y - firstCorner.Y);

        if (width <= 0 || height <= 0)
        {
            StatusText.Text = "Full-view rect is invalid. Capture two different corners.";
            return;
        }

        var mapConfigs = _mapConfigs.ToList();
        var mapConfig = mapConfigs.FirstOrDefault(config =>
            string.Equals(config.MapId, _settings.CurrentMapId, StringComparison.OrdinalIgnoreCase));

        if (mapConfig is null)
        {
            StatusText.Text = $"Current map config not found: {_settings.CurrentMapId}.";
            return;
        }

        mapConfig.FullViewScreenRect = new ScreenRect
        {
            Left = left,
            Top = top,
            Width = width,
            Height = height
        };

        var repository = new MapConfigRepository(_mapsPath);
        await repository.SaveAsync(mapConfigs);
        _mapConfigs = mapConfigs;
        RefreshOverlayPoints();

        StatusText.Text = $"Saved fullViewScreenRect for {_settings.CurrentMapId}: left={left}, top={top}, width={width}, height={height}.";
    }

    private async void ReloadAll_Click(object sender, RoutedEventArgs e)
    {
        await ReloadConfigurationAsync();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (IsConfiguredFunctionKey(e.Key, _settings.HotkeyToggleOverlay))
        {
            _ = ToggleOverlayAsync();
            e.Handled = true;
        }
        else if (IsConfiguredFunctionKey(e.Key, _settings.HotkeyReloadConfig))
        {
            _ = ReloadConfigurationAsync();
            e.Handled = true;
        }
        else if (IsConfiguredFunctionKey(e.Key, _settings.HotkeyRecordPoint))
        {
            _ = RecordDebugPointAsync();
            e.Handled = true;
        }
        else if (IsConfiguredFunctionKey(e.Key, _settings.HotkeyCaptureScreenshot))
        {
            _ = CaptureScreenshotAsync();
            e.Handled = true;
        }
        else if (IsConfiguredFunctionKey(e.Key, _settings.HotkeyUndoDebugPoint))
        {
            _ = UndoLastDebugPointAsync();
            e.Handled = true;
        }
    }

    private static bool IsConfiguredFunctionKey(Key pressedKey, string configuredHotkey)
    {
        if (!configuredHotkey.StartsWith("F", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(configuredHotkey[1..], out var functionNumber) || functionNumber is < 1 or > 24)
        {
            return false;
        }

        var expectedKey = Key.F1 + functionNumber - 1;
        return pressedKey == expectedKey;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        await SavePointsAsync();
    }

    private async void Duplicate_Click(object sender, RoutedEventArgs e)
    {
        var selectedPoints = GetSelectedPoints();
        if (selectedPoints.Count == 0)
        {
            StatusText.Text = "Select one or more points before duplicating.";
            return;
        }

        var now = DateTimeOffset.Now;
        var index = 0;
        foreach (var sourcePoint in selectedPoints)
        {
            index++;
            var duplicate = ClonePoint(sourcePoint);
            duplicate.Id = $"{sourcePoint.Id}_copy_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{index}";
            duplicate.Name = string.IsNullOrWhiteSpace(sourcePoint.Name) ? "Point Copy" : $"{sourcePoint.Name} Copy";
            duplicate.Source = "duplicate";
            duplicate.CreatedAt = now;
            duplicate.UpdatedAt = now;
            _editablePoints.Add(duplicate);
        }

        await SavePointsAsync($"Duplicated {selectedPoints.Count} point(s).");
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var selectedPoints = GetSelectedPoints();
        if (selectedPoints.Count == 0)
        {
            StatusText.Text = "Select one or more points before deleting.";
            return;
        }

        foreach (var point in selectedPoints)
        {
            _editablePoints.Remove(point);
        }

        await SavePointsAsync($"Deleted {selectedPoints.Count} point(s).");
    }

    private async void ApplyBatchType_Click(object sender, RoutedEventArgs e)
    {
        var selectedPoints = GetSelectedPoints();
        if (selectedPoints.Count == 0)
        {
            StatusText.Text = "Select one or more points before applying a group/type.";
            return;
        }

        var selectedGroup = BatchGroupComboBox.SelectedValue as string;
        var selectedType = BatchTypeComboBox.SelectedValue as string;
        var definition = MarkerTaxonomy.NormalizeDebugSelection(selectedGroup, selectedType);
        var now = DateTimeOffset.Now;

        foreach (var point in selectedPoints)
        {
            point.Group = definition.Group;
            point.Type = definition.Type;
            point.DisplayName = definition.DisplayName;
            point.UpdatedAt = now;
        }

        await SavePointsAsync($"Updated group/type for {selectedPoints.Count} point(s).");
    }

    private async void EnableSelected_Click(object sender, RoutedEventArgs e)
    {
        await SetSelectedEnabledAsync(true);
    }

    private async void DisableSelected_Click(object sender, RoutedEventArgs e)
    {
        await SetSelectedEnabledAsync(false);
    }

    private async void ExportBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var backupPath = await BackupPointsFileAsync("manual");
            RefreshOverlayPoints();
            StatusText.Text = backupPath is null
                ? "No points.json found to export."
                : $"Exported backup: {backupPath}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to export backup: {ex.Message}";
        }
    }

    private async void ImportBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_backupsDirectory);
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import points backup",
                InitialDirectory = _backupsDirectory,
                Filter = "JSON backup (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            var repository = new PointRepository(dialog.FileName);
            var points = await repository.LoadAsync();
            ReplaceEditablePoints(points);
            await SavePointsAsync($"Imported backup {Path.GetFileName(dialog.FileName)}.");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to import backup: {ex.Message}";
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async Task SavePointsAsync(string? successPrefix = null)
    {
        try
        {
            PointsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            PointsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            TouchSelectedPointsForManualSave();
            await BackupPointsFileAsync("autosave");

            var repository = new PointRepository(_pointsPath);
            await repository.SaveAsync(_editablePoints);
            RefreshPriorityFilterOptions();
            ApplyListFilters();
            RefreshOverlayPoints();

            StatusText.Text = successPrefix is null
                ? $"Saved {_editablePoints.Count} point(s) to {_pointsPath} and refreshed overlay."
                : $"{successPrefix} Saved {_editablePoints.Count} point(s) and refreshed overlay.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to save points: {ex.Message}";
        }
    }

    private async Task SetSelectedEnabledAsync(bool enabled)
    {
        var selectedPoints = GetSelectedPoints();
        if (selectedPoints.Count == 0)
        {
            StatusText.Text = enabled
                ? "Select one or more points before enabling."
                : "Select one or more points before disabling.";
            return;
        }

        var now = DateTimeOffset.Now;
        foreach (var point in selectedPoints)
        {
            point.Enabled = enabled;
            point.UpdatedAt = now;
        }

        await SavePointsAsync($"{(enabled ? "Enabled" : "Disabled")} {selectedPoints.Count} point(s).");
    }

    private async Task UndoLastDebugPointAsync()
    {
        var point = FindUndoableDebugPoint();
        if (point is null)
        {
            StatusText.Text = "No F10 debug point found to undo.";
            return;
        }

        _editablePoints.Remove(point);
        await SavePointsAsync($"Undid debug point {point.Name}.");
    }

    private MapPoint? FindUndoableDebugPoint()
    {
        while (_debugPointUndoStack.Count > 0)
        {
            var pointId = _debugPointUndoStack.Pop();
            var stackedPoint = _editablePoints.FirstOrDefault(point => string.Equals(point.Id, pointId, StringComparison.OrdinalIgnoreCase));
            if (stackedPoint is not null)
            {
                return stackedPoint;
            }
        }

        return _editablePoints
            .Where(point => string.Equals(point.Source, "debug_f10", StringComparison.OrdinalIgnoreCase) ||
                point.Id.StartsWith("debug_", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(point => point.CreatedAt ?? DateTimeOffset.MinValue)
            .ThenByDescending(point => point.Id)
            .FirstOrDefault();
    }

    private List<MapPoint> GetSelectedPoints()
    {
        return PointsGrid.SelectedItems.OfType<MapPoint>().ToList();
    }

    private void TouchSelectedPointsForManualSave()
    {
        var selectedPoints = GetSelectedPoints();
        if (selectedPoints.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        foreach (var point in selectedPoints)
        {
            point.UpdatedAt = now;
        }
    }

    private async Task<string?> BackupPointsFileAsync(string reason)
    {
        if (string.IsNullOrWhiteSpace(_pointsPath) || !File.Exists(_pointsPath))
        {
            return null;
        }

        Directory.CreateDirectory(_backupsDirectory);
        var fileName = $"points_{reason}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json";
        var backupPath = Path.Combine(_backupsDirectory, fileName);

        await using var source = File.OpenRead(_pointsPath);
        await using var destination = File.Create(backupPath);
        await source.CopyToAsync(destination);

        CleanupOldPointBackups();
        return backupPath;
    }

    private void CleanupOldPointBackups()
    {
        if (!Directory.Exists(_backupsDirectory))
        {
            return;
        }

        var backups = Directory
            .EnumerateFiles(_backupsDirectory, "points_*.json")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .ThenByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .Skip(MaxPointBackups)
            .ToList();

        foreach (var backup in backups)
        {
            try
            {
                backup.Delete();
            }
            catch
            {
                // Backup pruning should not block point saves.
            }
        }
    }

    private void ReplaceEditablePoints(IEnumerable<MapPoint> points)
    {
        _editablePoints.Clear();

        foreach (var point in points)
        {
            _editablePoints.Add(point);
        }

        ApplyListFilters();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyListFilters();
    }

    private void ListFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        if (sender == ListGroupFilterComboBox)
        {
            var selectedGroup = GetSelectedComboValue(ListGroupFilterComboBox);
            PopulateTypeComboBox(ListTypeFilterComboBox, selectedGroup, null, includeAll: true);
        }

        ApplyListFilters();
    }

    private void PointsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _overlayWindow.SetHighlightedPointIds(GetSelectedPoints().Select(point => point.Id));
    }

    private void BatchGroupComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        var selectedGroup = GetSelectedComboValue(BatchGroupComboBox);
        PopulateTypeComboBox(BatchTypeComboBox, selectedGroup, null, includeAll: false);
    }

    private void ApplyListFilters()
    {
        if (_pointsView is null)
        {
            return;
        }

        _pointsView.SortDescriptions.Clear();
        var sortMode = GetSelectedComboValue(PrioritySortComboBox);
        if (string.Equals(sortMode, "priority_asc", StringComparison.OrdinalIgnoreCase))
        {
            _pointsView.SortDescriptions.Add(new SortDescription(nameof(MapPoint.Priority), ListSortDirection.Ascending));
        }
        else if (string.Equals(sortMode, "priority_desc", StringComparison.OrdinalIgnoreCase))
        {
            _pointsView.SortDescriptions.Add(new SortDescription(nameof(MapPoint.Priority), ListSortDirection.Descending));
        }

        _pointsView.Refresh();
        UpdatePointCount();
        RefreshOverlayPoints();
    }

    private bool FilterPoint(object item)
    {
        if (item is not MapPoint point)
        {
            return false;
        }

        var query = SearchTextBox?.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(query) &&
            !ContainsIgnoreCase(point.Name, query) &&
            !ContainsIgnoreCase(point.Note, query))
        {
            return false;
        }

        var group = GetSelectedComboValue(ListGroupFilterComboBox);
        if (!string.IsNullOrWhiteSpace(group) &&
            !string.Equals(point.Group, group, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var type = GetSelectedComboValue(ListTypeFilterComboBox);
        if (!string.IsNullOrWhiteSpace(type) &&
            !string.Equals(point.Type, type, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var enabled = GetSelectedComboValue(ListEnabledFilterComboBox);
        if (string.Equals(enabled, "enabled", StringComparison.OrdinalIgnoreCase) && !point.Enabled)
        {
            return false;
        }

        if (string.Equals(enabled, "disabled", StringComparison.OrdinalIgnoreCase) && point.Enabled)
        {
            return false;
        }

        var priority = GetSelectedComboValue(ListPriorityFilterComboBox);
        if (!string.IsNullOrWhiteSpace(priority) &&
            int.TryParse(priority, out var priorityValue) &&
            point.Priority != priorityValue)
        {
            return false;
        }

        return true;
    }

    private void UpdatePointCount()
    {
        var filteredCount = _pointsView?.Cast<MapPoint>().Count() ?? _editablePoints.Count;
        PointCountText.Text = $"Points: {filteredCount} / {_editablePoints.Count}";
    }

    private static bool ContainsIgnoreCase(string value, string query)
    {
        return value?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void ApplyCategoryCheckboxes()
    {
        _isLoadingSettings = true;
        try
        {
            ApplyFilterPanelState(GroupFilterPanel, _settings.EnabledGroups);
            ApplyFilterPanelState(TypeFilterPanel, _settings.EnabledTypes);
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void ApplyDebugPointSelection()
    {
        _isLoadingSettings = true;
        try
        {
            var debugType = MarkerTaxonomy.NormalizeDebugSelection(_settings.DebugPointGroup, _settings.DebugPointType);
            _settings.DebugPointGroup = debugType.Group;
            _settings.DebugPointType = debugType.Type;
            _settings.DebugPointDisplayName = debugType.DisplayName;

            DebugGroupComboBox.SelectedValue = debugType.Group;
            PopulateDebugTypeComboBox(debugType.Group, debugType.Type);
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private async Task SaveCategorySettingsAsync()
    {
        try
        {
            _settings.EnabledGroups = GetSelectedFilterValues(GroupFilterPanel);
            _settings.EnabledTypes = GetSelectedFilterValues(TypeFilterPanel);

            var settingsService = new SettingsService(_settingsPath);
            await settingsService.SaveAsync(_settings);

            _overlayWindow.ApplySettings(_settings);
            RefreshOverlayPoints();

            StatusText.Text = $"Saved filters: groups={_settings.EnabledGroups.Count}, types={_settings.EnabledTypes.Count}. Overlay refreshed.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to save category settings: {ex.Message}";
        }
    }

    private async Task SaveDebugPointSelectionAsync()
    {
        try
        {
            var selectedGroup = DebugGroupComboBox.SelectedValue as string;
            var selectedType = DebugTypeComboBox.SelectedValue as string;
            var debugType = MarkerTaxonomy.NormalizeDebugSelection(selectedGroup, selectedType);

            _settings.DebugPointGroup = debugType.Group;
            _settings.DebugPointType = debugType.Type;
            _settings.DebugPointDisplayName = debugType.DisplayName;

            if (!string.Equals(DebugGroupComboBox.SelectedValue as string, debugType.Group, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(DebugTypeComboBox.SelectedValue as string, debugType.Type, StringComparison.OrdinalIgnoreCase))
            {
                ApplyDebugPointSelection();
            }

            var settingsService = new SettingsService(_settingsPath);
            await settingsService.SaveAsync(_settings);
            RefreshOverlayPoints();

            StatusText.Text = $"Saved F10 point type: {_settings.DebugPointGroup}/{_settings.DebugPointType} ({_settings.DebugPointDisplayName}).";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Failed to save F10 point type: {ex.Message}";
        }
    }

    private void BuildFilterPanels()
    {
        GroupFilterPanel.Children.Clear();
        TypeFilterPanel.Children.Clear();

        foreach (var group in MarkerTaxonomy.Groups)
        {
            GroupFilterPanel.Children.Add(CreateFilterCheckBox(group.Value, group.Key));
        }

        foreach (var type in MarkerTaxonomy.Types.OrderBy(type => type.Group).ThenBy(type => type.DisplayName))
        {
            TypeFilterPanel.Children.Add(CreateFilterCheckBox(type.DisplayName, type.Type));
        }
    }

    private void BuildDebugPointSelectors()
    {
        DebugGroupComboBox.ItemsSource = MarkerTaxonomy.Groups
            .Select(group => new ComboOption(group.Key, group.Value))
            .ToList();
    }

    private void BuildListFilterControls()
    {
        _isLoadingSettings = true;
        try
        {
            ListGroupFilterComboBox.ItemsSource = BuildGroupOptions(includeAll: true);
            ListGroupFilterComboBox.SelectedValue = string.Empty;
            PopulateTypeComboBox(ListTypeFilterComboBox, string.Empty, string.Empty, includeAll: true);

            ListEnabledFilterComboBox.ItemsSource = new List<ComboOption>
            {
                new(string.Empty, "All Enabled"),
                new("enabled", "Enabled"),
                new("disabled", "Disabled")
            };
            ListEnabledFilterComboBox.SelectedValue = string.Empty;

            PrioritySortComboBox.ItemsSource = new List<ComboOption>
            {
                new(string.Empty, "Default Sort"),
                new("priority_asc", "Priority Asc"),
                new("priority_desc", "Priority Desc")
            };
            PrioritySortComboBox.SelectedValue = string.Empty;

            RefreshPriorityFilterOptions();
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void BuildBatchTypeControls()
    {
        _isLoadingSettings = true;
        try
        {
            BatchGroupComboBox.ItemsSource = BuildGroupOptions(includeAll: false);
            var firstGroup = MarkerTaxonomy.Groups.Keys.FirstOrDefault() ?? "special_events";
            BatchGroupComboBox.SelectedValue = firstGroup;
            PopulateTypeComboBox(BatchTypeComboBox, firstGroup, null, includeAll: false);
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void RefreshPriorityFilterOptions()
    {
        if (ListPriorityFilterComboBox is null)
        {
            return;
        }

        var selectedPriority = GetSelectedComboValue(ListPriorityFilterComboBox);
        var options = new List<ComboOption> { new(string.Empty, "All Priority") };
        options.AddRange(_editablePoints
            .Select(point => point.Priority)
            .Distinct()
            .OrderBy(priority => priority)
            .Select(priority => new ComboOption(priority.ToString(), $"Priority {priority}")));

        ListPriorityFilterComboBox.ItemsSource = options;
        ListPriorityFilterComboBox.SelectedValue = options.Any(option => option.Value == selectedPriority)
            ? selectedPriority
            : string.Empty;
    }

    private void PopulateDebugTypeComboBox(string group, string? preferredType)
    {
        var types = MarkerTaxonomy.Types
            .Where(type => string.Equals(type.Group, group, StringComparison.OrdinalIgnoreCase))
            .OrderBy(type => type.DisplayName)
            .Select(type => new ComboOption(type.Type, type.DisplayName))
            .ToList();

        DebugTypeComboBox.ItemsSource = types;

        var selectedType = types.Any(type => string.Equals(type.Value, preferredType, StringComparison.OrdinalIgnoreCase))
            ? preferredType
            : types.FirstOrDefault()?.Value;

        DebugTypeComboBox.SelectedValue = selectedType;
    }

    private void PopulateTypeComboBox(System.Windows.Controls.ComboBox comboBox, string? group, string? preferredType, bool includeAll)
    {
        var types = MarkerTaxonomy.Types.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(group))
        {
            types = types.Where(type => string.Equals(type.Group, group, StringComparison.OrdinalIgnoreCase));
        }

        var options = types
            .OrderBy(type => type.Group)
            .ThenBy(type => type.DisplayName)
            .Select(type => new ComboOption(type.Type, type.DisplayName))
            .ToList();

        if (includeAll)
        {
            options.Insert(0, new ComboOption(string.Empty, "All Types"));
        }

        comboBox.ItemsSource = options;

        var selectedType = options.Any(option => string.Equals(option.Value, preferredType, StringComparison.OrdinalIgnoreCase))
            ? preferredType
            : options.FirstOrDefault()?.Value;

        comboBox.SelectedValue = selectedType;
    }

    private static List<ComboOption> BuildGroupOptions(bool includeAll)
    {
        var options = MarkerTaxonomy.Groups
            .Select(group => new ComboOption(group.Key, group.Value))
            .ToList();

        if (includeAll)
        {
            options.Insert(0, new ComboOption(string.Empty, "All Groups"));
        }

        return options;
    }

    private static string GetSelectedComboValue(System.Windows.Controls.ComboBox? comboBox)
    {
        return comboBox?.SelectedValue as string ?? string.Empty;
    }

    private System.Windows.Controls.CheckBox CreateFilterCheckBox(string label, string value)
    {
        var checkBox = new System.Windows.Controls.CheckBox
        {
            Content = label,
            Tag = value,
            Margin = new Thickness(0, 0, 14, 6),
            VerticalAlignment = VerticalAlignment.Center
        };

        checkBox.Checked += CategoryCheckBox_Changed;
        checkBox.Unchecked += CategoryCheckBox_Changed;
        return checkBox;
    }

    private static void ApplyFilterPanelState(System.Windows.Controls.Panel panel, IEnumerable<string> enabledValues)
    {
        var enabled = new HashSet<string>(enabledValues, StringComparer.OrdinalIgnoreCase);
        foreach (var checkBox in panel.Children.OfType<System.Windows.Controls.CheckBox>())
        {
            checkBox.IsChecked = checkBox.Tag is string value && enabled.Contains(value);
        }
    }

    private static List<string> GetSelectedFilterValues(System.Windows.Controls.Panel panel)
    {
        return panel.Children
            .OfType<System.Windows.Controls.CheckBox>()
            .Where(checkBox => checkBox.IsChecked == true)
            .Select(checkBox => checkBox.Tag as string)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();
    }

    private void RefreshOverlayPoints()
    {
        _overlayWindow.SetPoints(GetOverlayPoints());
        _overlayWindow.SetHighlightedPointIds(GetSelectedPoints().Select(point => point.Id));
    }

    private IReadOnlyList<MapPoint> GetOverlayPoints()
    {
        if (string.Equals(_settings.PointCoordinateMode, "screen", StringComparison.OrdinalIgnoreCase))
        {
            return _editablePoints.ToList();
        }

        var mapConfig = _mapConfigs.FirstOrDefault(config =>
            string.Equals(config.MapId, _settings.CurrentMapId, StringComparison.OrdinalIgnoreCase));

        if (mapConfig is null)
        {
            return _editablePoints.ToList();
        }

        var transformer = new CoordinateTransformer();
        return _editablePoints
            .Select(point => string.Equals(point.MapId, mapConfig.MapId, StringComparison.OrdinalIgnoreCase)
                ? transformer.ToScreenPoint(point, mapConfig, _settings.PointCoordinateMode)
                : point)
            .ToList();
    }

    private MapPoint ConvertRecordedPointToCurrentCoordinateMode(MapPoint point)
    {
        if (string.Equals(_settings.PointCoordinateMode, "screen", StringComparison.OrdinalIgnoreCase))
        {
            return point;
        }

        var mapConfig = _mapConfigs.FirstOrDefault(config =>
            string.Equals(config.MapId, _settings.CurrentMapId, StringComparison.OrdinalIgnoreCase));

        if (mapConfig is null)
        {
            return point;
        }

        var transformer = new CoordinateTransformer();
        return transformer.ToMapPoint(point, mapConfig, _settings.PointCoordinateMode);
    }

    private static string ResolveConfigDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "config");
            var isRepositoryRoot = Directory.Exists(Path.Combine(current.FullName, ".git")) ||
                File.Exists(Path.Combine(current.FullName, "README.md"));

            if (isRepositoryRoot &&
                File.Exists(Path.Combine(candidate, "settings.json")) &&
                File.Exists(Path.Combine(candidate, "points.json")))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "config");
    }

    private static string ResolveScreenshotsDirectory(string configDirectory)
    {
        var configInfo = new DirectoryInfo(configDirectory);
        var rootDirectory = configInfo.Parent?.FullName ?? AppContext.BaseDirectory;
        return Path.Combine(rootDirectory, "debug", "screenshots");
    }

    private static string ResolveBackupsDirectory(string configDirectory)
    {
        var configInfo = new DirectoryInfo(configDirectory);
        var rootDirectory = configInfo.Parent?.FullName ?? AppContext.BaseDirectory;
        return Path.Combine(rootDirectory, "debug", "backups");
    }

    private static string ResolveMapOpenTemplatePath(string configDirectory)
    {
        var configInfo = new DirectoryInfo(configDirectory);
        var rootDirectory = configInfo.Parent?.FullName ?? AppContext.BaseDirectory;
        return Path.Combine(rootDirectory, "assets", "maps", "buried_ruins", "templates", "map_tab_template.png");
    }

    private static MapPoint ClonePoint(MapPoint point)
    {
        return new MapPoint
        {
            Id = point.Id,
            Name = point.Name,
            Group = point.Group,
            Type = point.Type,
            DisplayName = point.DisplayName,
            MapId = point.MapId,
            MapX = point.MapX,
            MapY = point.MapY,
            X = point.X,
            Y = point.Y,
            Confidence = point.Confidence,
            Priority = point.Priority,
            Enabled = point.Enabled,
            Source = point.Source,
            CreatedAt = point.CreatedAt,
            UpdatedAt = point.UpdatedAt,
            Note = point.Note
        };
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out ScreenPoint point);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    private const int SystemMetricScreenWidth = 0;
    private const int SystemMetricScreenHeight = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct ScreenPoint
    {
        public int X;
        public int Y;
    }

    private sealed class ComboOption
    {
        public ComboOption(string value, string displayName)
        {
            Value = value;
            DisplayName = displayName;
        }

        public string Value { get; }
        public string DisplayName { get; }
    }
}
