<h1 align="center">fluxel</h1>
<p align="center"><img src="https://assets.flux.moe/avatar/318dbc829bb2978daa8b7edd6f8377f7dc549d0b51b8f068cb56779f094a3961" width="128" alt="fluxel logo"/></p>
<p align="center">The backend for fluXis.</p>

## Building and Developing

### Requirements

* A desktop computer running Windows, macOS, or Linux with
  the [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed.
* An IDE of your choice, for
  example [JetBrains Rider](https://www.jetbrains.com/rider/), [Visual Studio](https://visualstudio.microsoft.com/vs/)
  or [Visual Studio Code](https://code.visualstudio.com/).

### Downloading the source code

You can download the source code by cloning the repository using git:

```shell
git clone https://github.com/InventiveRhythm/fluxel
```

Make sure the [fluXis repo](https://github.com/InventiveRhythm/fluXis) is cloned next to where the fluxel repo is.

> Your filesystem should look like this if done correctly:
> ```
> ~/GitHub/InventiveRhythm> ls
> fluXis
>   fluXis/
>   fluXis.Resources/
>   fluXis.sln
>   ...
> fluxel
>   fluxel/
>   fluxel.Startup/
>   fluxel.sln
>   ...
> ```

To update the source code to the latest version, run the following command in the repository directory:

```shell
git pull
```

### Building

When running the project for the first time, make sure to copy `default.env` to `.env`.

To build and run the project, execute the following command in the repository directory (the folder where fluxel.sln is
located):

```shell
dotnet run --project fluxel.Startup
```

## Infrastructure

The repo is split up into separate projects and services.

> Entries starting with a "." are services (fluxel.Service.*).

| Project/Service | Task                                                                                                                                                       |
|-----------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| fluxel          | Shared methods and systems that all other services can use.                                                                                                |
| fluxel.Models   | Database schemas and migrations.                                                                                                                           |
| .Admin          | Admin panel backend for server admins and staff.                                                                                                           |
| .API            | Main REST API for everything. (except specific service things, those are handled by services themselves)                                                   |
| .Atlas          | Search indexing and querying for maps, players and more. (uses meilisearch as engine)                                                                      |
| .Beacon         | Central notifications hub for players. (online states, chat, etc.)                                                                                         |
| .Broadcast      | Anything related to sharing live gameplay from one client to another. (keyword: spectator)                                                                 |
| .Chronicle      | Score upload handler/processor, achievements and more. (anything related to play history)                                                                  |
| .Content        | Interfaces with the S3 storage to store and provide files (avatars, maps, etc.). Also handles the actual uploads of those files and adding them to the db. |
| .Multiplayer    | Matchmaking and lobbies for player matches.                                                                                                                |

Every service runs as it's own container and communicates with others through Valkey/Redis and HTTP.

## License

fluxel is licensed under the [MIT License](LICENSE). tl;dr: You can do whatever you want with the code, as long as you
include the original license.