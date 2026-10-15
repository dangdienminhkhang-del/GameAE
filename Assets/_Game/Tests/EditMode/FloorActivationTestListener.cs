using System;
using Game.Core.Floor;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Listener thường (không phải MonoBehaviour) ghi lại số lần được thông báo.
    /// Không dùng MonoBehaviour vì assembly test chỉ chạy trong Editor nên Unity không cho AddComponent.
    /// </summary>
    public class FloorActivationTestListener : IFloorActivationListener
    {
        public int ActivatedCount;
        public int DeactivatedCount;
        public FloorDefinition LastDefinition;

        /// <summary>Nếu đặt, listener ném lỗi này khi được thông báo (để test cách ly lỗi).</summary>
        public Exception ThrowOnNotify;

        public void OnFloorActivated(FloorDefinition def)
        {
            ActivatedCount++;
            LastDefinition = def;
            if (ThrowOnNotify != null) throw ThrowOnNotify;
        }

        public void OnFloorDeactivated()
        {
            DeactivatedCount++;
            if (ThrowOnNotify != null) throw ThrowOnNotify;
        }
    }
}
