using UnityEngine;

namespace GameSettings
{
    /// <summary>
    /// Danh sách các ảnh tâm súng (PNG) mà người chơi có thể chọn trong Cài đặt.
    ///
    /// CÁCH TẠO: chuột phải trong Project -> Create -> Settings -> Crosshair Library,
    /// rồi kéo các ảnh PNG (đã chuyển Texture Type = Sprite) vào danh sách "Sprites".
    ///
    /// Cả CrosshairUI (tâm súng trong game) lẫn GameplaySettingsUI (panel Cài đặt) cùng
    /// tham chiếu ĐÚNG 1 asset này, nên thêm ảnh vào đây là cả hai cùng thấy.
    /// Lưu ý: người chơi lưu lựa chọn theo VỊ TRÍ (số thứ tự) trong danh sách, nên khi
    /// cập nhật game hãy thêm ảnh mới vào CUỐI danh sách, đừng chèn giữa hoặc xóa ảnh cũ.
    /// </summary>
    [CreateAssetMenu(fileName = "CrosshairLibrary", menuName = "Settings/Crosshair Library")]
    public class CrosshairLibrary : ScriptableObject
    {
        public Sprite[] sprites;

        public int Count => sprites == null ? 0 : sprites.Length;

        /// <summary>Lấy ảnh theo số thứ tự, trả về null nếu số thứ tự không hợp lệ.</summary>
        public Sprite Get(int index)
        {
            if (sprites == null || index < 0 || index >= sprites.Length) return null;
            return sprites[index];
        }
    }
}