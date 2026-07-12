# Docker

Run the prototype with Docker Compose:

```powershell
docker compose up --build
```

Open the app at:

```txt
http://localhost:8080
```

The app uses SQLite at `/app/data/restaurant-terminal.db` inside the container. Docker Compose stores that file in the named volume `restaurant-terminal-data`, so mock data and test orders survive container restarts.

Run tests through Docker:

```powershell
docker compose --profile test run --rm backend-tests
docker compose --profile test run --rm frontend-tests
```

Reset the local Docker database:

```powershell
docker compose down -v
```
