using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// minijuego dos, atrapa la energia verde y esquiva las llamaradas rojas
public class DeflectorGame : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("gameFont")] TMP_FontAsset fuenteJuego;
    [SerializeField, FormerlySerializedAs("shipSpeed")] float velocidadNave = 520f;
    [SerializeField, FormerlySerializedAs("startLives")] int vidasIniciales = 3;

    class Cayente
    {
        public RectTransform rect;
        public bool esBueno;
        public float velocidad;
        public float tamano;
    }

    const int IdJuego = 1;
    const float AnchoNave = 96f;
    const float AltoNave = 32f;

    List<Cayente> objetos = new List<Cayente>();
    RectTransform campo, raizObjetos, nave;
    Image imagenNave;
    Slider progreso;
    TextMeshProUGUI textoVidas;
    Transform raiz;

    int nivel, meta, atrapadas, vidas;
    float temporizadorAparicion, intervalo, caidaBase, probLlamarada, temporizadorParpadeo;
    bool terminado;

    void Start()
    {
        GameState.CargarSiHaceFalta();
        UIBuilder.fuente = fuenteJuego;
        nivel = GameState.NivelActual(IdJuego);

        meta = 8 + nivel * 2;
        intervalo = Mathf.Max(0.35f, 0.95f - 0.1f * nivel);
        caidaBase = 200f + 45f * nivel;
        probLlamarada = 0.3f + 0.05f * nivel;
        vidas = vidasIniciales;

        Canvas canvas = UIBuilder.CrearCanvas();
        raiz = canvas.transform;
        campo = canvas.GetComponent<RectTransform>();
        UIBuilder.Fondo(raiz, "bg_deflector");
        // franja oscura para que las letras se lean sobre la parte clara del cielo
        UIBuilder.Caja(raiz, new Vector2(0, 320), new Vector2(1400, 76), new Color(0f, 0f, 0f, 0.45f));

        raizObjetos = UIBuilder.CrearRect("Objetos", raiz, Vector2.zero, Vector2.zero);
        UIBuilder.Estirar(raizObjetos);

        imagenNave = UIBuilder.Dibujo(raiz, "deflector", Vector2.zero, new Vector2(AnchoNave, AltoNave));
        nave = imagenNave.rectTransform;

        UIBuilder.Encabezado(raiz, "DEFLECTOR  LEVEL " + nivel, SalirAntes);
        progreso = UIBuilder.Barra(raiz, new Vector2(170, 320), new Vector2(340, 24), new Color(0.3f, 0.85f, 0.45f));
        textoVidas = UIBuilder.Etiqueta(raiz, "", 26, new Vector2(510, 320), new Vector2(240, 40), TextAlignmentOptions.Right);
        UIBuilder.Etiqueta(raiz, "Arrows or mouse to move. Flares throw out fast charged particles. A magnetic field can bend them away, like the one around Earth.", 18, new Vector2(0, -335), new Vector2(1150, 40), TextAlignmentOptions.Center);
        ActualizarHud();
    }

    void Update()
    {
        if (terminado) return;
        float dt = Time.deltaTime;
        float mitadAncho = campo.rect.width / 2f;
        float mitadAlto = campo.rect.height / 2f;

        MoverNave(dt, mitadAncho, mitadAlto);

        temporizadorAparicion -= dt;
        if (temporizadorAparicion <= 0f)
        {
            Aparecer(mitadAncho, mitadAlto);
            temporizadorAparicion = intervalo;
        }

        MoverObjetos(dt, mitadAlto);

        if (temporizadorParpadeo > 0f)
        {
            temporizadorParpadeo -= dt;
            imagenNave.color = new Color(1f, 0.4f, 0.4f);
        }
        else
        {
            imagenNave.color = Color.white;
        }
    }

    void MoverNave(float dt, float mitadAncho, float mitadAlto)
    {
        float x = nave.anchoredPosition.x;
        x += GameInput.Horizontal() * velocidadNave * dt;

        if (GameInput.MouseMantenido())
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(campo, GameInput.PosicionMouse(), null, out local);
            x = Mathf.MoveTowards(x, local.x, velocidadNave * 1.5f * dt);
        }

        x = Mathf.Clamp(x, -mitadAncho + AnchoNave / 2f, mitadAncho - AnchoNave / 2f);
        nave.anchoredPosition = new Vector2(x, -mitadAlto + 70f);
    }

    void Aparecer(float mitadAncho, float mitadAlto)
    {
        Cayente c = new Cayente();
        c.esBueno = Random.value > probLlamarada;
        c.tamano = c.esBueno ? 34f : 42f;
        c.velocidad = caidaBase + Random.Range(-30f, 60f);

        Image imagen = UIBuilder.Dibujo(raizObjetos, c.esBueno ? "energy_green" : "flare_red", Vector2.zero, new Vector2(c.tamano, c.tamano));
        c.rect = imagen.rectTransform;
        c.rect.anchoredPosition = new Vector2(Random.Range(-mitadAncho + 40f, mitadAncho - 40f), mitadAlto + 40f);
        objetos.Add(c);
    }

    void MoverObjetos(float dt, float mitadAlto)
    {
        Vector2 posNave = nave.anchoredPosition;
        for (int i = objetos.Count - 1; i >= 0; i--)
        {
            Cayente c = objetos[i];
            Vector2 p = c.rect.anchoredPosition;
            p.y -= c.velocidad * dt;
            c.rect.anchoredPosition = p;

            float dx = Mathf.Abs(p.x - posNave.x);
            float dy = Mathf.Abs(p.y - posNave.y);
            bool tocando = dx < (AnchoNave + c.tamano) / 2f - 6f && dy < (AltoNave + c.tamano) / 2f - 4f;

            if (tocando)
            {
                bool eraBueno = c.esBueno;
                Destroy(c.rect.gameObject);
                objetos.RemoveAt(i);
                if (eraBueno) atrapadas++;
                else
                {
                    vidas--;
                    temporizadorParpadeo = 0.25f;
                }
                ActualizarHud();
                if (atrapadas >= meta) Terminar(true);
                else if (vidas <= 0) Terminar(false);
                if (terminado) return;
            }
            else if (p.y < -mitadAlto - 60f)
            {
                Destroy(c.rect.gameObject);
                objetos.RemoveAt(i);
            }
        }
    }

    void ActualizarHud()
    {
        progreso.value = 100f * atrapadas / meta;
        textoVidas.text = "SHIELD " + vidas;
    }

    void Terminar(bool gano)
    {
        if (terminado) return;
        terminado = true;
        string mensaje = GameState.TerminarJuego(IdJuego, gano, nivel);
        UIBuilder.Ventana(raiz, gano ? "SHIELD CHARGED" : "SHIELD DOWN", mensaje, "CONTINUE", () => UIBuilder.IrA("Dashboard"));
    }

    void SalirAntes()
    {
        GameState.ultimoMensaje = "You left the mission early.";
        GameState.Guardar();
        UIBuilder.IrA("Dashboard");
    }
}
