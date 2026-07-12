# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-restore
WORKDIR /src
COPY RestaurantTerminal.slnx ./
COPY backend/RestaurantTerminal.Api/RestaurantTerminal.Api.csproj backend/RestaurantTerminal.Api/
COPY backend/RestaurantTerminal.Api.Tests/RestaurantTerminal.Api.Tests.csproj backend/RestaurantTerminal.Api.Tests/
RUN dotnet restore backend/RestaurantTerminal.Api.Tests/RestaurantTerminal.Api.Tests.csproj

FROM node:24-alpine AS frontend-deps
WORKDIR /web
COPY frontend/restaurant-terminal-web/package*.json ./
RUN npm ci

FROM frontend-deps AS frontend-build
COPY frontend/restaurant-terminal-web ./
RUN npm run build

FROM frontend-deps AS frontend-tests
COPY frontend/restaurant-terminal-web ./
CMD ["npm", "test"]

FROM dotnet-restore AS backend-build
COPY backend ./backend
RUN dotnet publish backend/RestaurantTerminal.Api/RestaurantTerminal.Api.csproj -c Release -o /app/publish --no-restore

FROM dotnet-restore AS backend-tests
COPY backend ./backend
CMD ["dotnet", "test", "backend/RestaurantTerminal.Api.Tests/RestaurantTerminal.Api.Tests.csproj", "--no-restore", "-v", "minimal"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS app
WORKDIR /app
COPY --from=backend-build /app/publish ./
COPY --from=frontend-build /web/dist ./wwwroot
RUN mkdir -p /app/data
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__RestaurantTerminal="Data Source=/app/data/restaurant-terminal.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "RestaurantTerminal.Api.dll"]
