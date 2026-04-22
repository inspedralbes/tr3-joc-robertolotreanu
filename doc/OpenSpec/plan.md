# Implementation Plan: Multiplayer Stability

## Fase 1: Càmera i Spawn
1. Modificar `CameraFollow.cs` per forçar la cerca del `LocalPlayer` en cada frame fins que es trobi.
2. Actualitzar `PlayerSpawner.cs` per reduir el delay de spawn i implementar la cerca de la plataforma base per nom.

## Fase 2: Sincronització d'Identitat
1. Afegir un diccionari persistent a `LobbySync.cs` per emmagatzemar la relació `clientId -> playerName`.
2. Actualitzar `MenuManager.cs` per omplir aquest diccionari durant l'`ApprovalCheck`.
3. Refactoritzar `PlayerMovement.cs` perquè el servidor assigni el nom des d'aquest diccionari al fer `OnNetworkSpawn`.

## Fase 3: Visualització HUD
1. Modificar `HUDController.cs` per eliminar el filtratge de jugadors morts a la llista superior.
2. Afegir estils visuals (color vermell) per a l'estat "MORTO" al HUD.
3. Actualitzar `LavaRise.cs` per usar la variable de xarxa `playerName` en les notificacions de mort.
