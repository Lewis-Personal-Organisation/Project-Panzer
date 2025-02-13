using ElRaccoone.Tweens;
using ElRaccoone.Tweens.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Interface.Elements.Scripts
{
    public class FriendItem : MonoBehaviour
    {
        private RectTransform rect;
        
        /// <summary>
        /// The starting height
        /// </summary>
        private float initHeight;

        [Tooltip("The maximum height to expand when clicked")]
        public float expandHeight;

        [Tooltip("The text to display the username")]
        public Text usernameText;
        
        [Tooltip("The text to display the status")]
        public Text statusText;

        [Tooltip("The avatar image")] 
        public Image avatarImage;
        
        [Tooltip("The background image")]
        public Image backgroundImage;

        [Tooltip("The button to invite player")] 
        public Button inviteButton;

        [Tooltip("The button to message player")] 
        public Button messageButton;

        [Tooltip("The button to delete friend")]
        public Button deleteButton;


        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            initHeight = rect.rect.height;
            ExpandOff();
        }

        /// <summary>
        /// Expand this item to show buttons
        /// </summary>
        public void ExpandOn()
        {
            transform.TweenValueFloat(expandHeight, 0.5f, f =>
            {
                var temp = rect.rect;
                rect.sizeDelta = new Vector2(temp.width, f);
            }).SetFrom(rect.rect.height).SetEase(EaseType.ExpoOut);
        }

        /// <summary>
        /// Contract this item and hide buttons
        /// </summary>
        public void ExpandOff()
        {
            transform.TweenValueFloat(initHeight, 0.5f, f =>
            {
                var temp = rect.rect;
                rect.sizeDelta = new Vector2(temp.width, f);
            }).SetFrom(rect.rect.height).SetEase(EaseType.ExpoOut);
        }
    }
}