using DICOMViews.Events;
using HoloToolkit.Unity.InputModule;
using UnityEngine;
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

        public PixelClicked OnPixelClick = new PixelClicked();

#if UNITY_EDITOR
        private bool _clicked = false;
#endif

        // Start is called before the first frame update
        private void Start()
        {
            _cursor = GameObject.FindGameObjectWithTag("HoloCursor").GetComponent<Cursor>();
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
            _rectTransform = GetComponent<RectTransform>();
        }


        /// <inheritdoc />
        public void OnPointerClick(PointerEventData eventData)
        {
            Vector2 position;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(),
                eventData.pressPosition, _mainCamera, out position);
#if UNITY_EDITOR
            if (_clicked)
            {
                _clicked = false;
                return;
            }

            _clicked = true;
#endif

            OnPixelSelected(position);
        }

        /// <inheritdoc />
        public void OnInputClicked(InputClickedEventData eventData)
        {
            if (!_cursor)
            {
                return;
            }

            Vector2 position;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, _mainCamera.WorldToScreenPoint(_cursor.Position), _mainCamera, out position);
#if UNITY_EDITOR
            if (_clicked)
            {
                _clicked = false;
                return;
            }

            _clicked = true;
#endif

            OnPixelSelected(position);
        }

        /// <summary>
        /// Invokes the pixel clicked event with the correct coordinates.
        /// </summary>
        /// <param name="textureSpace"></param>
        private void OnPixelSelected(Vector2 textureSpace)
        {
            var max = _rectTransform.rect.max;
            var min = _rectTransform.rect.min;

            var xRange = max.x - min.x;
            var yRange = max.y - min.y;

            float xCur = textureSpace.x - min.x;
            float yCur = textureSpace.y - min.y;

            xCur = xCur / xRange;
            yCur = yCur / yRange;

            OnPixelClick.Invoke(xCur, yCur);
        }

    }
}
