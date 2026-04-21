# El último samurai en pie — Guia del Projecte

## 🚀 Com arrancar-ho tot

### 1. Servidor Node.js (sempre primer)

```powershell
# Obre un terminal a:
cd C:\Users\rober\Desktop\2nDAM\3r\joc\server\src
node server.js
```

Hauries de veure:
```
🚀 Servidor a http://localhost:3000  |  WS natius: ws://localhost:3000/gs
```

> ⚠️ Si surt "EADDRINUSE" → el servidor JA ESTÀ CORRENT. No cal tornar-lo a arrancar.

### 2. Unity (client del joc)
1. Obre `c:\Users\rober\Desktop\2nDAM\3r\joc\client\Game` a Unity Hub
2. Obre l'escena `Lobby` (Assets → Scenes)
3. Prem ▶ **Play**

### 3. ParrelSync (segon jugador per provar)
1. Window → ParrelSync → Open Clone Project
2. El clon **automàticament esborra** `PlayerName` de PlayerPrefs (és un jugador diferent)
3. Entra al clon, posa un nom diferent (ex: "Roberto2") i inicia sessió

---

## 🏗️ Arquitectura del sistema

```
┌─────────────────┐     HTTP REST      ┌──────────────────────────┐
│   Unity Client  │ ────────────────→  │  Node.js (port 3000)     │
│  (MenuManager)  │ ←────────────────  │  Express API             │
│                 │                    │  ├─ /api/users            │
│  NGO (game)     │ ←── UDP :7777 ──→  │  ├─ /api/rooms           │
│  host ↔ client  │  (Unity Transport) │  └─ /api/users/:id/stats │
│                 │                    │                          │
│  WebSocketClient│ ──── WS /gs ────→  │  WebSocket natiu (/gs)   │
└─────────────────┘                    └──────────────────────────┘
          ↑
     (En producció)
          │
    ┌─────────────┐
    │    nginx    │  port 80 → proxy → port 3000
    │ (docker)    │  amaga ports interns
    └─────────────┘
```

---

## ✅ Requisits del spec — estat actual

### 3.1 Part Usuari

