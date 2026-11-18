namespace StarSystem.Physics;

public sealed class Planet
{
    public double X;
    public double Y;
    public double VX;
    public double VY;
    public double Mass;

    /// <summary>Начальное положение и скорость планеты в безразмерных координатах.</summary>
    public Planet(double x, double y, double vx, double vy, double mass)
    {
        X = x;
        Y = y;
        VX = vx;
        VY = vy;
        Mass = mass;
    }

    /// <summary>Закон всемирного тяготения и второй закон Ньютона; интегратор Эйлера–Кромера.</summary>
    public void Step(double dt, double gm, bool fieldOn, double cx, double cy, double k, double eps)
    {
        var radiusSquared = X * X + Y * Y;
        var radiusCubed = radiusSquared * Math.Sqrt(radiusSquared);
        var ax = -gm * X / radiusCubed;
        var ay = -gm * Y / radiusCubed;

        if (fieldOn)
        {
            // Поле курсора является внешним полем, а не силой звезды.
            var dx = X - cx;
            var dy = Y - cy;
            var softenedDistanceCubed = Math.Pow(dx * dx + dy * dy + eps * eps, 1.5);
            ax -= k * dx / softenedDistanceCubed;
            ay -= k * dy / softenedDistanceCubed;
        }

        // Эйлер–Кромер: скорость обновляется раньше положения, что помогает сохранять энергию на больших временах.
        VX += ax * dt;
        VY += ay * dt;
        X += VX * dt;
        Y += VY * dt;
    }

    /// <summary>Формула расстояния до начала координат r = sqrt(x² + y²).</summary>
    public double Radius() => Math.Sqrt(X * X + Y * Y);

    /// <summary>Формула модуля скорости |v| = sqrt(vx² + vy²).</summary>
    public double Speed() => Math.Sqrt(VX * VX + VY * VY);

    /// <summary>Кинетическая энергия по формуле Eкин = m(vx² + vy²)/2.</summary>
    public double KineticEnergy() => Mass * (VX * VX + VY * VY) / 2.0;

    /// <summary>Потенциальная энергия звезды по формуле Eпот = −GMm/r.</summary>
    public double PotentialEnergy(double gm) => -gm * Mass / Radius();

    /// <summary>Полная механическая энергия E = Eкин + Eпот.</summary>
    public double TotalEnergy(double gm) => KineticEnergy() + PotentialEnergy(gm);

    /// <summary>Сброс начальных условий движения.</summary>
    public void ResetTo(double x, double y, double vx, double vy)
    {
        X = x;
        Y = y;
        VX = vx;
        VY = vy;
    }
}
