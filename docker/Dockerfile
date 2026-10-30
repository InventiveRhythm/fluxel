ARG SERVICE=""

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG SERVICE
WORKDIR /src

COPY fluxel/fluxel.Models/fluxel.Models.csproj fluxel/fluxel.Models/fluxel.Models.csproj
COPY fluxel/fluxel/fluxel.csproj fluxel/fluxel/fluxel.csproj
COPY fluxel/fluxel.Services.${SERVICE}/fluxel.Services.${SERVICE}.csproj fluxel/fluxel.Services.${SERVICE}/fluxel.Services.${SERVICE}.csproj

RUN dotnet restore fluxel/fluxel.Services.${SERVICE}/fluxel.Services.${SERVICE}.csproj -r linux-x64

COPY fluxel/fluxel.Models fluxel/fluxel.Models
COPY fluxel/fluxel fluxel/fluxel
COPY fluxel/fluxel.Services.${SERVICE} fluxel/fluxel.Services.${SERVICE}

RUN dotnet publish fluxel/fluxel.Services.${SERVICE}/fluxel.Services.${SERVICE}.csproj -r linux-x64 -c Release -o /app/publish --no-restore
	
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS final
ARG SERVICE
# RUN apk add --no-cache ffmpeg
WORKDIR /app
COPY --from=build /app/publish .
ENV SERVICE=${SERVICE}
ENTRYPOINT ["sh", "-c", "dotnet fluxel.Services.${SERVICE}.dll"]