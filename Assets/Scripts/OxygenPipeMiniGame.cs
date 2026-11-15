using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// DifficultyStep = UN NIVEL DE DIFICULTAD del minijuego.
/// Puedes editar la tabla completa en el Inspector (campo "Difficulty Steps").
/// </summary>
[System.Serializable]
public class DifficultyStep
{
    public string name = "Aprendiz";               // Nombre que ve el jugador
    [Range(3, 6)] public int gridSize = 4;         // Tamaño de la cuadrícula (4 = 4x4)
    public float roundTime = 45f;                  // Segundos para resolver
    [Tooltip("Cada cuántos segundos una tubería se mueve sola. 0 = nunca.")]
    public float vibrationInterval = 12f;
    [Range(0.4f, 1f)]
    [Tooltip("Qué tan larga es la ruta correcta (más alto = ruta más larga y difícil).")]
    public float maxPathFill = 0.7f;
    public float oxygenReward = 12f;               // % de Comida/O2 que da resolverlo

    public DifficultyStep() { }
    public DifficultyStep(string name, int grid, float time, float vibration, float fill, float reward)
    {
        this.name = name; gridSize = grid; roundTime = time;
        vibrationInterval = vibration; maxPathFill = fill; oxygenReward = reward;
    }
}

/// <summary>
/// OxygenPipeMiniGame = Minijuego 1 (Comida / Oxígeno).
///
/// HISTORIA PARA LOS NIÑOS:
///   Un micrometeorito golpeó la base lunar y rompió una tubería de oxígeno.
///   El astronauta debe reparar la red conectando el tanque (O2) con el módulo (FIN).
///
/// CÓMO SE JUEGA:
///   - Al abrir el minijuego aparece una pantalla con un botón INICIAR en el centro.
///     Al pulsarlo, la pantalla desaparece y empieza el juego.
///   - Las piezas O2 (inicio) y FIN están bloqueadas: no se mueven.
///   - El jugador gira las demás tuberías para llevar el oxígeno de O2 hasta FIN.
///   - Cada ronda es MÁS DIFÍCIL que la anterior (tabla "Difficulty Steps"):
///     cuadrícula más grande, menos tiempo y vibraciones más seguidas.
///
/// CIENCIA REAL:
///   La Estación Espacial Internacional produce oxígeno separando el agua con
///   electricidad (electrólisis). En la Luna, la regolita tiene cerca de 45% de oxígeno
///   atrapado en sus minerales. ¡La NASA estudia cómo sacarlo!
///
/// CÓMO SE CONECTA AL RESTO DEL JUEGO:
///   - Botón del Dashboard  -> StartGame()           (abre la pantalla de inicio)
///   - Evento onGameStarted -> resta Energía del Astronauta (se dispara al pulsar INICIAR)
///   - Evento onGameFinished(float) -> SUMA ese % a Comida/O2
///   - SetDifficultyLevel(n) -> el Dashboard puede subir la dificultad según avance el juego
/// </summary>
public class OxygenPipeMiniGame : MonoBehaviour
{
    // ------------------------------------------------------------------
    //  CONFIGURACIÓN (todo editable en el Inspector)
    // ------------------------------------------------------------------
    [Header("Cuadrícula")]
    [SerializeField] private RectTransform gridParent;     // Panel con GridLayoutGroup
    [SerializeField] private GridLayoutGroup gridLayout;   // El GridLayoutGroup del panel
    [SerializeField] private PipeTile tilePrefab;          // Prefab de la pieza

    [Header("Sprites de tuberías (cada uno dibujado SIN girar)")]
    [SerializeField] private Sprite endSprite;       // Extremo: abierta solo ARRIBA
    [SerializeField] private Sprite straightSprite;  // Recta: abierta ARRIBA y ABAJO
    [SerializeField] private Sprite cornerSprite;    // Codo: abierta ARRIBA y DERECHA
    [SerializeField] private Sprite tSprite;         // T: abierta ARRIBA, DERECHA e IZQUIERDA
    [SerializeField] private Sprite crossSprite;     // Cruz: abierta en los 4 lados

