using System.Security.Cryptography.X509Certificates;
using OsmSharp.Tags;

namespace TRAINer.Data;

public class RailWay : Way
{
    public static readonly string[] StoreTags = ["railway"];

    public static float MaxSpeed { get; protected set; } = 0;

    public static float MinSpeed { get; protected set; } = float.PositiveInfinity;

    public static float MaxGauge { get; protected set; } = 0;

    public static float MinGauge { get; protected set; } = float.PositiveInfinity;

    public float Speed { get; } = 0;

    public float Gauge { get; } = 0;

    public override float Weight
    {
        get
        {
            // This way, we can distinguish visually between narrow gauge and standard gauge
            // as well as between high speed and low speed lines
            return 1 + (Speed - MinSpeed) / (MaxSpeed - MinSpeed) + (Gauge - MinGauge) / (MaxGauge - MinGauge);
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
                MinGauge = Math.Min(MinGauge, gaugeValue);
                MaxGauge = Math.Max(MaxGauge, gaugeValue);
            }
        }

        if (tags.TryGetValue("maxspeed", out var maxSpeed))
        {
            if (float.TryParse(maxSpeed, out var speedValue))
            {
                Speed = speedValue;
                MinSpeed = Math.Min(MinSpeed, speedValue);
                MaxSpeed = Math.Max(MaxSpeed, speedValue);
            }
        }

        foreach (var acceptedTag in StoreTags)
        {
            if (tags.TryGetValue(acceptedTag, out var value))
            {
                Tags.Add(acceptedTag, value);
            }
        }
    }

    public override bool Visible => true;
}
