using System;

namespace Game.Core.World
{
    /// <summary>
    /// Không đọc được Player Data từ NGUỒN CHÍNH (Cloud Save) nên không thể xác minh trạng thái.
    /// Dùng cho dữ liệu "fail-closed" như permadeath: không xác minh được thì KHÔNG cho vào game
    /// (docs/03 §12), thay vì lặng lẽ dùng file local rồi coi như "chưa chết".
    /// </summary>
    public class SaveUnavailableException : Exception
    {
        public string Key { get; }

        public SaveUnavailableException(string key, Exception inner)
            : base($"Không đọc được Player Data '{key}' từ nguồn chính.", inner)
        {
            Key = key;
        }
    }
}
