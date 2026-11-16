using UnityEditor;
using UnityEngine;

// deja nitidos todos los png de Resources Sprites y los prepara como sprites
public class SpriteImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("Resources/Sprites")) return;

        TextureImporter importador = (TextureImporter)assetImporter;
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spritePixelsPerUnit = 16;
        importador.filterMode = FilterMode.Point;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.mipmapEnabled = false;
    }
}
