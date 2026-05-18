FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Wander.Api/Wander.Api.csproj -c Release -o /app/publish \
    && rm /app/publish/appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Wander.Api.dll"]
