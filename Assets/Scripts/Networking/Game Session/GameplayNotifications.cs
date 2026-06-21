using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

#if UNITY_EDITOR
[ExecuteAlways]
#endif

[DisallowMultipleComponent]
public class GameplayNotifications : NetworkBehaviour
{
    [System.Serializable]
    public class TextElementController
    {
        public enum State
        {
            FadeIn,
            FadeOut
        }

        public TextElementController(TextMeshProUGUI textElementMeshElement, string text, float alpha, float fadeOutTime, Vector2 spawnPos)
        {
            this.textElement = textElementMeshElement;
            Set(text, alpha, fadeOutTime, spawnPos);
        }

        public void Set(string text, float alpha, float fadeOutTime, Vector2 spawnPos)
        {
            textElement.text = text;
            textElement.alpha = alpha;
            textElement.rectTransform.anchoredPosition = spawnPos;
            waitTimeForFadeOut = fadeOutTime;
            state = State.FadeIn;
        }

        public State state;
        public TextMeshProUGUI textElement;
        public float waitTimeForFadeOut;
        [SerializeField] private float alphaTarget;
    }

    [SerializeField] private Canvas canvas;
    public TextMeshProUGUI textElementPrefab;
    [SerializeField] public Vector2 spawnPos;
    [SerializeField] public Vector2 spawnSize;
    [SerializeField] private float moveSpeed = 1F;
    [SerializeField] private float messageMargin = 3F;
    [SerializeField] private float defaultExpiryTime = 1F;      // The default time before a text element begins to fade
   
    [SerializeField] private List<TextElementController> queuedElements = new List<TextElementController>();
    [SerializeField] private List<TextElementController> activeElements = new List<TextElementController>();
    [SerializeField] private List<TextElementController> inactiveElements = new List<TextElementController>();

    private float lastElementDistance => Vector2.Distance(spawnPos, activeElements[^1].textElement.rectTransform.anchoredPosition);
    private bool clearToSpawn => lastElementDistance > activeElements[^1].textElement.rectTransform.sizeDelta.y + messageMargin;


    private void Awake()
    {
        Setup();
    }

    public void Setup()
    {
        inactiveElements.Clear();
        TextMeshProUGUI[] textItems = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        spawnPos = textItems[0].rectTransform.anchoredPosition;
        for (int i = 0; i < textItems.Length; i++)
        {
            inactiveElements.Add(new TextElementController(textItems[i], "", 0, defaultExpiryTime, spawnPos));
        }
    }

    [ExecuteInEditMode]
    private void Update()
    {
        if (Application.isPlaying)
        {
            // DEBUG
            if (Input.GetKeyDown(KeyCode.A))
            {
                Queue($"Test {(int)Time.time}!");
            }
            
            // 1. If we have a queued element, check if the space is clear to spawn a new element
            // If so, we can spawn the queued item
            if (queuedElements.Count > 0)
            {
                if (activeElements.Count == 0 || (activeElements.Count > 0 && clearToSpawn))
                {
                    activeElements.Add(queuedElements[0]);
                    queuedElements.RemoveAt(0);
                    activeElements[^1].textElement.gameObject.SetActive(true);
                }
            }

            // 2. Act upon the elements state, fading in or out.
            // Then move the object. Once time has lapsed, fade it out
            for (int i = 0; i < activeElements.Count; i++)
            {
                // FADE IN
                if (activeElements[i].state == TextElementController.State.FadeIn)
                {
                    if (activeElements[i].textElement.alpha < 255F)
                    {
                        activeElements[i].textElement.alpha = Mathf.Clamp(activeElements[i].textElement.alpha += Time.deltaTime / 2F, 0F, 1F);
                    }
                }
                // FADE OUT
                else
                {
                    if (activeElements[i].textElement.alpha > 0)
                    {
                        activeElements[i].textElement.alpha = Mathf.Clamp(activeElements[i].textElement.alpha -= Time.deltaTime / 2F, 0F, 1F);
                    }

                    if (activeElements[i].textElement.alpha <= 0)
                    {
                        Debug.Log($"Moving to inactive: {activeElements[i].textElement.gameObject.name}");
                        activeElements[i].textElement.alpha = 0;
                        activeElements[i].textElement.gameObject.SetActive(false);
                        inactiveElements.Add(activeElements[i]);
                        activeElements.RemoveAt(i);
                        continue; // Skip this items movement below, as it doesn't exist in List!
                    }
                }

                activeElements[i].textElement.rectTransform.anchoredPosition += Vector2.up * (moveSpeed * Time.deltaTime);

                if ((activeElements[i].waitTimeForFadeOut -= Time.deltaTime) <= 0)
                {
                    activeElements[i].state = TextElementController.State.FadeOut;
                }
            }
        }
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
            inactiveElements[0].Set(text, 0, defaultExpiryTime, spawnPos);
            queuedElements.Add(inactiveElements[0]);
            inactiveElements.RemoveAt(0);
        }
        // Spawn an element
        else
        {
            TextMeshProUGUI textElement = Instantiate(textElementPrefab, canvas.transform);
            TextElementController controller = new TextElementController(textElement, text, 0, defaultExpiryTime, spawnPos);
            queuedElements.Add(controller);
        }
    }

    /// <summary>
    /// Send a network notification message to all players
    /// </summary>
    /// <param name="message"></param>
    public void GlobalMessage(string message)
    {
        SendNetworkNotificationServerRPC(message);
    }
    
    /// <summary>
    /// Queues a Gameplay Notification for the server
    /// </summary>
    /// <param name="playerName"></param>
    [ServerRpc(RequireOwnership = false)]
    private void SendNetworkNotificationServerRPC(string message)
    {
        Queue(message);
        SendNetworkNotifClientRPC(message);
    }

    /// <summary>
    /// Queues a Gameplay Notification for all clients. Excludes the servers client
    /// </summary>
    /// <param name="playerName"></param>
    [ClientRpc]
    private void SendNetworkNotifClientRPC(string message)
    {
        if (NetworkManager.Singleton.IsServer) return;
        Queue(message);
    }
}