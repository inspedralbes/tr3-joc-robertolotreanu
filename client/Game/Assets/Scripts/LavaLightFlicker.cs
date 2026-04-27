using UnityEngine;
using UnityEngine.Rendering.Universal; // Necessari per controlar la llum

public class LavaLightFlicker : MonoBehaviour
{
    private Light2D lavaLight;
    public float minIntensity = 1.5f;
    public float maxIntensity = 2.5f;

    void Start()
    {
        lavaLight = GetComponent<Light2D>();
    }

    void Update()
    {
        // Això fa que la intensitat canviï aleatòriament cada frame
        lavaLight.intensity = Random.Range(minIntensity, maxIntensity);
    }
}