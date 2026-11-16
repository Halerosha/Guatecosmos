using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// minijuego uno, gira los tubos hasta que el aire pase de O2 a FIN
public class PipeGame : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("gameFont")] TMP_FontAsset fuenteJuego;
    [SerializeField, FormerlySerializedAs("cellSize")] float tamanoCelda = 84f;
    [SerializeField, FormerlySerializedAs("startTime")] float tiempoInicial = 60f;

    const int IdJuego = 0;
    static readonly int[] tamanos = { 4, 4, 5, 5, 6 };

    int[] bitsDireccion = { 1, 2, 4, 8 };
    int[] cambioFila = { -1, 0, 1, 0 };
    int[] cambioColumna = { 0, 1, 0, -1 };

    int nivel, tam, filaInicio, filaFin, movimientos;
    int[,] tipoPieza;
    int[,] giroPieza;
    Image[,] fichas;
    float tiempoRestante;
    bool terminado;
    Transform raiz;
    TextMeshProUGUI textoTiempo, textoMovimientos;

    void Start()
    {
        GameState.CargarSiHaceFalta();
        UIBuilder.fuente = fuenteJuego;
        nivel = GameState.NivelActual(IdJuego);
        tam = tamanos[nivel - 1];
        tiempoRestante = tiempoInicial - nivel * 3f;

        Canvas canvas = UIBuilder.CrearCanvas();
        raiz = canvas.transform;
        UIBuilder.Fondo(raiz);
        UIBuilder.Encabezado(raiz, "PIPE REPAIR  LEVEL " + nivel, SalirAntes);

        textoTiempo = UIBuilder.Etiqueta(raiz, "", 28, new Vector2(500, 320), new Vector2(240, 40), TextAlignmentOptions.Right);
        textoMovimientos = UIBuilder.Etiqueta(raiz, "MOVES 0", 24, new Vector2(500, 280), new Vector2(240, 40), TextAlignmentOptions.Right);
        UIBuilder.Etiqueta(raiz, "Connect O2 with FIN. Tap a pipe to turn it. The space station recycles most of its water and air.", 20, new Vector2(0, -325), new Vector2(1100, 40), TextAlignmentOptions.Center);

        ConstruirRompecabezas();
        RevisarFlujo();
    }

    void ConstruirRompecabezas()
    {
        filaInicio = Random.Range(0, tam);
        filaFin = Random.Range(0, tam);
        tipoPieza = new int[tam, tam];
        giroPieza = new int[tam, tam];
        fichas = new Image[tam, tam];

        for (int f = 0; f < tam; f++)
        {
            for (int c = 0; c < tam; c++)
            {
                tipoPieza[f, c] = Random.Range(0, 2);
                giroPieza[f, c] = Random.Range(0, 4);
            }
        }

        // se avanza una columna a la vez para que el camino nunca se cruce
        List<Vector2Int> camino = new List<Vector2Int>();
        int fila = filaInicio;
        for (int c = 0; c < tam; c++)
        {
            int destino = (c == tam - 1) ? filaFin : Random.Range(0, tam);
            camino.Add(new Vector2Int(fila, c));
            while (fila != destino)
            {
                fila += (destino > fila) ? 1 : -1;
                camino.Add(new Vector2Int(fila, c));
            }
        }

        for (int k = 0; k < camino.Count; k++)
        {
            int bitAtras = (k == 0) ? 8 : BitHacia(camino[k], camino[k - 1]);
            int bitAdelante = (k == camino.Count - 1) ? 2 : BitHacia(camino[k], camino[k + 1]);
            int mascara = bitAtras | bitAdelante;

            int giroResuelto = 0;
            int tipo = 1;
            if (mascara == 5 || mascara == 10)
            {
                tipo = 0;
                giroResuelto = (mascara == 5) ? 0 : 1;
            }
            else
            {
                for (int g = 0; g < 4; g++)
                {
                    if (Girada(3, g) == mascara) giroResuelto = g;
                }
            }
            tipoPieza[camino[k].x, camino[k].y] = tipo;
            giroPieza[camino[k].x, camino[k].y] = (giroResuelto + Random.Range(1, 4)) % 4;
        }

        float anchoTablero = (tam + 2) * tamanoCelda + 30f;
        float altoTablero = tam * tamanoCelda + 30f;
        UIBuilder.Panel(raiz, new Vector2(0, -20), new Vector2(anchoTablero, altoTablero));

        for (int f = 0; f < tam; f++)
        {
            for (int c = 0; c < tam; c++)
            {
                int ff = f;
                int cc = c;
                string nombreSprite = tipoPieza[f, c] == 0 ? "pipe_straight" : "pipe_curve";
                Image imagen = UIBuilder.Dibujo(raiz, nombreSprite, PosFicha(f, c), new Vector2(tamanoCelda - 4, tamanoCelda - 4));
                imagen.raycastTarget = true;
                Button boton = imagen.gameObject.AddComponent<Button>();
                boton.transition = Selectable.Transition.None;
                boton.onClick.AddListener(() => Girar(ff, cc));
                fichas[f, c] = imagen;
            }
        }

        Vector2 posInicio = PosFicha(filaInicio, -1);
        Vector2 posFin = PosFicha(filaFin, tam);
        UIBuilder.Dibujo(raiz, "pipe_start", posInicio, new Vector2(tamanoCelda - 4, tamanoCelda - 4));
        UIBuilder.Dibujo(raiz, "pipe_end", posFin, new Vector2(tamanoCelda - 4, tamanoCelda - 4));
        UIBuilder.Etiqueta(raiz, "O2", 20, posInicio + new Vector2(-14, 0), new Vector2(50, 30), TextAlignmentOptions.Center);
        UIBuilder.Etiqueta(raiz, "FIN", 18, posFin + new Vector2(14, 0), new Vector2(50, 30), TextAlignmentOptions.Center);
    }

    Vector2 PosFicha(int f, int c)
    {
        float x = (c - (tam - 1) / 2f) * tamanoCelda;
        float y = ((tam - 1) / 2f - f) * tamanoCelda - 20f;
        return new Vector2(x, y);
    }

    int BitHacia(Vector2Int desde, Vector2Int hasta)
    {
        int df = hasta.x - desde.x;
        int dc = hasta.y - desde.y;
        if (df == -1) return 1;
        if (dc == 1) return 2;
        if (df == 1) return 4;
        return 8;
    }

    int Girada(int mascara, int veces)
    {
        for (int i = 0; i < veces; i++)
        {
            mascara = ((mascara << 1) | (mascara >> 3)) & 15;
        }
        return mascara;
    }

    int MascaraEn(int f, int c)
    {
        int mascaraBase = tipoPieza[f, c] == 0 ? 5 : 3;
        return Girada(mascaraBase, giroPieza[f, c]);
    }

    void Girar(int f, int c)
    {
        if (terminado) return;
        giroPieza[f, c] = (giroPieza[f, c] + 1) % 4;
        movimientos++;
        textoMovimientos.text = "MOVES " + movimientos;
        RevisarFlujo();
    }

    // el aire sale del inicio y pinta cada tubo al que llega
    void RevisarFlujo()
    {
        bool[,] alcanzado = new bool[tam, tam];
        Queue<Vector2Int> cola = new Queue<Vector2Int>();

        if ((MascaraEn(filaInicio, 0) & 8) != 0)
        {
            alcanzado[filaInicio, 0] = true;
            cola.Enqueue(new Vector2Int(filaInicio, 0));
        }

        while (cola.Count > 0)
        {
            Vector2Int actual = cola.Dequeue();
            int mascara = MascaraEn(actual.x, actual.y);
            for (int i = 0; i < 4; i++)
            {
                if ((mascara & bitsDireccion[i]) == 0) continue;
                int nf = actual.x + cambioFila[i];
                int nc = actual.y + cambioColumna[i];
                if (nf < 0 || nc < 0 || nf >= tam || nc >= tam) continue;
                if (alcanzado[nf, nc]) continue;
                if ((MascaraEn(nf, nc) & bitsDireccion[(i + 2) % 4]) == 0) continue;
                alcanzado[nf, nc] = true;
                cola.Enqueue(new Vector2Int(nf, nc));
            }
        }

        for (int f = 0; f < tam; f++)
        {
            for (int c = 0; c < tam; c++)
            {
                fichas[f, c].rectTransform.localRotation = Quaternion.Euler(0, 0, -90f * giroPieza[f, c]);
                fichas[f, c].color = alcanzado[f, c] ? Color.white : new Color(0.6f, 0.65f, 0.75f);
            }
        }

        bool listo = alcanzado[filaFin, tam - 1] && (MascaraEn(filaFin, tam - 1) & 2) != 0;
        if (listo) Terminar(true);
    }

    void Update()
    {
        if (terminado) return;
        tiempoRestante -= Time.deltaTime;
        textoTiempo.text = "TIME " + Mathf.Max(0, Mathf.CeilToInt(tiempoRestante));
        if (tiempoRestante <= 0f) Terminar(false);
    }

    void Terminar(bool gano)
    {
        if (terminado) return;
        terminado = true;
        string mensaje = GameState.TerminarJuego(IdJuego, gano, nivel);
        UIBuilder.Ventana(raiz, gano ? "LEAK FIXED" : "TIME IS UP", mensaje, "CONTINUE", () => UIBuilder.IrA("Dashboard"));
    }

    void SalirAntes()
    {
        GameState.ultimoMensaje = "You left the mission early.";
        GameState.Guardar();
        UIBuilder.IrA("Dashboard");
    }
}
