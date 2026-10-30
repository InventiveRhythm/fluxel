using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using fluxel.Config;
using Midori.Utils.Extensions;

namespace fluxel.Services.Content.Components;

public class PreviewGenerator
{
    private const int preview_length = 15;
    private const float fade_time = 0.8f;

    private readonly ServerConfig config;

    public PreviewGenerator(ServerConfig config)
    {
        this.config = config;
    }

    public async Task<Stream> Generate(Stream input, float start = 0, float length = preview_length)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = config.FfmpegPath,
            Arguments =
                $"-i pipe:0 -ss {start.ToStringInvariant()} -t {length.ToStringInvariant()} -af \"afade=t=in:st={start.ToStringInvariant()}:d={fade_time.ToStringInvariant()},afade=t=out:st={(start + length - fade_time).ToStringInvariant()}:d={fade_time.ToStringInvariant()}\" -c:a libvorbis -f ogg pipe:1",
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        process.Start();
        var output = new MemoryStream();

        var stdIn = process.StandardInput;

        var write = Task.Run(async () =>
        {
            try
            {
                await input.CopyToAsync(stdIn.BaseStream);
                await stdIn.BaseStream.FlushAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                try
                {
                    stdIn.Close();
                }
                catch
                {
                }
            }
        });

        var read = process.StandardOutput.BaseStream.CopyToAsync(output);

        await Task.WhenAll(write, read);
        await process.WaitForExitAsync();

        output.Position = 0;
        return output;
    }
}
