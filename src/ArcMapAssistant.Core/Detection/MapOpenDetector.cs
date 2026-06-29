using ArcMapAssistant.Core.Models;
using OpenCvSharp;
using System.Diagnostics;

namespace ArcMapAssistant.Core.Detection;

public sealed class MapOpenDetector
{
    private readonly string _templatePath;
    private readonly double _threshold;

    public MapOpenDetector(string templatePath, double threshold)
    {
        _templatePath = templatePath;
        _threshold = threshold;
    }

    public DetectionResult Detect(string screenshotPath)
    {
        var stopwatch = Stopwatch.StartNew();
        var templateName = Path.GetFileName(_templatePath);

        if (!File.Exists(_templatePath))
        {
            return Failed(0, templateName, stopwatch.ElapsedMilliseconds, $"Template not found: {_templatePath}");
        }

        if (!File.Exists(screenshotPath))
        {
            return Failed(0, templateName, stopwatch.ElapsedMilliseconds, $"Screenshot not found: {screenshotPath}");
        }

        using var screenshot = Cv2.ImRead(screenshotPath, ImreadModes.Grayscale);
        using var template = Cv2.ImRead(_templatePath, ImreadModes.Grayscale);

        if (screenshot.Empty())
        {
            return Failed(0, templateName, stopwatch.ElapsedMilliseconds, "Screenshot could not be read.");
        }

        if (template.Empty())
        {
            return Failed(0, templateName, stopwatch.ElapsedMilliseconds, "Template could not be read.");
        }

        var roi = GetTopMapTabSearchRegion(screenshot.Width, screenshot.Height, template.Width, template.Height);
        if (roi.Width < template.Width || roi.Height < template.Height)
        {
            return Failed(0, templateName, stopwatch.ElapsedMilliseconds, "Search region is smaller than template.");
        }

        using var searchRegion = new Mat(screenshot, roi);
        using var result = new Mat();

        Cv2.MatchTemplate(searchRegion, template, result, TemplateMatchModes.CCoeffNormed);
        Cv2.MinMaxLoc(result, out _, out var maxValue, out _, out _);
        stopwatch.Stop();

        return new DetectionResult
        {
            Success = maxValue >= _threshold,
            Matched = maxValue >= _threshold,
            MapId = "buried_ruins",
            Confidence = maxValue,
            Threshold = _threshold,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            TemplateName = templateName,
            Message = maxValue >= _threshold ? "map_open_template_matched" : "map_open_template_below_threshold"
        };
    }

    private DetectionResult Failed(double confidence, string templateName, long elapsedMs, string message)
    {
        return new DetectionResult
        {
            Success = false,
            Matched = false,
            MapId = "buried_ruins",
            Confidence = confidence,
            Threshold = _threshold,
            ElapsedMs = elapsedMs,
            TemplateName = templateName,
            Message = message
        };
    }

    private static Rect GetTopMapTabSearchRegion(int imageWidth, int imageHeight, int templateWidth, int templateHeight)
    {
        var left = (int)Math.Round(imageWidth * 0.43);
        var top = 0;
        var width = (int)Math.Round(imageWidth * 0.18);
        var height = (int)Math.Round(imageHeight * 0.10);

        width = Math.Max(width, templateWidth);
        height = Math.Max(height, templateHeight);

        if (left + width > imageWidth)
        {
            left = Math.Max(0, imageWidth - width);
        }

        if (top + height > imageHeight)
        {
            top = Math.Max(0, imageHeight - height);
        }

        return new Rect(left, top, Math.Min(width, imageWidth - left), Math.Min(height, imageHeight - top));
    }
}
