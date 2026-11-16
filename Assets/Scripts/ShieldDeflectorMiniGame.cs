using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// ShieldDeflectorMiniGame = Minijuego del Escudo Magnético.
///
/// HISTORIA PARA LOS NIÑOS:
///   El Sol lanzó una tormenta solar hacia la base lunar. Como la Luna no tiene un campo
///   magnético global como el de la Tierra, el astronauta debe usar un DEFLECTOR magnético
///   para atrapar la energía buena y desviar las llamaradas peligrosas.
///
/// CÓMO SE JUEGA:
///   - Al abrir el minijuego aparece una pantalla con un botón INICIAR en el centro.
///     Al pulsarlo, la pantalla desaparece y empieza el juego.
///   - El jugador ARRASTRA el deflector (mouse o dedo) a izquierda y derecha, en la parte de abajo.
///   - Partículas VERDES (energía del escudo) -> SUMAN puntos.
///   - Partículas ROJAS (llamaradas solares) -> RESTAN puntos y el deflector parpadea en rojo.
///   - Cada ronda es MÁS DIFÍCIL que la anterior (tabla "Difficulty Steps"):
///     caen más rápido, más seguido y con más llamaradas.
///
/// CIENCIA REAL:
///   El campo magnético de la Tierra (magnetosfera) desvía la mayoría de las partículas
///   cargadas del viento solar. Las que se escapan siguen las líneas del campo y crean auroras.
///
/// CÓMO SE CONECTA AL RESTO DEL JUEGO (igual que OxygenPipeMiniGame):
///   - Botón del Dashboard  -> StartGame()           (abre la pantalla de inicio)
///   - Evento onGameStarted -> resta Energía del Astronauta (se dispara al pulsar INICIAR)
///   - Evento onGameFinished(float) -> SUMA ese total de puntos de escudo
///   - SetDifficultyLevel(n) -> el Dashboard puede subir la dificultad según avance el juego
///
/// NOTA TÉCNICA: no usa Rigidbody ni colliders. Todo se mueve por código y la captura
/// se detecta con una revisión simple de rectángulos (AABB) en el panel de juego.
/// Usa Input clásico (Input.mousePosition); en móvil el toque se simula como mouse.
/// </summary>
public class ShieldDeflectorMiniGame : MonoBehaviour
{
    // ------------------------------------------------------------------
    //  DifficultyStep = UN NIVEL DE DIFICULTAD del minijuego.
    //  (Anidada dentro de esta clase para no chocar con la DifficultyStep de OxygenPipeMiniGame.)
    //  Puedes editar la tabla completa en el Inspector (campo "Difficulty Steps").
    // ------------------------------------------------------------------
    [System.Serializable]
    public class DifficultyStep
    {
        public string name = "apprentice";             // Nombre que ve el jugador
        public float roundTime = 25f;                  // Segundos que dura la ronda
        [Tooltip("Cada cuántos segundos aparece una partícula nueva (menor = más difícil).")]
        public float spawnInterval = 0.9f;
        [Tooltip("Velocidad de caída en ALTURAS DE PANTALLA por segundo (0.3 = tarda ~3 s en bajar).")]
        public float fallSpeed = 0.30f;
        [Range(0f, 0.8f)]
        [Tooltip("Probabilidad de que una partícula sea llamarada roja.")]
        public float flareChance = 0.25f;
        public float energyPoints = 5f;                // Puntos que da una partícula verde
        public float flarePenalty = 3f;                // Puntos que quita una llamarada roja

        public DifficultyStep() { }
        public DifficultyStep(string name, float time, float spawn, float speed, float flare, float points, float penalty)
        {
            this.name = name; roundTime = time; spawnInterval = spawn;
            fallSpeed = speed; flareChance = flare; energyPoints = points; flarePenalty = penalty;
        }
    }

    /// <summary>Una partícula que está cayendo en este momento.</summary>
    private class FallingParticle
    {
        public RectTransform rect;
        public Image image;
        public bool isFlare;
    }

