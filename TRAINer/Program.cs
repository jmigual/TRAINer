using System.CommandLine;
using OsmSharp.Streams;
using SkiaSharp;
using TRAINer.Data;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var dataOption = new Option<DirectoryInfo?>(
            name: "--data",
            description: "Location of the directory where the OpenStreetMap data is stored", parseArgument: result =>
            {
                // Get current directory
                string path;

                if (result.Tokens.Count == 0)
                {
                    path = Path.Combine(Environment.CurrentDirectory, "data");
                    Console.Error.WriteLine($"Using default location of {path}");
                }
                else
                {
                    path = result.Tokens.Single().Value;
                }

                if (!Directory.Exists(path))
                {
                    result.ErrorMessage = $"Directory {path} does not exist";
                    return null;
                }

                return new DirectoryInfo(path);
            }, isDefault: true);
        var rootCommand = new RootCommand("Sample app for System.CommandLine");
        rootCommand.AddOption(dataOption);

        rootCommand.SetHandler((data) =>
        {
            if (data == null)
            {
                Console.Error.WriteLine("No data directory specified");
                return;
            }
            Process(data);
        }, dataOption);
        return await rootCommand.InvokeAsync(args);
    }

    internal static void Process(DirectoryInfo dataFolder)
    {
        // Find raw rails file. You can generate it from the OSM data using osmium
        // osmium tags-filter -o rails.osm.pbf planet-latest.osm.pbf railway "r/admin_level=2,4" --output-format pbf,add_metadata=false
        var file = new FileInfo(Path.Combine(dataFolder.FullName, "raw", "rails.osm.pbf"));

        if (!file.Exists)
        {
            Console.Error.WriteLine($"Missing raw rails file {file.FullName}");
            return;
        }

        var (nodes, ways) = GetData(file);

        Console.Error.WriteLine($"Found {nodes.Count} nodes and {ways.Length} ways");

        // Now we need to paint the data
        DirectoryInfo latexFolder = new DirectoryInfo(Path.Combine(dataFolder.FullName, "latex"));
        if (!latexFolder.Exists)
        {
            latexFolder.Create();
        }

        var latexFile = new FileInfo(Path.Combine(latexFolder.FullName, "rails.png"));
        PaintPng(latexFile, nodes, ways);
    }

    internal static (Dictionary<long, Node> nodes, Way[] ways) GetData(FileInfo file)
    {
        Dictionary<long, Node> nodes = [];
        List<Way> ways = [];

        var source = new PBFOsmStreamSource(file.OpenRead());
        int count = 0;

        foreach (var element in source)
        {
            if (element.Type == OsmSharp.OsmGeoType.Node)
            {
                var node = (OsmSharp.Node)element;
                if (node == null || node.Id == null || node.Latitude == null || node.Longitude == null)
                {
                    continue;
                }

                Dictionary<string, string> tags = [];
                if (node.Tags != null && node.Tags.TryGetValue("railway", out var railway))
                {
                    tags.Add("railway", railway);
                }

                var ourNode = new Node((float)node.Latitude.Value, (float)node.Longitude.Value, node.Tags);
                nodes.Add(node.Id.Value, ourNode);
            }
            else if (element.Type == OsmSharp.OsmGeoType.Way)
            {
                var way = (OsmSharp.Way)element;
                if (way == null || way.Id == null)
                {
                    continue;
                }

                Way? ourWay = null;
                if (way.Tags != null && way.Tags.ContainsKey("railway"))
                {
                    ourWay = new RailWay(way.Id.Value, way.Nodes, way.Tags);
                }

                ways.Add(ourWay ?? new Way(way.Id.Value, way.Nodes, way.Tags));
            }

            if (++count % 200000 == 0)
            {
                Console.Error.WriteLine($"Processed {count:N0} elements");
            }
        }
        return (nodes, ways.ToArray());
    }

    internal static void PaintPng(FileInfo file, Dictionary<long, Node> nodes, Way[] ways)
    {
        // Find the bounding box
        float minLat = float.PositiveInfinity;
        float maxLat = float.NegativeInfinity;
        float minLon = float.PositiveInfinity;
        float maxLon = float.NegativeInfinity;

        foreach (var node in nodes.Values)
        {
            minLat = Math.Min(minLat, node.Latitude);
            maxLat = Math.Max(maxLat, node.Latitude);
            minLon = Math.Min(minLon, node.Longitude);
            maxLon = Math.Max(maxLon, node.Longitude);
        }

        // Add some margin
        minLat -= 0.1f;
        maxLat += 0.1f;
        minLon -= 0.1f;
        maxLon += 0.1f;

        float earthHorizontalPointDistance = 40075.0f / 360.0f;
        float earthVerticalPointDistance = 40007.0f / 360.0f;

        float ratio = 1;

        // Now we can paint
        var width = 10000;
        var height = (int)(width * ratio);
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        int count = 0;
        foreach (var way in ways)
        {
            if (!way.Visible)
            {
                continue;
            }
            var points = way.Nodes.Select(nodeId => nodes[nodeId]).Select(node => new SKPoint((node.Longitude - minLon) / (maxLon - minLon) * width, height - ((node.Latitude - minLat) / (maxLat - minLat) * height))).ToArray();
            // Convert these points to a path
            var path = new SKPath();
            path.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
            {
                path.LineTo(points[i]);
            }

            // Now we can paint
            var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = way.Weight,
                IsAntialias = true
            };
            canvas.DrawPath(path, paint);

            if (++count % 10000 == 0)
            {
                Console.Error.WriteLine($"Painted {count:N0} ways");
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(file.FullName);
        data.SaveTo(stream);
    }
}
