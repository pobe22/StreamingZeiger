# StreamingZeiger

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)
![.NET](https://img.shields.io/badge/.NET-8-blue)

**StreamingZeiger** ist eine Webanwendung zur Verwaltung und Empfehlung von Filmen. Nutzer können Filme durchsuchen, in ihre Watchlist aufnehmen und personalisierte Empfehlungen erhalten.

## 🌟 Features

- Anzeige von Filmen mit Details (Titel, Beschreibung, Poster, Streaming-Plattform)  
- Persönliche Watchlist für registrierte Nutzer  
- Empfehlungssystem basierend auf Nutzerpräferenzen  
- Administrationsbereich zum Verwalten von Filmen  
- Autocomplete-Suche für schnelle Navigation  
- Responsive Design für Desktop und Mobilgeräte

## 🛠️ Technologie-Stack

- **Backend:** ASP.NET Core MVC  
- **Datenbank:** SQLite (lokal)  
- **Frontend:** HTML, CSS, Bootstrap, JavaScript  
- **Authentifizierung:** ASP.NET Identity

## 🚀 Installation

1. Repository klonen:

```bash
git clone https://github.com/dein-benutzername/StreamingZeiger.git
cd StreamingZeiger
```

2. Abhängigkeiten installieren:

```bash
dotnet restore
```

3. Datenbank migrieren:

```bash
dotnet ef database update
```

4. Anwendung starten:

```bash
dotnet run
```

Die Anwendung ist dann unter `https://localhost:5001` erreichbar.

## 🎬 Nutzung

- Registriere dich als neuer Nutzer und melde dich an.  
- Filme können in der Watchlist gespeichert oder bewertet werden.  
- Admins können Filme hinzufügen, bearbeiten oder löschen.

## 🤝 Mitwirken

Beiträge sind willkommen!  
1. Forke das Repository  
2. Erstelle einen Branch (`git checkout -b feature/meine-feature`)  
3. Committe deine Änderungen (`git commit -am 'Meine Änderungen'`)  
4. Push auf den Branch (`git push origin feature/meine-feature`)  
5. Öffne einen Pull Request

## 📄 Lizenz

Dieses Projekt ist lizenziert unter der MIT License.

