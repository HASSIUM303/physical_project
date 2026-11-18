using System.Text.Json;

namespace StarSystem.Config;

public static class ConfigLoader
{
    public static SystemConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Файл конфигурации не найден: {path}", path);
        }

        SystemConfig config;
        try
        {
            var json = File.ReadAllText(path);
            config = JsonSerializer.Deserialize<SystemConfig>(json)
                ?? throw new InvalidDataException($"Файл '{Path.GetFileName(path)}' не содержит конфигурацию.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Повреждён JSON в файле '{Path.GetFileName(path)}': {exception.Message}",
                exception);
        }

        Validate(config, path);
        return config;
    }

    private static void Validate(SystemConfig config, string path)
    {
        var errors = new List<string>();
        var fileName = Path.GetFileName(path);

        if (config.Star is null || config.Planet is null || config.Simulation is null
            || config.Field is null || config.Camera is null || config.Window is null)
        {
            throw new InvalidDataException($"В конфигурации '{fileName}' отсутствует обязательный раздел.");
        }

        RequirePositive(config.Star.Radius, "star.radius", errors);
        RequirePositive(config.Star.Gm, "star.gm", errors);
        RequirePositive(config.Planet.Radius, "planet.radius", errors);
        RequirePositive(config.Planet.Mass, "planet.mass", errors);
        RequireFinite(config.Planet.X, "planet.x", errors);
        RequireFinite(config.Planet.Y, "planet.y", errors);
        RequireFinite(config.Planet.Vx, "planet.vx", errors);
        RequireFinite(config.Planet.Vy, "planet.vy", errors);
        RequirePositive(config.Simulation.Dt, "simulation.dt", errors);
        RequirePositive(config.Simulation.StepsPerFrame, "simulation.stepsPerFrame", errors);
        RequirePositive(config.Field.MaxStrength, "field.maxStrength", errors);
        RequirePositive(config.Field.StrengthStep, "field.strengthStep", errors);
        RequirePositive(config.Field.Softening, "field.softening", errors);
        RequireFinite(config.Field.InitialStrength, "field.initialStrength", errors);
        if (config.Field.InitialStrength < 0 || config.Field.InitialStrength > config.Field.MaxStrength)
        {
            errors.Add("field.initialStrength должен быть в диапазоне [0, field.maxStrength]");
        }

        RequirePositive(config.Camera.MinZoom, "camera.minZoom", errors);
        RequirePositive(config.Camera.MaxZoom, "camera.maxZoom", errors);
        if (config.Camera.MinZoom >= config.Camera.MaxZoom)
        {
            errors.Add("camera.minZoom должен быть меньше camera.maxZoom");
        }

        if (!double.IsFinite(config.Camera.InitialZoom)
            || config.Camera.InitialZoom < config.Camera.MinZoom
            || config.Camera.InitialZoom > config.Camera.MaxZoom)
        {
            errors.Add("camera.initialZoom должен находиться в диапазоне minZoom…maxZoom");
        }

        if (config.Window.Width < 400 || config.Window.Height < 400)
        {
            errors.Add("window.width и window.height должны быть не меньше 400");
        }

        if (string.IsNullOrWhiteSpace(config.Window.Title))
        {
            errors.Add("window.title не должен быть пустым");
        }

        if (errors.Count > 0)
        {
            throw new InvalidDataException(
                $"Некорректная конфигурация '{fileName}':{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }

    private static void RequirePositive(double value, string name, List<string> errors)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            errors.Add($"{name} должен быть конечным числом больше нуля");
        }
    }

    private static void RequirePositive(int value, string name, List<string> errors)
    {
        if (value <= 0)
        {
            errors.Add($"{name} должен быть больше нуля");
        }
    }

    private static void RequireFinite(double value, string name, List<string> errors)
    {
        if (!double.IsFinite(value))
        {
            errors.Add($"{name} должен быть конечным числом");
        }
    }
}
