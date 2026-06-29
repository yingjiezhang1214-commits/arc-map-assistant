using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using ArcMapAssistant.Core.Models;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;

namespace ArcMapAssistant.App;

public partial class OverlayWindow : Window
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolwindow = 0x00000080;
    private const int WsExNoactivate = 0x08000000;

    private IReadOnlyList<MapPoint> _points = Array.Empty<MapPoint>();
    private AppSettings _settings = new();
    private HashSet<string> _highlightedPointIds = new(StringComparer.OrdinalIgnoreCase);
    private bool _clickThroughApplied;

    public bool IsOverlayVisible { get; private set; }

    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        Width = settings.ScreenWidth;
        Height = settings.ScreenHeight;
        MarkerCanvas.Width = settings.ScreenWidth;
        MarkerCanvas.Height = settings.ScreenHeight;
        Opacity = Math.Clamp(settings.OverlayOpacity, 0.05, 1.0);
        DebugCoordinatePanel.Visibility = settings.EnableDebugMode ? Visibility.Visible : Visibility.Collapsed;
        RenderMarkers();
    }

    public void SetPoints(IReadOnlyList<MapPoint> points)
    {
        _points = points;
        RenderMarkers();
    }

    public void SetHighlightedPointIds(IEnumerable<string> pointIds)
    {
        _highlightedPointIds = pointIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        RenderMarkers();
    }

    public void ShowOverlay()
    {
        if (!IsVisible)
        {
            Show();
        }

        ApplyClickThroughStyles();
        IsOverlayVisible = true;
    }

    public void HideOverlay()
    {
        Hide();
        IsOverlayVisible = false;
    }

    public void UpdateDebugCoordinates(int x, int y)
    {
        if (!_settings.EnableDebugMode)
        {
            return;
        }

        DebugCoordinateText.Text = $"X: {x:0000}  Y: {y:0000}";
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyClickThroughStyles();
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        ApplyClickThroughStyles();
    }

    private void RenderMarkers()
    {
        if (MarkerCanvas is null)
        {
            return;
        }

        MarkerCanvas.Children.Clear();

        var enabledGroups = new HashSet<string>(_settings.EnabledGroups, StringComparer.OrdinalIgnoreCase);
        var enabledTypes = new HashSet<string>(_settings.EnabledTypes, StringComparer.OrdinalIgnoreCase);

        foreach (var point in _points.Where(point => point.Enabled && enabledGroups.Contains(point.Group) && enabledTypes.Contains(point.Type)).OrderBy(point => point.Priority))
        {
            AddMarker(point);
        }
    }

    private void AddMarker(MapPoint point)
    {
        var isHighlighted = _highlightedPointIds.Contains(point.Id);
        var markerSize = Math.Max(6, _settings.MarkerSize) + (isHighlighted ? 8 : 0);
        var color = GetMarkerColor(point.Type);

        var marker = new Grid
        {
            Width = _settings.ShowLabels ? 220 : markerSize,
            Height = Math.Max(markerSize, 24),
            IsHitTestVisible = false
        };

        marker.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(markerSize) });
        marker.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var ellipse = new Ellipse
        {
            Width = markerSize,
            Height = markerSize,
            Fill = color,
            Stroke = isHighlighted ? MediaBrushes.Yellow : MediaBrushes.White,
            StrokeThickness = isHighlighted ? 3.5 : 1.5,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = isHighlighted ? 10 : 4,
                ShadowDepth = 0,
                Opacity = isHighlighted ? 0.9 : 0.65
            }
        };

        marker.Children.Add(ellipse);

        if (_settings.ShowLabels)
        {
            var label = new Border
            {
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(6, 2, 6, 2),
                Background = new SolidColorBrush(isHighlighted ? MediaColor.FromArgb(220, 42, 34, 0) : MediaColor.FromArgb(150, 0, 0, 0)),
                CornerRadius = new CornerRadius(3),
                Child = new TextBlock
                {
                    Text = point.Name,
                    Foreground = MediaBrushes.White,
                    FontSize = 12,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 170
                }
            };

            Grid.SetColumn(label, 1);
            marker.Children.Add(label);
        }

        Canvas.SetLeft(marker, point.X - markerSize / 2);
        Canvas.SetTop(marker, point.Y - markerSize / 2);
        MarkerCanvas.Children.Add(marker);
    }

    private static System.Windows.Media.Brush GetMarkerColor(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "loose_loot" => MediaBrushes.Gold,
            "info" => MediaBrushes.DeepSkyBlue,
            "hatch_extract" or "metro_extract" or "metro_entrance" => MediaBrushes.LimeGreen,
            "key" => MediaBrushes.OrangeRed,
            _ => MediaBrushes.White
        };
    }

    private void ApplyClickThroughStyles()
    {
        if (_clickThroughApplied)
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var extendedStyle = GetWindowLong(handle, GwlExstyle);
        SetWindowLong(handle, GwlExstyle, extendedStyle | WsExTransparent | WsExToolwindow | WsExNoactivate);
        _clickThroughApplied = true;
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
}
