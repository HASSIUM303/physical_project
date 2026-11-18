using System.Numerics;
using Raylib_cs;
using StarSystem.Config;
using StarSystem.Physics;

namespace StarSystem.Rendering;

public sealed class Renderer : IDisposable
{
    private const byte GridAlpha = 40;
    private static readonly double[] NiceGridFactors = [1.0, 2.0, 5.0];
    private readonly bool _hasCyrillicFont;
    private readonly Font _font;

    public Renderer()
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "assets", "DejaVuSans.ttf");
        if (File.Exists(fontPath))
        {
            var codepoints = new int[95 + 256];
            for (var index = 0; index < 95; index++)
            {
                codepoints[index] = 32 + index;
            }

            for (var index = 0; index < 256; index++)
            {
                codepoints[95 + index] = 0x0400 + index;
            }

            _font = Raylib.LoadFontEx(fontPath, 20, codepoints, codepoints.Length);
            _hasCyrillicFont = Raylib.IsFontValid(_font);
            if (!_hasCyrillicFont)
            {
                Console.Error.WriteLine($"Не удалось загрузить шрифт '{fontPath}', используются английские подписи.");
                _font = Raylib.GetFontDefault();
            }
        }
        else
        {
            _font = Raylib.GetFontDefault();
            Console.Error.WriteLine($"Шрифт '{fontPath}' не найден, используются английские подписи.");
        }
    }

    public void DrawFrame(
        SystemConfig config,
        Planet planet,
        double time,
        double initialEnergy,
        double fieldStrength,
        bool fieldOn,
        double fieldX,
        double fieldY,
        double cameraX,
        double cameraY,
        double zoom,
        double[] trailX,
        double[] trailY,
        int trailStart,
        int trailCount,
        float fallMessageSeconds)
    {
        var screenWidth = Raylib.GetScreenWidth();
        var screenHeight = Raylib.GetScreenHeight();
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(10, 12, 30, 255));

        DrawGrid(screenWidth, screenHeight, cameraX, cameraY, zoom);
        DrawField(screenWidth, screenHeight, cameraX, cameraY, zoom, fieldStrength, fieldOn, fieldX, fieldY);
        DrawTrail(trailX, trailY, trailStart, trailCount, screenWidth, screenHeight, cameraX, cameraY, zoom);
        DrawBodies(config, planet, screenWidth, screenHeight, cameraX, cameraY, zoom);
        DrawPanel(config, planet, time, initialEnergy, fieldStrength, zoom, screenWidth, screenHeight);
        if (fallMessageSeconds > 0)
        {
            DrawFallMessage(screenWidth, screenHeight);
        }

        Raylib.EndDrawing();
    }

    public void Dispose()
    {
        if (_hasCyrillicFont)
        {
            Raylib.UnloadFont(_font);
        }
    }

    private static void DrawGrid(int width, int height, double cameraX, double cameraY, double zoom)
    {
        var step = SelectGridStep(zoom);
        var minX = cameraX - width / (2.0 * zoom);
        var maxX = cameraX + width / (2.0 * zoom);
        var minY = cameraY - height / (2.0 * zoom);
        var maxY = cameraY + height / (2.0 * zoom);
        var gridColor = new Color(255, 255, 255, (int)GridAlpha);
        var axisColor = new Color(255, 255, 255, 80);

        for (var x = Math.Floor(minX / step) * step; x <= maxX + step; x += step)
        {
            var color = Math.Abs(x) < step * 1e-8 ? axisColor : gridColor;
            var start = ToScreen(x, minY, width, height, cameraX, cameraY, zoom);
            var end = ToScreen(x, maxY, width, height, cameraX, cameraY, zoom);
            Raylib.DrawLineEx(start, end, 1, color);
        }

        for (var y = Math.Floor(minY / step) * step; y <= maxY + step; y += step)
        {
            var color = Math.Abs(y) < step * 1e-8 ? axisColor : gridColor;
            var start = ToScreen(minX, y, width, height, cameraX, cameraY, zoom);
            var end = ToScreen(maxX, y, width, height, cameraX, cameraY, zoom);
            Raylib.DrawLineEx(start, end, 1, color);
        }
    }

    private static double SelectGridStep(double zoom)
    {
        var target = 100.0 / zoom;
        var exponent = (int)Math.Floor(Math.Log10(target));
        var bestStep = target;
        var bestDistance = double.PositiveInfinity;
        for (var decade = exponent - 1; decade <= exponent + 1; decade++)
        {
            var scale = Math.Pow(10, decade);
            foreach (var factor in NiceGridFactors)
            {
                var candidate = factor * scale;
                var pixels = candidate * zoom;
                if (pixels is < 50 or > 150)
                {
                    continue;
                }

                var distance = Math.Abs(pixels - 100);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestStep = candidate;
                }
            }
        }

        return bestStep;
    }

    private static void DrawField(
        int width, int height, double cameraX, double cameraY, double zoom,
        double strength, bool fieldOn, double fieldX, double fieldY)
    {
        var center = ToScreen(fieldX, fieldY, width, height, cameraX, cameraY, zoom);
        var radius = Math.Max(3, strength * 10);
        if (fieldOn)
        {
            Raylib.DrawCircleV(center, (float)radius, new Color(165, 80, 255, 55));
            Raylib.DrawCircleLinesV(center, (float)radius, new Color(190, 130, 255, 180));
        }
        else
        {
            Raylib.DrawCircleLinesV(center, (float)radius, new Color(190, 130, 255, 150));
        }
    }

    private static void DrawTrail(
        double[] trailX, double[] trailY, int start, int count,
        int width, int height, double cameraX, double cameraY, double zoom)
    {
        for (var index = 1; index < count; index++)
        {
            var previous = (start + index - 1) % trailX.Length;
            var current = (start + index) % trailX.Length;
            var from = ToScreen(trailX[previous], trailY[previous], width, height, cameraX, cameraY, zoom);
            var to = ToScreen(trailX[current], trailY[current], width, height, cameraX, cameraY, zoom);
            var alpha = (byte)(20 + 210 * index / count);
            Raylib.DrawLineEx(from, to, 2, new Color(180, 190, 210, (int)alpha));
        }
    }

    private static void DrawBodies(
        SystemConfig config, Planet planet, int width, int height,
        double cameraX, double cameraY, double zoom)
    {
        var star = ToScreen(0, 0, width, height, cameraX, cameraY, zoom);
        var starRadius = (float)Math.Max(2, config.Star.Radius * zoom);
        Raylib.DrawCircleV(star, starRadius, new Color(255, 205, 70, 255));

        var planetPosition = ToScreen(planet.X, planet.Y, width, height, cameraX, cameraY, zoom);
        var planetRadius = (float)Math.Max(2, config.Planet.Radius * zoom);
        Raylib.DrawCircleV(planetPosition, planetRadius, new Color(80, 190, 255, 255));
    }

    private void DrawPanel(
        SystemConfig config, Planet planet, double time, double initialEnergy,
        double fieldStrength, double zoom, int width, int height)
    {
        var lines = _hasCyrillicFont
            ? new[]
            {
                $"t  = {time:F3}",
                $"r  = {planet.Radius():F4}",
                $"v  = {planet.Speed():F4}",
                $"E  = {planet.TotalEnergy(config.Star.Gm):F4}",
                $"E0 = {initialEnergy:F4}",
                $"K  = {fieldStrength:F1} (ЛКМ — поле, колёсико при ЛКМ — мощность)",
                $"Масштаб: {zoom:F0} px/ед."
            }
            : new[]
            {
                $"t  = {time:F3}",
                $"r  = {planet.Radius():F4}",
                $"v  = {planet.Speed():F4}",
                $"E  = {planet.TotalEnergy(config.Star.Gm):F4}",
                $"E0 = {initialEnergy:F4}",
                $"K  = {fieldStrength:F1} (LMB: field, wheel: strength)",
                $"Zoom: {zoom:F0} px/unit"
            };

        var fontSize = Math.Clamp(width / 55f, 8f, 18f);
        var lineHeight = (int)Math.Ceiling(fontSize * 1.4);
        var panelHeight = lines.Length * lineHeight + 14;
        Raylib.DrawRectangle(10, 10, Math.Min(width - 20, 690), panelHeight, new Color(5, 8, 24, 205));
        for (var index = 0; index < lines.Length; index++)
        {
            DrawText(lines[index], new Vector2(20, 16 + index * lineHeight), fontSize, Color.RayWhite);
        }

        var hint = _hasCyrillicFont
            ? "ЛКМ — поле, колёсико — масштаб или мощность, ПКМ/стрелки — камера, R — сброс, F11 — полный экран, Esc — выход"
            : "LMB: field, wheel: zoom/strength, RMB/arrows: camera, R: reset, F11: fullscreen, Esc: exit";
        var hintSize = Math.Clamp(width / 65f, 9f, 16f);
        var hintLines = WrapText(hint, width - 24, hintSize);
        var hintLineHeight = (int)Math.Ceiling(hintSize * 1.35);
        var firstHintY = Math.Max(8, height - 8 - hintLines.Count * hintLineHeight);
        for (var index = 0; index < hintLines.Count; index++)
        {
            DrawText(hintLines[index], new Vector2(12, firstHintY + index * hintLineHeight), hintSize, Color.RayWhite);
        }
    }

    private void DrawFallMessage(int width, int height)
    {
        var message = _hasCyrillicFont ? "Падение на звезду" : "Planet fell into the star";
        var size = 30f;
        var dimensions = Raylib.MeasureTextEx(_font, message, size, 1);
        var position = new Vector2((width - dimensions.X) / 2, (height - dimensions.Y) / 2);
        Raylib.DrawRectangle((int)position.X - 14, (int)position.Y - 8,
            (int)dimensions.X + 28, (int)dimensions.Y + 16, new Color(5, 8, 24, 220));
        DrawText(message, position, size, new Color(255, 215, 120, 255));
    }

    private void DrawText(string text, Vector2 position, float size, Color color)
    {
        Raylib.DrawTextEx(_font, text, position, size, 1, color);
    }

    private List<string> WrapText(string text, float maxWidth, float fontSize)
    {
        var lines = new List<string>();
        var current = string.Empty;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = current.Length == 0 ? word : $"{current} {word}";
            if (current.Length > 0 && Raylib.MeasureTextEx(_font, candidate, fontSize, 1).X > maxWidth)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = candidate;
            }
        }

        if (current.Length > 0)
        {
            lines.Add(current);
        }

        return lines;
    }

    private static Vector2 ToScreen(
        double x, double y, int width, int height,
        double cameraX, double cameraY, double zoom)
    {
        return new Vector2(
            (float)(width / 2.0 + (x - cameraX) * zoom),
            (float)(height / 2.0 - (y - cameraY) * zoom));
    }
}