    [Header("Dificultad progresiva")]
    [SerializeField] private int roundsToWin = 3;          // Cuántas rondas dura una partida
    [Min(1)]
    [SerializeField] private int startLevel = 1;           // Nivel con el que empieza (1 = primero de la tabla)
    [SerializeField] private DifficultyStep[] difficultySteps = DefaultSteps();

    [Header("Pantallas")]
    [SerializeField] private GameObject introPanel;        // Pantalla de inicio (botón INICIAR en el centro)
    [SerializeField] private GameObject gameplayRoot;      // Todo lo que se ve mientras se juega (HUD + cuadrícula)
    [SerializeField] private GameObject resultPanel;       // Panel de resultado final

    [Header("Interfaz (HUD)")]
    [SerializeField] private TMP_Text levelText;           // "NIVEL 2"
    [SerializeField] private TMP_Text roundText;           // "RONDA 1/3"
    [SerializeField] private TMP_Text timerText;           // "00:39"
    [SerializeField] private TMP_Text movesText;           // "MOV 5"
    [SerializeField] private TMP_Text statusText;          // Mensajes (ej. vibración)
    [SerializeField] private Slider oxygenGainedBar;       // Barra del oxígeno recuperado
    [SerializeField] private TMP_Text introInfoText;       // "DIFICULTAD: NIVEL 1 - APRENDIZ"
    [SerializeField] private TMP_Text resultTitleText;     // "¡Sistema reparado!"
    [SerializeField] private TMP_Text resultBodyText;      // Cuánto oxígeno ganaste
    [SerializeField] private TMP_Text factText;            // Dato curioso de la NASA

    [Header("Eventos (conecta con tu Dashboard)")]
    public UnityEvent onGameStarted;           // Aquí resta Energía del Astronauta
    public UnityEvent<float> onGameFinished;   // Recibe el % de oxígeno ganado

    // ------------------------------------------------------------------
    //  DATOS INTERNOS
    // ------------------------------------------------------------------
    // Los 4 lados como bits: Arriba, Derecha, Abajo, Izquierda
    private static readonly int[] Directions = { 1, 2, 4, 8 };

    // Formas posibles de las piezas "señuelo" (algunas aparecen más seguido)
    private static readonly int[] DecoyShapes = { 3, 3, 3, 5, 5, 5, 11, 11, 1, 15 };

    private readonly List<PipeTile> tiles = new List<PipeTile>();
    private DifficultyStep currentStep;   // Dificultad de la ronda actual
    private int currentGrid = 4;          // Tamaño de cuadrícula de la ronda actual
    private int startIndex;               // Casilla de la tubería de INICIO (O2)
    private int endIndex;                 // Casilla de la tubería de FIN
    private int currentRound;
    private int roundsCompleted;
    private float rewardTotal;            // % de oxígeno ganado en esta partida
    private float timeLeft;
    private float vibrationTimer;
    private int moves;
    private bool playing;

    // Datos curiosos reales (en español, simples para niños)
    private readonly string[] facts =
    {
        "¿Sabías que? En la Estación Espacial Internacional el oxígeno se produce separando el agua con electricidad. ¡Se llama electrólisis!",
        "¿Sabías que? La regolita lunar (el polvo de la Luna) tiene cerca de 45% de oxígeno escondido dentro de sus minerales.",
        "¿Sabías que? Las plantas también ayudan: absorben el CO2 que exhalamos y liberan oxígeno nuevo.",
        "¿Sabías que? Los astronautas exhalan CO2, y la nave tiene filtros especiales para quitarlo del aire.",
        "¿Sabías que? Los micrometeoritos son granitos de roca espacial que viajan muy rápido. ¡Por eso los equipos se revisan siempre!"
    };

    /// <summary>Tabla de dificultad por defecto (se puede editar en el Inspector).</summary>
    private static DifficultyStep[] DefaultSteps()
    {
        return new[]
        {
            //                    nombre       grid  tiempo  vibración  ruta  premio
            new DifficultyStep("Aprendiz",     3,    60f,    0f,        0.60f, 8f),
            new DifficultyStep("Tecnico",      4,    50f,    20f,       0.60f, 10f),
            new DifficultyStep("Ingeniero",    4,    45f,    14f,       0.70f, 12f),
            new DifficultyStep("Especialista", 5,    50f,    12f,       0.70f, 14f),
            new DifficultyStep("Comandante",   5,    45f,    9f,        0.80f, 16f),
            new DifficultyStep("Leyenda",      6,    50f,    8f,        0.80f, 18f),
        };
    }

