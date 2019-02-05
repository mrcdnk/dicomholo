using System;
using HoloToolkit.Unity.InputModule;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Cursor = HoloToolkit.Unity.InputModule.Cursor;

namespace DICOMViews
{
    [RequireComponent(typeof(RectTransform))]
    public class PixelClickHandler : MonoBehaviour, IPointerClickHandler, IInputClickHandler
    {
        private Cursor _cursor;
        private Camera _mainCamera;
        private RectTransform _rectTransform;
        private Slice2DView _slice2DView;

        public PixelClicked PixelClick = new PixelClicked();

        // Start is called before the first frame update
        void Start()
        {
            _cursor = GameObject.FindGameObjectWithTag("HoloCursor").GetComponent<Cursor>();
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            _rectTransform = GetComponent<RectTransform>();
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Vector2 position;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(),
                eventData.pressPosition, _mainCamera, out position);

            OnPixelSelected(position);
        }

        public void OnInputClicked(InputClickedEventData eventData)
        {
            if (!_cursor)
            {
                return;
            }

            Vector2 position;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, _mainCamera.WorldToScreenPoint(_cursor.Position), _mainCamera, out position);

            OnPixelSelected(position);
        }

        private void OnPixelSelected(Vector2 textureSpace)
        {
            Vector2 max = _rectTransform.rect.max;
            Vector2 min = _rectTransform.rect.min;

            float xRange = max.x - min.x;
            float yRange = max.y - min.y;

            var xCur = textureSpace.x - min.x;
            var yCur = textureSpace.y - min.y;

            xCur = xCur / xRange;
            yCur = yCur / yRange;

            PixelClick.Invoke(xCur, yCur);
        }

        public class PixelClicked : UnityEvent<float, float> { }
    }
}
