# Log de Prompts i Traçabilitat (Bot AI)

**Objectiu:** Solucionar l'avaluació caòtica del Pathfinding de l'IA (Bots) i implementar de forma traçable el nou sistema de salt autònom basat en Spec-Driven Development.

---

### [Iteració 1] Primer error detectat
**Problema Detectat:** El client reporta que els bots *“saltaban como locos”* i es bugejaven sota les plataformes o a terra.

**Causa (Debugging):** 
Analitzant el codi de `BotAI.cs` i `PlayerMovement.cs`, el mètode `ForceBotJump()` anul·lava la validació física de la boleana `isGrounded`. Això provocava que els bots fessin "spam" del salt i s'elevaresin volant pel mig de l'escena o saltant constantment quan apropaven el seu centre X a una plataforma molt superior (fent "pogo-sticking").

**Canvi / Fix Proposat:**
Es va demanar aplicar directrius de limitació espacial, recollides al *plan.md*. Primer, protegir `ForceBotJump()`:
```csharp
    public void ForceBotJump()
    {
        if (isGrounded && Time.time >= lastJumpTime + jumpCooldown)
        {
            jumpRequested = true;
            botJumpRequested = false;
        }
    }
```

---

### [Iteració 2] Detecció de variables nul·les
**Problema Detectat:** A meitat de les proves es feia servir el comando de Raycast i Codi basat en Unity Edit "Tag". Això feia llençar una Excepció de Unity tipus "Tag 'Platform' does not exist" i aturava completament l'execució. Els bots quedaven vegetatius en terra.

**Causa (Debugging):** 
Unity requereix que tota "Tag" es crei a mà per un humà a la UI abans de ser cridada en codi font C#. El desenvolupador no ho tenia, i l'script s'enviava directament a Null Exception aturant l'Update de Generació (`PlatformSpawner.cs`).

**Canvi / Fix Proposat:**
Sincronitzar per referències estàtiques a memòria deslligades de configuracions estètiques de l'editor de Unity com es demostra al *foundations*. Es creà `PlatformSpawner.activePlatforms` i aixo omplí `BotAI` de les meta-referencies reals i segures al mil·lisegon sense col·lapsar cap llibreria Base.

---

### [Iteració 3] Comportament de Caça de Plataformes Sòlides
**Problema Detectat:** Els bots saltaven una sola vegada però s'obrien el cap contra la llosa i queien directament avall perquè les plataformes són totalment sòlides inferiors i no deixaven que travessessin al saltar just sota elles.

**Causa (Debugging):**
L'algoritme només perseguia els components X i buscava situar-lo a `distX = 0`, posant-lo directament al forat cec per sota la plataforma abans de processar el salt. Això impedia l'ascenció de nivells en "modo solo", resultant en un bucle mort que frustrava els intents i donava la pèrdua immediata de la "vida". 

**Refinament SDD i Solució:** 
Implementació d'un PUNT DE SALT fix seguit de directrius al spec.
`float puntoSaltoX = (distX >= 0) ? 1.2f : -1.2f;`
Si hi ha ombra, allunya't. Si arribes, salta agafant velocitat horitzontal `1f` simultàniament a l'impulsió Y.

D'aquesta manera tota traça ha seguit el procediment OpenSpec a l'hora que l'IA va documentant i construïnt en un context totalment tancat i modular.
