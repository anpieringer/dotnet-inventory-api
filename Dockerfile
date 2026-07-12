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

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "DotnetInventoryApi.dll"]