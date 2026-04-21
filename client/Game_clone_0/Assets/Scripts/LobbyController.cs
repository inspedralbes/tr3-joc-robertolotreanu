using UnityEngine;
using UnityEngine.UIElements;

public class LobbyController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement characterPreview;

    [Header("Sprites de los Personajes")]
    public Sprite spriteSamurai;    
    public Sprite spriteCaballero;    

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // 1. Encontrar el recuadro donde se muestra el personaje
        characterPreview = root.Q<VisualElement>("CharacterSpritePreview");

        Button btnSamurai = root.Q<Button>("BtnChar0");
        Button btnCaballero = root.Q<Button>("BtnChar1");

        // 2. Asignar la función a cada botón cuando se hace clic (con comprobación de seguridad)
        if (btnSamurai != null) btnSamurai.clicked += () => ChangeCharacterPreview(spriteSamurai);
        if (btnCaballero != null) btnCaballero.clicked += () => ChangeCharacterPreview(spriteCaballero);

        // Opcional: Poner el Samurai por defecto al empezar
        ChangeCharacterPreview(spriteSamurai);
    }

    // Esta es la función que cambia la imagen
    private void ChangeCharacterPreview(Sprite newSprite)
    {
        if (characterPreview != null && newSprite != null)
        {
            // Cambiamos el fondo del VisualElement por el sprite seleccionado
            characterPreview.style.backgroundImage = new StyleBackground(newSprite);
        }
    }
}