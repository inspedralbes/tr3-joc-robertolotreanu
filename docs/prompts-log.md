# Prompts Log de la Especificació: Panel d'Usuari al Lobby

*Aquest fitxer fa el seguiment i la traçabilitat de tota la interacció amb l'Agent d'IA.*

## 1. Generació de l'Especificació (OpenSpec)

**Timestamp:** 2026-04-16
**Prompt per generar la especificació:**
> "valebien, me gustaria tenerlo mas organizado lo de las salas, saber mi Alias, mis estadisticas (integrarlo con esto: Us d’IA amb desenvolupament guiat per especificació (OpenSpec) Es demana treballar una funcionalitat concreta de l’aplicació utilitzant una metodologia de Spec-Driven Development (SDD) amb suport d’Intel·ligència Artificial..."

**Acció d'IA i resultats:**
L'IA (Antigravity) proposa establir la funcionalitat completa de "Panell de Perfil del Jugador: Àlies i Estadístiques" i crea automàticament l'estructura `/docs/OpenSpec` i aquest mateix log.

## 2. Pendent d'Implementació
*(Aquí anirem afegint les ordres de com hem començat a generar el codi, iterat els errors...)*

## 2. Fase d'Implementació Inicial (Backend)

**Timestamp:** 2026-04-16
**Prompt:**
> "Executa la planificació del backend. Actualitza l'InMemoryUserRepository i el UserService per tenir estadístiques a 0 al registrar l'usuari, i exposa el GET /users/username/stats."

**Acció d'IA i resultats:**
Antigravity executa canvis a `UserService.js` per dotar el model de persistència de camps estadístics. S'afegeix un mètode get, s'exposa al `UserController` i s'aixeca a la ruta paramètrica. Es lliura funcional de primer intent i es documenta en aquest fitxer.

## 3. Fase d'Implementació Final (Frontend)

**Timestamp:** 2026-04-16
**Prompt:**
> "He afegit els elements NameLabel i StatsLabel al MainMenuUI.uxml. Toca el MenuManager.cs perquè llegeixi les dades de l'endpoint des del UnityWebRequest en entrar al Lobby i me les mostri per pantalla. Actualitza també el document tracker."

**Acció d'IA i resultats:**
S'aplica la lectura dels objectes *visual element* per part de la IA en C#. Es crea la corrutina `FetchUserStats` que utilitza un nou model DTO (`UserStatsData`) i un mètode GET d'`UnityWebRequest` que interpreta el JSON asíncronament mostrant un loader temporal i el formatatge *"Partides / Victòries / Baixes"*. Es lliga la corrutina a tots els fluxos on es carrega el lobby i es documenta en el *prompts-log*.

## 4. Reflexió i Iteració sobre la lògica d'Estadístiques

**Timestamp:** 2026-04-16
**Prompt:**
> "sale perfecto, pero mi juego como sabe que he ganado? al fin y al cabo es un mapa sin limite... no seria mejor registrar los mejores tiempos?"

**Acció d'IA i resultats:**
Davant d'aquesta deducció per part de l'usuari sobre la naturalesa contínua ("Survival") del joc associada a la mecànica de *LavaRise*, es reconsidera l'especificació. Antigravity adapta automàticament tant Node.js `UserService.js` (`stats: { gamesPlayed: 0, bestTime: 0 }`) com la part de Unity C# en `MenuManager.cs` per utilitzar i renderitzar floats de temps ("Millor Temps: XX.Xs"). Això completa una perfecta demostració d'adaptabilitat Agile en OpenSpec!

# FEATURE 2: MODES DE JOC I SCOREBOARD GLOBAL

**Timestamp:** 2026-04-17
**Prompt:**
> "vale, mi proxima idea es tener dos opciones, solitario o multijugador. si, que el SCOREBOARD SEA MUNDIAL, el mejor tiempo lo marque al usuario, da igual si lo ha hecho en multijugador o en solitario. 1 pantalla previa a crear sala"

**Acció d'IA i resultats:**
L'assistent tanca l'anterior mòdul i estructura l'OpenSpec d'aquesta nova mecànica a `docs/OpenSpec/feature2_modes.md`. Es proposa una alteració de flux on el Login porta a una nova capa de la UI (`ModeSelectionPanel`). Atès que el Scoreboard Mundial compartit és el nucli ideològic, el Backend de Node.js s'aprofita intacte gràcies al refactor previ de `bestTime`. Ens trobem a l'espera de la confirmació del pla (`opsx:apply`).

**Implementació Tècnica (opsx:apply):**
L'IA aïlla l'error previ de memòria del servidor de Node permetent el joc Solitari sense forçar el des-login. S'insereix al XML la nova Pantalla de Selecció de rutes i `MenuManager.cs` interromp el flux natiu modificant l'arrancada perquè executi `NetworkManager.Singleton.Shutdown()` prèviament a engegar `StartSoloMode()` per resoldre l'amenaça bloquejant de punts cecs ("Sockets binding") sobre els quals l'usuari havia emès logs vermells. Feature 2 implementada en C#!
