#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/DapperPlayground.API/DapperPlayground.API.csproj", "src/DapperPlayground.API/"]
RUN dotnet restore "src/DapperPlayground.API/DapperPlayground.API.csproj"
COPY . .
WORKDIR "/src/src/DapperPlayground.API"
RUN dotnet build "DapperPlayground.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DapperPlayground.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DapperPlayground.API.dll"]