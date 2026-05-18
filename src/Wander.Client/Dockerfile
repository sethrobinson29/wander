FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Wander.Client/Wander.Client.csproj -c Release -o /app/publish && \
    find /app/publish/wwwroot/_framework -maxdepth 1 -name "*.js" ! -name "*.br" ! -name "*.gz" | \
    grep -E '\.[a-z0-9]{8,12}\.js$' | \
    while IFS= read -r f; do \
        target=$(echo "$f" | sed -E 's/\.[a-z0-9]{8,12}\.js$/.js/'); \
        cp "$f" "$target"; \
    done

FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY nginx/container.conf /etc/nginx/conf.d/default.conf
