FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/FakeNewsScamDetector.Core/FakeNewsScamDetector.Core.csproj src/FakeNewsScamDetector.Core/
COPY src/FakeNewsScamDetector.Data/FakeNewsScamDetector.Data.csproj src/FakeNewsScamDetector.Data/
COPY src/FakeNewsScamDetector.ML/FakeNewsScamDetector.ML.csproj src/FakeNewsScamDetector.ML/
COPY src/FakeNewsScamDetector.Services/FakeNewsScamDetector.Services.csproj src/FakeNewsScamDetector.Services/
COPY src/FakeNewsScamDetector.Web/FakeNewsScamDetector.Web.csproj src/FakeNewsScamDetector.Web/
RUN dotnet restore src/FakeNewsScamDetector.Web/FakeNewsScamDetector.Web.csproj

COPY src/FakeNewsScamDetector.Core/ src/FakeNewsScamDetector.Core/
COPY src/FakeNewsScamDetector.Data/ src/FakeNewsScamDetector.Data/
COPY src/FakeNewsScamDetector.ML/ src/FakeNewsScamDetector.ML/
COPY src/FakeNewsScamDetector.Services/ src/FakeNewsScamDetector.Services/
COPY src/FakeNewsScamDetector.Web/ src/FakeNewsScamDetector.Web/

RUN dotnet publish src/FakeNewsScamDetector.Web/FakeNewsScamDetector.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
CMD ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet FakeNewsScamDetector.Web.dll"]
