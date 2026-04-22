# Resum del Projecte: El Último Samurai
**Curs:** 2DAM 2025-2026 | **Autor:** Roberto Lotreanu

**Objectiu:** Joc 2D multijugador competitiu on l'objectiu és sobreviure a l'ascens de la lava i als bots enemics en un entorn sincronitzat en temps real.

**Arquitectura:** 
- **Frontend:** Desenvolupat en Unity (C#) utilitzant Netcode for GameObjects (NGO) per a la sincronització de físiques i posicions.
- **Backend:** Servidor Node.js (Express) desplegat en un VPS. Gestiona sales, usuaris i estadístiques.
- **Comunicació:** Dualitat HTTP (per a API REST) i WebSockets natius (per a esdeveniments de partida com morts o fi de joc).
- **Persistència:** Implementació del **Patró Repository** amb dues fonts de dades:
    - **MongoDB + Mongoose** per al servidor de producció (Dades reals).
    - **JSON File Storage** per a l'entorn de desenvolupament local.
- **Seguretat i Robutesa:** Ús de **Fail2Ban** per a la protecció del servidor i **PM2** per a la gestió del procés.

[Enllaç al Vídeo (Canva)](LINK_AQUI)
