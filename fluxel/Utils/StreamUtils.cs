using System.Text;

namespace fluxel.Utils;

// ReSharper disable MustUseReturnValue
public static class StreamUtils {
    public static byte[] GetPostFile(Encoding enc, string type, MemoryStream stream) {
        var boundaryBytes = enc.GetBytes(getBoundary(type));
        var boundaryLen = boundaryBytes.Length;

        int startPos;

        // Find start boundary
        while (true)
        {
            if (stream.Length == 0)
                throw new Exception("Start Boundaray Not Found");

            startPos = indexOf(stream.ToArray(), (int) stream.Length, boundaryBytes);
            if (startPos >= 0)
                break;

            var temp = new MemoryStream();
            stream.Position = stream.Length - boundaryLen;
            stream.CopyTo(temp);
            stream.Dispose();
            stream = temp;
        }

        for (var i = 0; i < 4; i++)
        {
            while (true)
            {
                if (stream.Length == 0)
                    throw new Exception("Preamble not Found.");

                startPos = indexOf(stream, enc.GetBytes("\n")[0], startPos);
                if (startPos < 0) continue;

                startPos++;
                break;
            }
        }

        var buffer = new byte[stream.Length - startPos];
        stream.Position = startPos;
        stream.Read(buffer, 0, buffer.Length);

        return buffer;
    }

    private static string getBoundary(string ctype)
    {
        return "--" + ctype.Split(';')[1].Split('=')[1];
    }

    private static int indexOf(IReadOnlyList<byte> buffer, int len, IReadOnlyList<byte> boundaryBytes)
    {
        for (var i = 0; i <= len - boundaryBytes.Count; i++)
        {
            var match = true;
            for (var j = 0; j < boundaryBytes.Count && match; j++)
                match = buffer[i + j] == boundaryBytes[j];

            if (match)
                return i;
        }

        return -1;
    }

    private static int indexOf(Stream stream, byte bytes, int start) {
        var buffer = new byte[stream.Length];
        stream.Position = 0;
        stream.Read(buffer, 0, (int) stream.Length);

        for (var i = start; i < buffer.Length; i++) {
            if (buffer[i] == bytes) {
                return i;
            }
        }

        return -1;
    }
}
