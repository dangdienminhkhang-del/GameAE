using Game.Core.Events;
using Game.Core.Time;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Time
{
    /// <summary>
    /// Đồng hồ nhỏ trên HUD (docs/08 §2.2).
    /// - Có subText: bodyText hiện giờ ("21:15"), subText hiện "Ngày 1 · Đêm" (bố cục kiểu SAO).
    /// - Không có subText: bodyText hiện gộp "Ngày 1 · 08:30 · Buổi sáng" (tương thích cũ).
    /// Chỉ hiển thị — thời gian do WorldClock (PvE) quản lý.
    /// </summary>
    public class TimeOfDayHUD : MonoBehaviour
    {
        [SerializeField] private Text bodyText;
        [Tooltip("Dòng phụ 'Ngày N · Pha'. Để trống = gộp tất cả vào bodyText.")]
        [SerializeField] private Text subText;
        [SerializeField] private Image phaseIcon;
        [SerializeField] private Sprite dawnIcon;
        [SerializeField] private Sprite morningIcon;
        [SerializeField] private Sprite noonIcon;
        [SerializeField] private Sprite afternoonIcon;
        [SerializeField] private Sprite eveningIcon;
        [SerializeField] private Sprite nightIcon;

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

            int hours = (int)args.GameHours;
            int minutes = (int)((args.GameHours - hours) * 60.0);

            if (subText != null)
            {
                if (bodyText != null) bodyText.text = $"{hours:00}:{minutes:00}";
                subText.text = $"Ngày {args.Day} · {PhaseName(args.Phase)}";
            }
            else if (bodyText != null)
            {
                bodyText.text = $"Ngày {args.Day} · {hours:00}:{minutes:00} · {PhaseName(args.Phase)}";
            }

            if (phaseIcon != null)
            {
                var sp = GetPhaseSprite(args.Phase);
                phaseIcon.sprite = sp;
                phaseIcon.gameObject.SetActive(sp != null);
            }
        }

        public Sprite GetPhaseSprite(TimeOfDay phase)
        {
            switch (phase)
            {
                case TimeOfDay.Dawn: return dawnIcon;
                case TimeOfDay.Morning: return morningIcon;
                case TimeOfDay.Noon: return noonIcon;
                case TimeOfDay.Afternoon: return afternoonIcon;
                case TimeOfDay.Evening: return eveningIcon;
                case TimeOfDay.Night: return nightIcon;
                default: return null;
            }
        }

        public void Configure(Text text, Image icon, Text sub = null)
        {
            bodyText = text;
            phaseIcon = icon;
            subText = sub;
        }

        public void SetPhaseSprites(Sprite dawn, Sprite morning, Sprite noon, Sprite afternoon, Sprite evening, Sprite night)
        {
            dawnIcon = dawn;
            morningIcon = morning;
            noonIcon = noon;
            afternoonIcon = afternoon;
            eveningIcon = evening;
            nightIcon = night;
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