    // ------------------------------------------------------------------
    //  CONFIGURACIÓN (todo editable en el Inspector)
    // ------------------------------------------------------------------
    [Header("Zona de juego")]
    [SerializeField] private RectTransform playArea;       // Panel donde caen las partículas (hijo de gameplayRoot)
    [SerializeField] private RectTransform deflector;      // El deflector (hijo de playArea)
    [SerializeField] private Image deflectorImage;         // Imagen del deflector (para parpadear en rojo)
    [SerializeField] private Image particlePrefab;         // Prefab de partícula: un Image simple (hijo de nada)
    [SerializeField] private Canvas canvas;                // Canvas que contiene el minijuego

    [Header("Sprites de partículas (opcionales: si están vacíos se usa solo el color)")]
    [SerializeField] private Sprite shieldEnergySprite;    // Energía buena (verde)
    [SerializeField] private Sprite solarFlareSprite;      // Llamarada solar (roja)
    [SerializeField] private Color energyColor = new Color(0.2f, 1f, 0.4f);
    [SerializeField] private Color flareColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private float particleSize = 60f;     // Tamaño de cada partícula (píxeles)
    [SerializeField] private float deflectorBottomMargin = 30f; // Separación del deflector con el borde de abajo

    [Header("Dificultad progresiva")]
    [SerializeField] private int roundsToWin = 3;          // Cuántas rondas dura una partida (pon 1 para un solo temporizador)
    [Min(1)]
    [SerializeField] private int startLevel = 1;           // Nivel con el que empieza (1 = primero de la tabla)
    [SerializeField] private float targetPoints = 60f;     // Puntos para llenar la barra al 100%
    [SerializeField] private DifficultyStep[] difficultySteps = DefaultSteps();

    [Header("Pantallas")]
    [SerializeField] private GameObject introPanel;        // Pantalla de inicio (botón INICIAR en el centro)
    [SerializeField] private GameObject gameplayRoot;      // Todo lo que se ve mientras se juega (HUD + zona de juego)
    [SerializeField] private GameObject resultPanel;       // Panel de resultado final

    [Header("Interfaz (HUD)")]
    [SerializeField] private TMP_Text levelText;           // "NIVEL 2"
    [SerializeField] private TMP_Text roundText;           // "RONDA 1/3"
    [SerializeField] private TMP_Text timerText;           // "00:19"
    [SerializeField] private TMP_Text scoreText;           // "SHIELD 25"
    [SerializeField] private TMP_Text statusText;          // Mensajes (ej. llamarada)
    [SerializeField] private Slider shieldGainedBar;       // Barra del escudo cargado
    [SerializeField] private TMP_Text introInfoText;       // "DIFICULTAD: NIVEL 1 - APRENDIZ"
    [SerializeField] private TMP_Text resultTitleText;     // "Shield charged!"
    [SerializeField] private TMP_Text resultBodyText;      // Cuántos puntos ganaste
    [SerializeField] private TMP_Text factText;            // Dato curioso de la NASA

    [Header("Eventos (conecta con tu Dashboard)")]
    public UnityEvent onGameStarted;           // Aquí resta Energía del Astronauta
    public UnityEvent<float> onGameFinished;   // Recibe los puntos de escudo ganados

    // ------------------------------------------------------------------
    //  DATOS INTERNOS
    // ------------------------------------------------------------------
    private const float FlashDuration = 0.35f;   // Segundos que dura el parpadeo del deflector

    private readonly List<FallingParticle> particles = new List<FallingParticle>();
    private DifficultyStep currentStep;   // Dificultad de la ronda actual
    private int currentRound;
    private float rewardTotal;            // Puntos de escudo ganados en esta partida
    private float timeLeft;
    private float spawnTimer;
    private float flashTimer;
    private Color flashColor;
    private Color deflectorBaseColor = Color.white;
    private bool baseColorSaved;
    private bool playing;

