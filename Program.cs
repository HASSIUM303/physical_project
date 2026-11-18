using System.Numerics;
using Raylib_cs;
using StarSystem.Config;
using StarSystem.Physics;
using StarSystem.Rendering;

namespace StarSystem;

internal static class Program
{
    private const int TrailCapacity = 2000;

    private static int Main(string[] args)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "config", "system.json");
        SystemConfig config;
        try
        {
            config = ConfigLoader.Load(configPath);
        }
        catch (InvalidDataException exception)
        {
            Console.Error.WriteLine($"Ошибка конфигурации: {exception.Message}");
            return 1;
        }
        catch (IOException exception)
        {
            Console.Error.WriteLine($"Ошибка конфигурации: {exception.Message}");
            return 1;
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.Error.WriteLine($"Нет доступа к конфигурации '{configPath}': {exception.Message}");
            return 1;
        }

        if (args.Contains("--check", StringComparer.Ordinal))
        {
            Checks.RunEnergyCheck(config);
            return 0;
        }

        RunWindow(config);
        return 0;
    }

    private static void RunWindow(SystemConfig config)
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(config.Window.Width, config.Window.Height, config.Window.Title);
        Raylib.SetWindowMinSize(400, 400);
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.Escape);

        Renderer? renderer = null;
        try
        {
            renderer = new Renderer();
            var state = new SimulationState(config);
            while (!Raylib.WindowShouldClose())
            {
                var frameTime = Raylib.GetFrameTime();
                var reset = HandleInput(config, state, frameTime);
                if (!reset)
                {
                    AdvanceSimulation(config, state, frameTime);
                    state.AddTrailPoint();
                }

                var mouseWorld = ScreenToWorld(
                    Raylib.GetMousePosition(),
                    Raylib.GetScreenWidth(),
                    Raylib.GetScreenHeight(),
                    state.CameraX,
                    state.CameraY,
                    state.Zoom);
                renderer.DrawFrame(
                    config,
                    state.Planet,
                    state.Time,
                    state.InitialEnergy,
                    state.FieldStrength,
                    Raylib.IsMouseButtonDown(MouseButton.Left),
                    mouseWorld.X,
                    mouseWorld.Y,
                    state.CameraX,
                    state.CameraY,
                    state.Zoom,
                    state.TrailX,
                    state.TrailY,
                    state.TrailStart,
                    state.TrailCount,
                    state.FallMessageSeconds);
            }
        }
        finally
        {
            renderer?.Dispose();
            Raylib.CloseWindow();
        }
    }

    private static bool HandleInput(SystemConfig config, SimulationState state, float frameTime)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F11))
        {
            Raylib.ToggleFullscreen();
        }

        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            state.ResetSystem();
            return true;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Home))
        {
            state.CameraX = 0;
            state.CameraY = 0;
            state.Zoom = config.Camera.InitialZoom;
        }

        var mousePosition = Raylib.GetMousePosition();
        var wheel = Raylib.GetMouseWheelMove();
        var fieldOn = Raylib.IsMouseButtonDown(MouseButton.Left);
        if (fieldOn && wheel != 0)
        {
            state.FieldStrength = Math.Clamp(
                state.FieldStrength + wheel * config.Field.StrengthStep,
                0,
                config.Field.MaxStrength);
        }
        else if (!fieldOn && wheel != 0)
        {
            ZoomAtCursor(config, state, mousePosition, wheel);
        }

        if (Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            var mouseDelta = Raylib.GetMouseDelta();
            state.CameraX -= mouseDelta.X / state.Zoom;
            state.CameraY += mouseDelta.Y / state.Zoom;
        }

        var cameraSpeed = 400 * frameTime / state.Zoom;
        if (Raylib.IsKeyDown(KeyboardKey.Right))
        {
            state.CameraX += cameraSpeed;
        }

        if (Raylib.IsKeyDown(KeyboardKey.Left))
        {
            state.CameraX -= cameraSpeed;
        }

        if (Raylib.IsKeyDown(KeyboardKey.Up))
        {
            state.CameraY += cameraSpeed;
        }

        if (Raylib.IsKeyDown(KeyboardKey.Down))
        {
            state.CameraY -= cameraSpeed;
        }

        return false;
    }

    private static void ZoomAtCursor(SystemConfig config, SimulationState state, Vector2 cursor, float wheel)
    {
        var width = Raylib.GetScreenWidth();
        var height = Raylib.GetScreenHeight();
        var anchor = ScreenToWorld(cursor, width, height, state.CameraX, state.CameraY, state.Zoom);
        var newZoom = Math.Clamp(
            state.Zoom * Math.Pow(1.1, wheel),
            config.Camera.MinZoom,
            config.Camera.MaxZoom);
        state.CameraX = anchor.X - (cursor.X - width / 2.0) / newZoom;
        state.CameraY = anchor.Y + (cursor.Y - height / 2.0) / newZoom;
        state.Zoom = newZoom;
    }

    private static void AdvanceSimulation(SystemConfig config, SimulationState state, float frameTime)
    {
        state.FallMessageSeconds = Math.Max(0, state.FallMessageSeconds - frameTime);
        var fieldOn = Raylib.IsMouseButtonDown(MouseButton.Left);
        var fieldCenter = ScreenToWorld(
            Raylib.GetMousePosition(),
            Raylib.GetScreenWidth(),
            Raylib.GetScreenHeight(),
            state.CameraX,
            state.CameraY,
            state.Zoom);

        for (var step = 0; step < config.Simulation.StepsPerFrame; step++)
        {
            state.Planet.Step(
                config.Simulation.Dt,
                config.Star.Gm,
                fieldOn,
                fieldCenter.X,
                fieldCenter.Y,
                state.FieldStrength,
                config.Field.Softening);
            state.Time += config.Simulation.Dt;

            if (state.Planet.Radius() < config.Star.Radius + config.Planet.Radius)
            {
                state.ResetSystem();
                state.FallMessageSeconds = 2;
                break;
            }
        }
    }

    private static (double X, double Y) ScreenToWorld(
        Vector2 screen, int width, int height, double cameraX, double cameraY, double zoom)
    {
        return (
            cameraX + (screen.X - width / 2.0) / zoom,
            cameraY - (screen.Y - height / 2.0) / zoom);
    }

    private sealed class SimulationState
    {
        private readonly SystemConfig _config;
        private int _trailNextIndex;

        public Planet Planet { get; }
        public double InitialEnergy { get; }
        public double Time { get; set; }
        public double FieldStrength { get; set; }
        public double CameraX { get; set; }
        public double CameraY { get; set; }
        public double Zoom { get; set; }
        public float FallMessageSeconds { get; set; }
        public double[] TrailX { get; } = new double[TrailCapacity];
        public double[] TrailY { get; } = new double[TrailCapacity];
        public int TrailCount { get; private set; }
        public int TrailStart => (_trailNextIndex - TrailCount + TrailX.Length) % TrailX.Length;

        public SimulationState(SystemConfig config)
        {
            _config = config;
            Planet = new Planet(config.Planet.X, config.Planet.Y, config.Planet.Vx, config.Planet.Vy, config.Planet.Mass);
            InitialEnergy = Planet.TotalEnergy(config.Star.Gm);
            FieldStrength = config.Field.InitialStrength;
            Zoom = config.Camera.InitialZoom;
            AddTrailPoint();
        }

        public void AddTrailPoint()
        {
            TrailX[_trailNextIndex] = Planet.X;
            TrailY[_trailNextIndex] = Planet.Y;
            _trailNextIndex = (_trailNextIndex + 1) % TrailX.Length;
            TrailCount = Math.Min(TrailCount + 1, TrailX.Length);
        }

        public void ResetSystem()
        {
            Planet.ResetTo(_config.Planet.X, _config.Planet.Y, _config.Planet.Vx, _config.Planet.Vy);
            FieldStrength = _config.Field.InitialStrength;
            Time = 0;
            FallMessageSeconds = 0;
            TrailCount = 0;
            _trailNextIndex = 0;
            AddTrailPoint();
        }
    }
}
