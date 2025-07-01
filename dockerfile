# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Устанавливаем зависимости для Chromium
RUN apt-get update && apt-get install -y \
    libnss3 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxcomposite1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxrandr2 \
    libxrender1 \
    libxss1 \
    libxtst6 \
    libgbm1 \
    libasound2 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libgtk-3-0 \
    fonts-liberation \
    libappindicator3-1 \
    libnspr4 \
    libdrm2 \
    libxkbcommon0 \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

# Настройка пользователя и прав
RUN groupadd -r chromium && \
    useradd -r -g chromium -G audio,video chromium && \
    mkdir -p /home/chromium/Downloads && \
    mkdir -p /app/.cache/puppeteer && \
    mkdir -p /app/PriceLists && \
    chown -R chromium:chromium /home/chromium && \
    chown -R chromium:chromium /app && \
    chmod -R 777 /app/.cache

WORKDIR /app
COPY --from=build /app/publish .
COPY PriceLists/ /app/PriceLists/

USER chromium

ENTRYPOINT ["dotnet", "testparser.dll"]	