    // Datos curiosos reales (simples para niños)
    private readonly string[] facts =
    {
        "Did you know - Earth's magnetic field, called the magnetosphere, bends away most of the charged particles that the Sun sends our way.",

        "Did you know - A solar flare is a sudden burst of energy from the Sun. Its light reaches Earth in about 8 minutes!",

        "Did you know - Auroras happen when solar particles follow Earth's magnetic field and crash into the air near the poles.",

        "Did you know - The Moon has no global magnetic field like Earth's, so future moon bases need special shielding to protect astronauts.",

        "Did you know - NASA's Parker Solar Probe has flown through the Sun's outer atmosphere, the corona, to study the solar wind.",

        "Did you know - The Sun goes through an activity cycle of about 11 years. Near its peak, there are many more solar flares.",

        "Did you know - On Artemis I, NASA flew mannequins with radiation sensors to test how to keep astronauts safe in deep space."
    };

    /// <summary>Tabla de dificultad por defecto (se puede editar en el Inspector).</summary>
    private static DifficultyStep[] DefaultSteps()
    {
        return new[]
        {
            //                    nombre       tiempo  aparece cada  velocidad  %rojas  premio  castigo
            new DifficultyStep("Aprendiz",     25f,    0.90f,        0.30f,     0.25f,  5f,     3f),
            new DifficultyStep("Tecnico",      25f,    0.80f,        0.35f,     0.30f,  5f,     3f),
            new DifficultyStep("Ingeniero",    25f,    0.70f,        0.42f,     0.35f,  6f,     4f),
            new DifficultyStep("Especialista", 25f,    0.60f,        0.50f,     0.40f,  6f,     4f),
            new DifficultyStep("Comandante",   25f,    0.50f,        0.60f,     0.45f,  7f,     5f),
            new DifficultyStep("Leyenda",      25f,    0.40f,        0.70f,     0.50f,  8f,     5f),
        };
    }

    // ------------------------------------------------------------------
    //  API PÚBLICA
    // ------------------------------------------------------------------

    /// <summary>
    /// ABRE el minijuego mostrando la pantalla de inicio (botón INICIAR al centro).
    /// Conéctalo al botón del Dashboard que abre este minijuego.
    /// </summary>
    public void StartGame()
    {
        CancelInvoke();
        playing = false;
        gameObject.SetActive(true);

        introPanel.SetActive(true);
        gameplayRoot.SetActive(false);
        resultPanel.SetActive(false);

        DifficultyStep first = StepForLevel(startLevel);
        if (introInfoText != null)
            introInfoText.text = $"DIFICULTAD: NIVEL {startLevel} - {first.name.ToUpper()}";
    }

    /// <summary>Botón INICIAR: la pantalla de inicio desaparece y empieza la partida.</summary>
    public void BeginGame()
    {
        introPanel.SetActive(false);
        gameplayRoot.SetActive(true);
        resultPanel.SetActive(false);

        currentRound = 0;
        rewardTotal = 0f;
        SaveDeflectorColor();
        UpdateShieldBar();
        UpdateScoreText();

        onGameStarted?.Invoke();   // <- Aquí tu Dashboard descuenta Energía del Astronauta
        StartNextRound();
    }

    /// <summary>Botón VOLVER de la pantalla de inicio: cierra sin gastar energía.</summary>
    public void CancelIntro()
    {
        gameObject.SetActive(false);
    }

    /// <summary>Botón "Continuar" del panel de resultado: entrega la recompensa y cierra.</summary>
    public void OnContinueButton()
    {
        onGameFinished?.Invoke(rewardTotal);  // <- Tu Dashboard suma estos puntos
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Cambia el nivel inicial (1 = más fácil). Llámalo desde tu Dashboard para que el
    /// minijuego sea más difícil a medida que avanza la partida principal.
    /// </summary>
    public void SetDifficultyLevel(int level)
    {
        startLevel = Mathf.Max(1, level);
    }

    private void OnDisable()
    {
        CancelInvoke(); // Evita que una ronda "fantasma" empiece después de cerrar
        ClearParticles();
    }

    // ------------------------------------------------------------------
    //  FLUJO DE RONDAS
    // ------------------------------------------------------------------

    /// <summary>Devuelve la dificultad de un nivel (si te pasas del último, repite el más difícil).</summary>
    private DifficultyStep StepForLevel(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, difficultySteps.Length - 1);
        return difficultySteps[index];
    }

