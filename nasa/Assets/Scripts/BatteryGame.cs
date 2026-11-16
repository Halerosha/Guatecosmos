using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// minijuego tres, empuja cada bateria hasta una ranura dorada de carga
public class BatteryGame : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("gameFont")] TMP_FontAsset fuenteJuego;
    [SerializeField, FormerlySerializedAs("cellSize")] float tamanoCelda = 72f;

    class Foto
    {
        public Vector2Int jugador;
        public Vector2Int[] cajas;
        public int movimientos;
    }

    const int IdJuego = 2;

    // # pared, . ranura, $ bateria, @ astronauta
    static readonly string[][] mapas = new string[][]
    {
        new string[] { "#######", "#.    #", "#  $  #", "#  @  #", "#######" },
        new string[] { "#######", "#. .  #", "#  $$ #", "#  @  #", "#######" },
        new string[] { "########", "#  #   #", "# .$  .#", "#  @ $ #", "#   #  #", "########" },
        new string[] { "#########", "#   #   #", "# . $ . #", "#  $@$  #", "#   .   #", "#########" },
        new string[] { "########", "#  .   #", "# #$ # #", "#  $@  #", "# . $ .#", "#  #   #", "########" }
    };
    static readonly int[] pares = { 5, 11, 9, 12, 18 };

    int nivel, ancho, alto, movimientos, mejor;
    bool[,] pared;
    bool[,] ranura;
    List<Vector2Int> cajas = new List<Vector2Int>();
    List<Vector2Int> cajasIniciales = new List<Vector2Int>();
    Vector2Int jugador, jugadorInicial;
    List<Image> imagenesCajas = new List<Image>();
    Image imagenJugador;
    Stack<Foto> pilaDeshacer = new Stack<Foto>();
    Vector2 centroTablero = new Vector2(-150, -20);
    bool terminado;
    Transform raiz;
    TextMeshProUGUI textoMovimientos;

    void Start()
    {
        GameState.CargarSiHaceFalta();
        UIBuilder.fuente = fuenteJuego;
        nivel = GameState.NivelActual(IdJuego);
        mejor = PlayerPrefs.GetInt("battery_best_" + nivel, 0);

        Canvas canvas = UIBuilder.CrearCanvas();
        raiz = canvas.transform;
        UIBuilder.Fondo(raiz);
        UIBuilder.Encabezado(raiz, "BATTERY GRID  LEVEL " + nivel, SalirAntes);

        LeerMapa(mapas[nivel - 1]);
        ConstruirTablero();
        ConstruirPanelLateral();
        UIBuilder.Etiqueta(raiz, "Zero gravity does not make things weightless to push. Heavy batteries still have mass, so you can only push one at a time.", 18, new Vector2(0, -335), new Vector2(1150, 40), TextAlignmentOptions.Center);
        Redibujar();
    }

    void LeerMapa(string[] filas)
    {
        alto = filas.Length;
        ancho = 0;
        foreach (string fila in filas) ancho = Mathf.Max(ancho, fila.Length);
        pared = new bool[ancho, alto];
        ranura = new bool[ancho, alto];

        for (int y = 0; y < alto; y++)
        {
            string linea = filas[y].PadRight(ancho);
            for (int x = 0; x < ancho; x++)
            {
                char letra = linea[x];
                if (letra == '#') pared[x, y] = true;
                if (letra == '.') ranura[x, y] = true;
                if (letra == '$') cajasIniciales.Add(new Vector2Int(x, y));
                if (letra == '@') jugadorInicial = new Vector2Int(x, y);
            }
        }
        cajas = new List<Vector2Int>(cajasIniciales);
        jugador = jugadorInicial;
    }

    Vector2 PosCelda(Vector2Int p)
    {
        float x = (p.x - (ancho - 1) / 2f) * tamanoCelda + centroTablero.x;
        float y = ((alto - 1) / 2f - p.y) * tamanoCelda + centroTablero.y;
        return new Vector2(x, y);
    }

    void ConstruirTablero()
    {
        UIBuilder.Panel(raiz, centroTablero, new Vector2(ancho * tamanoCelda + 30, alto * tamanoCelda + 30));
        Vector2 tamano = new Vector2(tamanoCelda, tamanoCelda);

        for (int y = 0; y < alto; y++)
        {
            for (int x = 0; x < ancho; x++)
            {
                Vector2Int p = new Vector2Int(x, y);
                UIBuilder.Dibujo(raiz, pared[x, y] ? "wall" : "floor", PosCelda(p), tamano);
                if (ranura[x, y]) UIBuilder.Dibujo(raiz, "slot", PosCelda(p), tamano);
            }
        }

        for (int i = 0; i < cajas.Count; i++)
        {
            imagenesCajas.Add(UIBuilder.Dibujo(raiz, "battery", Vector2.zero, tamano * 0.9f));
        }
        imagenJugador = UIBuilder.Dibujo(raiz, "astronaut", Vector2.zero, tamano * 0.9f);
    }

    void ConstruirPanelLateral()
    {
        UIBuilder.Panel(raiz, new Vector2(430, 110), new Vector2(330, 180));
        textoMovimientos = UIBuilder.Etiqueta(raiz, "", 28, new Vector2(430, 165), new Vector2(300, 40), TextAlignmentOptions.Center);
        UIBuilder.Etiqueta(raiz, "PAR " + pares[nivel - 1], 24, new Vector2(430, 120), new Vector2(300, 40), TextAlignmentOptions.Center);
        string textoMejor = mejor > 0 ? mejor.ToString() : "none";
        UIBuilder.Etiqueta(raiz, "BEST " + textoMejor, 24, new Vector2(430, 75), new Vector2(300, 40), TextAlignmentOptions.Center);

        Color color = UIBuilder.azul;
        UIBuilder.Boton(raiz, "UP", new Vector2(440, -10), new Vector2(84, 50), color, () => IntentarMover(0, -1), 18);
        UIBuilder.Boton(raiz, "LEFT", new Vector2(346, -68), new Vector2(84, 50), color, () => IntentarMover(-1, 0), 18);
        UIBuilder.Boton(raiz, "DOWN", new Vector2(440, -68), new Vector2(84, 50), color, () => IntentarMover(0, 1), 18);
        UIBuilder.Boton(raiz, "RIGHT", new Vector2(534, -68), new Vector2(84, 50), color, () => IntentarMover(1, 0), 18);
        UIBuilder.Boton(raiz, "UNDO", new Vector2(380, -150), new Vector2(150, 48), UIBuilder.dorado * 0.7f, Deshacer, 20);
        UIBuilder.Boton(raiz, "RESTART", new Vector2(540, -150), new Vector2(150, 48), UIBuilder.rojo, Reiniciar, 20);
    }

    void Update()
    {
        if (terminado) return;
        if (GameInput.ArribaPulsado()) IntentarMover(0, -1);
        if (GameInput.AbajoPulsado()) IntentarMover(0, 1);
        if (GameInput.IzquierdaPulsado()) IntentarMover(-1, 0);
        if (GameInput.DerechaPulsado()) IntentarMover(1, 0);
        if (GameInput.DeshacerPulsado()) Deshacer();
    }

    int CajaEn(Vector2Int p)
    {
        for (int i = 0; i < cajas.Count; i++)
        {
            if (cajas[i] == p) return i;
        }
        return -1;
    }

    void GuardarFoto()
    {
        Foto foto = new Foto();
        foto.jugador = jugador;
        foto.cajas = cajas.ToArray();
        foto.movimientos = movimientos;
        pilaDeshacer.Push(foto);
    }

    void IntentarMover(int dx, int dy)
    {
        if (terminado) return;
        Vector2Int direccion = new Vector2Int(dx, dy);
        Vector2Int siguiente = jugador + direccion;
        if (pared[siguiente.x, siguiente.y]) return;

        int golpeada = CajaEn(siguiente);
        if (golpeada >= 0)
        {
            Vector2Int masAlla = siguiente + direccion;
            if (pared[masAlla.x, masAlla.y] || CajaEn(masAlla) >= 0) return;
            GuardarFoto();
            cajas[golpeada] = masAlla;
        }
        else
        {
            GuardarFoto();
        }

        jugador = siguiente;
        movimientos++;
        Redibujar();
        RevisarVictoria();
    }

    void Deshacer()
    {
        if (terminado || pilaDeshacer.Count == 0) return;
        Foto foto = pilaDeshacer.Pop();
        jugador = foto.jugador;
        cajas = new List<Vector2Int>(foto.cajas);
        movimientos = foto.movimientos;
        Redibujar();
    }

    void Reiniciar()
    {
        if (terminado) return;
        cajas = new List<Vector2Int>(cajasIniciales);
        jugador = jugadorInicial;
        movimientos = 0;
        pilaDeshacer.Clear();
        Redibujar();
    }

    void Redibujar()
    {
        imagenJugador.rectTransform.anchoredPosition = PosCelda(jugador);
        for (int i = 0; i < cajas.Count; i++)
        {
            imagenesCajas[i].rectTransform.anchoredPosition = PosCelda(cajas[i]);
            bool cargada = ranura[cajas[i].x, cajas[i].y];
            imagenesCajas[i].color = cargada ? new Color(0.6f, 1f, 0.7f) : Color.white;
        }
        textoMovimientos.text = "MOVES " + movimientos;
    }

    void RevisarVictoria()
    {
        foreach (Vector2Int caja in cajas)
        {
            if (!ranura[caja.x, caja.y]) return;
        }
        terminado = true;
        if (mejor == 0 || movimientos < mejor) PlayerPrefs.SetInt("battery_best_" + nivel, movimientos);
        string mensaje = GameState.TerminarJuego(IdJuego, true, nivel);
        UIBuilder.Ventana(raiz, "ALL CHARGING", mensaje, "CONTINUE", () => UIBuilder.IrA("Dashboard"));
    }

    void SalirAntes()
    {
        GameState.ultimoMensaje = "You left the mission early.";
        GameState.Guardar();
        UIBuilder.IrA("Dashboard");
    }
}
