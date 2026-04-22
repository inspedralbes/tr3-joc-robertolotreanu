# El último samurai en pie — Guia del Projecte
**Curs:** 2DAM 2025-2026 | **Autor:** Roberto Lotreanu

## 🚀 Servidor en Producció (VPS)
El servidor està desplegat en un VPS a la IP: `204.168.211.127`.

- **API URL:** `http://204.168.211.127:3000/api`
- **WS URL:** `ws://204.168.211.127:3000/gs`
- **Gestió:** PM2 (Robustesa) i Fail2Ban (Seguretat).

---

## 🏗️ Com arrancar-ho en local

### 1. Servidor Node.js
```powershell
cd server/src
npm install
npm start
```
🚀 Servidor a `http://localhost:3000`.

### 2. Unity
1. Obre el projecte a Unity Hub.
2. Obre l'escena `Lobby`.
3. Prem ▶ **Play**.

---

## 🏗️ Arquitectura del sistema
El sistema utilitza una arquitectura de microserveis on Unity actua com a frontend i Node.js com a backend centralitzat.

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
- **Multijugador Real**: Sincronització via NGO i WebSockets per a esdeveniments.
- **Patró Repository**: Persistència de dades d'usuaris i partides.
- **Seguretat**: Xifrat de contrasenyes amb bcrypt i protecció d'accés.
- **IA**: Bots controlats per `BotAI.cs`.
- **Proxy Invers**: Nginx configurat al servidor per centralitzar el trànsit.

[Enllaç al Vídeo Canva](LINK_AQUI)