| Requisit | Implementat | On |
|---|---|---|
| Identificació jugador | ✅ | `MenuManager.cs` → Login → Node.js `/api/users/login` |
| Configuració bàsica (nom, personatge) | ✅ | Panel esquerre + selecció de personatge (Samurái / Caballero) |
| Crear partida | ✅ | Botó "CREAR SALA" → `POST /api/rooms/create` |
| Unir-se a partida existent | ✅ | Botó "UNIR-SE" → `POST /api/rooms/join` |
| Esperar inici (sala d'espera) | ✅ | WaitingRoomPanel amb llista de jugadors sincronitzada |
| Partida compartida (moviment) | ✅ | NGO amb UnityTransport (UDP) |
| Actualitzar estat en pantalla | ✅ | HUDController: temps, notificacions de mort, estat jugadors |
| Final de partida + resultats | ✅ | Podio amb temps de supervivència, guanyador destacat |
| Tancar sessió | ✅ | Botó "TANCAR SESSIÓ" → esborra PlayerPrefs + desconnecta |

### 3.2 Part Tècnica

| Requisit | Implementat | On |
|---|---|---|
| Comunicació HTTP (UnityWebRequest) | ✅ | MenuManager → login, stats, rooms |
| WebSockets actius durant partida | ✅ | `GameWebSocketClient.cs` → ws://localhost:3000/gs |
| Node.js backend | ✅ | `server/src/server.js` |
| WebSockets natius Node.js | ✅ | `ws` package, path `/gs` |
| Proxy invers (nginx) | ✅ | `server/nginx.conf` + `docker-compose.yml` |
| Persistència amb Repository | ✅ | `repositories/` → InMemory + MySQL |
| Usuaris / Partides / Resultats | ✅ | `models/`, `services/`, `controllers/` |
| ML-Agents (IA) | ✅ | `BotAI.cs` - bots amb moviment automàtic |

### 4.1 Requisits Tècnics Backend

| Requisit | Implementat |
|---|---|
| Node.js + Express | ✅ |
| WebSockets natius (ws) | ✅ |
| Patró Repository | ✅ |
| Validació de dades | ✅ (al servidor) |
| Arquitectura separada (Controller/Service/Repository) | ✅ |

---

## 🎮 Flux de joc complet

```
1. Arranca el servidor Node.js
2. Obre Unity → Play
3. Inicia sessió (nom + botó INICIAR SESSIÓ)
4. Crea una sala (CREAR SALA → escriu nom → CREAR)
5. [ParrelSync] Inicia sessió com altre jugador
6. [ParrelSync] Selecciona la sala → UNIR-SE
7. [Host Unity] Prem INICIAR (botó verd)
8. Els dos jugadors apareixen al joc
9. La lava puja → l'últim en peu guanya
10. Podio → MENÚ PRINCIPAL → torna al lobby
```

---

## 📡 Events WebSocket (prova via consola Node.js)

Quan jugues, el servidor hauria de mostrar:
```
[WS] Client Unity connectat per events de partida
[WS] Esdeveniment: gameStart  { room: 'partida' }
[WS] Esdeveniment: playerDied { player: 'Roberto', time: '12.34' }
[WS] Esdeveniment: gameOver   { winner: 'Roberto2', time: '15.67' }
```

---

## ⚙️ Fitxers clau del projecte

### Unity (Assets/Scripts)
| Fitxer | Funció |
|---|---|
| `MenuManager.cs` | Lobby, login, crear/unir sales, sincronització |
| `LobbySync.cs` | Sincronitza noms de jugadors via NGO CustomMessaging |
| `PlayerMovement.cs` | Moviment + `NetworkVariable playerName` |
| `LavaRise.cs` | Lava, mort de jugadors, podio final |
| `HUDController.cs` | UI en joc (temps, notificacions, estat jugadors) |
| `BotAI.cs` | Intel·ligència artificial dels bots |
| `GameWebSocketClient.cs` | Connexió WebSocket a Node.js |

### Servidor (server/src)
| Fitxer | Funció |
|---|---|
| `server.js` | Entry point: Express + socket.io + ws natiu |
| `routes/userRoutes.js` | Login, stats, registre |
| `controllers/` | Lògica HTTP |
| `services/` | Lògica de negoci |
| `repositories/` | Accés a dades (InMemory + MySQL) |

### Infraestructura (server/)
| Fitxer | Funció |
|---|---|
| `nginx.conf` | Proxy invers (port 80 → 3000) |
| `docker-compose.yml` | Contenidors node + nginx |

---

## 🐛 Problemes coneguts i solucions

| Problema | Solució |
|---|---|
| `EADDRINUSE :3000` | El servidor ja corre. No cal tornar a arrancar |
| `EADDRINUSE :7777` | `Get-Process -Id (netstat -ano \| findstr :7777 \| ...`). O simplement reinicia Unity |
| Noms erronis al podio | El `NetworkVariable playerName` tarda ~1 frame en sincronitzar. Si passes mol ràpid és possible |
| "Solo una persona visible" | Afegeix `GameWebSocketClient` a la Game Scene. El HUD ara busca tots els `PlayerMovement` actius |

---

## 🐳 Engegar en Docker (producció)

```powershell
cd C:\Users\rober\Desktop\2nDAM\3r\joc\server
docker-compose up --build
# Accés via http://localhost (port 80, proxy invers)
```

---

## 📋 Tecnologies utilitzades

| Tecnologia | Ús |
|---|---|
| **Unity** | Client principal, UI Toolkit, física 2D |
| **C# + NGO** | Netcode for GameObjects, sincronització multijugador en temps real |
| **Node.js + Express** | API REST per a usuaris, sales, estadístiques |
| **WebSockets natius (ws)** | Esdeveniments de partida en temps real (mort, fi de joc) |
| **socket.io** | Infraestructura addicional de comunicació |
| **nginx** | Proxy invers, amaga ports interns |
| **Docker** | Contenidorització del servidor |
| **Patró Repository** | Separació d'accés a dades (InMemory + BD real) |
| **ML-Agents / BotAI** | Agent d'IA integrat al joc |
| **UnityWebRequest** | Peticions HTTP des de Unity |
