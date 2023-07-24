using System.IO.Compression;
using System.Net;
using fluxel.API.Components;
using fluxel.Components.Maps;
using fluxel.Components.Maps.Json;
using fluxel.Components.Users;
using fluxel.Constants;
using fluxel.Database;
using fluxel.Utils;
using Newtonsoft.Json;

namespace fluxel.API.Routes.Maps; 

public class MapUploadRoute : IApiRoute {
    public string Path => "/maps/upload";
    public string Method => "POST";
    
    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        var token = req.Headers["Authorization"];
        
        if (token == null) {
            return new ApiResponse {
                Status = HttpStatusCode.Unauthorized,
                Message = ResponseStrings.NoToken
            };
        }
        
        var userToken = UserToken.GetByToken(token);
        
        if (userToken == null) {
            return new ApiResponse {
                Status = HttpStatusCode.Unauthorized,
                Message = ResponseStrings.InvalidToken
            };
        }
        var user = User.FindById(userToken.UserId);
        
        if (user == null) {
            return new ApiResponse {
                Status = HttpStatusCode.NotFound,
                Message = ResponseStrings.TokenUserNotFound
            };
        }
        
        var stream = new MemoryStream();
        req.InputStream.CopyTo(stream);

        var cleaned = StreamUtils.GetPostFile(req.ContentEncoding, req.ContentType, stream);
        stream = new MemoryStream(cleaned);
        
        var zip = new ZipArchive(stream);
        
        var set = new MapSet {
            Id = MapSet.GetNextId(),
            CreatorId = user.Id,
        };
        
        var maps = new List<Map>();
        var id = Map.GetNextId();
        
        var backgroundStream = new MemoryStream();
        var hasBackground = false;
        
        foreach (var entry in zip.Entries) {
            if (!entry.Name.EndsWith(".fsc")) continue;
            
            var json = new StreamReader(entry.Open()).ReadToEnd();
            var mapJson = JsonConvert.DeserializeObject<MapJson>(json);
            if (mapJson == null || !mapJson.Validate()) {
                return new ApiResponse {
                    Status = HttpStatusCode.BadRequest,
                    Message = "The file " + entry.Name + " is not a valid map file."
                };
            }

            if (!hasBackground) {
                try {
                    var background = zip.GetEntry(mapJson.BackgroundFile);
                    background?.Open().CopyTo(backgroundStream);
                    hasBackground = true;
                }
                catch {
                    // ignored
                }
            }


            var hash = Hashing.GetHash(json);
                
            var mapper = User.FindByUsername(mapJson.Metadata.Mapper) ?? user;

            var map = new Map {
                Id = id,
                SetId = set.Id,
                Hash = hash,
                MapperId = mapper.Id,
                Title = mapJson.Metadata.Title,
                Artist = mapJson.Metadata.Artist,
                Source = mapJson.Metadata.Source,
                Tags = mapJson.Metadata.Tags,
                Bpm = mapJson.TimingPoints.First().BPM,
                Difficulty = mapJson.Metadata.Difficulty,
                Mode = mapJson.KeyCount,
                Length = (int)mapJson.HitObjects.Max(h => h.Time),
                Rating = 0,
                Hits = mapJson.HitObjects.Count(h => h.HoldTime == 0),
                LongNotes = mapJson.HitObjects.Count(h => h.HoldTime > 0) * 2
            };
                
            id++;
            maps.Add(map);
        }
        
        if (maps.Count == 0) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = "The zip file does not contain any valid map files."
            };
        }
        
        set.Maps = string.Join(",", maps.Select(m => m.Id));
        set.Title = maps.First().Title;
        set.Artist = maps.First().Artist;
        set.Status = 0;
        
        // write file to disk
        var path = $"{Environment.CurrentDirectory}/Maps";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        var file = $"{path}/{set.Id}.zip";
        stream.Seek(0, SeekOrigin.Begin);
            
        var dest = File.Create(file);
        stream.CopyTo(dest);
        dest.Flush();
        
        var backgroundPath = $"{Environment.CurrentDirectory}/Assets/Backgrounds";
        if (!Directory.Exists(backgroundPath)) Directory.CreateDirectory(backgroundPath);
        var backgroundFile = $"{backgroundPath}/{set.Id}.png";
        backgroundStream.Seek(0, SeekOrigin.Begin);
        backgroundStream.CopyTo(File.Create(backgroundFile));

        RealmAccess.Run(realm => {
            realm.Add(set);
            
            foreach (var map in maps) {
                realm.Add(map);
            }
        });
        
        zip.Dispose();

        return new ApiResponse {
            Message = "Successfully uploaded mapset.",
            Data = set
        };
    }
}