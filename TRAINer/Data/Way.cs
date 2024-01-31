using OsmSharp.Tags;

namespace TRAINer.Data;

public class Way
{
    public long Id { get; }
    public long[] Nodes { get; set; }
    public Way(long id, long[] nodes)
    {
        Id = id;
        Nodes = nodes;
    }

    public virtual bool Visible => false;

    public virtual float Weight { get { return 0; } }

    public virtual float Color { get { return 0; } }
}
