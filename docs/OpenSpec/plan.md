## Estratègia d'Implementació

**Actuació 0: Registre en Bitàcola**
- Registrar la situació inicial de control trencat de la IA a `prompts-log.md` per traçabilitat del projecte.

**Actuació 1: Sincronització Fixa i Sensorial**
- Fitxer objectiu: `BotAI.cs`.
- Eliminar totalment `FindGameObjectsWithTag` ja que és perillós depenent de la configuració de l'entorn.
- Substituir per pas net d'una llista de instàncies viva `PlatformSpawner.activePlatforms`.

**Actuació 2: Refactor del 'Update' Lògic i Cinemàtic**
- Fitxer objectiu: `BotAI.cs`.
- A l'avaluació de distàncies, separar per estats: "Sota Sostre", "A Punt de Salt", "Salt i Moviment Aeri".
- Ús de variable auxiliar `puntoSaltoX` pre-calculada basat en la posició del jugador vs plataforma.

**Actuació 3: Protecció Física**
- Fitxer objectiu: `PlayerMovement.cs`.
- Modificar mètode `ForceBotJump()` incrustant de forma fixa `isGrounded` per validar sempre legalitat física. Evitar l'escalament espacial sense plataformes de suport.
