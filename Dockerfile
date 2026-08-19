FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/EventForge.Api/EventForge.Api.csproj", "src/EventForge.Api/"]
RUN dotnet restore "src/EventForge.Api/EventForge.Api.csproj"

COPY . .
WORKDIR "/src/src/EventForge.Api"
RUN dotnet publish "EventForge.Api.csproj" --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG APP_UID=1654
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "EventForge.Api.dll"]
