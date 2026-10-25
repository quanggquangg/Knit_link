// Import settings for the artist's UI art in Resources/Skin (added 25/10). The PNG files themselves are never changed;
// only their .meta import settings are set here:
//   readable (Game/Skin.cs builds the rope, centred balls and bag front from the pixels), mipmaps (smooth when drawn small),
//   no compression (crisp UI, exact alpha), clamp, max size 2048.
// Folder "Demo" (full-screen mockups) is reference only: keep it OUT of Resources, everything in Resources ships in the build.
using UnityEditor;
using UnityEngine;

namespace KnitLink.EditorTools
{
    public class SkinImporter : AssetPostprocessor
    {
        const string Folder = "Assets/KnitLink/Resources/Skin/";

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(Folder)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.isReadable = true;
            ti.mipmapEnabled = true;   // smooth edges when the art is drawn smaller than its pixels (25/10: jagged buttons without)
            ti.mipmapFilter = TextureImporterMipFilter.BoxFilter;   // Box, not Kaiser: Kaiser sharpens and drew a light halo round the baskets
            ti.borderMipmap = false;
            ti.alphaIsTransparency = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Trilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = 2048;
        }

        [MenuItem("Knit Link/Reimport Skin Art")]
        static void Reimport()
        {
            AssetDatabase.ImportAsset(Folder.TrimEnd('/'), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("[Skin] Skin art reimported");
        }
    }
}
