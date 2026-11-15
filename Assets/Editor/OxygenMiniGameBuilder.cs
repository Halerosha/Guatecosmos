#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// OxygenMiniGameBuilder = HERRAMIENTA DE EDITOR (no se usa dentro del juego).
///
/// Con un clic en  Tools > Minijuego Oxigeno > Construir interfaz completa  crea TODO:
///   - Prefab de la pieza de tubería (casillas pegadas, sin huecos)
///   - Pantalla de inicio con el botón INICIAR en el centro
///   - HUD estilo lobby: Nivel / Ronda, barra de oxígeno, Tiempo / Movimientos
///   - Cuadrícula sobre el fondo de la base lunar
///   - Panel de resultado y conexión de todos los campos del script
///
/// IMPORTANTE: este archivo debe estar en  Assets/Editor/
/// </summary>
public static class OxygenMiniGameBuilder
{
    private const string PanelName = "OxygenMiniGamePanel";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string PrefabPath = "Assets/Prefabs/PipeTilePrefab.prefab";

    // ---- Paleta del lobby ----
    private static readonly Color CyanNeon    = new Color(0.20f, 0.88f, 1.00f, 1f);
    private static readonly Color OrangeNeon  = new Color(1.00f, 0.69f, 0.13f, 1f);
    private static readonly Color SoftText    = new Color(0.72f, 0.80f, 1.00f, 1f);
    private static readonly Color GridColor   = new Color(0.03f, 0.03f, 0.13f, 1f);

    // ---- Sprites (se cargan desde Assets/Sprites) ----
    private static Sprite sprBackground, sprPanel, sprButton, sprButtonPlay;
    private static Sprite sprBarBg, sprBarFill, sprCell, sprPlayIcon, sprTitleBg;
    private static TMP_FontAsset retroFont;

