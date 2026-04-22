# El último samurai en pie — Guia del Projecte
**Curs:** 2DAM 2025-2026 | **Autor:** Roberto Lotreanu

## 🚀 Com arrancar-ho tot (Desenvolupament Local)

### 1. Servidor Node.js (sempre primer)
```powershell
# Obre un terminal a:
cd C:\Users\rober\Desktop\2nDAM\3r\joc\server\src
node server.js
```
🚀 Servidor a `http://localhost:3000` | WS natius: `ws://localhost:3000/gs`

### 2. Unity (client del joc)
1. Obre `c:\Users\rober\Desktop\2nDAM\3r\joc\client\Game` a Unity Hub.
2. Obre l'escena `Lobby` (Assets → Scenes).
3. Prem ▶ **Play**.

### 3. ParrelSync (segon jugador per provar)
1. Window → ParrelSync → Open Clone Project.
2. El clon **automàticament esborra** `PlayerName` de PlayerPrefs (és un jugador diferent).
3. Entra al clon, posa un nom diferent (ex: "Roberto2") i inicia sessió.

---

## 🌍 Servidor en Producció (VPS)
El servidor està desplegat i protegit en un VPS a la IP: `204.168.211.127`.

- **API URL:** `http://204.168.211.127:3000/api`
- **WS URL:** `ws://204.168.211.127:3000/gs`
- **Seguretat:** Fail2Ban actiu (8 IPs baneades actualment).
- **Robustesa:** Gestionat amb PM2.

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
```

---

## ✅ Requisits Tècnics Implementats
- **Multijugador**: Sincronització via NGO i WebSockets per a esdeveniments.
- **Patró Repository**: Persistència de dades d'usuaris i partides (InMemory + MongoDB).
- **Seguretat**: Xifrat de contrasenyes amb bcrypt.
- **IA**: Bots controlats per `BotAI.cs`.
- **Proxy Invers**: Nginx configurat al port 80.

[Enllaç al Vídeo Canva](LINK_AQUI)
