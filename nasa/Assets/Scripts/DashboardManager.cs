using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// pantalla principal de la base, muestra las cuatro barras y lanza todo lo demas
public class DashboardManager : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("gameFont")] TMP_FontAsset fuenteJuego;
    [SerializeField, FormerlySerializedAs("telescope")] TelescopePanel telescopio;
    [SerializeField, FormerlySerializedAs("tickSeconds")] float segundosPorPaso = 6f;
    [SerializeField, FormerlySerializedAs("dilemmaEverySeconds")] float segundosEntreDilemas = 40f;
    [SerializeField, FormerlySerializedAs("firstDilemmaAt")] float primerDilemaEn = 20f;
    [SerializeField, FormerlySerializedAs("passiveRest")] float descansoPasivo = 1f;

    Slider[] barras = new Slider[4];
    TextMeshProUGUI[] textosBarra = new TextMeshProUGUI[4];
    Color[] coloresBarra = new Color[4];
    TextMeshProUGUI textoEstado, textoDia, textoProgreso;

    GameObject panelDilema, panelFin;
    TextMeshProUGUI tituloDilema, textoDilema, datoDilema, textoFin;
    Button opcionA, opcionB;
    Dilemma actual;

    float temporizadorPaso;
    float temporizadorDilema;

    void Start()
    {
        GameState.CargarSiHaceFalta();
        UIBuilder.fuente = fuenteJuego;

        coloresBarra[0] = new Color(0.3f, 0.8f, 0.4f);
        coloresBarra[1] = new Color(0.95f, 0.75f, 0.2f);
        coloresBarra[2] = new Color(0.6f, 0.45f, 0.95f);
        coloresBarra[3] = new Color(0.3f, 0.85f, 0.95f);

        Canvas canvas = UIBuilder.CrearCanvas();
        UIBuilder.Fondo(canvas.transform, "bg_lobby");
        ConstruirPrincipal(canvas.transform);

        if (telescopio == null) telescopio = gameObject.AddComponent<TelescopePanel>();
        telescopio.Construir(canvas.transform);
        telescopio.alCerrar = Actualizar;

        ConstruirPanelDilema(canvas.transform);
        ConstruirPanelFin(canvas.transform);

        temporizadorDilema = segundosEntreDilemas - primerDilemaEn;

        if (GameState.ultimoMensaje != "")
        {
            textoEstado.text = GameState.ultimoMensaje;
            GameState.ultimoMensaje = "";
        }
        Actualizar();
        RevisarFinDelJuego();
    }

    void ConstruirPrincipal(Transform raiz)
    {
        TextMeshProUGUI titulo = UIBuilder.Etiqueta(raiz, "LUNAR BASE CONTROL", 42, new Vector2(-270, 310), new Vector2(700, 60), TextAlignmentOptions.Left);
        titulo.color = UIBuilder.acento;
        textoDia = UIBuilder.Etiqueta(raiz, "DAY 1", 32, new Vector2(480, 310), new Vector2(300, 50), TextAlignmentOptions.Right);

        UIBuilder.Panel(raiz, new Vector2(-290, 60), new Vector2(600, 340));
        string[] nombres = { "FOOD AND OXYGEN", "POWER", "RADIATION SHIELD", "ASTRO ENERGY" };
        for (int i = 0; i < 4; i++)
        {
            float y = 185 - i * 78;
            textosBarra[i] = UIBuilder.Etiqueta(raiz, nombres[i], 22, new Vector2(-290, y), new Vector2(560, 30), TextAlignmentOptions.Left);
            barras[i] = UIBuilder.Barra(raiz, new Vector2(-290, y - 30), new Vector2(560, 22), coloresBarra[i]);
        }

        UIBuilder.Boton(raiz, "TELESCOPE  REST", new Vector2(330, 190), new Vector2(440, 64), UIBuilder.azul, AbrirTelescopio);
        UIBuilder.Boton(raiz, "PIPES  FOOD AND OXYGEN", new Vector2(330, 110), new Vector2(440, 64), UIBuilder.verde, () => IniciarJuego(0));
        UIBuilder.Boton(raiz, "DEFLECTOR  RADIATION", new Vector2(330, 30), new Vector2(440, 64), UIBuilder.verde, () => IniciarJuego(1));
        UIBuilder.Boton(raiz, "BATTERIES  POWER", new Vector2(330, -50), new Vector2(440, 64), UIBuilder.verde, () => IniciarJuego(2));

        textoProgreso = UIBuilder.Etiqueta(raiz, "", 20, new Vector2(-290, -165), new Vector2(600, 40), TextAlignmentOptions.Center);
        textoEstado = UIBuilder.Etiqueta(raiz, "Each mission costs 10 astro energy. Rest at the telescope to get it back.", 24, new Vector2(0, -250), new Vector2(1100, 70), TextAlignmentOptions.Center);

        UIBuilder.Boton(raiz, "NEW GAME", new Vector2(530, -320), new Vector2(200, 44), UIBuilder.rojo, ReiniciarTodo, 18);
    }

    void ConstruirPanelDilema(Transform raiz)
    {
        Image velo = UIBuilder.Caja(raiz, Vector2.zero, new Vector2(3000, 2000), new Color(0, 0, 0, 0.75f));
        panelDilema = velo.gameObject;
        Image tarjeta = UIBuilder.Panel(velo.transform, Vector2.zero, new Vector2(900, 540));

        tituloDilema = UIBuilder.Etiqueta(tarjeta.transform, "", 38, new Vector2(0, 210), new Vector2(800, 60), TextAlignmentOptions.Center);
        tituloDilema.color = UIBuilder.dorado;
        textoDilema = UIBuilder.Etiqueta(tarjeta.transform, "", 26, new Vector2(0, 120), new Vector2(800, 110), TextAlignmentOptions.Center);
        datoDilema = UIBuilder.Etiqueta(tarjeta.transform, "", 20, new Vector2(0, 30), new Vector2(800, 70), TextAlignmentOptions.Center);
        datoDilema.color = UIBuilder.acento;

        opcionA = UIBuilder.Boton(tarjeta.transform, "", new Vector2(-220, -130), new Vector2(400, 220), UIBuilder.azul, () => Elegir(true), 20);
        opcionB = UIBuilder.Boton(tarjeta.transform, "", new Vector2(220, -130), new Vector2(400, 220), UIBuilder.azul, () => Elegir(false), 20);
        panelDilema.SetActive(false);
    }

    void ConstruirPanelFin(Transform raiz)
    {
        Image velo = UIBuilder.Caja(raiz, Vector2.zero, new Vector2(3000, 2000), new Color(0, 0, 0, 0.85f));
        panelFin = velo.gameObject;
        Image tarjeta = UIBuilder.Panel(velo.transform, Vector2.zero, new Vector2(700, 340));
        TextMeshProUGUI cabecera = UIBuilder.Etiqueta(tarjeta.transform, "BASE LOST", 44, new Vector2(0, 110), new Vector2(600, 70), TextAlignmentOptions.Center);
        cabecera.color = new Color(1f, 0.45f, 0.4f);
        textoFin = UIBuilder.Etiqueta(tarjeta.transform, "", 24, new Vector2(0, 10), new Vector2(600, 130), TextAlignmentOptions.Center);
        UIBuilder.Boton(tarjeta.transform, "TRY AGAIN", new Vector2(0, -115), new Vector2(260, 60), UIBuilder.verde, ReiniciarTodo);
        panelFin.SetActive(false);
    }

    void Update()
    {
        if (panelDilema.activeSelf || panelFin.activeSelf || telescopio.EstaAbierto) return;

        temporizadorPaso += Time.deltaTime;
        if (temporizadorPaso >= segundosPorPaso)
        {
            temporizadorPaso = 0f;
            PasoDelTiempo();
        }

        temporizadorDilema += Time.deltaTime;
        if (temporizadorDilema >= segundosEntreDilemas)
        {
            temporizadorDilema = 0f;
            MostrarDilema();
        }
    }

    // la base se desgasta poco a poco y la tripulacion va descansando
    void PasoDelTiempo()
    {
        GameState.Sumar(Res.Comida, -Random.Range(1, 4));
        GameState.Sumar(Res.Electricidad, -Random.Range(1, 4));
        GameState.Sumar(Res.Radiacion, -Random.Range(0, 3));
        GameState.Sumar(Res.Descanso, descansoPasivo);
        GameState.Guardar();
        Actualizar();
        RevisarFinDelJuego();
    }

    void Actualizar()
    {
        string[] nombres = { "FOOD AND OXYGEN", "POWER", "RADIATION SHIELD", "ASTRO ENERGY" };
        for (int i = 0; i < 4; i++)
        {
            float v = GameState.valores[i];
            barras[i].value = v;
            textosBarra[i].text = nombres[i] + "   " + Mathf.RoundToInt(v) + "%";
            Image relleno = barras[i].fillRect.GetComponent<Image>();
            relleno.color = v < 25f ? new Color(0.9f, 0.3f, 0.3f) : coloresBarra[i];
        }
        textoDia.text = "DAY " + GameState.dia;
        textoProgreso.text = "Pipes " + GameState.superados[0] + "/5     Deflector " + GameState.superados[1] + "/5     Batteries " + GameState.superados[2] + "/5";
    }

    void AbrirTelescopio()
    {
        telescopio.Abrir();
    }

    void IniciarJuego(int juego)
    {
        if (GameState.Obtener(Res.Descanso) < GameState.CostoJuego)
        {
            textoEstado.text = "The crew is too tired. Go rest at the telescope first.";
            return;
        }
        GameState.Sumar(Res.Descanso, -GameState.CostoJuego);
        GameState.juegoActivo = juego;
        GameState.Guardar();
        UIBuilder.IrA(GameState.nombresEscenas[juego]);
    }

    void MostrarDilema()
    {
        actual = DilemmaList.ObtenerAleatorio();
        tituloDilema.text = actual.titulo;
        textoDilema.text = actual.texto;
        datoDilema.text = actual.dato;
        opcionA.GetComponentInChildren<TextMeshProUGUI>().text = actual.opcionA + "\n\n" + Resumen(actual.efectoA);
        opcionB.GetComponentInChildren<TextMeshProUGUI>().text = actual.opcionB + "\n\n" + Resumen(actual.efectoB);
        panelDilema.SetActive(true);
    }

    // muestra lo que cuesta cada decision para que se vea claro
    string Resumen(float[] efecto)
    {
        string[] nombresCortos = { "Food", "Power", "Shield", "Energy" };
        string resultado = "";
        for (int i = 0; i < 4; i++)
        {
            if (efecto[i] == 0) continue;
            string signo = efecto[i] > 0 ? "+" : "";
            resultado += nombresCortos[i] + " " + signo + efecto[i] + "   ";
        }
        return resultado;
    }

    void Elegir(bool eligioA)
    {
        float[] efecto = eligioA ? actual.efectoA : actual.efectoB;
        for (int i = 0; i < 4; i++)
        {
            GameState.Sumar((Res)i, efecto[i]);
        }
        GameState.dia++;
        GameState.Guardar();
        panelDilema.SetActive(false);
        textoEstado.text = "Choice made. Every decision in space has a price.";
        Actualizar();
        RevisarFinDelJuego();
    }

    void RevisarFinDelJuego()
    {
        if (!GameState.AlgoSeAgoto()) return;
        if (GameState.valores[0] <= 0f) textoFin.text = "The crew ran out of food and oxygen. Real bases must grow and recycle both.";
        else if (GameState.valores[1] <= 0f) textoFin.text = "The base lost all its power. Without power nothing keeps people alive.";
        else textoFin.text = "The radiation shield failed. The crew had to leave the base.";
        panelFin.SetActive(true);
    }

    void ReiniciarTodo()
    {
        GameState.NuevaPartida();
        UIBuilder.IrA("Dashboard");
    }
}