    [MenuItem("Tools/Minijuego Oxigeno/Construir interfaz completa")]
    public static void Build()
    {
        Canvas canvas = GetOrCreateCanvas();

        // ---- Si ya existe una versión anterior, la reemplazamos ----
        Transform oldPanel = canvas.transform.Find(PanelName);
        Transform oldTile = canvas.transform.Find("PipeTilePrefab");
        if (oldPanel != null || oldTile != null)
        {
            bool ok = EditorUtility.DisplayDialog(
                "Minijuego de Oxígeno",
                "Ya existe un panel del minijuego en el Canvas.\n\n" +
                "¿Quieres borrarlo y construirlo de nuevo automáticamente?",
                "Sí, reemplazar", "Cancelar");
            if (!ok) return;
            if (oldPanel != null) Undo.DestroyObjectImmediate(oldPanel.gameObject);
            if (oldTile != null) Undo.DestroyObjectImmediate(oldTile.gameObject);
        }
        Transform oldTest = canvas.transform.Find("TestButton");
        if (oldTest != null) Undo.DestroyObjectImmediate(oldTest.gameObject);
        Transform oldStart = canvas.transform.Find("StartScreen");
        if (oldStart != null) Undo.DestroyObjectImmediate(oldStart.gameObject);

        // ---- 1) Cargar sprites (los configura como pixel-art automáticamente) ----
        Sprite spEnd = LoadPipe("Pipe_End");
        Sprite spStraight = LoadPipe("Pipe_Straight");
        Sprite spCorner = LoadPipe("Pipe_Corner");
        Sprite spT = LoadPipe("Pipe_T");
        Sprite spCross = LoadPipe("Pipe_Cross");

        sprBackground = LoadUISprite("Background_Moon", Vector4.zero, 100);
        sprPanel      = LoadUISprite("UI_Panel", new Vector4(8, 8, 8, 8), 25);
        sprButton     = LoadUISprite("UI_Button", new Vector4(5, 5, 5, 5), 25);
        sprButtonPlay = LoadUISprite("UI_ButtonPlay", new Vector4(5, 5, 5, 5), 25);
        sprBarBg      = LoadUISprite("UI_BarBg", new Vector4(3, 3, 3, 3), 25);
        sprBarFill    = LoadUISprite("UI_BarFill", Vector4.zero, 25);
        sprCell       = LoadUISprite("UI_Cell", Vector4.zero, 25);
        sprPlayIcon   = LoadUISprite("UI_PlayIcon", Vector4.zero, 25);
        sprTitleBg    = LoadUISprite("Background_Title", Vector4.zero, 100);

        retroFont = FindRetroFont();
        if (retroFont == null)
        {
            EditorUtility.DisplayDialog("Fuente no encontrada",
                "No encontré la fuente \"Electronic Highway Sign\" en tu proyecto.\n\n" +
                "Se usará la fuente por defecto. Para ponerla:\n" +
                "1) Window > TextMeshPro > Font Asset Creator\n" +
                "2) Elige \"Electronic Highway Sign\", pulsa Generate y luego Save as...\n" +
                "3) Guárdala DENTRO de la carpeta Assets de tu proyecto\n" +
                "4) Vuelve a ejecutar esta herramienta.", "Entendido");
        }

        // ---- 2b) Pantalla inicial: fondo + botón en el centro ----
        // Se crea ANTES que el minijuego, así queda debajo: al pulsar el botón el minijuego
        // la tapa por completo, y cuando el minijuego se cierra vuelve a aparecer.
        GameObject startGO = new GameObject("StartScreen", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(startGO, "Crear pantalla inicial");
        startGO.transform.SetParent(canvas.transform, false);
        Stretch((RectTransform)startGO.transform);
        Skin(startGO.GetComponent<Image>(), sprTitleBg, Color.white, false, new Color(0.05f, 0.04f, 0.18f, 1f));

        TextMeshProUGUI gameTitle = MakeText(startGO.transform, "GameTitle", "GUATECOSMOS", 130,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(1500, 160), TextAlignmentOptions.Center);
        gameTitle.color = new Color(0.95f, 0.97f, 1f, 1f);
        TextMeshProUGUI gameSub = MakeText(startGO.transform, "GameSubtitle", "BASE LUNAR", 58,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -215), new Vector2(900, 72), TextAlignmentOptions.Center);
        gameSub.color = CyanNeon;

        Button startBtn = MakeButton(startGO.transform, "StartButton", "PROBAR MINIJUEGO", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(660, 140), sprButtonPlay, 46, Color.white);
        MakeIcon(startBtn.transform, "PlayIcon", sprPlayIcon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(46, 0), new Vector2(56, 64));
        RectTransform startLabel = (RectTransform)startBtn.transform.Find("Label");
        startLabel.offsetMin = new Vector2(115, 6);   // deja libre el icono de la izquierda
        startLabel.offsetMax = new Vector2(-20, -6);

        // ---- 2) Prefab de la pieza ----
        PipeTile tilePrefab = BuildTilePrefab(spStraight);

        // ---- 3) Panel principal con el fondo de la base lunar ----
        GameObject panelGO = new GameObject(PanelName, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panelGO, "Crear panel minijuego");
        panelGO.transform.SetParent(canvas.transform, false);
        Stretch((RectTransform)panelGO.transform);
        Skin(panelGO.GetComponent<Image>(), sprBackground, Color.white, false, new Color(0.05f, 0.04f, 0.18f, 1f));
        OxygenPipeMiniGame game = panelGO.AddComponent<OxygenPipeMiniGame>();
        Transform panel = panelGO.transform;

        // =========================  PANTALLA DE JUEGO  =========================
        GameObject gameplayGO = NewStretch("GameplayRoot", panel);
        Transform gp = gameplayGO.transform;

        // HUD izquierda: nivel y ronda
        RectTransform hudL = MakePanel(gp, "HudLeft", new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -24), new Vector2(420, 112));
        TextMeshProUGUI levelText = MakeText(hudL, "LevelText", "NIVEL 1", 50,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(380, 56), TextAlignmentOptions.Center);
        levelText.color = CyanNeon;
        TextMeshProUGUI roundText = MakeText(hudL, "RoundText", "RONDA 1/3", 30,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -66), new Vector2(380, 36), TextAlignmentOptions.Center);
        roundText.color = SoftText;

        // HUD centro: barra de oxígeno recuperado
        RectTransform hudC = MakePanel(gp, "HudCenter", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(640, 112));
        TextMeshProUGUI barLabel = MakeText(hudC, "BarLabel", "OXIGENO RECUPERADO", 28,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(560, 36), TextAlignmentOptions.Center);
        barLabel.color = SoftText;
        Slider bar = MakeSlider(hudC, new Vector2(0, -60), new Vector2(540, 34));

        // HUD derecha: tiempo y movimientos
        RectTransform hudR = MakePanel(gp, "HudRight", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -24), new Vector2(420, 112));
        TextMeshProUGUI timerText = MakeText(hudR, "TimerText", "00:45", 54,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(380, 60), TextAlignmentOptions.Center);
        timerText.color = OrangeNeon;
        TextMeshProUGUI movesText = MakeText(hudR, "MovesText", "MOV 0", 30,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -68), new Vector2(380, 36), TextAlignmentOptions.Center);
        movesText.color = SoftText;

        // Marco de la cuadrícula (decoración) y cuadrícula
        RectTransform frameRT = MakePanel(gp, "GridFrame", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -12), new Vector2(724, 724));

        GameObject gridGO = new GameObject("PipeGrid", typeof(RectTransform), typeof(Image), typeof(GridLayoutGroup));
        gridGO.transform.SetParent(gp, false);
        RectTransform gridRT = (RectTransform)gridGO.transform;
        gridRT.anchorMin = gridRT.anchorMax = new Vector2(0.5f, 0.5f);
        gridRT.pivot = new Vector2(0.5f, 0.5f);
        gridRT.anchoredPosition = new Vector2(0, -12);
        gridRT.sizeDelta = new Vector2(660, 660);
        gridGO.GetComponent<Image>().color = GridColor;
        GridLayoutGroup grid = gridGO.GetComponent<GridLayoutGroup>();
        grid.spacing = Vector2.zero;                       // Casillas PEGADAS: las tuberías se ven unidas
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.cellSize = new Vector2(100, 100);             // El script lo recalcula al empezar

        TextMeshProUGUI statusText = MakeText(gp, "StatusText", "Conecta el INICIO (O2) con el FIN para reparar la fuga", 32,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(1500, 60), TextAlignmentOptions.Center);
        statusText.color = SoftText;

        // =========================  PANTALLA DE INICIO  =========================
        GameObject introGO = NewStretch("IntroPanel", panel);
        RectTransform dlg = MakePanel(introGO.transform, "IntroDialog", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 640));

        TextMeshProUGUI introTitle = MakeText(dlg, "IntroTitle", "REPARAR SOPORTE VITAL", 62,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(1000, 80), TextAlignmentOptions.Center);
        introTitle.color = OrangeNeon;
        TextMeshProUGUI introInfo = MakeText(dlg, "IntroInfoText", "DIFICULTAD: NIVEL 1 - APRENDIZ", 34,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(1000, 46), TextAlignmentOptions.Center);
        introInfo.color = CyanNeon;

        // Botón INICIAR: en el CENTRO exacto de la pantalla
        Button playBtn = MakeButton(dlg, "PlayButton", "INICIAR", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(460, 130), sprButtonPlay, 54, Color.white);
        MakeIcon(playBtn.transform, "PlayIcon", sprPlayIcon, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(46, 0), new Vector2(56, 64));

        TextMeshProUGUI introDesc = MakeText(dlg, "IntroDesc",
            "UN MICROMETEORITO ROMPIO UNA TUBERIA DE OXIGENO. GIRA LAS TUBERIAS PARA CONECTAR EL TANQUE O2 CON EL MODULO FIN. LAS PIEZAS O2 Y FIN NO SE MUEVEN.", 28,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(940, 130), TextAlignmentOptions.Center);
        introDesc.color = SoftText;

        Button backBtn = MakeButton(dlg, "BackButton", "VOLVER", new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 28), new Vector2(260, 64), sprButton, 30, Color.white);

        // =========================  PANEL DE RESULTADO  =========================
        RectTransform resultRT = MakePanel(panel, "ResultPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 600));
        GameObject resultGO = resultRT.gameObject;

        TextMeshProUGUI resultTitle = MakeText(resultRT, "ResultTitleText", "SISTEMA REPARADO!", 58,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(900, 80), TextAlignmentOptions.Center);
        resultTitle.color = OrangeNeon;
        TextMeshProUGUI resultBody = MakeText(resultRT, "ResultBodyText", "Recuperaste 36% de Comida y Oxigeno.", 36,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -135), new Vector2(900, 60), TextAlignmentOptions.Center);
        TextMeshProUGUI factText = MakeText(resultRT, "FactText", "Dato curioso de la NASA", 28,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -215), new Vector2(880, 190), TextAlignmentOptions.Center);
        factText.color = SoftText;

        Button continueBtn = MakeButton(resultRT, "ContinueButton", "CONTINUAR", new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 40), new Vector2(380, 90), sprButtonPlay, 40, Color.white);

        // ---- Conectar los botones ----
        UnityEventTools.AddVoidPersistentListener(playBtn.onClick, game.BeginGame);
        UnityEventTools.AddVoidPersistentListener(backBtn.onClick, game.CancelIntro);
        UnityEventTools.AddVoidPersistentListener(continueBtn.onClick, game.OnContinueButton);

        // ---- 4) Conectar TODOS los campos del script ----
        SerializedObject so = new SerializedObject(game);
        SetRef(so, "gridParent", gridRT);
        SetRef(so, "gridLayout", grid);
        SetRef(so, "tilePrefab", tilePrefab);
        SetRef(so, "endSprite", spEnd);
        SetRef(so, "straightSprite", spStraight);
        SetRef(so, "cornerSprite", spCorner);
        SetRef(so, "tSprite", spT);
        SetRef(so, "crossSprite", spCross);
        SetRef(so, "introPanel", introGO);
        SetRef(so, "gameplayRoot", gameplayGO);
        SetRef(so, "resultPanel", resultGO);
        SetRef(so, "levelText", levelText);
        SetRef(so, "roundText", roundText);
        SetRef(so, "timerText", timerText);
        SetRef(so, "movesText", movesText);
        SetRef(so, "statusText", statusText);
        SetRef(so, "oxygenGainedBar", bar);
        SetRef(so, "introInfoText", introInfo);
        SetRef(so, "resultTitleText", resultTitle);
        SetRef(so, "resultBodyText", resultBody);
        SetRef(so, "factText", factText);
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- 5) El botón central de la pantalla inicial abre el minijuego ----
        UnityEventTools.AddVoidPersistentListener(startBtn.onClick, game.StartGame);

        // Estado inicial: el panel empieza oculto; al abrirlo muestra la pantalla de inicio
        introGO.SetActive(true);
        gameplayGO.SetActive(false);
        resultGO.SetActive(false);
        panelGO.SetActive(false);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = panelGO;

        EditorUtility.DisplayDialog("Minijuego de Oxígeno",
            "¡Listo! Interfaz construida.\n\nPulsa PLAY y haz clic en el botón del centro " +
            "de la pantalla inicial.", "Entendido");
    }

    // ------------------------------------------------------------------
    //  PREFAB DE LA PIEZA
    // ------------------------------------------------------------------
    private static PipeTile BuildTilePrefab(Sprite previewSprite)
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // Raíz: suelo de la casilla (sin bordes, así las casillas se ven continuas) + Button sin transición
        GameObject root = new GameObject("PipeTilePrefab", typeof(RectTransform), typeof(Image));
        ((RectTransform)root.transform).sizeDelta = new Vector2(100, 100);
        Image rootImg = root.GetComponent<Image>();
        Skin(rootImg, sprCell, Color.white, false, new Color(0.06f, 0.05f, 0.2f, 1f));
        rootImg.raycastTarget = true;
        Button btn = root.AddComponent<Button>();
        btn.targetGraphic = rootImg;
        btn.transition = Selectable.Transition.None;
        PipeTile tile = root.AddComponent<PipeTile>();

        // Hijo 1: la imagen de la tubería (llena toda la casilla)
        GameObject pipeGO = new GameObject("PipeImage", typeof(RectTransform), typeof(Image));
        pipeGO.transform.SetParent(root.transform, false);
        Stretch((RectTransform)pipeGO.transform);
        Image pipeImg = pipeGO.GetComponent<Image>();
        pipeImg.sprite = previewSprite;
        pipeImg.raycastTarget = false;

        // Hijos 2 y 3: iconos de INICIO (O2) y FIN (apagados por defecto)
        GameObject startMarker = MakeMarker(root.transform, "SourceMarker", "O2", new Color(0.2f, 0.7f, 1f, 1f));
        GameObject endMarker = MakeMarker(root.transform, "EndMarker", "FIN", new Color(1f, 0.65f, 0.2f, 1f));

        // Conectar campos y colores del script PipeTile
        SerializedObject so = new SerializedObject(tile);
        SetRef(so, "pipeImage", pipeImg);
        SetRef(so, "sourceMarker", startMarker);
        SetRef(so, "endMarker", endMarker);
        SerializedProperty empty = so.FindProperty("emptyColor");
        SerializedProperty filled = so.FindProperty("filledColor");
        if (empty != null) empty.colorValue = new Color(0.62f, 0.66f, 0.86f, 1f);   // Sin oxígeno: gris azulado
        if (filled != null) filled.colorValue = new Color(0.25f, 0.95f, 1f, 1f);    // Con oxígeno: celeste
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<PipeTile>();
    }

    /// <summary>Icono circular con texto (INICIO "O2" o FIN). Se escala con la casilla. Empieza apagado.</summary>
    private static GameObject MakeMarker(Transform parent, string name, string label, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.3f, 0.3f);
        rt.anchorMax = new Vector2(0.7f, 0.7f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        img.color = color;
        img.raycastTarget = false;

        TextMeshProUGUI t = MakeText(go.transform, "Label", label, 36,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64), TextAlignmentOptions.Center);
        t.color = new Color(0.05f, 0.08f, 0.14f, 1f);
        t.fontStyle = FontStyles.Bold;
        Stretch((RectTransform)t.transform);

        go.SetActive(false);
        return go;
    }

    // ------------------------------------------------------------------
    //  FUNCIONES AUXILIARES DE INTERFAZ
    // ------------------------------------------------------------------

    private static Canvas GetOrCreateCanvas()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Crear Canvas");
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (Object.FindFirstObjectByType<EventSystem>() == null)
            Debug.LogWarning("No hay EventSystem en la escena. Créalo con clic derecho en la Jerarquía > UI > Event System.");

        return canvas;
    }

    /// <summary>Pone un sprite a una Image. Si el sprite no existe, usa un color liso de respaldo.</summary>
    private static void Skin(Image img, Sprite sprite, Color tint, bool sliced, Color fallback)
    {
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.color = tint;
        }
        else
        {
            img.color = fallback;
        }
    }

    /// <summary>
    /// Carga un sprite pixel-art: lo convierte a Sprite, desactiva el suavizado (Point)
    /// para que los píxeles se vean nítidos y define el borde para estirarlo (9-slice).
    /// </summary>
    private static Sprite LoadUISprite(string name, Vector4 border, float pixelsPerUnit)
    {
        string path = $"Assets/Sprites/{name}.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"No encontré {path}. Se usará un color liso. Copia el PNG a Assets/Sprites y vuelve a ejecutar.");
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spriteBorder = border;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>Sprite de tubería (si falta el PNG, usa un cuadrado de relleno).</summary>
    private static Sprite LoadPipe(string name)
    {
        Sprite s = LoadUISprite(name, Vector4.zero, 100);
        return s != null ? s : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    /// <summary>Busca la fuente "Electronic Highway Sign" (o cualquier fuente pixelada) en el proyecto.</summary>
    private static TMP_FontAsset FindRetroFont()
    {
        string[] keys = { "electronic", "highway", "pressstart", "press start", "pixel", "retro", "silkscreen", "vt323", "8bit" };
        foreach (string key in keys)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path).ToLower();
                if (!file.Contains(key)) continue;

                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                // Si a la fuente le faltan letras (tildes, ¿, ¡), usa la fuente por defecto para esas
                if (font != null && TMP_Settings.defaultFontAsset != null &&
                    font != TMP_Settings.defaultFontAsset &&
                    !font.fallbackFontAssetTable.Contains(TMP_Settings.defaultFontAsset))
                {
                    font.fallbackFontAssetTable.Add(TMP_Settings.defaultFontAsset);
                    EditorUtility.SetDirty(font);
                }
                return font;
            }
        }
        return null;
    }

    private static GameObject NewStretch(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform);
        return go;
    }

    /// <summary>Panel con marco retro (borde cian, fondo azul marino) como los del lobby.</summary>
    private static RectTransform MakePanel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        Skin(img, sprPanel, Color.white, true, new Color(0.05f, 0.07f, 0.2f, 1f));
        img.raycastTarget = false;
        return rt;
    }

    private static Slider MakeSlider(Transform parent, Vector2 pos, Vector2 size)
    {
        DefaultControls.Resources res = new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
        };
        GameObject go = DefaultControls.CreateSlider(res);
        go.name = "OxygenGainedBar";
        go.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Slider s = go.GetComponent<Slider>();
        s.interactable = false;
        s.transition = Selectable.Transition.None;
        s.minValue = 0;
        s.maxValue = 1;
        s.value = 0;

        // Es solo una barra de progreso: quitamos la "bolita" (handle)
        Transform handleArea = go.transform.Find("Handle Slide Area");
        if (handleArea != null) Object.DestroyImmediate(handleArea.gameObject);
        s.handleRect = null;

        RectTransform bg = (RectTransform)go.transform.Find("Background");
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
        bg.offsetMin = Vector2.zero; bg.offsetMax = Vector2.zero;
        Skin(bg.GetComponent<Image>(), sprBarBg, Color.white, true, new Color(0.03f, 0.03f, 0.14f, 1f));

        RectTransform fillArea = (RectTransform)go.transform.Find("Fill Area");
        fillArea.anchorMin = Vector2.zero; fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(6, 6); fillArea.offsetMax = new Vector2(-6, -6);
        s.fillRect.offsetMin = Vector2.zero; s.fillRect.offsetMax = Vector2.zero;
        Image fill = s.fillRect.GetComponent<Image>();
        if (sprBarFill != null) { fill.sprite = sprBarFill; fill.type = Image.Type.Tiled; fill.color = Color.white; }
        else fill.color = new Color(0.25f, 0.9f, 0.5f, 1f);
        return s;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size,
        Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 boxSize, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = boxSize;

        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        // Si el texto no cabe en su caja, se hace más pequeño solo (sirve con cualquier fuente)
        t.enableAutoSizing = true;
        t.fontSizeMax = size;
        t.fontSizeMin = size * 0.45f;
        if (retroFont != null) t.font = retroFont;
        return t;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pivot,
        Vector2 pos, Vector2 size, Sprite sprite, float fontSize, Color tint)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        Skin(img, sprite, tint, true, new Color(0.1f, 0.3f, 0.5f, 1f));
        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;

        TextMeshProUGUI t = MakeText(go.transform, "Label", label, fontSize,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size, TextAlignmentOptions.Center);
        Stretch((RectTransform)t.transform);
        t.rectTransform.offsetMin = new Vector2(20, 6);
        t.rectTransform.offsetMax = new Vector2(-20, -6);
        return b;
    }

    private static void MakeIcon(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        if (sprite == null) return;
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
    }

    /// <summary>Hace que un RectTransform llene por completo a su padre.</summary>
    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Asigna un objeto a un campo [SerializeField] por su nombre.</summary>
    private static void SetRef(SerializedObject so, string field, Object value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"No encontré el campo '{field}' en {so.targetObject.GetType().Name}."); return; }
        p.objectReferenceValue = value;
    }
}
#endif
