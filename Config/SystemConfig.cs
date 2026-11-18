using System.Text.Json.Serialization;

namespace StarSystem.Config;

public sealed class SystemConfig
{
    [JsonPropertyName("star")]
    public StarConfig Star { get; set; } = new();

    [JsonPropertyName("planet")]
    public PlanetConfig Planet { get; set; } = new();

    [JsonPropertyName("simulation")]
    public SimulationConfig Simulation { get; set; } = new();

    [JsonPropertyName("field")]
    public FieldConfig Field { get; set; } = new();

    [JsonPropertyName("camera")]
    public CameraConfig Camera { get; set; } = new();

    [JsonPropertyName("window")]
    public WindowConfig Window { get; set; } = new();

    public sealed class StarConfig
    {
        [JsonPropertyName("radius")]
        public double Radius { get; set; }

        [JsonPropertyName("gm")]
        public double Gm { get; set; }
    }

    public sealed class PlanetConfig
    {
        [JsonPropertyName("radius")]
        public double Radius { get; set; }

        [JsonPropertyName("mass")]
        public double Mass { get; set; }

        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("vx")]
        public double Vx { get; set; }

        [JsonPropertyName("vy")]
        public double Vy { get; set; }
    }

    public sealed class SimulationConfig
    {
        [JsonPropertyName("dt")]
        public double Dt { get; set; }

        [JsonPropertyName("stepsPerFrame")]
        public int StepsPerFrame { get; set; }
    }

    public sealed class FieldConfig
    {
        [JsonPropertyName("initialStrength")]
        public double InitialStrength { get; set; }

        [JsonPropertyName("maxStrength")]
        public double MaxStrength { get; set; }

        [JsonPropertyName("strengthStep")]
        public double StrengthStep { get; set; }

        [JsonPropertyName("softening")]
        public double Softening { get; set; }
    }

    public sealed class CameraConfig
    {
        [JsonPropertyName("initialZoom")]
        public double InitialZoom { get; set; }

        [JsonPropertyName("minZoom")]
        public double MinZoom { get; set; }

        [JsonPropertyName("maxZoom")]
        public double MaxZoom { get; set; }
    }

    public sealed class WindowConfig
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
    }
}
