# Foundations

## Context
Actualment el menú del joc només llista de forma molt bàsica les sales i permet unir-se. El jugador necessita sentir que el seu comportament dins el joc està enregistrat. Volem organitzar el "Lobby" per incloure un panell on el jugador pugui veure el seu Àlies i les seves estadístiques de joc (vegades que ha guanyat, perdut, etc.). Això integra directament el patró Repository que hem muntat al Backend.

## Objectius
1. Mostrar l'àlies de l'usuari de forma clara un cop entra al Lobby.
2. Recuperar del Backend i mostrar les estadístiques persistents del jugador.
3. Organitzar millor l'espai del Lobby al Unity UI (UI Toolkit).

## Restriccions
- S'ha de desenvolupar fent ús de l'arquitectura de Microserveis/Capes del Backend.
- Les dades s'han d'obtenir mitjançant `UnityWebRequest` a través de l'API HTTP.
- S'ha de seguir fidelment la metodologia Spec-Driven Development (SDD).
