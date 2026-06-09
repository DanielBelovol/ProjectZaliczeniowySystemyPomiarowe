# Projekt zaliczeniowy - Systemy Pomiarowe

Projekt polega na monitorowaniu sejfu za pomoca czujnikow temperatury i odleglosci. Calosc dziala na ESP32 w symulatorze Wokwi.

## Co jest potrzebne

- VS Code
- Rozszerzenie PlatformIO (do budowania kodu na ESP32)
- Rozszerzenie Wokwi (do symulacji ukladu)
- .NET 7.0 SDK (do uruchomienia Bridge i Client)

## Jak uruchomic

1. Otworz folder w VS Code

2. Zbuduj firmware komenda "pio run" w terminalu albo przyciskiem Build w PlatformIO

3. Otworz plik diagram.json i kliknij Start Simulation w Wokwi

4. W nowym terminalu wejdz do folderu Client i uruchom go:
   cd Client
   dotnet run

5. W kolejnym terminalu wejdz do folderu Bridge i uruchom go:
   cd Bridge
   dotnet run

6. Teraz wszystko powinno dzialac - Bridge odbiera dane z Wokwi i przesyla je do Client

## Jak to dziala

ESP32 co 2 sekundy odczytuje temperature z czujnika BMP180 i odleglosc z czujnika HC-SR04. Dane sa wysylane jako JSON przez port szeregowy.

Bridge laczy sie z Wokwi przez TCP na porcie 4000 i przesyla te dane przez HTTP do Clienta na port 5100.

Client odbiera dane i sprawdza czy wszystko jest ok. Jesli drzwi sa otwarte (odleglosc powyzej 100cm) albo temperatura jest poza zakresem 2-8 stopni to system zmienia stan na alarm. Incydenty sa zapisywane do pliku CSV.

## Reset systemu

Jesli system jest zablokowany mozna go zresetowac komenda:
curl -X POST http://localhost:5100/api/reset
