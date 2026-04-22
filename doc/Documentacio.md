# Documentació Tècnica: El Último Samurai

## 📊 Diagrama Entitat-Relació (E/R)
Com que fem servir **MongoDB** (NoSQL), l'esquema és documental. Aquesta és la representació de la col·lecció d'usuaris:

```mermaid
erDiagram
    USER {
        string _id PK
        string username "Unique"
        string password "Hashed (bcrypt)"
        datetime createdAt
    }
    STATS {
        int gamesPlayed
        float bestTime "Seconds"
    }
    USER ||--|| STATS : "té"
```

## 🏗️ Diagrama d'Arquitectura (Flux de dades)
Representació de com interactuen els diferents components del sistema:

```mermaid
sequenceDiagram
    participant U as Unity Client
    participant N as Node.js API
    participant W as WebSockets
    participant D as MongoDB

    U->>N: POST /login (Auth)
    N->>D: Find User
    D-->>N: User Data
    N-->>U: Token/Sessió
    
    U->>N: POST /rooms/create
    N-->>U: Room Info (Port 7777)
    
    U->>W: Connect ws://IP:3000/gs
    W-->>U: WebSocket Connection Established
    
    Note over U,W: Durant la partida
    U->>W: Send playerDied event
    W->>U: Broadcast event to all clients
```

## 🛠️ Tecnologies Utilitzades
- **Motor:** Unity 2022.3 LTS
- **Networking:** Netcode for GameObjects (NGO)
- **Backend:** Node.js + Express
- **Base de Dades:** MongoDB 7.0
- **Servidor:** VPS Linux (Ubuntu 24.04)
- **Proxy:** Nginx
- **Control de Processos:** PM2
