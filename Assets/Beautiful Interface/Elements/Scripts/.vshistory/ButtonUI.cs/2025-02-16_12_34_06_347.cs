using System;
using System.Collections.Generic;
using ElRaccoone.Tweens;
using ElRaccoone.Tweens.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Interface.Elements.Scripts
{
    public class ButtonUI : Button
    {
        /// <summary>
        /// The original position of the Text (init. before animating)
        /// </summary>
        private Vector3 originalTextPos;

        private bool persistHighlight;
        
        /// <summary>
        /// The duration for each tween animation
        /// </summary>
        public float duration = 0.15f;
        
        /// <summary>
        /// Stay highlighted
        /// </summary>
        public bool PersistHighlight
        {
            get => persistHighlight;
            set
            {
                persistHighlight = value;
                if (value)
                    Highlight();
                else Normal();
            }
        }

        /// <summary>
        /// Represents whether the state is not in its normal state and therefore resetable
        /// </summary>
        public bool StateResetable = false;


        #region Has Booleans

        /// <summary>
        /// The button has slider effect
        /// </summary>
        public bool hasSlider;
        
        /// <summary>
        /// The button has on mouse hover sound
        /// </summary>
        public bool hasHoverSound;
        
        /// <summary>
        /// The button has on mouse click sound
        /// </summary>
        public bool hasClickSound;

        #endregion


        #region Normal

        /// <summary>
        /// The button state when normal
        /// </summary>
        public List<ButtonState> normalStates;

        #endregion


        #region On Highlight

        /// <summary>
        /// The button state when highlighted
        /// </summary>
        public List<ButtonState> highlightStates;

        #endregion


        #region On Click
        
        /// <summary>
        /// The button state when clicked
        /// </summary>
        public List<ButtonState> clickStates;

        #endregion


        #region Slider Effect
        
        /// <summary>
        /// The slider used for the slider effect
        /// </summary>
        public Slider slider;

        #endregion


        #region Sounds
        
        /// <summary>
        /// The audio clip to play on mouse hover
        /// </summary>
        public AudioClip onHoverAudio;
        /// <summary>
        /// The audio clip to play on mouse click
        /// </summary>
        public AudioClip onClickAudio;

        #endregion

#if UNITY_EDITOR
        
        protected override void OnValidate()
        {
            base.OnValidate();

            foreach (var state in normalStates)
            {
                var image = state.image;
                if (!image) continue;
                if (state.color == default)
                {
                    state.color = image.color;
                    state.position = image.rectTransform.anchoredPosition;
                    state.sizeDelta = image.rectTransform.sizeDelta;
                    state.rotation = image.transform.localRotation.eulerAngles;
                }
                
                image.color = state.color;
                image.rectTransform.anchoredPosition = state.position;
                image.rectTransform.sizeDelta = state.sizeDelta;
                image.transform.localRotation = Quaternion.Euler(state.rotation);
            }
            if (hasSlider && slider) slider.value = 0;
            
            
            // Set highlight state to copy normal state
            int i;
            for (i = 0; i < highlightStates.Count; i++)
            {
                if (normalStates.Count > i && highlightStates[i].image == null)
                {
                    highlightStates[i].image = normalStates[i].image;
                    highlightStates[i].color = normalStates[i].color;
                    highlightStates[i].position = normalStates[i].position;
                    highlightStates[i].rotation = normalStates[i].rotation;
                    highlightStates[i].sizeDelta = normalStates[i].sizeDelta;
                }
            }

            if (i < normalStates.Count)
                for (; i < normalStates.Count; i++)
                    highlightStates.Add(new ButtonState());

            {
                // Set click state to copy normal state
                for (i = 0; i < clickStates.Count; i++)
                {
                    if (normalStates.Count > i && clickStates[i].image == null)
                    {
                        clickStates[i].image = normalStates[i].image;
						if (clickStates[i].colourPersist == false)
                            clickStates[i].color = normalStates[i].color;
                        clickStates[i].position = normalStates[i].position;
                        clickStates[i].rotation = normalStates[i].rotation;
                        clickStates[i].sizeDelta = normalStates[i].sizeDelta;
                    }
                }
            }
            
            if (i < normalStates.Count)
                for (; i < normalStates.Count; i++)
                    clickStates.Add(new ButtonState());
        }

#endif

        protected override void OnDisable()
        {
            if (!EditorApplication.isPlaying || !StateResetable) return;

            ResetState();
        }

        public void ResetState()
        {
            Debug.Log($"Resetting Button {this.gameObject.name} state Values!");
            RectTransform rectTransform = ((RectTransform)this.transform);

            if (rectTransform.TryGetComponent(out Graphic graphic))
            {
                if (normalStates[0].image != null)
                {
                    graphic = normalStates[0].image;
                }

                graphic.color = normalStates[0].color;
            }

            rectTransform.sizeDelta = normalStates[0].sizeDelta;
			rectTransform.anchoredPosition = normalStates[0].position;
			rectTransform.localRotation = Quaternion.Euler(normalStates[0].rotation);
		}


		public void Set(ButtonState state)
        {
            state.image.TweenGraphicColor(state.color, duration).SetEase(EaseType.ExpoOut);
            state.image.TweenAnchoredPosition(state.position, duration).SetEase(EaseType.ExpoOut);
            state.image.TweenLocalRotation(state.rotation, duration).SetEase(EaseType.ExpoOut);

            // Tween sizeDelta values
            var transform = state.image.rectTransform;
            var size = transform.sizeDelta;
            this.TweenValueFloat(state.sizeDelta.x, duration, x =>
            {
                transform.sizeDelta = new Vector2(x, transform.sizeDelta.y);
            }).SetFrom(size.x).SetEase(EaseType.ExpoOut);
            this.TweenValueFloat(state.sizeDelta.y, duration, y =>
            {
                transform.sizeDelta = new Vector2(transform.sizeDelta.x, y);
            }).SetFrom(size.y).SetEase(EaseType.ExpoOut);
        }


        public void Normal()
        {
            if (persistHighlight) return;

			StateResetable = false;

			foreach (var state in normalStates)
            {
                Set(state);
            }
            
            // Tween slider value
            if (hasSlider)
            {
                this.TweenValueFloat(0, 0.2f, f =>
                {
                    slider.value = f;
                }).SetEase(EaseType.ExpoOut).SetFrom(slider.value);
            }
        }

        public void Highlight()
        {
            StateResetable = true;
            foreach (var state in highlightStates)
            {
                Set(state);
            }
            
            // Tween slider value
            if (hasSlider)
            {
                this.TweenValueFloat(1, 0.2f, f =>
                {
                    slider.value = f;
                }).SetEase(EaseType.ExpoOut);
            }
        }
        
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            
            if (!interactable) return;
            
            Highlight();
            
            if (hasHoverSound) AudioManager.Play(onHoverAudio);
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            
            if (!interactable || persistHighlight) return;

			StateResetable = true;

			foreach (var state in clickStates)
            {
                Set(state);
            }

            if (hasClickSound) AudioManager.Play(onClickAudio);
            Invoke(nameof(Normal), duration);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            
            if (!interactable) return;
            
            Normal();
        }
    }

    [Serializable]
    public class ButtonState
    {
        public Graphic image;
        public Color color;
        public Vector2 position;
        public Vector2 sizeDelta;
        public Vector3 rotation;
        public bool colourPersist;
	}
}