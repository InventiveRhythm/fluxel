dotnet sln fluxel.sln add ../fluXis/fluXis/fluXis.csproj
dotnet sln fluxel.sln add ../fluXis/fluXis.Modes.Keys/fluXis.Modes.Keys.csproj

dotnet remove fluxel.Models package InventiveRhythm.fluXis
dotnet add fluxel.Models reference ../fluXis/fluXis/fluXis.csproj

dotnet remove fluxel package InventiveRhythm.fluXis.Modes.Keys
dotnet add fluxel reference ../fluXis/fluXis.Modes.Keys/fluXis.Modes.Keys.csproj