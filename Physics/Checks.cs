using System.Globalization;
using StarSystem.Config;

namespace StarSystem.Physics;

public static class Checks
{
    public static void RunEnergyCheck(SystemConfig config)
    {
        var planet = new Planet(
            config.Planet.X,
            config.Planet.Y,
            config.Planet.Vx,
            config.Planet.Vy,
            config.Planet.Mass);
        var initialEnergy = planet.TotalEnergy(config.Star.Gm);
        var duration = 10.0 * 2.0 * Math.PI;
        var maxRelativeError = 0.0;
        var time = 0.0;
        var steps = 0;

        Console.WriteLine("step       t             x             y             E          (E-E0)/|E0|");
        while (time < duration)
        {
            // Закон всемирного тяготения: проверка интегрирования изолированной системы без внешнего поля.
            planet.Step(config.Simulation.Dt, config.Star.Gm, false, 0, 0, 0, config.Field.Softening);
            time += config.Simulation.Dt;
            steps++;
            var energy = planet.TotalEnergy(config.Star.Gm);
            var relativeError = RelativeError(energy, initialEnergy);
            maxRelativeError = Math.Max(maxRelativeError, Math.Abs(relativeError));

            if (steps % 1000 == 0)
            {
                PrintRow(steps, time, planet, energy, relativeError);
            }
        }

        if (steps % 1000 != 0)
        {
            PrintRow(steps, time, planet, planet.TotalEnergy(config.Star.Gm),
                RelativeError(planet.TotalEnergy(config.Star.Gm), initialEnergy));
        }

        Console.WriteLine($"max |E-E0|/|E0| = {maxRelativeError.ToString("F6", CultureInfo.InvariantCulture)}");
        Console.WriteLine(maxRelativeError < 0.001 ? "Вердикт: OK" : "Вердикт: ПРЕВЫШЕНО");
        // Если проверка не проходит, уменьшайте simulation.dt; для метода Эйлера–Кромера ошибка снижается вместе с шагом.
    }

    private static double RelativeError(double energy, double initialEnergy)
    {
        // Относительное изменение полной механической энергии относительно E0.
        return initialEnergy == 0
            ? double.PositiveInfinity
            : (energy - initialEnergy) / Math.Abs(initialEnergy);
    }

    private static void PrintRow(int step, double time, Planet planet, double energy, double relativeError)
    {
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"{step,-8} {time,10:F6} {planet.X,13:F6} {planet.Y,13:F6} {energy,13:F6} {relativeError,14:F6}"));
    }
}
