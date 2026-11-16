using UnityEngine;

public enum Res { Comida = 0, Electricidad = 1, Radiacion = 2, Descanso = 3 }

// aqui viven los datos que tienen que durar entre escenas
public static class GameState
{
    public static float[] valores = new float[] { 70f, 70f, 70f, 70f };
    public static int[] superados = new int[3];
    public static int[] nivelesMaximos = new int[] { 5, 5, 5 };
    public static Res[] recursoPremio = new Res[] { Res.Comida, Res.Radiacion, Res.Electricidad };
    public static string[] nombresEscenas = new string[] { "PipesGame", "DeflectorGame", "BatteryGame" };
    public static string[] nombresRecursos = new string[] { "Food and Oxygen", "Power", "Radiation Shield", "Astro Energy" };

    public static int dia = 1;
    public static int juegoActivo = 0;
    public static string ultimoMensaje = "";
    public const float CostoJuego = 10f;

    static bool cargado = false;

    // las claves de PlayerPrefs se quedan igual para no perder partidas viejas
    public static void CargarSiHaceFalta()
    {
        if (cargado) return;
        cargado = true;
        valores[0] = PlayerPrefs.GetFloat("res_food", 70f);
        valores[1] = PlayerPrefs.GetFloat("res_power", 70f);
        valores[2] = PlayerPrefs.GetFloat("res_radiation", 70f);
        valores[3] = PlayerPrefs.GetFloat("res_energy", 70f);
        for (int i = 0; i < superados.Length; i++)
        {
            superados[i] = PlayerPrefs.GetInt("cleared_" + i, 0);
        }
        dia = PlayerPrefs.GetInt("day", 1);
    }

    public static void Guardar()
    {
        PlayerPrefs.SetFloat("res_food", valores[0]);
        PlayerPrefs.SetFloat("res_power", valores[1]);
        PlayerPrefs.SetFloat("res_radiation", valores[2]);
        PlayerPrefs.SetFloat("res_energy", valores[3]);
        for (int i = 0; i < superados.Length; i++)
        {
            PlayerPrefs.SetInt("cleared_" + i, superados[i]);
        }
        PlayerPrefs.SetInt("day", dia);
        PlayerPrefs.Save();
    }

    public static void Sumar(Res recurso, float cantidad)
    {
        int i = (int)recurso;
        valores[i] = Mathf.Clamp(valores[i] + cantidad, 0f, 100f);
    }

    public static float Obtener(Res recurso)
    {
        return valores[(int)recurso];
    }

    // el descanso no esta aqui a proposito, una tripulacion cansada no acaba la partida
    public static bool AlgoSeAgoto()
    {
        return valores[0] <= 0f || valores[1] <= 0f || valores[2] <= 0f;
    }

    public static void NuevaPartida()
    {
        for (int i = 0; i < valores.Length; i++) valores[i] = 70f;
        for (int i = 0; i < superados.Length; i++) superados[i] = 0;
        dia = 1;
        ultimoMensaje = "";
        Guardar();
    }

    public static int NivelActual(int juego)
    {
        return Mathf.Min(superados[juego] + 1, nivelesMaximos[juego]);
    }

    // lo llama cada minijuego cuando termina
    public static string TerminarJuego(int juego, bool gano, int nivel)
    {
        string mensaje;
        if (gano)
        {
            if (nivel > superados[juego]) superados[juego] = nivel;
            float premio = 18f + nivel * 3f;
            Res recurso = recursoPremio[juego];
            Sumar(recurso, premio);
            mensaje = "Mission complete! " + nombresRecursos[(int)recurso] + " went up by " + Mathf.RoundToInt(premio) + ".";
        }
        else
        {
            mensaje = "The mission failed. The crew is tired and nothing was recovered.";
        }
        ultimoMensaje = mensaje;
        Guardar();
        return mensaje;
    }
}
