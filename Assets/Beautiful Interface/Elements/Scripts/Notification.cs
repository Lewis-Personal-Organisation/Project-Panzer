using System;
using System.Collections;
using System.Collections.Generic;
using ElRaccoone.Tweens;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Interface.Elements.Scripts
{
    public class Notification : MonoBehaviour
    {
        private static Dictionary<int, NotificationUI> ShowingNotifs = new Dictionary<int, NotificationUI>();

        /// <summary>
        /// The previous mid - center notification
        /// </summary>
        private int prevCenterNotif;

        /// <summary>
        /// Background image is active only when mid - center notification is shown
        /// </summary>
        public Image background;

        [FormerlySerializedAs("notificationPrefab1")] 
        public GameObject notificationRectangle;
        [FormerlySerializedAs("notificationPrefab2")] 
        public GameObject notificationRounded;

        [Tooltip("The starting position for the notification corresponding to NotifPosition enum")]
        public Transform[] notifPositions = new Transform[9];

        public bool defaultIsRound;
        public float defaultDelay = 3;
        public Sprite defaultIcon;
        public NotifPosition defaultPosition = NotifPosition.BottomRight;
        public NotifStyle defaultStyle = NotifStyle.Rectangle;

        /// <summary>
        /// Static reference to the main handler
        /// </summary>
        private static Notification _i;

        private void Awake()
        {
            // Make this a persistant instance
            if (_i)
            {
                Destroy(gameObject);
            }
            else
            {
                _i = this;
                DontDestroyOnLoad(gameObject);
            }

        }


        #region Testing Methods

        public void Test(int position)
        {
            defaultPosition = (NotifPosition) position;
            var x = Show("Hello World", "Test description");
        }

        #endregion

        /// <summary>
        /// Show notification
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="description">Description</param>
        /// <returns>The notification ID (used to destroy or hide the notification)</returns>
        public static int Show(string title, string description)
        {
            // Check if manager instance is running
            if (!_i)
            {
                Debug.LogError("No notification handler. Make sure you have the Notification component in the scene");
                return 0;
            }
            
            return Show(title, description, _i.defaultIcon, _i.defaultDelay, _i.defaultPosition, _i.defaultStyle, _i.defaultIsRound);
        }

        /// <summary>
        /// Show notification
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="description">Description</param>
        /// <param name="icon">Icon</param>
        /// <param name="delay">The time to show the notification</param>
        /// <param name="position">The position to show the notification</param>
        /// <param name="style">The notification style</param>
        /// <param name="isRoundIcon">The notification icon is round</param>
        /// <param name="outlineColor">The color of the icon outline</param>
        /// <param name="showButtons">Show extra buttons</param>
        /// <param name="positiveText">The text on the positive button</param>
        /// <param name="onPositive">The action to execute when the positive button is clicked</param>
        /// <param name="negativeText">The text on the negative button</param>
        /// <param name="onNegative">The action to execute when the negative button is clicked</param>
        /// <returns>The notification ID (used to destroy or hide the notification)</returns>
        public static int Show(string title, string description, Sprite icon, float delay = 3,
            NotifPosition position = NotifPosition.BottomRight, NotifStyle style = NotifStyle.Rectangle,
            bool isRoundIcon = false, Color outlineColor = default, bool showButtons = false,
            UnityAction onPositive = null, UnityAction onNegative = null, 
            string positiveText = "ACCEPT", string negativeText = "CANCEL")
        {
            // Check if manager instance is running
            if (!_i)
            {
                Debug.LogError("No notification handler");
                return 0;
            }
            
            // Check if prefabs are filled in
            if (!_i.notificationRectangle || !_i.notificationRounded)
            {
                Debug.LogError("No notification prefab");
                return 0;
            }

            // Get the notification prefab based on the style
            GameObject prefab;
            switch (style)
            {
                case NotifStyle.Rectangle:
                    prefab = _i.notificationRectangle;
                    break;
                case NotifStyle.Rounded:
                    prefab = _i.notificationRounded;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(style), style, null);
            }
            
            if (!icon) icon = _i.defaultIcon;

            // Spawn the prefab
            var notif = Instantiate(prefab, _i.notifPositions[(int) position]);
            notif.transform.localPosition = Vector3.zero;
            var notifUI = notif.GetComponent<NotificationUI>();

            // Assign it to the dictionary (using a randomly generated ID)
            var id = Random.Range(int.MinValue, int.MaxValue);
            ShowingNotifs[id] = notifUI;

            // Set the necessary details of the notification
            notifUI.SetText(title, description);
            notifUI.SetIcon(isRoundIcon, icon, outlineColor);
            notifUI.SetButtons(showButtons, positiveText, onPositive, negativeText, onNegative);
            notifUI.Animate(position);
            
            // Hide after delay
            _i.StartCoroutine(Hide(id, delay));
            
            // Show background if mid-center
            if (position == NotifPosition.MidCenter)
            {
                _i.background.TweenGraphicAlpha(0.8f, 0.1f);
                _i.background.raycastTarget = true;
                _i.prevCenterNotif = id;
            }
            
            return id;
        }

        private static IEnumerator Hide(int id, float delay)
        {
            yield return new WaitForSeconds(delay);
            Hide(id);
        }
        
        /// <summary>
        /// Hides a notification by referencing the ID
        /// </summary>
        public static void Hide(int id)
        {
            if (ShowingNotifs.ContainsKey(id) && ShowingNotifs[id])
            {
                ShowingNotifs[id].Hide();
            }
        }

        /// <summary>
        /// Get a notification UI by the id
        /// </summary>
        /// <param name="id">The id of the spawned notification</param>
        public static NotificationUI Get(int id)
        {
            return ShowingNotifs[id];
        }

        /// <summary>
        /// Destroys a notification by referencing the ID
        /// </summary>
        public static void Destroy(int id)
        {
            if (ShowingNotifs.ContainsKey(id) && ShowingNotifs[id])
            {
                Destroy(ShowingNotifs[id].gameObject);
            }
        }

        public void BackgroundClicked(BaseEventData eventData) => BackgroundClicked();


        public static void BackgroundClicked()
        {
            _i.background.TweenGraphicAlpha(0, 0.1f);
            _i.background.raycastTarget = false;
            Hide(_i.prevCenterNotif);
        }
    }

    public enum NotifPosition
    {
        TopLeft = 0,
        TopCenter = 1,
        TopRight = 2,
        MidLeft = 3,
        MidCenter = 4,
        MidRight = 5,
        BottomLeft = 6,
        BottomCenter = 7,
        BottomRight = 8
    }

    public enum NotifStyle
    {
        Rectangle,
        Rounded
    }
}