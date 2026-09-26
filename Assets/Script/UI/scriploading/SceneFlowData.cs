namespace GameCore
{
    /// <summary>
    /// Nơi lưu tạm TÊN SCENE cần load tiếp theo, dùng để "truyền" thông tin
    /// từ Scene Video/Intro sang Scene Loading (vì 2 scene không thể gọi thẳng
    /// hàm của nhau — cần 1 nơi trung gian static để lưu tạm).
    ///
    /// Lưu ý: biến static sẽ MẤT dữ liệu nếu thoát hẳn game, nhưng vẫn giữ nguyên
    /// khi chuyển qua lại giữa các scene trong cùng 1 lần chạy game — đúng nhu cầu ở đây.
    /// </summary>
    public static class SceneFlowData
    {
        public static string NextSceneName;
    }
}