# Specification (Spec)

## Comportament Esperat

### Backend (Node.js)
1. **Entitat i Repositori:** La informació de l'usuari haurà de rebre un camp d'`estadístiques` o es crearà un servei de "Resultats" (es demanava al document principal). 
2. **Endpoint de Lectura:** L'API exposarà un endpoint HTTP GET `/api/users/:username/stats` (o dins el login) que retornarà les partides jugades i les victòries.

### Frontend (Unity)
1. **UI Lobby:** S'afegirà un nou panell dins de l'espai de Lobby anomenat "ProfilePanel".
2. **Informació:** Exhibirà com a títol "Hola, [Àlies]" i recuperarà els valors numèrics enviats pel backend.
3. **Controlador (MenuManager.cs):** En el moment de mostrar el Lobby, es llançarà una corrutina que demanarà aquesta informació al servidor i actualitzarà els labels visuals pertinents.
