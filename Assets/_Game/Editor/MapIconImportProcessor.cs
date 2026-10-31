using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Tự đặt thông số nhập cho icon bản đồ khi bạn thả <c>Assets/_Game/Resources/UISkin/Icon_Map.png</c> vào project:
    /// Sprite đơn, lọc Point, không nén, không mipmap (đúng docs/14: pixel art không được nhoè).
    /// Chỉ áp dụng cho đúng file này. Không cần chạy menu hay sửa scene: MapHudController tự nạp icon qua Resources.
    /// </summary>
    public sealed class MapIconImportProcessor : AssetPostprocessor
    {
        public const string IconPath = "Assets/_Game/Resources/UISkin/Icon_Map.png";

        private void OnPreprocessTexture()
        {
            if (assetPath != IconPath) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 32f;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
