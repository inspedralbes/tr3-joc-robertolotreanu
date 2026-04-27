# OpenSpec: Feature 2 - Modes de Joc i Global Scoreboard

## 1. Foundations (Fonaments)
**Motiu de la Feature:** Separar de forma orgànica i intuïtiva l'experiència "LavaRise". Es permet a un jugador entrar al mapa sense haver d'esperar o dependre d'una connexió de tercers.
**Objectius:**
1. Mantenir un registre unificat a la base de dades on els temps de supervivència d'ambdós modes valen exactament igual.
2. Inserir una pantalla de decisií "Mode de Joc" just abans d'anar al Lobby de Servidors actius.

## 2. Spec (Especificació Tècnica)
- **Frontend (Unity UI):** 
  - Creació de `ModeSelectionPanel` al UXML amb dos botons grans: "SOLITARI" i "MULTIJUGADOR".
  - A l'iniciar sessió, amagar `LoginPanel` i mostrar `ModeSelectionPanel`.
- **Lògica de Joc (MenuManager i NetCode):**
  - Si tria "Solitari": Amagar panell i cridar a l'inici directe del joc utilitzant `NetworkManager.Singleton.StartHost()` però limitant-lo a una instància i evitant el Broadcast de la sala a NodeJS.
  - Si tria "Multijugador": Mostrar `LobbyPanel` tal i com ha estat programat fins ara, permetent que consulti sales als altres hosts.
- **Backend (NodeJS):** No es requereixen variacions en rutes ni models, ja que l'score global de "Millor Temps" és comú i l'enviarem al final de la partida de la mateixa manera independentment del mode.

## 3. Plan (Pla d'Implementació)
**Fase 1:** Actualitzar `MainMenuUI.uxml` crear el panell `#ModeSelectionPanel`.
**Fase 2:** Alterar el flux a `MenuManager.cs` per interceptar el Login i enviar a la Selecció de Mode en lloc del Lobby.
**Fase 3:** Implementar el mètode auxiliar `StartSoloMode()` que obre el mapa sense anar a la lògica de Lobby Multijugador.
