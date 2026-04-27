# Specification: Multiplayer Camera & Spawning

## Comportament Esperat

### 1. Camera Follow
- Al carregar l'escena `Game`, la càmera pot estar en qualsevol posició.
- En el moment que el `LocalPlayer` és instanciat i spawnejat per la xarxa, la càmera ha de fer un "snap" o transició cap a ell.
- Si un BotAI apareix abans, la càmera NO s'ha de quedar bloquejada en ell.

### 2. Player Spawner
- El servidor ha d'esperar el mínim temps possible (`0.1s`) per spawnejar els jugadors.
- El servidor ha de localitzar l'objecte "Floor" a l'escena i calcular la posició de spawn just a sobre d'aquest.
- Es permet un fallback a la primera plataforma procedimental si el "Floor" no existeix.

### 3. Name Synchronization
- El nom del jugador s'ha de transmetre des del login fins al personatge final.
- El servidor és l'única autoritat per assignar el nom al personatge (WritePermission.Server).
- El HUD ha de llistar a tots els participants humans (vius o morts) amb el seu nom real i estat.

## Casos d'Ús
- **Cas 1 (Solo vs Bots)**: Jugador entra, la càmera el segueix a ell, no als bots.
- **Cas 2 (Multijugador local)**: Dos jugadors en la mateixa màquina tenen noms diferents (p.e. "User1" i "User2") i el HUD els diferencia correctament.
- **Cas 3 (Mort)**: Un jugador cau a la lava, el seu nom segueix al HUD amb l'estat "MORTO".
