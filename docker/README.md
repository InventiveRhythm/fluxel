# Docker Setup

## Setting up

Create 4 folders for data:

- `./data`
- `./data/meili`
- `./data/postgres`
- `./data/seaweed`

Make sure to create these before running any compose file else docker will create them using the root user.

## Running a development setup

```sh
docker compose -f docker-compose.core.yml up -d
```

If you've changed the models add `--build` afterwards.

Also make sure your run configurations have their working directory set to the root of the project.

## Full setup

To run everything and test changes in-game use this:

```sh
docker compose up -d --build
```