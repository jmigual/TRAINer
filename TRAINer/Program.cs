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
        // osmium tags-filter -o rails.osm.pbf -t --output-format pbf,add_metadata=false rails_bak.osm.pbf "w/railway=rail,subway,light_rail,tram,narrow_gauge,bridge,goods,monorail"

        // All railway types: rail, abandoned, subway, light_rail, razed, funicular, tram, narrow_gauge, disused, construction,
        // dismantled, platform, miniature, proposed, historic, overline_bridge, depot, workshop, monorail, roundhouse,
        // turntable, platform_edge, ferry, station, museum, preserved, wash, signal_box, yard, engine_shed, stop, service_station,
        // ventilation_shaft, halt;station, loading_ramp, container_terminal, disused_station, no, train_depot, a, signal_bridge,
        // traverser, subway_entrance, historic_path, ticket_office, goods, interlocking, site, historic_station, level_crossing,
        // water_tower, crossing_box, goods_shed, transfer_shed, coaling_facility, halt, demolished, terminal, planned, technical_center,
        // DE, crossover, yes, crossing, unused, gauge_conversion, train_station_entrance, facility, trolley_rails, works, tram_stop, service,
        // blockpost, station_site, fuel, station_area, control_tower, signal_box_site, track_diagram, bridge, waiting_room, approved, signal_box;crossing_box,
        // terminal_site, phone, interlocking_tower, junction, incline, funicular_entrance, engine shed, switch, store, water_crane, overbridge, spur_junction,
        // buffer_stop, tram_level_crossing, crossing_controller, miniature_facility, watchmans_house, air_shaft, station_master, crane_rail,
        // power_mast, never_built, storage, gantry, pit, single_rail, meadow, Wendeanlage, elevator, crane, track_ballast, ground_frame, office,
        // Ortsstellbereich, tram_crossing, shed, uncompleted, Hilfshandlungstafel, signalbox, waste_disposal, Rangierbezirk,
        // track_scale, loading_gauge, communication, electric_supply, compressed_air_supply, sand_store, abandoned:cableway,
        // booth, technical_station, cranetrack, driveway, 4, disused_platform, ticket_hall, radio, jetty, model, residential, hyperloop,
        // ticket office, loading_rack, loading_zone, abandoned;razed, boat_slipway, abandoned:platform, level, modeltrain, Retarder,
        // weight, railway_crossing, storage_area, switchgear, underline_bridge, 16, debris_pile, shop, Construction, monorack, recovery_train,
        // *, disused:station, ash_pit, telephone, without, signal, train, industrial_rail, proposed:platform, station_building, abandoned:rail, loading_dock

        // Important tags: rail,subway,light_rail,tram,narrow_gauge,bridge,goods,monorail

        var file = new FileInfo(Path.Combine(dataFolder.FullName, "raw", "rails.osm.pbf"));

        if (!file.Exists)
        {
            Console.Error.WriteLine($"Missing raw rails file {file.FullName}");
            return;
        }

        var (nodes, ways) = GetData(file);

        Console.Error.WriteLine($"Found {nodes.Count} nodes and {ways.Length} ways");

        // Now we need to paint the data
        DirectoryInfo output = new DirectoryInfo(Path.Combine(dataFolder.FullName, "output"));
        if (!output.Exists)
        {
            output.Create();
        }

        var latexFile = new FileInfo(Path.Combine(output.FullName, "rails.png"));
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

                var ourNode = new Node((float)node.Latitude.Value, (float)node.Longitude.Value);
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
        Console.Error.WriteLine($"Gauges: {RailWay.MinGauge} - {RailWay.MaxGauge}");
        Console.Error.WriteLine($"Speeds: {RailWay.MinSpeed} - {RailWay.MaxSpeed}");

        // Find the bounding box
        float minLat = float.PositiveInfinity;
        float maxLat = float.NegativeInfinity;
        float minAbsLat = float.PositiveInfinity;
        float minLon = float.PositiveInfinity;
        float maxLon = float.NegativeInfinity;

        foreach (var node in nodes.Values)
        {
            minLat = Math.Min(minLat, node.Latitude);
            maxLat = Math.Max(maxLat, node.Latitude);
            minLon = Math.Min(minLon, node.Longitude);
            maxLon = Math.Max(maxLon, node.Longitude);
            minAbsLat = Math.Min(minAbsLat, Math.Abs(node.Latitude));
        }

        // Calculate the horizontal distance in meters
        var horizontalDistance = (float)(Math.Cos(minAbsLat * Math.PI / 180) * 6371000 * (maxLon - minLon) * Math.PI / 180);
        // Calculate the vertical distance in meters
        var verticalDistance = (float)(6371000 * (maxLat - minLat) * Math.PI / 180);

        float ratio = verticalDistance / horizontalDistance;

        var colorStart = SKColors.DarkGreen;
        var colorEnd = SKColors.RoyalBlue;

        int count = 0;

        var paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true
            };

        // Now we can paint
        var width = 20000;
        var height = (int)(width * ratio);
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        // Sort by Color
        var selectedWays = ways.Where(way => way.Visible).ToArray();
        Array.Sort(selectedWays, (a, b) => a.Color.CompareTo(b.Color));
        foreach (var way in selectedWays)
        {
            var points = way.Nodes.Select(nodeId => nodes[nodeId]).Select(node => new SKPoint((node.Longitude - minLon) / (maxLon - minLon) * width, height - ((node.Latitude - minLat) / (maxLat - minLat) * height))).ToArray();
            // Convert these points to a path
            var path = new SKPath();
            path.MoveTo(points[0]);
            for (int i = 1; i < points.Length; i++)
            {
                path.LineTo(points[i]);
            }

            // Now we can paint
            paint.Color = Lerp(colorStart, colorEnd, way.Color);
            paint.StrokeWidth = way.Weight;

            canvas.DrawPath(path, paint);

            if (++count % 20000 == 0)
            {
                Console.Error.WriteLine($"Painted {count:N0} ways");
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 80);
        using var stream = File.OpenWrite(file.FullName);
        data.SaveTo(stream);
    }

    internal static SKColor Lerp(SKColor cA, SKColor cB, float fraction) {
        fraction = Math.Clamp(fraction, 0, 1);

        var r = (byte)(cA.Red + (cB.Red - cA.Red) * fraction);
        var g = (byte)(cA.Green + (cB.Green - cA.Green) * fraction);
        var b = (byte)(cA.Blue + (cB.Blue - cA.Blue) * fraction);
        var a = (byte)(cA.Alpha + (cB.Alpha - cA.Alpha) * fraction);

        return new SKColor(r, g, b, a);
    }
}
