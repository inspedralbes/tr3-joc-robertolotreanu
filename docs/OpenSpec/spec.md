## Comportament Esperat

### 1. Detecció de Visió de l'Entorn
- La IA no farà un raycast pesat, sinó que accedirà en temps real a la variable estàtica `PlatformSpawner.activePlatforms`.
- Iterarà per buscar la plataforma més propera a la qual sigui vàlid saltar (ha de tenir un `distY > 0.5f` per asegurar-nos que hi ha un objectiu ascendent).

### 2. Presa de Decisions Geometral
- **Si el bot sota la plataforma (`distY > 1.2f` i `distX` petit):** El bot rebrà l'ordre de caminar en direcció oposada (calcularà un `targetOffset` d'aprox. 1.8 metres). No pot saltar metre estigui sota l'ombra de la plataforma.
- **Si el bot està a distància de salt òptim:** Realitzarà una comprovació d'embranzida i demanarà el salt mantenint el botó `HorizontalInput`.

### 3. Físiques del Salt
- S'invocarà de forma atòmica `ForceBotJump()`. Aquest mètode és el mur de contenció que assegura que sols s'activa el trigger si `isGrounded == true`. Això evitarà que pugin "volant".
- Un petit cooldown protegirà contra repeticions ràpides d'avaluacions a `Update()`.

### 4. Gestor de Bloquejos (Desatascos)
- S'inclou una condició de fallada: si la velocitat en X del Rigidbody és propera a 0 pero l'input és actiu, significa col·lisió contra mur. Es procedirà a un salt aleatori evasiu.
