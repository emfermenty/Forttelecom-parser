# Используем более легковесный образ для восстановления
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
COPY ["testparser.csproj", "./"]
RUN dotnet restore "testparser.csproj" -r linux-x64

# Основная сборка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY --from=restore /src/obj ./obj
COPY . .
RUN dotnet publish "testparser.csproj" -c Release -o /app/publish

# Финальный образ
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "testparser.dll"]