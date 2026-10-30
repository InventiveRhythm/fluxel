dotnet remove fluxel reference ../fluXis/fluXis.Modes.Keys/fluXis.Modes.Keys.csproj
dotnet add fluxel package InventiveRhythm.fluXis.Modes.Keys

dotnet remove fluxel.Models reference ../fluXis/fluXis/fluXis.csproj
dotnet add fluxel.Models package InventiveRhythm.fluXis

dotnet sln fluxel.sln remove ../fluXis/fluXis/fluXis.csproj
dotnet sln fluxel.sln remove ../fluXis/fluXis.Modes.Keys/fluXis.Modes.Keys.csproj

