# Greenhouse Monitor

Domáci projekt pre monitorovanie teploty, vlhkosti a tlaku v skleníku pomocou ESP32 a presného senzora BME280. Data sa cez WiFi posielajú na moj Ubuntu server. 

***projekt je funkčný no stále vo výstavbe***

## Zložky

- `esp32_firmware/greenhouse_monitor.ino` - Arduino sketch pre ESP32.
- `GreenhouseMonitorApi/` - ASP.NET Core Web API a jednoduché UI.
- `GreenhouseMonitorApi/Dockerfile` - Docker image pre ASP.NET aplikáciu.
- `docker-compose.yml` - voliteľné spustenie služby v Docker kontejnery.

## Ako to funguje

1. ESP32 sa pripojí k domácej WiFi a každých 5 minút odosiela HTTP POST na server.
2. ASP.NET API prijme dáta, uloží ich do databázy a poskytuje rozhranie s poslednými meraniami.
3. Dashboard je dostupný cez webový prehliadač na `http://<server-ip>:5000`.

## Nastavenie ESP32

1. Otvor `esp32_firmware/greenhouse_monitor.ino` v Arduino IDE alebo PlatformIO.
2. Nastav `ssid`, `password`, `serverUrl` a `apiKey`.
3. Nahraj sketch do ESP32.

## Nasadenie ASP.NET API

1. V `GreenhouseMonitorApi/Program.cs` nastav hodnotu `apiKey` na silný tajný kľúč.
2. Postav kontajner:
   ```bash
   cd GreenhouseMonitorApi
   dotnet restore
   dotnet build
   dotnet run
   ```
3. Alebo spusti s Dockerom:
   ```bash
   docker compose up --build
   ```

## Prístup z internetu

- Router musí smerovať port 5000 na Ubuntu server.
- Pre bezpečnosť odporúčam nastaviť reverzný proxy s HTTPS (Nginx / Traefik) alebo Cloudflare Tunnel.
- ESP32 bude posielať dáta na URL `http://<public-ip>:5000/api/greenhouse/readings`.

## API

- `POST /api/greenhouse/readings` - prijatie merania (vyžaduje hlavičku `x-api-key`).
- `GET /api/greenhouse/readings/latest` - posledné meranie.
- `GET /api/greenhouse/readings` - posledných 100 meraní.


