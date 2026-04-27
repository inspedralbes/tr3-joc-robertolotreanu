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

---

### [Iteració 4] Càmera bloquejada en bots al mode SOLO
**Problema Detectat:** Al iniciar la partida amb bots, la càmera s'assignava aleatòriament a un bot en lloc del jugador local, fent el joc injugable des del segon 0.

**Causa (Debugging):** 
`CameraFollow.cs` buscava qualsevol objecte amb el script de moviment. Com que els bots spawnejaven uns mil·lisegons abans que el jugador (per la latència de xarxa de Netcode), la càmera es quedava "enganxada" al primer que trobava.

**Solució:**
Es va implementar una prioritat absoluta al `LocalPlayer`. Si el jugador local apareix, la càmera canvia immediatament el seu objectiu cap a ell, ignorant qualsevol bot previ.

---

### [Iteració 5] Spawn tardà i en plataforma incorrecta
**Problema Detectat:** El jugador trigava gairebé un segon a aparèixer i, quan ho feia, ho feia en una plataforma aleatòria o a l'aire, morint sovint abans de començar.

**Causa (Debugging):** 
`PlayerSpawner.cs` tenia un delay de `0.5s` i feia servir el `lastSpawnY` del generador procedimental, que no sempre coincideix amb el terra inicial de l'escena.

**Solució:**
Es va reduir el delay a `0.1s` i es va modificar el cercador de plataformes per buscar l'objecte **"Floor"** (la base estàtica de l'escena). Ara el jugador apareix exactament sobre el terra principal en cada inici de sessió.

---

### [Iteració 6] Noms duplicats en Multijugador (ParrelSync)
**Problema Detectat:** En provar amb dos clients a la vegada, tots dos jugadors apareixien amb el mateix nom (per exemple, "2"), fins i tot si s'havien identificat amb noms diferents.

**Causa (Debugging):** 
S'estava utilitzant `PlayerPrefs` per recuperar el nom en l'escena de joc. Com que ParrelSync comparteix el registre de Windows en la mateixa màquina, el segon client sobreescrivia el nom del primer en el fitxer local abans que el servidor pogués llegir-lo.

**Solució:**
Sincronització **autoritària des del Servidor**. Es va crear un diccionari persistent a `LobbySync.cs` que guarda el nom aprovat durant el login. En entrar a la partida, el servidor assigna el nom directament a la `NetworkVariable`, ignorant el fitxer local del client.

---

### [Iteració 7] HUD inconsistent i jugadors "invisibles"
**Problema Detectat:** El llistat de jugadors (Top-Right) no mostrava els mateixos jugadors en cada pantalla, i els jugadors morts desapareixien de la llista sobtadament.

**Causa (Debugging):** 
`HUDController.cs` filtrava els jugadors basant-se en `isAlive.Value`. Això feia que, en morir, el jugador s'esborrés de la llista en lloc de mostrar el seu estat final.

**Solució:**
S'ha eliminat el filtre de vida al HUD. Ara tots els jugadors apareixen sempre, i si moren, el seu estat canvia visualment a **"MORTO"** (en vermell), mantenint la coherència entre tots els clients.

---

### [Anàlisi del Resultat i Reflexió]
1. **Seguiment de l'especificació:** L'agent ha seguit les directrius de prioritzar la traçabilitat i la persistència de dades (Repository pattern indirecte mitjançant LobbySync).
2. **Iteracions:** S'han necessitat 4 iteracions per estabilitzar el multijugador (càmera -> spawn -> noms -> HUD).
3. **Punts de fallada de l'IA:** L'IA falla principalment en la interpretació de dependències externes (com el registre compartit de ParrelSync) si no se li detalla el context de l'entorn de test. Una vegada detectat el conflicte de `PlayerPrefs`, la solució arquitectònica ha estat sòlida.
4. **Modificació de l'OpenSpec:** S'ha hagut d'actualitzar el `plan.md` per incloure la sincronització de noms via diccionari persistent en lloc de rely en variables locals.
