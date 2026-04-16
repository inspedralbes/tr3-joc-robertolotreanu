using UnityEngine;
using System.Collections.Generic;

public class PlatformSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject platformPrefab; 
    public Transform player;          
    
    [Header("Configuració de Distància (Y)")]
    public float stepY = 1.5f;       // Altura de los saltos mucho más asequible
    
    [Header("Configuració Horizontal (X)")]
    [Tooltip("El hueco MÍNIMO garantizado entre los bordes de las plataformas")]
    public float huecoMinimo = 0.5f; // Espacio por donde pasará el Samurai
    public float maxJumpX = 3.5f;    // Distancia máxima humana para saltar
    public float width = 14f;         
    
    [Header("Configuració de Mida")]
    public float minWidthScale = 0.8f; 
    public float maxWidthScale = 2.0f; 
    public float minThickness = 0.5f;  
    public float maxThickness = 1.0f;  
    
    private float lastSpawnY = -2f;
    private float lastSpawnX = 0f; 
    private float lastPlatformWidth = 2.0f; // Guardamos el ancho de la última
    private List<GameObject> activePlatforms = new List<GameObject>();

    void Start()
    {
        for (int i = 0; i < 6; i++) SpawnPlatform();
    }

    void Update()
    {
        if (player != null && player.position.y + 12f > lastSpawnY)
        {
            SpawnPlatform();
        }
        if (player != null) CleanupPlatforms();
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

        activePlatforms.Add(newPlatform);
    }

    void CleanupPlatforms()
    {
        if (activePlatforms.Count == 0) return;

        GameObject lowestPlatform = activePlatforms[0];
        if (lowestPlatform != null && (player.position.y - lowestPlatform.transform.position.y) > 10f)
        {
            activePlatforms.RemoveAt(0);
            Destroy(lowestPlatform);
        }
    }
}