# El último samurai en pie — Guia del Projecte
**Curs:** 2DAM 2025-2026 | **Autor:** Roberto Lotreanu

- **Servidor:** Node.js (PM2) a `http://204.168.211.127:3000`
- **Base de Dades Real:** **MongoDB 7.0** (Gestió d'usuaris i rànquings).
- **Seguretat:** **Fail2Ban** (Protecció contra atacs de força bruta) + Nginx Proxy Invers.
- **Sincronització:** WebSockets natius (`ws`) i Unity Netcode (NGO).

---

## 🏗️ Com arrancar-ho en local (Desenvolupament)

### 1. Servidor Node.js
```powershell
# Obre un terminal a:
cd server/src
node server.js
```
*En local, el servidor utilitzarà automàticament `data/users.json` per facilitar el desenvolupament sense instal·lar MongoDB.*

### 2. Unity
1. Obre el projecte a Unity Hub.
2. Obre l'escena `Lobby` i prem ▶ **Play**.

---

## ✅ Requisits Tècnics — Estat Final
| Requisit | Implementació |
|---|---|
| **Base de Dades Real** | ✅ **MongoDB** actiu al VPS |
| **Patró Repository** | ✅ Implementació Dual (JSON / Mongo) |
| **Proxy Invers** | ✅ Nginx configurat |
| **Seguretat** | ✅ Fail2Ban instal·lat i configurat |
| **IA (ML-Agents)** | ✅ Bots amb IA integrats |
| **Multijugador** | ✅ Sincronització NGO (UDP) + WS |

---

## 🏗️ Arquitectura
El projecte segueix una arquitectura de **separació de responsabilitats** (Controller-Service-Repository), permetent que la lògica de negoci sigui independent de si les dades es guarden en un fitxer o en una base de dades NoSQL.

## 🛠️ Gestió del Servidor (PM2)
Comandes essencials per gestionar el servidor en producció:

- **Veure estat:** `pm2 status`
- **Veure logs en temps real:** `pm2 logs joc-server`
- **Reiniciar servidor:** `pm2 restart joc-server`
- **Aturar servidor:** `pm2 stop joc-server`
- **Arrancar servidor:** `pm2 start joc-server`

---

[Enllaç al Vídeo Canva](https://canva.link/wi3135leyxlfbl0)
