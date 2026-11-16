using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// el menu Lunar Base crea las cuatro escenas y las mete al build
public class SceneBuilder
{
    [MenuItem("Lunar Base/Create Scenes")]
    static void CrearEscenas()
    {
        Directory.CreateDirectory("Assets/Scenes");

        string[] nombres = { "Dashboard", "PipesGame", "DeflectorGame", "BatteryGame" };
        EditorBuildSettingsScene[] lista = new EditorBuildSettingsScene[nombres.Length];

        for (int i = 0; i < nombres.Length; i++)
        {
            var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject administrador = new GameObject("Manager");

            if (nombres[i] == "Dashboard") administrador.AddComponent<DashboardManager>();
            if (nombres[i] == "PipesGame") administrador.AddComponent<PipeGame>();
            if (nombres[i] == "DeflectorGame") administrador.AddComponent<DeflectorGame>();
            if (nombres[i] == "BatteryGame") administrador.AddComponent<BatteryGame>();

            string ruta = "Assets/Scenes/" + nombres[i] + ".unity";
            EditorSceneManager.SaveScene(escena, ruta);
            lista[i] = new EditorBuildSettingsScene(ruta, true);
        }

        EditorBuildSettings.scenes = lista;
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene("Assets/Scenes/Dashboard.unity");
        Debug.Log("Scenes ready. Press Play on the Dashboard scene.");
    }

    [MenuItem("Lunar Base/Reset Saved Progress")]
    static void BorrarProgreso()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Saved progress deleted.");
    }
}
