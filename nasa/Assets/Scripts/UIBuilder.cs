using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ayudante chiquito para armar las pantallas desde codigo
public static class UIBuilder
{
    public static TMP_FontAsset fuente;

    static TMP_FontAsset fuenteRetro;

    public static Color colorPanel = new Color(0.07f, 0.09f, 0.20f, 0.93f);
    public static Color acento = new Color(0.35f, 0.85f, 1f);
    public static Color verde = new Color(0.18f, 0.55f, 0.38f);
    public static Color rojo = new Color(0.65f, 0.2f, 0.3f);
    public static Color azul = new Color(0.15f, 0.3f, 0.5f);
    public static Color dorado = new Color(0.95f, 0.75f, 0.25f);

    // si no hay fuente puesta en el Inspector se arma una retro con el archivo de Resources Fonts
    static TMP_FontAsset ObtenerFuente()
    {
        if (fuente != null) return fuente;
        if (fuenteRetro == null)
        {
            Font origen = Resources.Load<Font>("Fonts/Silkscreen-Regular");
            if (origen != null)
            {
                fuenteRetro = TMP_FontAsset.CreateFontAsset(origen);
                // las letras raras que el pixel no tenga se piden a la fuente normal
                if (TMP_Settings.defaultFontAsset != null)
                {
                    fuenteRetro.fallbackFontAssetTable = new List<TMP_FontAsset> { TMP_Settings.defaultFontAsset };
                }
            }
        }
        return fuenteRetro;
    }

    public static Sprite CargarSprite(string nombreSprite)
    {
        return Resources.Load<Sprite>("Sprites/" + nombreSprite);
    }

