using UnityEngine;
using System.Collections.Generic;

using Unity.Netcode;

public class PlatformSpawner : NetworkBehaviour
{
    private NetworkVariable<int> _mapSeed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [Header("Referencias")]
    public GameObject platformPrefab; 
    public Transform player;          
    
    [Header("Configuració de Distància (Y)")]
    public float stepY = 2.2f;       // Aumentat per saltos més còmodes (abans 1.3)
    
    [Header("Configuració Horizontal (X)")]
    [Tooltip("El hueco MÍNIMO garantizado entre los bordes de las plataformas")]
    public float huecoMinimo = 0.5f; // Hueco mínim garantit per evitar que es toquin (abans 0.1)
    public float maxJumpX = 4.5f;    // Distància màxima humana augmentada (abans 2.7)
    public float width = 14f;         
    
    [Header("Configuració de Mida")]
    public float minWidthScale = 0.8f; 
    public float maxWidthScale = 2.0f; 
    public float minThickness = 0.5f;  
    public float maxThickness = 1.0f;  
    
    private float lastSpawnY = -2f;
    private float lastSpawnX = 0f; 
    private float lastPlatformWidth = 2.0f;
    public static List<GameObject> activePlatforms = new List<GameObject>();

    // IMPORTANT: do NOT use the Inspector 'player' field to track the live player.
    // It is a prefab reference, not a scene instance. Always use PlayerMovement.LocalPlayer.
    private Transform _trackedPlayer;

    void Awake()
    {
        // Neteja la llista estàtica SEMPRE que el spawner es crea de nou (nova escena/sessió).
        // Sense això, la 2a partida tindria referències mortes a plataformes destruïdes
        // de la 1a partida i el spawner no generaria res.
        foreach (var p in activePlatforms)
        {
            if (p != null) Destroy(p);
        }
        activePlatforms.Clear();
        Debug.Log("<color=orange>SPAWNER:</color> Llista estàtica netejada a Awake.");
    }

    void Start()
    {
        // El Reset se hará en OnNetworkSpawn para asegurar la semilla
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ResetSpawner();
    }

    public void ResetSpawner()
    {
        // Solo el servidor/host decide la semilla inicial
        if (IsServer) {
            _mapSeed.Value = Random.Range(1, 999999);
        }
        
        // Aplicamos la semilla sincronizada
        Random.InitState(_mapSeed.Value);

        Debug.Log($"<color=orange>SPAWNER:</color> Resetejant amb llavor: {_mapSeed.Value}");
        lastSpawnY = -2f;
        lastSpawnX = 0f;
        lastPlatformWidth = 2.0f;
        
        // Destruir plataformes físiques que hagin pogut quedar (si el spawner és persistent)
        foreach (var p in activePlatforms) {
            if (p != null) Destroy(p);
        }
        activePlatforms.Clear();
        
        // FORÇAR VALORS PER CODI (Ignora l'Inspector per seguretat)
        stepY = 2.2f;
        huecoMinimo = 0.5f;
        maxJumpX = 4.5f;

        for (int i = 0; i < 6; i++) SpawnPlatform();
    }

    void Update()
    {
        // 1. Sanitize static list from dead references
        activePlatforms.RemoveAll(item => item == null);

        // 2. Try to find the local player if not tracked yet
        if (_trackedPlayer == null && PlayerMovement.LocalPlayer != null)
        {
            _trackedPlayer = PlayerMovement.LocalPlayer.transform;

            // Si les plataformes de Start() ja estan per sobre del jugador, les conservem
            // i simplement enganchem el tracking. Altrament resetegem des de la posició actual.
            // Açò evita el bug on el reset tirava lastSpawnY enrere i bloqueava la generació
            // fins que el jugador pujava prou per tornar a superar el llindar.
            if (lastSpawnY < _trackedPlayer.position.y + 5f)
            {
                lastSpawnY = _trackedPlayer.position.y - 3f;
                lastSpawnX = 0f;
                for (int i = 0; i < 6; i++) SpawnPlatform();
            }
            // Si lastSpawnY ja és prou alt, generar unes poques més cap amunt per assegurar marge
            else
            {
                for (int i = 0; i < 3; i++) SpawnPlatform();
            }
            Debug.Log($"<color=cyan>SPAWNER:</color> Jugador local trobat. lastSpawnY={lastSpawnY:F1}. Generant plataformes.");
        }

        if (_trackedPlayer == null) return;

        // 3. Generate upward as the player climbs
        if (_trackedPlayer.position.y + 12f > lastSpawnY)
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
        if (activePlatforms.Count == 0 || _trackedPlayer == null) return;

        GameObject lowestPlatform = activePlatforms[0];
        
        if (lowestPlatform == null)
        {
            activePlatforms.RemoveAt(0);
            return;
        }

        if ((_trackedPlayer.position.y - lowestPlatform.transform.position.y) > 10f)
        {
            activePlatforms.RemoveAt(0);
            Destroy(lowestPlatform);
            Debug.Log("<color=gray>[SPAWNER]</color> Plataforma inferior destruida para ahorrar memoria.");
        }
    }
}