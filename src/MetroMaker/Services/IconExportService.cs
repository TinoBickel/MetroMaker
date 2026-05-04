using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MetroMaker.Models;

namespace MetroMaker.Services;

public class IconExportService
{
    private const int OutputSize = 24;
    private const double SymbolSize = 15.0;
    private static readonly Color IconColor = Color.FromRgb(0xFF, 0xFF, 0xFF);

    private readonly BitmapImage _circleTemplate;

    public IconExportService()
    {
        var templatePath = FindTemplatePath();
        _circleTemplate = new BitmapImage();
        _circleTemplate.BeginInit();
        _circleTemplate.UriSource = new Uri(templatePath, UriKind.Absolute);
        _circleTemplate.CacheOption = BitmapCacheOption.OnLoad;
        _circleTemplate.EndInit();
        _circleTemplate.Freeze();
    }

    public DrawingImage RenderPreview(IconEntry icon, double renderSize)
    {
        var drawingGroup = new DrawingGroup();
        var scale = renderSize / OutputSize;

        using (var context = drawingGroup.Open())
        {
            var imageRect = new Rect(0, 0, renderSize, renderSize);
            context.DrawImage(_circleTemplate, imageRect);

            var center = new Point(renderSize / 2, renderSize / 2);
            DrawIconSymbol(context, icon.PathData, center, SymbolSize * scale);
        }

        drawingGroup.Freeze();
        return new DrawingImage(drawingGroup);
    }

    public string Export(IconEntry icon, string filename, string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        var drawingVisual = new DrawingVisual();
        using (var context = drawingVisual.RenderOpen())
        {
            var imageRect = new Rect(0, 0, OutputSize, OutputSize);
            context.DrawImage(_circleTemplate, imageRect);

            var center = new Point(OutputSize / 2.0, OutputSize / 2.0);
            DrawIconSymbol(context, icon.PathData, center, SymbolSize);
        }

        var renderBitmap = new RenderTargetBitmap(OutputSize, OutputSize, 96, 96, PixelFormats.Pbgra32);
        renderBitmap.Render(drawingVisual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        var sanitizedFilename = SanitizeFilename(filename);
        var filePath = Path.Combine(outputDir, $"{sanitizedFilename}.png");

        using var stream = File.Create(filePath);
        encoder.Save(stream);

        return filePath;
    }

    private static void DrawIconSymbol(DrawingContext context, string pathData, Point center, double targetSize)
    {
        var geometry = Geometry.Parse(pathData);
        var bounds = geometry.Bounds;

        if (bounds.Width == 0 || bounds.Height == 0)
            return;

        var iconScale = targetSize / Math.Max(bounds.Width, bounds.Height);

        var transform = new TransformGroup();
        transform.Children.Add(new TranslateTransform(
            -bounds.Left - bounds.Width / 2,
            -bounds.Top - bounds.Height / 2));
        transform.Children.Add(new ScaleTransform(iconScale, iconScale));
        transform.Children.Add(new TranslateTransform(center.X, center.Y));
        transform.Freeze();

        var brush = new SolidColorBrush(IconColor);
        brush.Freeze();

        context.PushTransform(transform);
        context.DrawGeometry(brush, null, geometry);
        context.Pop();
    }

    private static string FindTemplatePath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var searchPaths = new[]
        {
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "References", "_Sample.png"),
            Path.Combine(baseDir, "References", "_Sample.png"),
            Path.Combine(baseDir, "_Sample.png"),
        };

        foreach (var path in searchPaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                return fullPath;
        }

        throw new FileNotFoundException("Circle template '_Sample.png' not found in References folder.");
    }

    private static string SanitizeFilename(string filename)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(filename.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
