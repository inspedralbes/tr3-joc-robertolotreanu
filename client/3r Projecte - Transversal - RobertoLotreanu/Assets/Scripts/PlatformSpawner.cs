using UnityEngine;
using System.Collections.Generic;

public class PlatformSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject platformPrefab; 
    public Transform player;          
    
    [Header("Configuració de Distància (Y)")]
    public float stepY = 2.5f;       // Distancia vertical FIJA (ajusta esto si no llegas)
    
    [Header("Configuració Horizontal (X)")]
    public float maxJumpX = 4.0f;    // Distancia horizontal MÁXIMA que puede cubrir tu Samurai
    public float width = 14f;         // Ancho total de la cámara
    
    [Header("Configuració de Mida")]
    public float minWidthScale = 0.8f; 
    public float maxWidthScale = 2.0f; 
    public float minThickness = 0.5f;  
    public float maxThickness = 1.0f;  
    
    private float lastSpawnY = -2f;
    private float lastSpawnX = 0f; // Guardamos dónde estaba la última plataforma en X
    private List<GameObject> activePlatforms = new List<GameObject>();

    void Start()
    {
        // Generamos las primeras plataformas de golpe
        for (int i = 0; i < 6; i++)
        {
            SpawnPlatform();
        }
    }

    void Update()
    {
        // 1. GENERACIÓN: Si el jugador sube, creamos plataformas por encima
        if (player != null && player.position.y + 12f > lastSpawnY)
        {
            SpawnPlatform();
        }

        // 2. OPTIMIZACIÓN: Destruimos las plataformas que van quedando muy abajo
        if (player != null)
        {
            CleanupPlatforms();
        }
    }
    
    void SpawnPlatform()
    {
        // 1. DISTANCIA VERTICAL (Ahora es siempre exactamente la misma)
        lastSpawnY += stepY;

        // 2. POSICIÓ HORIZONTAL (Limitada para que sea alcanzable)
        float halfWidth = width / 2f;
        
        // Calculamos dónde puede aparecer la siguiente plataforma basándonos en la anterior
        float minX = Mathf.Max(-halfWidth, lastSpawnX - maxJumpX);
        float maxX = Mathf.Min(halfWidth, lastSpawnX + maxJumpX);
        
        float randomX = Random.Range(minX, maxX);
        lastSpawnX = randomX; // Guardamos esta posición para usarla en el siguiente salto
        
        Vector3 pos = new Vector3(randomX, lastSpawnY, 0);
        GameObject newPlatform = Instantiate(platformPrefab, pos, Quaternion.identity);

        // 3. MIDA
        float randomW = Random.Range(minWidthScale, maxWidthScale); 
        float randomThick = Random.Range(minThickness, maxThickness);
        newPlatform.transform.localScale = new Vector3(randomW, randomThick, 1f); 

        // La guardamos en nuestra lista para optimizar después
        activePlatforms.Add(newPlatform);
    }

    // NUEVA FUNCIÓN: Destruye las plataformas para optimizar
    void CleanupPlatforms()
    {
        if (activePlatforms.Count == 0) return;

        // Revisamos siempre la plataforma más vieja (la posición 0 de la lista)
        GameObject lowestPlatform = activePlatforms[0];
        
        // Si la plataforma está 10 unidades por debajo del jugador...
        if (lowestPlatform != null && (player.position.y - lowestPlatform.transform.position.y) > 10f)
        {
            // La quitamos de la lista y la destruimos para liberar memoria
            activePlatforms.RemoveAt(0);
            Destroy(lowestPlatform);
        }
    }
}