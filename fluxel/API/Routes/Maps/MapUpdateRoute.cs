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

public class MapUpdateRoute : IApiRoute {
    public string Path => "/map/:id/update";
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
        
        var id = parameters["id"];
        
        if (!int.TryParse(id, out var mapId)) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = ResponseStrings.InvalidParameter("id", "integer")
            };
        }

        return RealmAccess.Run(realm => {
            var set = realm.Find<MapSet>(mapId);
        
            if (set == null) {
                return new ApiResponse {
                    Status = HttpStatusCode.NotFound,
                    Message = ResponseStrings.MapSetNotFound
                };
            }
        
            if (set.CreatorId != user.Id) {
                return new ApiResponse {
                    Status = HttpStatusCode.Forbidden,
                    Message = "You are not the creator of this mapset."
                };
            }
        
            if (set.Status == 3) { // map ranked
                return new ApiResponse {
                    Status = HttpStatusCode.Forbidden,
                    Message = "You cannot update a pure map."
                };
            }
        
            var stream = new MemoryStream();
            req.InputStream.CopyTo(stream);

            var cleaned = StreamUtils.GetPostFile(req.ContentEncoding, req.ContentType, stream);
            stream = new MemoryStream(cleaned);
        
            var zip = new ZipArchive(stream);
        
            var diffNames = new List<string>();
        
            var newMaps = new List<Map>();
            var newId = Map.GetNextId();
        
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
            
                diffNames.Add(mapJson.Metadata.Difficulty);
            
                var hash = Hashing.GetHash(json);
                var mapper = User.FindByUsername(mapJson.Metadata.Mapper) ?? user;

                if (set.MapsList.Any(m => m.Difficulty == mapJson.Metadata.Difficulty)) {
                    var map = set.MapsList.First(m => m.Difficulty == mapJson.Metadata.Difficulty);
                    if (map.Hash == hash) continue;
                
                    map.Hash = hash;
                    map.MapperId = mapper.Id;
                    map.Title = mapJson.Metadata.Title;
                    map.Artist = mapJson.Metadata.Artist;
                    map.Source = mapJson.Metadata.Source;
                    map.Tags = mapJson.Metadata.Tags;
                    map.Bpm = mapJson.TimingPoints.First().BPM;
                    map.Mode = mapJson.KeyCount;
                    map.Length = (int)mapJson.HitObjects.Max(h => h.Time);
                    map.Hits = mapJson.HitObjects.Count(h => h.HoldTime == 0);
                    map.LongNotes = mapJson.HitObjects.Count(h => h.HoldTime > 0) * 2;
                } else {
                    var map = new Map {
                        Id = newId,
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
                
                    newId++;
                    newMaps.Add(map);
                }
            }
        
            var newSplit = new List<int>();
        
            // delete old maps
            foreach (var map in set.MapsList) {
                if (diffNames.Contains(map.Difficulty)) {
                    newSplit.Add(map.Id);
                } else {
                    realm.Remove(map);
                }
            }
        
            // add new maps
            foreach (var map in newMaps) {
                realm.Add(map);
                newSplit.Add(map.Id);
            }
        
            // write file to disk
            var path = $"{Environment.CurrentDirectory}/Maps";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            var file = $"{path}/{set.Id}.zip";
            
            if (File.Exists(file)) File.Delete(file);
            
            stream.Seek(0, SeekOrigin.Begin);
            
            var dest = File.Create(file);
            stream.CopyTo(dest);
            dest.Flush();
            
            zip.Dispose();

            set.Maps = string.Join(',', newSplit);
            set.LastUpdated = DateTime.Now;

            return new ApiResponse {
                Message = "Successfully updated mapset.",
                Data = set
            };
        });
    }
}