# Plan

## Estratègia d'implementació

### Pas 1: Ampliació del Repository i Service (Backend)
- Actualitzar `InMemoryUserRepository.js` per inicialitzar els usuaris amb camps `stats: { gamesPlayed: 0, wins: 0 }`.
- Crear el mètode en `UserService.js` per recuperar les dades de l'usuari (`getUserStats`).
- Exposar l'endpoint `GET /api/users/stats` dins de `userRoutes.js` (passant l'àlies pel query param, o pel path).

### Pas 2: Disseny del Panell UI a Unity
- Entrar al fitxer `MainMenuUI.uxml` i mitjançant l'UI Builder (o manualment), agrupar la secció del Lobby.
- Afegir dues `Label`: una per a l'Àlies i una altra per a les Estadístiques.

### Pas 3: Integració de la Lògica al Unity
- Modificar el `MenuManager.cs` per declarar aquestes Labels.
- Crear una corrutina `FetchStats()` que cridi a l'API lliurant el `PlayerPrefs.GetString("PlayerName")`.
- Mostra la informació un cop retorni la petició.
