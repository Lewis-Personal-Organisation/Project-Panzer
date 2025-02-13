using System;
using ElRaccoone.Tweens;
using ElRaccoone.Tweens.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Interface.Elements.Scripts
{
    public class ToggleUI : Toggle
    {
        /// <summary>
        /// The animation time for handle
        /// </summary>
        private const float HandleDuration = 0.4f;
        
        /// <summary>
        /// The animation time for transition between color states
        /// </summary>
        private const float TransitionDuration = 0.15f;

        private bool isDragging;
        private float startingX;
        private RectTransform rect;

        [Tooltip("The background color when ON")]
        public Color onColor = Color.white;
        [Tooltip("The background color when OFF")]
        public Color offColor = Color.gray;

        [Space]

        [Tooltip("The background image")]
        public Image background;
        [Tooltip("The outline image")]
        public Image outline;
        [Tooltip("The highlighter image that will move left or right")]
        public Image highlighter;

        [Space]

        [Tooltip("The text shown when ON")]
        public Text onText;
        [Tooltip("The image shown when ON")]
        public Image onImage;
        
        [Space]

        [Tooltip("The text shown when OFF")]
        public Text offText;
        [Tooltip("The image shown when OFF")]
        public Image offImage;

        [Space] 
        
        [Tooltip("The highlighter on the left signifies ON")] 
        public bool leftIsOn;


#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            background.color = onColor;
            outline.color = onColor;
        }
#endif
        

        protected override void Start()
        {
            base.Start();

            if (Application.isPlaying)
            {
                rect = GetComponent<RectTransform>();
                startingX = highlighter.transform.localPosition.x;
                onValueChanged.AddListener(SetValue);
                Press();
            }
        }

        private void Update()
        {
            if (isDragging) Drag();
        }

        private void SetValue(bool on)
        {
            isOn = on;
            Press();
        }

        /// <summary>
        /// Action called when user is dragging the toggle
        /// </summary>
        private void Drag()
        {
            var clamp = rect.rect.width / 4;

            var pos = highlighter.transform.localPosition;
            if (pos.x > Math.Abs(clamp))
                return;

            var dragPos = Input.mousePosition.x - transform.position.x;
            dragPos = Mathf.Clamp(dragPos, -clamp, clamp);
            highlighter.transform.localPosition = new Vector3(dragPos, pos.y, pos.z);

            if (leftIsOn)
            {
                isOn = dragPos < startingX;
            }
            else
            {
                isOn = dragPos > startingX;
            }
        }

        public void BeginDrag()
        {
            isDragging = true;
        }

        public void EndDrag()
        {
            isDragging = false;
            Press();
        }

        /// <summary>
        /// Action called when the user pressed the toggle
        /// </summary>
        public void Press()
        {
            if (!interactable) return;

            if (isDragging) return;

            
            var width = rect.rect.width / 4;
            float to;
            
            if (isOn)
            {
                if (leftIsOn)
                {
                    to = startingX - width;
                }
                else
                {
                    to = startingX + width;
                }
            }
            else
            {
                if (leftIsOn)
                {
                    to = startingX + width;
                }
                else
                {
                    to = startingX - width;
                }
            }
            
            highlighter.TweenLocalPositionX(to, HandleDuration).SetEase(EaseType.ExpoInOut);

            if (isOn)
            {
                background.TweenGraphicColor(onColor, TransitionDuration);
                onText.TweenGraphicAlpha(1, TransitionDuration);
                onImage.TweenGraphicAlpha(1, TransitionDuration);
                offText.TweenGraphicAlpha(0, TransitionDuration);
                offImage.TweenGraphicAlpha(0, TransitionDuration);
            }
            else
            {
                background.TweenGraphicColor(offColor, TransitionDuration);
                onText.TweenGraphicAlpha(0, TransitionDuration);
                onImage.TweenGraphicAlpha(0, TransitionDuration);
                offText.TweenGraphicAlpha(1, TransitionDuration);
                offImage.TweenGraphicAlpha(1, TransitionDuration);
            }
        }
    }
}