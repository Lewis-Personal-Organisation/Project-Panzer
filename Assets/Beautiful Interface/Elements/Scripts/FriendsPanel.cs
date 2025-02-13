using ElRaccoone.Tweens;
using ElRaccoone.Tweens.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Interface.Elements.Scripts
{
    public class FriendsPanel : MonoBehaviour
    {
        [Tooltip("The title image/text. This will be hidden when the panel is closed")]
        public GameObject title;

        [Tooltip("The position when closed")]
        public Vector3 closedPosition = new Vector3(1120, 0);
        
        [Tooltip("The position when opened")]
        public Vector3 openPosition = new Vector3(960, 0);
        
        
        private void Start()
        {
            Close();
        }
        

        /// <summary>
        /// Called by Event Trigger
        /// Slide open the friends panel
        /// </summary>
        public void Open()
        {
            if (title)
            {
                title.SetActive(true);
                title.GetComponent<TextUI>()?.StartAnimation();
            }
            
            transform.TweenLocalPosition(openPosition, 0.5f).SetEase(EaseType.ExpoOut);
        }

        /// <summary>
        /// Called by Event Trigger
        /// Slide close the friends panel
        /// </summary>
        public void Close()
        {
            if (title)
            {
                title.SetActive(false);
            }
            transform.TweenLocalPosition(closedPosition, 0.5f).SetEase(EaseType.ExpoOut);
        }
    }
}