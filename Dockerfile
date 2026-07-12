FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["src/DotnetInventoryApi/DotnetInventoryApi.csproj", "src/DotnetInventoryApi/"]

RUN dotnet restore "src/DotnetInventoryApi/DotnetInventoryApi.csproj"

COPY . .

WORKDIR "/src/src/DotnetInventoryApi"

RUN dotnet publish "DotnetInventoryApi.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV PORT=8080

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["sh", "-c", "exec dotnet DotnetInventoryApi.dll --urls http://0.0.0.0:${PORT:-8080}"]