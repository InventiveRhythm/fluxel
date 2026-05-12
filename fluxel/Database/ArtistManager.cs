using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using fluxel.Models.Featured;
using Midori.Database;

namespace fluxel.Database;

public class ArtistManager
{
    private readonly IDatabaseTable<FeaturedArtist> artists;
    private readonly IDatabaseTable<FeaturedArtistAlbum> albums;
    private readonly IDatabaseTable<FeaturedArtistTrack> songs;

    public List<FeaturedArtist> AllArtists => artists.Find(m => true).ToList();

    public ArtistManager(IDatabaseProvider db)
    {
        artists = db.GetTable<FeaturedArtist>("fa-artists");
        albums = db.GetTable<FeaturedArtistAlbum>("fa-albums");
        songs = db.GetTable<FeaturedArtistTrack>("fa-tracks");
    }

    public List<FeaturedArtistAlbum> FromArtist(string artist)
    {
        var list = albums.Find(m => m.InternalID.StartsWith($"{artist}/")).ToList();
        list.Sort((a, b) => a.ReleaseDate.CompareTo(b.ReleaseDate));
        list.Reverse();
        return list;
    }

    public List<FeaturedArtistTrack> FromAlbum(string artist, string album)
    {
        var list = songs.Find(m => m.InternalID.StartsWith($"{artist}/{album}/")).ToList();
        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase));
        return list;
    }

    public void Add(FeaturedArtist artist) => artists.Add(artist);
    public void Add(FeaturedArtistAlbum album) => albums.Add(album);
    public void Add(FeaturedArtistTrack track) => songs.Add(track);

    public FeaturedArtist? GetArtist(string id) => artists.Find(m => m.ID == id).FirstOrDefault();
    public FeaturedArtistAlbum? GetAlbum(string artist, string id) => albums.Find(m => m.InternalID == $"{artist}/{id}").FirstOrDefault();
    public FeaturedArtistTrack? GetTrack(string artist, string album, string id) => songs.Find(m => m.InternalID == $"{artist}/{album}/{id}").FirstOrDefault();

    public bool TryGetArtist(string id, [NotNullWhen(true)] out FeaturedArtist? artist)
    {
        artist = GetArtist(id);
        return artist != null;
    }

    public bool TryGetAlbum(string artist, string id, [NotNullWhen(true)] out FeaturedArtistAlbum? album)
    {
        album = GetAlbum(artist, id);
        return album != null;
    }

    public bool TryGetTrack(string artist, string album, string id, [NotNullWhen(true)] out FeaturedArtistTrack? song)
    {
        song = GetTrack(artist, album, id);
        return song != null;
    }

    public void UpdateArtist(FeaturedArtist artist) => artists.Replace(m => m.ID == artist.ID, artist);
    public void UpdateAlbum(FeaturedArtistAlbum album) => albums.Replace(m => m.InternalID == album.InternalID, album);
    public void UpdateTrack(FeaturedArtistTrack track) => songs.Replace(m => m.InternalID == track.InternalID, track);
}
