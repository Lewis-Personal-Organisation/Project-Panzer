using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GameplayUI : Singleton<GameplayUI>
{
    [SerializeField] private TextMeshProUGUI scoresText;

    [SerializeField] private Notifications notifications;
        
    [System.Serializable]
    public class Notifications
    {
        [System.Serializable]
        public class TextElementController
        {
            public enum State
            {
                FadeIn,
                FadeOut
            }
            
            public TextElementController(TextMeshProUGUI textMeshElement, string text, float alpha, float fadeOutTime, Vector2 spawnPos)
            {
                textMeshElement.text = text;
                textMeshElement.alpha = alpha;
                textMeshElement.rectTransform.anchoredPosition = spawnPos;
                waitTimeForFadeOut = fadeOutTime;
                state = State.FadeIn;
            }

            public State state;
            [SerializeField] private TextMeshProUGUI text;
            [FormerlySerializedAs("waitTimeForExpiry")]
            public float waitTimeForFadeOut;
            [SerializeField] private float alphaTarget;                     // The time before the text fades out

            public TextMeshProUGUI textElement => text;
        }
        
        [SerializeField] private Canvas canvas;
        public TextMeshProUGUI textElementPrefab;                         // The four text elements
        [SerializeField] public Vector2 spawnPos;
        [SerializeField] private float moveSpeed = 1F;
        [SerializeField] private float messageMargin = 3F;
        [SerializeField] private static float defaultExpiryTime = 1.5F;            // The default time before a text element begins to fade
        [SerializeField] private static float minimumLastElementTime = 0.5F;       // The minimum expiry time of the last spawned element before we can spawn another
        
        [SerializeField] private List<TextElementController> queuedElements = new List<TextElementController>(); 
        [SerializeField] private List<TextElementController> activeElements = new List<TextElementController>();
        [SerializeField] private List<TextElementController> inactiveElements = new List<TextElementController>();

        private float lastElementDistance => Vector2.Distance(spawnPos, activeElements[^1].textElement.rectTransform.anchoredPosition);
        private bool clearToSpawn => lastElementDistance > activeElements[^1].textElement.rectTransform.sizeDelta.y + messageMargin;
        
        
        public void Setup()
        {
            spawnPos = inactiveElements[0].textElement.rectTransform.anchoredPosition;
        }
        
        public void Update()
        {
            // 1.
            // If we have a queued element, check if the space is clear to spawn a new element
            // If so, we can spawn the queued item
            if (queuedElements.Count > 0)
            {
                if (activeElements.Count == 0 || (activeElements.Count > 0 && clearToSpawn))
                {
                    TextElementController elementToMove = queuedElements[0];
                    queuedElements.RemoveAt(0);
                    activeElements.Add(elementToMove);
                    activeElements[^1].textElement.gameObject.SetActive(true);
                }
            }

            // 2.
            // Move all spawned elements upwards while increasing their alpha
            // Check if they should start lowering their alpha
            for (int i = 0; i < activeElements.Count; i++)
            {
                // FADE IN
                if (activeElements[i].state == TextElementController.State.FadeIn)
                {
                    if (activeElements[i].textElement.alpha < 255F)
                    {
                        activeElements[i].textElement.alpha = Mathf.Clamp(activeElements[i].textElement.alpha += Time.deltaTime / 2F, 0F, 1F) ;
                    }
                }
                // FADE OUT
                else
                {
                    if (activeElements[i].textElement.alpha > 0)
                    {
                        activeElements[i].textElement.alpha = Mathf.Clamp(activeElements[i].textElement.alpha -= Time.deltaTime / 2F, 0F, 1F) ;
                    }
                        
                    if (activeElements[i].textElement.alpha <= 0)
                    {
                        activeElements[i].textElement.alpha = 0;
                        activeElements[i].textElement.gameObject.SetActive(false);
                        TextElementController elementToMove = activeElements[0];
                        activeElements.RemoveAt(0);
                        inactiveElements.Add(elementToMove);
                    }
                }
                
                activeElements[i].textElement.rectTransform.anchoredPosition += Vector2.up * (moveSpeed * Time.deltaTime);
                activeElements[i].waitTimeForFadeOut -= Time.deltaTime;
            }
            
            // Debug.Log($"Travel Distance: {lastElementDistance}. Comp: {lastElementDistance} > {activeElements[^1].textElement.rectTransform.sizeDelta.y + messageMargin} ? {clearToSpawn}");
        }
        
        /// <summary>
        /// Call to schedule a message popup
        /// </summary>
        /// <param name="text"></param>
        public void Queue(string text)
        {
            // Re-use pooled elements
            if (inactiveElements.Count > 0)
            {
                TextElementController elementToMove = inactiveElements[0];
                elementToMove.textElement.text = text;
                inactiveElements.RemoveAt(0);
                queuedElements.Add(elementToMove);
            }
            // Spawn an element
            else
            { 
                TextMeshProUGUI textElement = Instantiate(textElementPrefab, canvas.transform);
                TextElementController controller = new TextElementController(textElement, text, 0, defaultExpiryTime, spawnPos);
                queuedElements.Add(controller);
            }
        }
    }
    
    
    private new void Awake()
    {
        base.Awake();
        notifications.Setup();
        notifications.Queue("Test!");
        notifications.Queue("Test 2!");
    }

    private void Update()
    {
        notifications.Update();
    }

    public void UpdateScores()
    {
        if (GameplayNetworkManager.Instance == null)
            return;

        List<PlayerAvatar> playerAvatars = GameplayNetworkManager.Instance.playerAvatars;

        scoresText.text = string.Join("\n", playerAvatars.Select(playerAvatar => $"{playerAvatar.name}: {playerAvatar.score}").ToArray());
    }
}
