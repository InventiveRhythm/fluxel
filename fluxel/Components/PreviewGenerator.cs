using System.Diagnostics;
using System.Globalization;
using System.IO;
using fluxel.Config;

namespace fluxel.Components;

public class PreviewGenerator
{
    private const int preview_length = 15;
    private const int fade_time = 1;

    private readonly ServerConfig config;

    public PreviewGenerator(ServerConfig config)
    {
        this.config = config;
    }

    public void Generate(string path, string output, float start = 0)
    {
        if (File.Exists(output))
            File.Delete(output);

        var folder = Path.GetDirectoryName(path);
        var filename = Path.GetFileName(path);
        var ext = Path.GetExtension(path);

        var temp = Path.Combine(folder!, $"{filename}_temp{ext}");

        if (File.Exists(temp))
            File.Delete(temp);

        var cutter = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = config.FfmpegPath,
                Arguments = $"-y -i \"{path}\" -ss {start.ToString(CultureInfo.InvariantCulture)} -t {preview_length} -map 0:a \"{temp}\"",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            }
        };

        cutter.Start();
        cutter.WaitForExit();

        var fader = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = config.FfmpegPath,
                Arguments = $"-y -i \"{temp}\" -af \"afade=t=in:st=0:d={fade_time},afade=t=out:st={preview_length - fade_time}:d={fade_time}\" \"{output}\"",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            }
        };

        fader.Start();
        fader.WaitForExit();

        if (File.Exists(temp))
            File.Delete(temp);
    }
}