    public static Canvas CrearCanvas()
    {
        GameObject objeto = new GameObject("Canvas");
        Canvas canvas = objeto.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler escalador = objeto.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1280, 720);
        escalador.matchWidthOrHeight = 0.5f;
        objeto.AddComponent<GraphicRaycaster>();

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject sistema = new GameObject("EventSystem");
            sistema.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            sistema.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            sistema.AddComponent<StandaloneInputModule>();
#endif
        }
        return canvas;
    }

    public static RectTransform CrearRect(string nombreObjeto, Transform padre, Vector2 posicion, Vector2 tamano)
    {
        GameObject objeto = new GameObject(nombreObjeto, typeof(RectTransform));
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.SetParent(padre, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;
        return rect;
    }

    public static void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static Image Caja(Transform padre, Vector2 posicion, Vector2 tamano, Color color)
    {
        RectTransform rect = CrearRect("Caja", padre, posicion, tamano);
        Image imagen = rect.gameObject.AddComponent<Image>();
        imagen.color = color;
        return imagen;
    }

    public static Image Panel(Transform padre, Vector2 posicion, Vector2 tamano)
    {
        Image imagen = Caja(padre, posicion, tamano, colorPanel);
        Outline borde = imagen.gameObject.AddComponent<Outline>();
        borde.effectColor = acento;
        borde.effectDistance = new Vector2(2, -2);
        return imagen;
    }

    public static Image Dibujo(Transform padre, string nombreSprite, Vector2 posicion, Vector2 tamano)
    {
        Image imagen = Caja(padre, posicion, tamano, Color.white);
        imagen.sprite = CargarSprite(nombreSprite);
        imagen.raycastTarget = false;
        return imagen;
    }

    // fondo a pantalla completa, por defecto el espacio de siempre
    public static Image Fondo(Transform padre, string nombreSprite = "bg_space")
    {
        RectTransform rect = CrearRect("Fondo", padre, Vector2.zero, Vector2.zero);
        Estirar(rect);
        Image imagen = rect.gameObject.AddComponent<Image>();
        imagen.sprite = CargarSprite(nombreSprite);
        imagen.raycastTarget = false;
        return imagen;
    }

    public static TextMeshProUGUI Etiqueta(Transform padre, string texto, int tamanoLetra, Vector2 posicion, Vector2 caja, TextAlignmentOptions alineacion)
    {
        RectTransform rect = CrearRect("Etiqueta", padre, posicion, caja);
        TextMeshProUGUI t = rect.gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset fuenteUsada = ObtenerFuente();
        if (fuenteUsada != null) t.font = fuenteUsada;
        t.text = texto;
        t.fontSize = tamanoLetra;
        // esta fuente es ancha, asi que el texto se encoge solo si no cabe en su caja
        t.enableAutoSizing = true;
        t.fontSizeMin = Mathf.Max(10f, tamanoLetra * 0.55f);
        t.fontSizeMax = tamanoLetra;
        t.alignment = alineacion;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    public static Button Boton(Transform padre, string texto, Vector2 posicion, Vector2 tamano, Color color, UnityAction alPulsar, int tamanoLetra = 22)
    {
        Image imagen = Caja(padre, posicion, tamano, color);
        Outline borde = imagen.gameObject.AddComponent<Outline>();
        borde.effectColor = new Color(0.45f, 0.9f, 1f, 0.8f);
        borde.effectDistance = new Vector2(2, -2);

        Button boton = imagen.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen;
        boton.onClick.AddListener(alPulsar);
        Etiqueta(imagen.transform, texto, tamanoLetra, Vector2.zero, tamano - new Vector2(10, 6), TextAlignmentOptions.Center);
        return boton;
    }

    public static Slider Barra(Transform padre, Vector2 posicion, Vector2 tamano, Color relleno)
    {
        Image fondo = Caja(padre, posicion, tamano, new Color(0.04f, 0.05f, 0.13f, 1f));
        Outline borde = fondo.gameObject.AddComponent<Outline>();
        borde.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.9f);
        borde.effectDistance = new Vector2(1, -1);

        Slider deslizador = fondo.gameObject.AddComponent<Slider>();
        RectTransform zona = CrearRect("ZonaRelleno", fondo.transform, Vector2.zero, Vector2.zero);
        Estirar(zona);
        zona.offsetMin = new Vector2(3, 3);
        zona.offsetMax = new Vector2(-3, -3);

        Image imagenRelleno = Caja(zona, Vector2.zero, Vector2.zero, relleno);
        Estirar(imagenRelleno.rectTransform);

        deslizador.fillRect = imagenRelleno.rectTransform;
        deslizador.direction = Slider.Direction.LeftToRight;
        deslizador.minValue = 0f;
        deslizador.maxValue = 100f;
        deslizador.transition = Selectable.Transition.None;
        deslizador.interactable = false;
        deslizador.value = 0f;
        return deslizador;
    }

    // titulo y boton de volver para las pantallas de minijuegos
    public static void Encabezado(Transform padre, string titulo, UnityAction alVolver)
    {
        Boton(padre, "BACK", new Vector2(-540, 320), new Vector2(140, 46), azul, alVolver, 20);
        Etiqueta(padre, titulo, 30, new Vector2(-190, 320), new Vector2(500, 50), TextAlignmentOptions.Left);
    }

    public static void Ventana(Transform padre, string titulo, string mensaje, string textoBoton, UnityAction alPulsar)
    {
        Image velo = Caja(padre, Vector2.zero, new Vector2(3000, 2000), new Color(0f, 0f, 0f, 0.7f));
        Image tarjeta = Panel(velo.transform, Vector2.zero, new Vector2(640, 320));
        TextMeshProUGUI cabecera = Etiqueta(tarjeta.transform, titulo, 40, new Vector2(0, 100), new Vector2(600, 60), TextAlignmentOptions.Center);
        cabecera.color = acento;
        Etiqueta(tarjeta.transform, mensaje, 24, new Vector2(0, 10), new Vector2(560, 120), TextAlignmentOptions.Center);
        Boton(tarjeta.transform, textoBoton, new Vector2(0, -110), new Vector2(260, 60), verde, alPulsar);
    }

    public static void IrA(string nombreEscena)
    {
        SceneManager.LoadScene(nombreEscena);
    }
}
