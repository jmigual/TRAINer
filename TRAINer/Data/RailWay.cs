using System.Security.Cryptography.X509Certificates;
using OsmSharp.Tags;

namespace TRAINer.Data;

public class RailWay : Way
{
    public static float MaxSpeed { get; protected set; } = 0;

    public static float MinSpeed { get; protected set; } = float.PositiveInfinity;

    public static float MaxGauge { get; protected set; } = 0;

    public static float MinGauge { get; protected set; } = float.PositiveInfinity;

    public float Speed { get; } = 0;

    public float Gauge { get; } = 0;

    public string Railway { get; } = "";

    public override float Weight
    {
        get
        {
            // This way, we can distinguish visually between narrow gauge and standard gauge
            // as well as between high speed and low speed lines
            return 2 + 10 * (Math.Clamp(Speed, MinSpeed, MaxSpeed) - MinSpeed) / (MaxSpeed - MinSpeed) + 5 * (Math.Clamp(Gauge, MinGauge, MaxGauge) - MinGauge) / (MaxGauge - MinGauge);
        }
    }

    public override float Color
    {
        get
        {
            // This way, we can distinguish visually between narrow gauge and standard gauge
            // as well as between high speed and low speed lines
            return (Gauge - MinGauge) / (MaxGauge - MinGauge);
        }
    }

    public RailWay(long id, long[] nodes, TagsCollectionBase? tags) : base(id, nodes, tags)
    {
        if (tags == null)
        {
            return;
        }

        if (tags.TryGetValue("gauge", out var gauge))
        {
            if (float.TryParse(gauge, out var gaugeValue))
            {
                Gauge = gaugeValue;
                MinGauge = Math.Max(Math.Min(MinGauge, gaugeValue), 800);
                MaxGauge = Math.Min(Math.Max(MaxGauge, gaugeValue), 1700);
            }
        }

        if (tags.TryGetValue("maxspeed", out var maxSpeed))
        {
            if (float.TryParse(maxSpeed, out var speedValue))
            {
                Speed = speedValue;
                MinSpeed = Math.Max(Math.Min(MinSpeed, speedValue), 10);
                MaxSpeed = Math.Min(Math.Max(MaxSpeed, speedValue), 400);
            }
        }

        if (tags.TryGetValue("railway", out var railway))
        {
            Railway = railway;
        }
    }

    public override bool Visible => true;
}
