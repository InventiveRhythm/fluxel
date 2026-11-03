FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY fluxel/fluxel.Models/fluxel.Models.csproj fluxel/fluxel.Models/fluxel.Models.csproj
COPY fluxel/fluxel.Models.Init/fluxel.Models.Init.csproj fluxel/fluxel.Models.Init/fluxel.Models.Init.csproj

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
	dotnet restore fluxel/fluxel.Models.Init/fluxel.Models.Init.csproj -r linux-x64

COPY fluxel/fluxel.Models fluxel/fluxel.Models
COPY fluxel/fluxel.Models.Init fluxel/fluxel.Models.Init

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
   	dotnet publish fluxel/fluxel.Models.Init/fluxel.Models.Init.csproj -r linux-x64 -c Release -o /app/publish --no-restore
	
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "fluxel.Models.Init.dll"]