# Foundations: Multiplayer Synchronization and Stability

## Context
Aquest mòdul gestiona l'estabilització del sistema multijugador en temps real (NGO), centrant-se en la coherència de la càmera, el posicionament dels jugadors (Spawning) i la identitat dels usuaris (Noms).

## Objectius
1. **Càmera Intel·ligent**: Garantir que la càmera segueixi sempre al jugador local, fins i tot si hi ha bots o altres jugadors presents des del primer frame.
2. **Spawn Controlat**: Eliminar la latència d'entrada al joc i forçar l'aparició en la plataforma base de l'escena ("Floor").
3. **Identitat Persistent**: Sincronitzar els noms dels jugadors de manera que sobrevisquin als canvis d'escena i no col·lisionin en entorns de test local (ParrelSync).

## Restriccions
- **Netcode for GameObjects (NGO)**: Tota sincronització ha de passar pel servidor.
- **Persistent Data**: No es pot confiar en `PlayerPrefs` per a dades compartides en entorns de test local.
- **Scene Persistence**: Les dades aprovades al Lobby s'han de mantenir accessibles a la escena Game.
