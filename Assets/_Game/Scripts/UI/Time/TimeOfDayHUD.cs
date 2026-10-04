using Game.Core.Events;
using Game.Core.Time;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Time
{
    /// <summary>
    /// Đồng hồ nhỏ trên HUD (docs/08 §2.2): "Ngày 1 · 08:30 · Buổi sáng".
    /// Chỉ hiển thị — thời gian do WorldClock (PvE) quản lý.
    /// </summary>
    public class TimeOfDayHUD : MonoBehaviour
    {
        [SerializeField] private Text bodyText;

        private int _lastMinute = -1;

        private void OnEnable()
        {
            GameEvents.OnTimeChanged += HandleTimeChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnTimeChanged -= HandleTimeChanged;
        }

        private void HandleTimeChanged(object sender, TimeChangedArgs args)
        {
            int minute = (int)(args.GameHours * 60.0);
            if (minute == _lastMinute) return;
            _lastMinute = minute;

            if (bodyText == null) return;

            int hours = (int)args.GameHours;
            int minutes = (int)((args.GameHours - hours) * 60.0);
            bodyText.text = $"Ngày {args.Day} · {hours:00}:{minutes:00} · {PhaseName(args.Phase)}";
        }

        public static string PhaseName(TimeOfDay phase)
        {
            switch (phase)
            {
                case TimeOfDay.Night: return "Đêm";
                case TimeOfDay.Dawn: return "Sáng sớm";
                case TimeOfDay.Morning: return "Buổi sáng";
                case TimeOfDay.Noon: return "Buổi trưa";
                case TimeOfDay.Afternoon: return "Buổi chiều";
                case TimeOfDay.Evening: return "Chạng vạng";
                default: return phase.ToString();
            }
        }
    }
}