    // ------------------------------------------------------------------
    //  API PÚBLICA
    // ------------------------------------------------------------------

    /// <summary>
    /// ABRE el minijuego mostrando la pantalla de inicio (botón INICIAR al centro).
    /// Conéctalo al botón "Reparar Soporte Vital" del Dashboard.
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
        roundsCompleted = 0;
        rewardTotal = 0f;
        UpdateOxygenBar();

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
        onGameFinished?.Invoke(rewardTotal);  // <- Tu Dashboard suma este % a Comida/O2
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
        currentGrid = currentStep.gridSize;

        timeLeft = currentStep.roundTime;
        vibrationTimer = currentStep.vibrationInterval;
        moves = 0;

        levelText.text = $"NIVEL {level}";
        roundText.text = $"RONDA {currentRound}/{roundsToWin}";
        movesText.text = "MOV 0";
        statusText.text = "Conecta el INICIO (O2) con el FIN para reparar la fuga";
        UpdateTimerText();

        BuildPuzzle();
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
            EndGame(false); // Se acabó el tiempo
            return;
        }

        // --- Vibración a intervalos regulares (si este nivel la tiene) ---
        if (currentStep.vibrationInterval > 0f)
        {
            vibrationTimer -= Time.deltaTime;
            if (vibrationTimer <= 0f)
            {
                vibrationTimer = currentStep.vibrationInterval;
                ShakeRandomPipe();
            }
        }
    }

    private void UpdateTimerText()
    {
        int total = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
        timerText.text = $"{total / 60:00}:{total % 60:00}";
    }

    /// <summary>Un micrometeorito sacude el sistema: una tubería (que no sea INICIO/FIN) gira sola.</summary>
    private void ShakeRandomPipe()
    {
        List<PipeTile> movable = GetMovableTiles();
        if (movable.Count == 0) return;

        movable[Random.Range(0, movable.Count)].Rotate(Random.Range(1, 4));
        statusText.text = "Una vibracion movio una tuberia. Revisa la ruta!";
        Evaluate();
    }

    private List<PipeTile> GetMovableTiles()
    {
        List<PipeTile> list = new List<PipeTile>();
        foreach (var t in tiles) if (!t.IsLocked) list.Add(t);
        return list;
    }

    // ------------------------------------------------------------------
    //  CREACIÓN DEL PUZLE
    // ------------------------------------------------------------------

    private void BuildPuzzle()
    {
        // 1) Borrar piezas de la ronda anterior
        foreach (var t in tiles) if (t != null) Destroy(t.gameObject);
        tiles.Clear();

        // 2) Ajustar la cuadrícula al tamaño del panel
        int n = currentGrid;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = n;
        float cell = Mathf.Min(gridParent.rect.width, gridParent.rect.height) / n;
        gridLayout.cellSize = new Vector2(cell, cell);

        // 3) Elegir INICIO (columna izquierda) y FIN (columna derecha) en filas al azar
        startIndex = Random.Range(0, n) * n;               // columna 0
        endIndex = Random.Range(0, n) * n + (n - 1);       // última columna

        // 4) Buscar una ruta que SIEMPRE existe (así el puzle tiene solución).
        List<int> path = null;
        int maxLength = Mathf.Max(n, Mathf.RoundToInt(n * n * currentStep.maxPathFill));
        for (int attempt = 0; attempt < 20; attempt++)
        {
            path = FindRandomPath(n, startIndex, endIndex);
            if (path.Count <= maxLength) break;
        }

        // 5) Convertir la ruta en máscaras de conexión (qué lados abre cada casilla)
        int[] solution = new int[n * n];
        bool[] onPath = new bool[n * n];
        for (int i = 0; i < path.Count; i++)
        {
            onPath[path[i]] = true;
            if (i == 0) continue;
            int dir = DirectionBetween(path[i - 1], path[i], n);
            solution[path[i - 1]] |= dir;           // la anterior se abre hacia esta
            solution[path[i]] |= Opposite(dir);     // esta se abre hacia la anterior
        }

        // 6) Crear cada pieza
        for (int i = 0; i < n * n; i++)
        {
            TileRole role = i == startIndex ? TileRole.Start : (i == endIndex ? TileRole.End : TileRole.Normal);

            // Casillas de la ruta usan su forma correcta; las demás son señuelos al azar
            int target = onPath[i] ? solution[i] : DecoyShapes[Random.Range(0, DecoyShapes.Length)];
            int baseMask = GetBaseMask(target);
            Sprite sprite = GetSpriteForMask(baseMask);

            // INICIO y FIN nacen YA girados en la posición correcta (y quedan bloqueados).
            // Las demás nacen con una rotación al azar (¡desordenadas!).
            int rotation = role != TileRole.Normal ? RotationFor(baseMask, target) : Random.Range(0, 4);

            PipeTile tile = Instantiate(tilePrefab, gridParent);
            tile.Setup(baseMask, sprite, rotation, role, OnTileClicked);
            tiles.Add(tile);
        }

        // 7) Si por casualidad ya salió resuelto, giramos piezas hasta que no lo esté
        List<PipeTile> movable = GetMovableTiles();
        int safety = 60;
        while (FloodFill() && safety-- > 0 && movable.Count > 0)
            movable[Random.Range(0, movable.Count)].Rotate(1);

        FloodFill(); // Solo para pintar los colores iniciales
    }

    /// <summary>
    /// Busca un camino de casillas desde INICIO hasta FIN usando "recorrido aleatorio" (DFS).
    /// Como el mapa es una cuadrícula completa, SIEMPRE encuentra un camino.
    /// </summary>
    private List<int> FindRandomPath(int n, int start, int end)
    {
        bool[] visited = new bool[n * n];
        List<int> path = new List<int>();
        Dfs(start, end, n, visited, path);
        return path;
    }

    private bool Dfs(int current, int end, int n, bool[] visited, List<int> path)
    {
        visited[current] = true;
        path.Add(current);
        if (current == end) return true;

        // Probamos las 4 direcciones en orden aleatorio
        int[] dirs = (int[])Directions.Clone();
        for (int i = dirs.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = dirs[i]; dirs[i] = dirs[j]; dirs[j] = tmp;
        }

        foreach (int dir in dirs)
        {
            if (TryGetNeighbor(current, dir, n, out int nb) && !visited[nb])
                if (Dfs(nb, end, n, visited, path)) return true;
        }

        path.RemoveAt(path.Count - 1); // Callejón sin salida: retroceder
        return false;
    }

    // ------------------------------------------------------------------
    //  LÓGICA DE CONEXIÓN
    // ------------------------------------------------------------------

    private void OnTileClicked(PipeTile tile)
    {
        if (!playing || tile.IsLocked) return;
        tile.Rotate(1);
        moves++;
        movesText.text = $"MOV {moves}";
        Evaluate();
    }

    /// <summary>Revisa por dónde pasa el oxígeno y si ya llegó al FIN.</summary>
    private void Evaluate()
    {
        if (FloodFill()) RoundSolved();
    }

    /// <summary>
    /// "Derrama" oxígeno desde el INICIO: pasa de pieza en pieza SOLO si ambas
    /// tienen apertura una frente a la otra. Pinta de celeste lo que recibe oxígeno.
    /// Devuelve true si el oxígeno LLEGÓ a la pieza de FIN.
    /// </summary>
    private bool FloodFill()
    {
        bool[] reached = new bool[tiles.Count];
        Queue<int> queue = new Queue<int>();
        queue.Enqueue(startIndex);
        reached[startIndex] = true;

        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            int curMask = tiles[cur].CurrentMask;

            foreach (int dir in Directions)
            {
                if ((curMask & dir) == 0) continue;                              // Esta pieza no abre hacia ahí
                if (!TryGetNeighbor(cur, dir, currentGrid, out int nb)) continue;// Borde del mapa
                if (reached[nb]) continue;
                if ((tiles[nb].CurrentMask & Opposite(dir)) == 0) continue;      // El vecino no abre hacia nosotros

                reached[nb] = true;
                queue.Enqueue(nb);
            }
        }

        // Pintar de celeste las que tienen oxígeno
        for (int i = 0; i < tiles.Count; i++) tiles[i].SetFilled(reached[i]);
        return reached[endIndex];
    }

    private void RoundSolved()
    {
        playing = false;
        roundsCompleted++;
        rewardTotal += currentStep.oxygenReward;
        UpdateOxygenBar();
        foreach (var t in tiles) t.SetInteractable(false);

        if (currentRound >= roundsToWin)
        {
            EndGame(true);
        }
        else
        {
            statusText.text = "Oxigeno entregado! La siguiente reparacion sera mas dificil...";
            Invoke(nameof(StartNextRound), 1.8f); // Pequeña pausa antes de la siguiente ronda
        }
    }

    private void EndGame(bool won)
    {
        playing = false;
        CancelInvoke();
        foreach (var t in tiles) if (t != null) t.SetInteractable(false);

        resultPanel.SetActive(true);
        resultTitleText.text = won ? "SISTEMA REPARADO!" : "SE ACABO EL TIEMPO!";
        resultBodyText.text = $"Recuperaste {rewardTotal:0}% de Comida y Oxigeno.";
        factText.text = facts[Random.Range(0, facts.Length)];
    }

    /// <summary>La barra se llena según el premio total que se puede ganar en la partida.</summary>
    private void UpdateOxygenBar()
    {
        if (oxygenGainedBar == null) return;
        float max = 0f;
        for (int r = 1; r <= roundsToWin; r++) max += StepForLevel(startLevel + r - 1).oxygenReward;
        oxygenGainedBar.maxValue = Mathf.Max(1f, max);
        oxygenGainedBar.value = rewardTotal;
    }

    // ------------------------------------------------------------------
    //  FUNCIONES AUXILIARES
    // ------------------------------------------------------------------

    /// <summary>Lado opuesto: Arriba(1)<->Abajo(4), Derecha(2)<->Izquierda(8).</summary>
    private static int Opposite(int dir) => ((dir << 2) | (dir >> 2)) & 15;

    /// <summary>Dirección para ir de la casilla "a" a su vecina "b".</summary>
    private static int DirectionBetween(int a, int b, int n)
    {
        if (b == a - n) return 1;   // Arriba
        if (b == a + 1) return 2;   // Derecha
        if (b == a + n) return 4;   // Abajo
        return 8;                   // Izquierda
    }

    /// <summary>Cuántos giros de 90° necesita una pieza base para quedar con la máscara "target".</summary>
    private static int RotationFor(int baseMask, int target)
    {
        for (int r = 0; r < 4; r++)
            if (PipeTile.RotateMask(baseMask, r) == target) return r;
        return 0;
    }

    /// <summary>Busca la casilla vecina en una dirección. Devuelve false si está fuera del mapa.</summary>
    private static bool TryGetNeighbor(int index, int dir, int n, out int neighbor)
    {
        int row = index / n, col = index % n;
        neighbor = -1;
        switch (dir)
        {
            case 1: if (row > 0)     { neighbor = index - n; return true; } break; // Arriba
            case 2: if (col < n - 1) { neighbor = index + 1; return true; } break; // Derecha
            case 4: if (row < n - 1) { neighbor = index + n; return true; } break; // Abajo
            case 8: if (col > 0)     { neighbor = index - 1; return true; } break; // Izquierda
        }
        return false;
    }

    /// <summary>
    /// Convierte una máscara en la máscara base de un dibujo (sin girar):
    ///   1 apertura -> Extremo (1) | 2 opuestas -> Recta (5) | 2 juntas -> Codo (3)
    ///   3 aperturas -> T (11)     | 4 aperturas -> Cruz (15)
    /// </summary>
    private static int GetBaseMask(int mask)
    {
        int bits = 0;
        for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0) bits++;

        switch (bits)
        {
            case 1: return 1;
            case 2: return (mask == 5 || mask == 10) ? 5 : 3;
            case 3: return 11;
            default: return 15;
        }
    }

    private Sprite GetSpriteForMask(int baseMask)
    {
        switch (baseMask)
        {
            case 1:  return endSprite;
            case 5:  return straightSprite;
            case 3:  return cornerSprite;
            case 11: return tSprite;
            default: return crossSprite;
        }
    }
}
