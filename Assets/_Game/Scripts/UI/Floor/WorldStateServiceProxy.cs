using Game.Core.World;
using UnityEngine;

namespace Game.UI.Floor
{
    /// <summary>
    /// MonoBehaviour proxy cho IWorldStateService.
    /// Dùng để gán cài đặt thật (WorldStateService) vào Inspector của FloorSelectorUI
    /// mà không vi phạm nguyên tắc "Core không biết Services".
    ///
    /// Trong Unity Editor:
    ///   1. Gắn component này lên GameObject trong scene.
    ///   2. Gán WorldStateService (hoặc implementation khác) vào field 'service'.
    ///   3. Gán WorldStateServiceProxy này vào FloorSelectorUI.worldStateProxy.
    /// </summary>
    public class WorldStateServiceProxy : MonoBehaviour
    {
        [Tooltip("Kéo WorldStateService component vào đây")]
        [SerializeField] private MonoBehaviour service;

        private IWorldStateService _cached;

        /// <summary>IWorldStateService implementation đang được dùng.</summary>
        public IWorldStateService Service
        {
            get
            {
                if (_cached == null)
                    _cached = service as IWorldStateService;
                return _cached;
            }
        }

        private void Awake()
        {
            if (service == null || !(service is IWorldStateService))
            {
                Debug.LogError("[Floor] WorldStateServiceProxy: 'service' phải là MonoBehaviour implement IWorldStateService!");
            }
        }
    }
}
