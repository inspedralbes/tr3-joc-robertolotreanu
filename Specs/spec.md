📜 ESPECIFICACIÓN TÉCNICA: EL ÚLTIMO SAMURAI EN PIE     
1. RESUMEN DEL PROYECTO

Juego de plataformas infinito 2D donde un Samurai debe escalar plataformas generadas procedimentalmente mientras el nivel de lava sube constantemente.
2. REGLAS DE FÍSICA Y MOVIMIENTO (PLAYER)

    Rigidbody 2D: Uso de Dynamic. Collision Detection: Continuous.

    Cooldown de Salto: Implementar un timer para evitar el "spam" de salto. El jugador solo puede saltar cada X segundos.

    Limitaciones: Sin doble salto. Movimiento lateral permitido en el aire (Air Control).

3. SISTEMA DE MUERTE INSTANTÁNEA (LAVA)

    Problema actual: El jugador se queda "pillado" antes de morir.

    Solución Spec: * La Lava debe ser un Trigger.

        Al detectar OnTriggerEnter2D con el Tag Player, se debe ejecutar un evento de muerte inmediato (Die()).

        Die() debe: 1. Desactivar el SpriteRenderer, 2. Congelar el Rigidbody, 3. Reiniciar la escena o mostrar UI de Game Over sin latencia.

4. GENERACIÓN PROCEDIMENTAL (SPAWNER)

    Variedad: Plataformas con escalas aleatorias en X (ancho) e Y (grosor).

    Algoritmo de Altura: La distancia entre plataformas debe estar entre minStep y maxStep para garantizar que siempre sean alcanzables según el JumpForce del jugador.

    Pool: Las plataformas que quedan por debajo de la cámara deben destruirse para optimizar memoria.

5. ESTÁNDARES DE CÓDIGO (PARA IA)

    Variables: Usar [SerializeField] private para parámetros ajustables.

    Naming: Clases en PascalCase, variables en camelCase.

    Clean Code: Separar Input de Physics (usar FixedUpdate para fuerzas).