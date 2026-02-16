# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Kopiert den kompletten Solution-Root (kommt aus docker-compose context)
COPY . .

# Restore & Publish direkt über das API-Projekt (robust, unabhängig vom SLN-Namen)
RUN dotnet restore "AiTextAnalyzer.Api/AiTextAnalyzer.Api.csproj"
RUN dotnet publish "AiTextAnalyzer.Api/AiTextAnalyzer.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:1234
EXPOSE 1234

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "AiTextAnalyzer.Api.dll"]
