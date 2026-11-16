using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;

// los nombres de campos de estas clases tienen que ser iguales a los del JSON de la NASA
[System.Serializable]
public class DatosApod
{
    public string title;
    public string url;
    public string hdurl;
    public string media_type;
    public string explanation;
}

[System.Serializable]
public class BibliotecaRespuesta { public BibliotecaColeccion collection; }

[System.Serializable]
public class BibliotecaColeccion { public BibliotecaItem[] items; }

[System.Serializable]
public class BibliotecaItem
{
    public BibliotecaDato[] data;
    public BibliotecaEnlace[] links;
}

[System.Serializable]
public class BibliotecaDato
{
    public string title;
    public string description;
    public string nasa_id;
    public string[] keywords;
}

[System.Serializable]
public class BibliotecaEnlace { public string href; }

// muestra una foto de la NASA dentro del telescopio, si te quedas unos segundos la tripulacion descansa
public class TelescopePanel : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("apiKey")] string claveApi = "DEMO_KEY";
    [SerializeField, FormerlySerializedAs("restSeconds")] float segundosDescanso = 5f;
    [SerializeField, FormerlySerializedAs("restAmount")] float cantidadDescanso = 15f;
    [SerializeField, FormerlySerializedAs("cooldownSeconds")] float segundosEnfriamiento = 20f;

    public System.Action alCerrar;

    // medidas sacadas del dibujo del telescopio, la pantalla de referencia es de 1280 por 720
    const float DiametroLente = 515f;
    static readonly Vector2 PosicionLente = new Vector2(-224f, -4f);
    static readonly Vector2 PosicionCuadro = new Vector2(344f, 20f);
    static readonly Vector2 TamanoCuadro = new Vector2(325f, 456f);

    static float ultimoPremio = -999f;

    GameObject panel;
    RawImage foto;
    TextMeshProUGUI textoTitulo, textoInfo, textoDescanso;
    float tiempoMirando;
    bool premiado;

    public bool EstaAbierto
    {
        get { return panel != null && panel.activeSelf; }
    }

    public void Construir(Transform raiz)
    {
        // el dibujo del telescopio ocupa toda la pantalla y tapa el dashboard
        Image fondo = UIBuilder.Fondo(raiz, "telescope");
        fondo.raycastTarget = true;
        panel = fondo.gameObject;

        // el circulo recorta la foto para que quede dentro del lente
        Image circulo = UIBuilder.Caja(panel.transform, PosicionLente, new Vector2(DiametroLente, DiametroLente), Color.white);
        circulo.sprite = UIBuilder.CargarSprite("circle_mask");
        circulo.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform rectFoto = UIBuilder.CrearRect("Foto", circulo.transform, Vector2.zero, new Vector2(DiametroLente, DiametroLente));
        foto = rectFoto.gameObject.AddComponent<RawImage>();
        foto.color = new Color(0.035f, 0.01f, 0.12f);

        Image cuadro = UIBuilder.Caja(panel.transform, PosicionCuadro, TamanoCuadro, new Color(0.2f, 0.8f, 1f));
        Outline borde = cuadro.gameObject.AddComponent<Outline>();
        borde.effectColor = new Color(0.25f, 0.26f, 0.3f);
        borde.effectDistance = new Vector2(3, -3);

        Color letra = new Color(0.04f, 0.06f, 0.18f);
        textoTitulo = UIBuilder.Etiqueta(cuadro.transform, "", 22, new Vector2(0, 175), new Vector2(295, 90), TextAlignmentOptions.Center);
        textoTitulo.color = letra;
        textoTitulo.fontStyle = FontStyles.Bold;
        textoInfo = UIBuilder.Etiqueta(cuadro.transform, "", 17, new Vector2(0, -50), new Vector2(295, 310), TextAlignmentOptions.TopLeft);
        textoInfo.color = letra;
        textoInfo.enableAutoSizing = true;
        textoInfo.fontSizeMin = 11;
        textoInfo.fontSizeMax = 17;

        textoDescanso = UIBuilder.Etiqueta(panel.transform, "", 24, new Vector2(-224, -308), new Vector2(560, 44), TextAlignmentOptions.Center);
        textoDescanso.color = UIBuilder.dorado;
        UIBuilder.Boton(panel.transform, "CLOSE", new Vector2(344, -308), new Vector2(220, 50), UIBuilder.rojo, Cerrar, 20);
        panel.SetActive(false);
    }

    public void Abrir()
    {
        panel.SetActive(true);
        tiempoMirando = 0f;
        premiado = false;
        foto.texture = null;
        foto.rectTransform.sizeDelta = new Vector2(DiametroLente, DiametroLente);
        foto.color = new Color(0.035f, 0.01f, 0.12f);
        textoTitulo.text = "Calling NASA...";
        textoInfo.text = "";
        textoDescanso.text = "";
        StartCoroutine(Descargar());
    }

    void Cerrar()
    {
        StopAllCoroutines();
        panel.SetActive(false);
        if (alCerrar != null) alCerrar();
    }

    void Update()
    {
        if (!EstaAbierto) return;
        tiempoMirando += Time.deltaTime;

        if (premiado) return;
        if (tiempoMirando < segundosDescanso)
        {
            textoDescanso.text = "Resting... " + Mathf.CeilToInt(segundosDescanso - tiempoMirando);
            return;
        }

        premiado = true;
        if (Time.time - ultimoPremio >= segundosEnfriamiento)
        {
            ultimoPremio = Time.time;
            GameState.Sumar(Res.Descanso, cantidadDescanso);
            GameState.Guardar();
            textoDescanso.text = "The crew feels rested. Astro energy +" + Mathf.RoundToInt(cantidadDescanso);
        }
        else
        {
            textoDescanso.text = "The crew is already rested. Come back in a little while.";
        }
    }

    // primero intenta la foto del dia, si falla usa la biblioteca de imagenes de la NASA
    IEnumerator Descargar()
    {
        string urlFoto = "";
        string detalleError = "";

        string urlApod = "https://api.nasa.gov/planetary/apod?api_key=" + claveApi;
        using (UnityWebRequest peticion = UnityWebRequest.Get(urlApod))
        {
            peticion.timeout = 15;
            yield return peticion.SendWebRequest();
            if (peticion.result == UnityWebRequest.Result.Success)
            {
                DatosApod datos = JsonUtility.FromJson<DatosApod>(peticion.downloadHandler.text);
                if (datos != null && datos.media_type == "image" && !string.IsNullOrEmpty(datos.url) && !EsLogo(datos.title, datos.explanation, null, datos.url))
                {
                    urlFoto = datos.url;
                    textoTitulo.text = datos.title;
                    textoInfo.text = Acortar(datos.explanation, 420);
                }
            }
            else
            {
                detalleError = "APOD " + peticion.responseCode + " " + peticion.error;
            }
        }

        if (urlFoto == "")
        {
            string[] temas = { "nebula", "galaxy", "moon", "mars", "aurora" };
            string urlBiblioteca = "https://images-api.nasa.gov/search?media_type=image&q=" + temas[Random.Range(0, temas.Length)];
            using (UnityWebRequest peticion = UnityWebRequest.Get(urlBiblioteca))
            {
                peticion.timeout = 15;
                yield return peticion.SendWebRequest();
                if (peticion.result == UnityWebRequest.Result.Success)
                {
                    BibliotecaRespuesta respuesta = JsonUtility.FromJson<BibliotecaRespuesta>(peticion.downloadHandler.text);
                    if (respuesta != null && respuesta.collection != null && respuesta.collection.items != null && respuesta.collection.items.Length > 0)
                    {
                        // se juntan solo los resultados buenos y se descartan los logos
                        List<BibliotecaItem> validos = new List<BibliotecaItem>();
                        int tope = Mathf.Min(40, respuesta.collection.items.Length);
                        for (int i = 0; i < tope; i++)
                        {
                            BibliotecaItem candidato = respuesta.collection.items[i];
                            if (candidato.links == null || candidato.links.Length == 0) continue;
                            if (candidato.data == null || candidato.data.Length == 0) continue;
                            BibliotecaDato d = candidato.data[0];
                            if (EsLogo(d.title, d.description, d.keywords, d.nasa_id + " " + candidato.links[0].href)) continue;
                            validos.Add(candidato);
                        }

                        if (validos.Count > 0)
                        {
                            BibliotecaItem item = validos[Random.Range(0, validos.Count)];
                            urlFoto = item.links[0].href;
                            textoTitulo.text = item.data[0].title;
                            textoInfo.text = Acortar(item.data[0].description, 420);
                        }
                    }
                }
                else
                {
                    detalleError += "   LIBRARY " + peticion.responseCode + " " + peticion.error;
                }
            }
        }

        if (urlFoto == "")
        {
            textoTitulo.text = "No signal";
            textoInfo.text = "Could not reach NASA. You can still rest here. " + detalleError;
            yield break;
        }

        // Unity bloquea las direcciones http sin candado, asi que se pasan a https
        urlFoto = urlFoto.Replace("http://", "https://");

        using (UnityWebRequest peticionFoto = UnityWebRequestTexture.GetTexture(urlFoto))
        {
            peticionFoto.timeout = 30;
            yield return peticionFoto.SendWebRequest();
            if (peticionFoto.result != UnityWebRequest.Result.Success)
            {
                textoInfo.text = "The photo could not be loaded. " + peticionFoto.responseCode + " " + peticionFoto.error;
                yield break;
            }

            Texture2D textura = DownloadHandlerTexture.GetContent(peticionFoto);
            foto.texture = textura;
            foto.color = Color.white;
            // la foto se agranda hasta cubrir el circulo y lo que sobra se recorta
            float escala = Mathf.Max(DiametroLente / textura.width, DiametroLente / textura.height);
            foto.rectTransform.sizeDelta = new Vector2(textura.width * escala, textura.height * escala);
        }
    }

    // revisa titulo, descripcion y palabras clave para no mostrar logos ni emblemas de la NASA
    bool EsLogo(string titulo, string descripcion, string[] palabrasClave, string extra)
    {
        string todo = (titulo + " " + descripcion + " " + extra).ToLower();
        if (palabrasClave != null) todo += " " + string.Join(" ", palabrasClave).ToLower();

        string[] prohibidas = { "logo", "insignia", "meatball", "emblem", "nasa seal", "wordmark" };
        foreach (string palabra in prohibidas)
        {
            if (todo.Contains(palabra)) return true;
        }
        return !string.IsNullOrEmpty(titulo) && titulo.Trim().ToLower() == "nasa";
    }

    string Acortar(string texto, int maximo)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        if (texto.Length <= maximo) return texto;
        return texto.Substring(0, maximo) + "...";
    }
}
