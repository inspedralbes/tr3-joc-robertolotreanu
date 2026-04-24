# Informe de Projecte: Sincronització i Estabilitat Multijugador

**Alumne:** Roberto Lotreanu
**Projecte:** Joc 2D Multijugador (NGO + Node.js)
**Metodologia:** Spec-Driven Development (OpenSpec)

---

## 1. Explicació de la Funcionalitat
La feature seleccionada per al desenvolupament guiat per especificació ha estat la **Sincronització i Estabilitat del sistema Multijugador**. Aquesta inclou:
- **Càmera intel·ligent**: Priorització del jugador local sobre els bots.
- **Spawning autoritari**: Aparició immediata sobre la plataforma base ("Floor") de l'escena.
- **Sincronització de noms**: Sistema robust per evitar conflictes en entorns de test (ParrelSync) mitjançant un diccionari persistent al servidor.
- **HUD Dinàmic**: Llistat en temps real de tots els participants amb indicadors d'estat (Viu/Mort).
- **Unity Relay & Nat Traversal**: Migració d'un sistema basat en IP a Unity Relay per permetre connexions globals sense obertura de ports i un sistema de Join Codes de 6 caràcters.

## 2. Procés seguit amb la IA
S'ha seguit el flux de treball **OpenSpec**:
1.  **Definició de Foundations**: Establiment de les restriccions del sistema de xarxa (NGO).
2.  **Redacció de la Spec**: Definició detallada del comportament de la càmera i el HUD.
3.  **Planificació**: Divisió de la implementació en fases (Càmera -> Spawn -> Identitat).
4.  **Implementació Iterativa**: Ús de l'agent d'IA per generar el codi, seguit de proves en temps real.

## 3. Principals problemes trobats
- **Conflictes de Port i NAT**: El sistema inicial IP-to-IP fallava fora de la xarxa local. Es va resoldre integrant Unity Relay.
- **Col·lisions d'Host**: Diverses sales podien tenir el mateix hostName, provocant el tancament prematur de partides actives. S'ha solucionat amb un sistema d'ID d'un sol ús (GUID).
- **Conflicte de PlayerPrefs**: Durant el test local amb ParrelSync, les dues instàncies del joc compartien el mateix fitxer de configuració de Windows, provocant que tots els jugadors tinguessin el mateix nom.
- **Prioritat de Càmera**: Els bots s'instanciaven mil·lisegons abans que el jugador, provocant que la càmera s'enganxés a la IA en lloc de l'usuari.
- **Estructures de Braces**: Errors de sintaxi menors durant la refactorització de mètodes complexos a `PlayerMovement.cs`.

## 4. Decisions preses
- **Persistent Storage**: Es va decidir crear una classe `LobbySync` persistent (`DontDestroyOnLoad`) per actuar com a memòria cau del servidor per als noms dels jugadors, eliminant la dependència de `PlayerPrefs`.
- **Simplificació d'RPCs**: Es va centralitzar la notificació de mort al servidor per garantir que el HUD de tots els clients es mantingués sincronitzat amb la font de veritat del personatge.

## 5. Valoració crítica real
L'ús d'Spec-Driven Development amb IA permet mantenir un control molt més estricte sobre l'arquitectura del codi. Tot i que l'IA pot cometre errors de sintaxi o ignorar l'entorn de test (com el registre de Windows), disposar d'una especificació formal (`spec.md`) facilita enormement la detecció d'aquestes desviacions i la seva ràpida correcció. La metodologia redueix el temps de desenvolupament de sistemes de xarxa complexos en un 60-70%.
