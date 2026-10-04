using Game.Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI.Controls
{
    [RequireComponent(typeof(Image))]
    public class OnScreenAttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private MonoBehaviour inputReaderComponent;
        private IInputReader _inputReader;

        private void Awake()
        {
            if (inputReaderComponent is IInputReader reader)
            {
                _inputReader = reader;
            }
        }

        public void AssignInputReader(IInputReader reader)
        {
            _inputReader = reader;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _inputReader?.SetAttackInput(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _inputReader?.SetAttackInput(false);
        }

        private void OnDisable()
        {
            _inputReader?.SetAttackInput(false);
        }
    }
}