    private void StartNextRound()
    {
        currentRound++;
        int level = startLevel + currentRound - 1;     // Cada ronda sube un nivel
        currentStep = StepForLevel(level);

        timeLeft = currentStep.roundTime;
        spawnTimer = 0.5f;                             // Primera partícula casi de inmediato

        levelText.text = $"NIVEL {level}";
        roundText.text = $"RONDA {currentRound}/{roundsToWin}";
        statusText.text = "Drag the deflector: catch GREEN energy and dodge RED solar flares!";
        UpdateTimerText();

        ClearParticles();
        PlaceDeflector();
        playing = true;
    }

    private void Update()
    {
        if (!playing) return;

        // --- Cuenta regresiva ---
        timeLeft -= Time.deltaTime;
        UpdateTimerText();
        if (timeLeft <= 0f)
        {
            RoundFinished(); // Se acabó el tiempo de la ronda
            return;
        }

        // --- Mover el deflector con mouse / dedo ---
        HandleDrag();

        // --- Aparición de partículas a intervalos regulares ---
        spawnTimer -= Time.deltaTime;
        while (spawnTimer <= 0f)
        {
            spawnTimer += currentStep.spawnInterval;
            SpawnParticle();
        }

        // --- Caída y captura ---
        UpdateParticles();

        // --- Parpadeo del deflector ---
        UpdateFlash();
    }

