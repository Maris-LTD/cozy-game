using System.Collections.Generic;
using CozyGachapon.Contracts;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CozyGachapon
{
    public sealed class EventSystemUiHitTester : MonoBehaviour, IUiHitTester
    {
        [SerializeField] private EventSystem _eventSystem;

        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(8);
        private PointerEventData _pointerEventData;

        public bool HitsUi(float screenX, float screenY, int pointerId)
        {
            if (_eventSystem == null)
            {
                return false;
            }

            if (_pointerEventData == null)
            {
                _pointerEventData = new PointerEventData(_eventSystem);
            }

            _pointerEventData.position = new Vector2(screenX, screenY);
            _pointerEventData.pointerId = pointerId;
            _raycastResults.Clear();
            _eventSystem.RaycastAll(_pointerEventData, _raycastResults);
            return _raycastResults.Count > 0;
        }
    }
}
