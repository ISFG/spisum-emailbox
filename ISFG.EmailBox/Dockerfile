#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 7778

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["ISFG.EmailBox/ISFG.EmailBox.csproj", "ISFG.EmailBox/"]
RUN dotnet restore "ISFG.EmailBox/ISFG.EmailBox.csproj"
COPY . .
WORKDIR "/src/ISFG.EmailBox"
RUN dotnet build "ISFG.EmailBox.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ISFG.EmailBox.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ISFG.EmailBox.dll"]