using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DSharpPlus.Entities;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;
using fluxel.Database;
using fluxel.Utils;
using Microsoft.Extensions.DependencyInjection;
using Midori.Logging;

namespace fluxel.Bot.Commands.Management.Regenerate;

public class RegenerateMapSetAssetsCommand : ISlashCommand
{
    public string Name => "mapset-assets";
    public string Description => "Regenerates all mapset assets.";

    public void Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        interaction.Reply("Recalculating all assets...", true);
        var maps = services.GetRequiredService<MapManager>();

        try
        {
            var i = 0;
            var sets = maps.AllSets;

            var issues = new List<string>();

            foreach (var set in sets)
            {
                var path = $"{Environment.CurrentDirectory}/Assets/map";

                var zipStream = File.OpenRead($"{path}/{set.ID}.zip");
                var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);

                if (ServerMapUtils.TryLoadFromZip($"{path}/{set.ID}.zip", out var jsons))
                {
                    var first = jsons.First();

                    var background = first.BackgroundFile;
                    var cover = string.IsNullOrEmpty(first.CoverFile) ? first.BackgroundFile : first.CoverFile;

                    var backgroundEntry = zip.GetEntry(background);
                    var coverEntry = zip.GetEntry(cover);

                    if (backgroundEntry != null)
                    {
                        using var stream = backgroundEntry.Open();
                        Assets.WriteImage(AssetType.Background, set.ID, stream);
                    }
                    else
                        issues.Add($"Failed to load background for mapset {set.ID}!");

                    if (coverEntry != null)
                    {
                        using var stream = coverEntry.Open();
                        Assets.WriteImage(AssetType.Cover, set.ID, stream);
                    }
                    else
                        issues.Add($"Failed to load cover for mapset {set.ID}!");
                }
                else
                    issues.Add($"Failed to load mapset {set.ID}!");

                if (++i % 20 != 0) continue;

                Logger.Log($"{i / (float)sets.Count * 100}% done. ({i}/{sets.Count}) ({sets.Count - i} left)");
                interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder
                {
                    Content = $"{i / (float)sets.Count * 100}% done. ({i}/{sets.Count}) ({sets.Count - i} left)"
                });
            }

            Logger.Log("Recalculated all sets!");
            interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder
            {
                Content = "Recalculated all sets!\n\n" + string.Join("\n", issues)
            });
        }
        catch (Exception e)
        {
            Logger.Error(e, "An error occurred while recalculating all sets!");
            interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder
            {
                Content = "An error occurred while recalculating all sets!\n\n" + e
            });
        }
    }
}
