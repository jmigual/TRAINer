using System.Security.Cryptography.X509Certificates;
using OsmSharp.Tags;

namespace TRAINer.Data;

public class RailWay : Way
{
    public enum RailWayType
    {
        Unknown,
        Rail,
        Bridge,
        Station,
        Tram,
        Subway,
        LightRail,
        Monorail,
        Goods
    }

    public static float MaxSpeed { get; protected set; } = 0;

    public static float MinSpeed { get; protected set; } = float.PositiveInfinity;

    public static float MaxGauge { get; protected set; } = 0;

    public static float MinGauge { get; protected set; } = float.PositiveInfinity;

    public float Speed { get; } = 0;

    public float Gauge { get; } = 0;

    public RailWayType Railway { get; } = RailWayType.Unknown;

    public override float Weight
    {
        get
        {
            // This way, we can distinguish visually between narrow gauge and standard gauge
            // as well as between high speed and low speed lines
            return 3
                + 20 * (Math.Clamp(Speed, MinSpeed, MaxSpeed) - MinSpeed) / (MaxSpeed - MinSpeed)
                + 5 * (Math.Clamp(Gauge, MinGauge, MaxGauge) - MinGauge) / (MaxGauge - MinGauge);
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

    public RailWay(long id, long[] nodes, TagsCollectionBase? tags)
        : base(id, nodes)
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
            switch (railway)
            {
                case "bridge":
                    Railway = RailWayType.Bridge;
                    break;
                case "goods":
                    Railway = RailWayType.Goods;
                    break;
                case "light_rail":
                    Railway = RailWayType.LightRail;
                    break;
                case "monorail":
                    Railway = RailWayType.Monorail;
                    break;
                case "rail":
                    Railway = RailWayType.Rail;
                    break;
                case "station":
                    Railway = RailWayType.Station;
                    break;
                case "subway":
                    Railway = RailWayType.Subway;
                    break;
                case "tram":
                    Railway = RailWayType.Tram;
                    break;
            }
        }
    }

    public override bool Visible => true;
}
