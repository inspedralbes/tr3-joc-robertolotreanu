## Context
El joc és un multijugador 2D de salts sobre plataformes infinites generades proceduralment, on la lava puja constantment. S'ha introduït un mode "Solo" (Entrenament) on el jugador humà competeix contra bots controlats per la màquina. Actualment, el joc pateix de "pathfinding" ineficient, fent que els bots xoquin constantment o saltin sense parar bloquejant-se ("bugejant-se").

## Objectius
1. Desenvolupar un sistema d'Intel·ligència Artificial (`BotAI`) que permeti als bots navegar per les plataformes de forma intel·ligent i completament autònoma.
2. Els bots han de poder calcular les paràboles de salt, retirant-se si estan sota un sostre sòlid, agafant embranzida i saltant cap a la plataforma de destinació.
3. El sistema ha de ser net, lliure de salts escombraria ("spam jumping") i visualment similar a un jugador real intentant escalar.

## Restriccions
1. El moviment està rígidament lligat a les físiques de Unity (`Rigidbody2D`) i al component de xarxa `NetworkTransform`.
2. Les plataformes són completament sòlides (no es poden traspassar per sota tipus 'PlatformEffector2D'). S'han de saltar obligatòriament des dels laterals.
3. No es pot dependre de la funció `FindGameObjectsWithTag` ja que l'etiqueta pot no existir a l'Editor del client i trencar l'execució.
