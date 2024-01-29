using System.CommandLine;
using OSMPBF;

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
        // Create folder if it doesn't exist
        var rawDataFolder = new DirectoryInfo(Path.Combine(dataFolder.FullName, "raw"));

        if (!rawDataFolder.Exists)
        {
            Console.Error.WriteLine($"Missing raw data folder {rawDataFolder.FullName}");
        }

        // Find files ending in .osm.pbf
        var files = rawDataFolder.EnumerateFiles("*.osm.pbf", SearchOption.AllDirectories);

        Console.WriteLine($"Found {files.Count()} files");
        foreach (var file in files)
        {
            Console.WriteLine($"Processing {file.FullName}");
            using var stream = file.OpenRead();

            // Read the first 4 bytes in big endian order
            var lengthBytes = new byte[4];
            stream.Read(lengthBytes, 0, 4);
            var length = BitConverter.ToInt32(lengthBytes.Reverse().ToArray(), 0);

            Console.WriteLine($"Length: {length}");

            // Now read as many bytes as the length we just read
            var blobBytes = new byte[length];
            stream.Read(blobBytes, 0, length);

            // Now parse the blob header
            using var blobStream = new MemoryStream(blobBytes);
            var blobHeader = BlobHeader.Parser.ParseFrom(blobStream);
            Console.WriteLine($"Blob type: {blobHeader.Type}");
            Console.WriteLine($"Blob index data: {blobHeader.Indexdata.ToString()}");
            Console.WriteLine($"Blob data size: {blobHeader.Datasize}");

            // Now read the blob data
            var blobDataBytes = new byte[blobHeader.Datasize];
            stream.Read(blobDataBytes, 0, blobHeader.Datasize);

            // Now parse the blob data
            using var blobDataStream = new MemoryStream(blobDataBytes);
            var blobData = Blob.Parser.ParseFrom(blobDataStream);
            Console.WriteLine($"Blob data raw size: {blobData.RawSize}");
            Console.WriteLine($"Blob data zlib data size: {blobData.HasZlibData}");

        }

    }
}