    private void UpdateTimerText()
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
        timerText.text = $"{total / 60:00}:{total % 60:00}";
    }

    /// <summary>Termina el tiempo de una ronda: pasa a la siguiente o cierra la partida.</summary>
    private void RoundFinished()
    {
        playing = false;
        ClearParticles();

        if (currentRound >= roundsToWin)
        {
            EndGame();
        }
        else
        {
            statusText.text = "Round complete - The solar storm is getting stronger...";
            Invoke(nameof(StartNextRound), 1.8f); // Pequeña pausa antes de la siguiente ronda
        }
    }

    private void EndGame()
    {
        playing = false;
        CancelInvoke();
        ClearParticles();
        ResetDeflectorColor();

        bool won = rewardTotal > 0f;
        resultPanel.SetActive(true);
        resultTitleText.text = won ? "Shield charged!" : "Shield too weak...";
        resultBodyText.text = $"Gained {rewardTotal:0} shield points.";
        factText.text = facts[Random.Range(0, facts.Length)];
    }

    // ------------------------------------------------------------------
    //  DEFLECTOR (arrastrar)
    // ------------------------------------------------------------------

    /// <summary>Pone el deflector centrado abajo (usa el centro del panel como referencia).</summary>
    private void PlaceDeflector()
    {
        // Anclas y pivote al centro, para que todo se calcule en el mismo sistema de coordenadas.
        deflector.anchorMin = deflector.anchorMax = new Vector2(0.5f, 0.5f);
        deflector.pivot = new Vector2(0.5f, 0.5f);

        float y = -playArea.rect.height * 0.5f + deflector.rect.height * 0.5f + deflectorBottomMargin;
        deflector.anchoredPosition = new Vector2(0f, y);
    }

    /// <summary>Mientras el jugador mantiene pulsado (mouse o dedo), el deflector sigue su posición horizontal.</summary>
    private void HandleDrag()
    {
        if (!Input.GetMouseButton(0)) return;   // En móvil el toque cuenta como clic izquierdo

        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(playArea, Input.mousePosition, cam, out Vector2 local))
        {
            float limit = playArea.rect.width * 0.5f - deflector.rect.width * 0.5f;
            Vector2 pos = deflector.anchoredPosition;
            pos.x = Mathf.Clamp(local.x, -limit, limit);
            deflector.anchoredPosition = pos;
        }
    }

    // ------------------------------------------------------------------
    //  PARTÍCULAS
    // ------------------------------------------------------------------

    private void SpawnParticle()
    {
        bool isFlare = Random.value < currentStep.flareChance;

        Image img = Instantiate(particlePrefab, playArea);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(particleSize, particleSize);

        float limit = playArea.rect.width * 0.5f - particleSize * 0.5f;
        float startY = playArea.rect.height * 0.5f + particleSize;   // Justo encima del borde de arriba
        rt.anchoredPosition = new Vector2(Random.Range(-limit, limit), startY);

        Sprite sprite = isFlare ? solarFlareSprite : shieldEnergySprite;
        if (sprite != null) img.sprite = sprite;
        img.color = isFlare ? flareColor : energyColor;
        img.raycastTarget = false;   // No bloquea los toques del jugador

        particles.Add(new FallingParticle { rect = rt, image = img, isFlare = isFlare });
    }

    /// <summary>Mueve cada partícula hacia abajo y revisa si el deflector la atrapó.</summary>
    private void UpdateParticles()
    {
        float speedPx = currentStep.fallSpeed * playArea.rect.height;
        float bottom = -playArea.rect.height * 0.5f - particleSize;

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            FallingParticle p = particles[i];
            if (p.rect == null) { particles.RemoveAt(i); continue; }

            Vector2 pos = p.rect.anchoredPosition;
            pos.y -= speedPx * Time.deltaTime;
            p.rect.anchoredPosition = pos;

            if (Overlaps(p.rect, deflector))
            {
                OnParticleCaught(p);
                Destroy(p.rect.gameObject);
                particles.RemoveAt(i);
            }
            else if (pos.y < bottom)
            {
                // Se salió por abajo: no pasa nada (perder energía verde no resta puntos)
                Destroy(p.rect.gameObject);
                particles.RemoveAt(i);
            }
        }
    }

    /// <summary>Revisión simple de rectángulos (AABB). Ambos usan ancla/pivote al centro del panel.</summary>
    private static bool Overlaps(RectTransform a, RectTransform b)
    {
        Vector2 aPos = a.anchoredPosition, bPos = b.anchoredPosition;
        Vector2 aHalf = a.rect.size * 0.5f, bHalf = b.rect.size * 0.5f;
        return Mathf.Abs(aPos.x - bPos.x) < aHalf.x + bHalf.x &&
               Mathf.Abs(aPos.y - bPos.y) < aHalf.y + bHalf.y;
    }

    private void OnParticleCaught(FallingParticle p)
    {
        if (p.isFlare)
        {
            rewardTotal = Mathf.Max(0f, rewardTotal - currentStep.flarePenalty);
            statusText.text = "Solar flare! The shield took damage.";
            StartFlash(flareColor);
        }
        else
        {
            rewardTotal += currentStep.energyPoints;
            statusText.text = "Shield energy collected!";
            StartFlash(energyColor);
        }
        UpdateScoreText();
        UpdateShieldBar();
    }

    private void ClearParticles()
    {
        foreach (var p in particles)
            if (p != null && p.rect != null) Destroy(p.rect.gameObject);
        particles.Clear();
    }

    // ------------------------------------------------------------------
    //  EFECTOS Y HUD
    // ------------------------------------------------------------------

    private void SaveDeflectorColor()
    {
        if (baseColorSaved || deflectorImage == null) return;
        deflectorBaseColor = deflectorImage.color;
        baseColorSaved = true;
    }

    private void StartFlash(Color color)
    {
        flashColor = color;
        flashTimer = FlashDuration;
    }

    private void UpdateFlash()
    {
        if (deflectorImage == null || flashTimer <= 0f) return;
        flashTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(flashTimer / FlashDuration);
        deflectorImage.color = Color.Lerp(deflectorBaseColor, flashColor, t);
    }

    private void ResetDeflectorColor()
    {
        flashTimer = 0f;
        if (deflectorImage != null) deflectorImage.color = deflectorBaseColor;
    }

    private void UpdateScoreText()
    {
        if (scoreText != null) scoreText.text = $"SHIELD {rewardTotal:0}";
    }

    /// <summary>La barra se llena según los puntos ganados (100% = "Target Points").</summary>
    private void UpdateShieldBar()
    {
        if (shieldGainedBar == null) return;
        shieldGainedBar.maxValue = Mathf.Max(1f, targetPoints);
        shieldGainedBar.value = Mathf.Min(rewardTotal, shieldGainedBar.maxValue);
    }
}
