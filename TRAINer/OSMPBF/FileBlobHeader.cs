namespace OSMPBF;
public class FileBlobHeader
{
    public string Type { get; private set; }

    public int Datasize { get; private set; }

    protected FileBlobHeader(string type, int datasize)
    {
        Type = type;
        Datasize = datasize;
    }


    public static FileBlobHeader Parse(Stream stream)
    {
        // Read the first 4 bytes in big endian order
        var lengthBytes = new byte[4];
        stream.Read(lengthBytes, 0, 4);
        var length = BitConverter.ToInt32(lengthBytes.Reverse().ToArray(), 0);

        // Now read as many bytes as the length we just read
        var blobBytes = new byte[length];
        stream.Read(blobBytes, 0, length);

        // Now parse the blob header
        var blobHeader = BlobHeader.Parser.ParseFrom(blobBytes);

        return new FileBlobHeader(blobHeader.Type, blobHeader.Datasize);
    }
}
