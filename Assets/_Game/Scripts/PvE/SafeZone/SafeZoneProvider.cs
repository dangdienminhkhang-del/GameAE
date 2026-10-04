using Game.Core.Character;
using UnityEngine;

namespace Game.PvE.SafeZone
{
    /// <summary>
    /// Một SafeZoneService dùng chung trong scene PvE. Static nên PHẢI reset mỗi lần vào Play
    /// (Enter Play Mode Options có thể tắt domain reload).
    /// </summary>
    public static class SafeZoneProvider
    {
        private static SafeZoneService _service;

        public static SafeZoneService GetOrCreate()
        {
            if (_service == null) _service = new SafeZoneService();
            return _service;
        }

        public static void Reset()
        {
            _service?.Reset();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            _service = null;
        }
    }
}
