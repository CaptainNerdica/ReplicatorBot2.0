#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ReplicatorBot/ReplicatorBot.csproj", "ReplicatorBot/"]
COPY ["DiscordBotCore/DiscordBotCore.csproj", "DiscordBotCore/"]
RUN dotnet restore "ReplicatorBot/ReplicatorBot.csproj"
COPY . .
WORKDIR "/src/ReplicatorBot"
RUN dotnet build "ReplicatorBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ReplicatorBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReplicatorBot.dll"]