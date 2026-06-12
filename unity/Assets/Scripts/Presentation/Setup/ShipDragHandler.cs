// Navy.Presentation.Setup

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Navy.Presentation.Setup
{
    /// <summary>
    /// Ship drag handler. Attach to each ship icon GameObject in the setup panel.
    /// Implements IBeginDragHandler, IDragHandler, IEndDragHandler.
    /// </summary>
    public sealed class ShipDragHandler : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int Decks;   // Set in Inspector or by SetupPresenter

        private RectTransform _rectTransform;
        private Canvas        _canvas;
        private Vector3       _originalPosition;
        private Transform     _originalParent;
        private CanvasGroup   _canvasGroup;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas        = GetComponentInParent<Canvas>();
            _canvasGroup   = GetComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _originalPosition = _rectTransform.position;
            _originalParent   = transform.parent;

            // Bring to front
            transform.SetParent(_canvas.transform, true);
            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            _rectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_canvasGroup != null) _canvasGroup.blocksRaycasts = true;

            // SetupPresenter.OnShipDropped handles placement logic
            var presenter = FindObjectOfType<SetupPresenter>();
            presenter?.OnShipDropped(this, eventData);
        }

        public void ResetPosition()
        {
            transform.SetParent(_originalParent, true);
            _rectTransform.position = _originalPosition;
        }
    }
}
