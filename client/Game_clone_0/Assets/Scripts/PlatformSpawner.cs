using UnityEngine;
using System.Collections.Generic;

public class PlatformSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject platformPrefab; 
    public Transform player;          
    
    [Header("Configuració de Distància (Y)")]
    public float stepY = 1.3f;       // Reducido para saltos más seguros (antes 1.5)
    
    [Header("Configuració Horizontal (X)")]
    [Tooltip("El hueco MÍNIMO garantizado entre los bordes de las plataformas")]
    public float huecoMinimo = 0.1f; // Bastante cerca para no forzar saltos imposibles
    public float maxJumpX = 2.7f;    // Distancia máxima humana reducida (antes 3.5)
    public float width = 14f;         
    
    [Header("Configuració de Mida")]
    public float minWidthScale = 0.8f; 
    public float maxWidthScale = 2.0f; 
    public float minThickness = 0.5f;  
    public float maxThickness = 1.0f;  
    
    private float lastSpawnY = -2f;
    private float lastSpawnX = 0f; 
    private float lastPlatformWidth = 2.0f; // Guardamos el ancho de la última
    public static List<GameObject> activePlatforms = new List<GameObject>(); // Público y estático para los bots

    private bool _initialized = false;

    void Start()
    {
        ResetSpawner();
    }

    public void ResetSpawner()
    {
        Debug.Log("<color=orange>SPAWNER:</color> Resetejant estat del Spawner...");
        lastSpawnY = -2f;
        lastSpawnX = 0f;
        lastPlatformWidth = 2.0f;
        
        // Destruir plataformes físiques que hagin pogut quedar (si el spawner és persistent)
        foreach (var p in activePlatforms) {
            if (p != null) Destroy(p);
        }
        activePlatforms.Clear();

        for (int i = 0; i < 6; i++) SpawnPlatform();
    }

    void Update()
    {
        // 1. Sanetització de la llista estàtica (per si hi ha referències mortes de la partida 1)
        activePlatforms.RemoveAll(item => item == null);

        // 2. Si el jugador local ha canviat (nova partida), forçem un reset
        if (PlayerMovement.LocalPlayer != null && (player == null || player != PlayerMovement.LocalPlayer.transform))
        {
            player = PlayerMovement.LocalPlayer.transform;
            ResetSpawner();
            Debug.Log("<color=cyan>SPAWNER:</color> Nova partida detectada. Resetejant.");
        }

        if (player == null) return;

        // 3. AUTO-REPARACIÓ: Si el jugador està molt per sobre de l'última plataforma 
        // (perquè el reset de Start() ha fallat o lastSpawnY tenia dades brossa), 
        // forçem que el spawner "salte" fins a la posició del jugador.
        if (player.position.y > lastSpawnY + 20f)
        {
            Debug.LogWarning("[SPAWNER] El jugador està massa lluny de les plataformes! Forçant salt de generació.");
            lastSpawnY = player.position.y - 5f;
        }

        // 4. Generació normal
        if (player.position.y + 12f > lastSpawnY)
        {
            SpawnPlatform();
        }
        CleanupPlatforms();
    }
    
    void SpawnPlatform()
    {
        lastSpawnY += stepY;
        float halfWidth = width / 2f;
        
        // 1. Decidimos el tamaño de la NUEVA plataforma primero
        float randomW = Random.Range(minWidthScale, maxWidthScale); 
        float randomThick = Random.Range(minThickness, maxThickness);

        // 2. LA MAGIA: Calculamos la distancia mínima para que los BORDES no se toquen
        // (Mitad de la plataforma anterior + Mitad de la nueva + El hueco para que salte el jugador)
        float distanciaMinimaSegura = (lastPlatformWidth / 2f) + (randomW / 2f) + huecoMinimo;
        
        // Nos aseguramos de que el salto máximo siempre sea mayor que la distancia mínima
        float saltoMaximoReal = Mathf.Max(maxJumpX, distanciaMinimaSegura + 0.5f);

        // 3. Calculamos la posición con esa distancia segura garantizada
        float offsetX = Random.Range(distanciaMinimaSegura, saltoMaximoReal);
        
        if (Random.value > 0.5f) { offsetX = -offsetX; }
        float randomX = lastSpawnX + offsetX;

        // Si nos chocamos con la pared, rebotamos hacia el lado contrario
        if (randomX < -halfWidth || randomX > halfWidth) 
        {
            randomX = lastSpawnX - offsetX; 
        }

        // Límite absoluto de la pantalla
        randomX = Mathf.Clamp(randomX, -halfWidth, halfWidth);
        
        // 4. Guardamos los datos para la siguiente plataforma
        lastSpawnX = randomX; 
        lastPlatformWidth = randomW; 
        
        // 5. Instanciamos
        Vector3 pos = new Vector3(randomX, lastSpawnY, 0);
        GameObject newPlatform = Instantiate(platformPrefab, pos, Quaternion.identity);
        newPlatform.transform.localScale = new Vector3(randomW, randomThick, 1f); 

        // Eliminado: newPlatform.tag = "Platform" (para evitar errores si la etiqueta no está creada en el proyecto)

        activePlatforms.Add(newPlatform);
    }

    void CleanupPlatforms()
    {
        if (activePlatforms.Count == 0) return;

        GameObject lowestPlatform = activePlatforms[0];
        
        // Prevención de cuelgues si la lava ya se la ha comido
        if (lowestPlatform == null)
        {
            activePlatforms.RemoveAt(0);
            return;
        }

        if (player != null && (player.position.y - lowestPlatform.transform.position.y) > 10f)
        {
            activePlatforms.RemoveAt(0);
            Destroy(lowestPlatform);
        }
    }
}