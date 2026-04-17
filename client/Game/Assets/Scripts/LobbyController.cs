using UnityEngine;
using UnityEngine.UIElements;

public class LobbyController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement characterPreview;

    [Header("Sprites de los Personajes")]
    public Sprite spriteRonin;    
    public Sprite spriteNinja;    
    public Sprite spriteMonje;     
    public Sprite spriteKunoichi;  

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // 1. Encontrar el recuadro donde se muestra el personaje
        characterPreview = root.Q<VisualElement>("CharacterSpritePreview");

        Button btnRonin = root.Q<Button>("BtnChar0");
        Button btnNinja = root.Q<Button>("BtnChar1");
        Button btnMonje = root.Q<Button>("BtnChar2");
        Button btnKunoichi = root.Q<Button>("BtnChar3");

        // 2. Asignar la función a cada botón cuando se hace clic (con comprobación de seguridad)
        if (btnRonin != null) btnRonin.clicked += () => ChangeCharacterPreview(spriteRonin);
        if (btnNinja != null) btnNinja.clicked += () => ChangeCharacterPreview(spriteNinja);
        if (btnMonje != null) btnMonje.clicked += () => ChangeCharacterPreview(spriteMonje);
        if (btnKunoichi != null) btnKunoichi.clicked += () => ChangeCharacterPreview(spriteKunoichi);

        // Opcional: Poner el Ronin por defecto al empezar
        ChangeCharacterPreview(spriteRonin);
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