using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

#if UNITY_EDITOR
[ExecuteAlways]
#endif

[DisallowMultipleComponent]
public class GameplayNotifications : MonoBehaviour
{
    [System.Serializable]
    public class TextElementController
    {
        public enum State
        {
            FadeIn,
            FadeOut
        }

        public TextElementController(TextMeshProUGUI textMeshElement, string text, float alpha, float fadeOutTime, Vector2 spawnPos, Vector2 rectSize)
        {
            this.text = textMeshElement;
            textMeshElement.rectTransform.sizeDelta = rectSize;
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
        [SerializeField] private float alphaTarget; // The time before the text fades out

        public TextMeshProUGUI textElement => text;
    }

    [SerializeField] private Canvas canvas;
    public TextMeshProUGUI textElementPrefab; // The four text elements
    [SerializeField] public Vector2 spawnPos;
    [SerializeField] public Vector2 spawnSize;
    [SerializeField] private float moveSpeed = 1F;
    [SerializeField] private float messageMargin = 3F;
    [SerializeField] private float defaultExpiryTime = 1.5F;      // The default time before a text element begins to fade
   
    [SerializeField] private List<TextElementController> queuedElements = new List<TextElementController>();
    [SerializeField] private List<TextElementController> activeElements = new List<TextElementController>();
    [SerializeField] private List<TextElementController> inactiveElements = new List<TextElementController>();

    private float lastElementDistance => Vector2.Distance(spawnPos, activeElements[^1].textElement.rectTransform.anchoredPosition);
    private bool clearToSpawn => lastElementDistance > activeElements[^1].textElement.rectTransform.sizeDelta.y + messageMargin;

    
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private RectTransform canvasRectTransform;
    [Range(0F, 1000F)]
    [SerializeField] private float lineWidth;
    

    public void Setup()
    {
        TextMeshProUGUI[] textItems = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        spawnPos = textItems[0].rectTransform.anchoredPosition;
        for (int i = 0; i < textItems.Length; i++)
        {
            inactiveElements.Add(new TextElementController(textItems[i], "", 0, defaultExpiryTime, spawnPos, spawnSize));
        }
    }

    [ExecuteInEditMode]
    private void Update()
    {
        if (Application.isPlaying)
        {
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
            inactiveElements[0].textElement.text = text;
            inactiveElements[0].waitTimeForFadeOut = defaultExpiryTime;
            inactiveElements[0].textElement.alpha = 0;
            inactiveElements[0].state = TextElementController.State.FadeIn;
            inactiveElements[0].textElement.rectTransform.anchoredPosition = spawnPos;
            queuedElements.Add(inactiveElements[0]);
            inactiveElements.RemoveAt(0);
        }
        // Spawn an element
        else
        {
            TextMeshProUGUI textElement = Instantiate(textElementPrefab, canvas.transform);
            TextElementController controller = new TextElementController(textElement, text, 0, defaultExpiryTime, spawnPos, spawnSize);
            queuedElements.Add(controller);
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        EnsureLineRenderer();
    }

    private void OnEnable()
    {
        EnsureLineRenderer();
    }
    
    private void EnsureLineRenderer()
    {
        if (canvas == null) return;
        
        if (canvasRectTransform == null) 
            canvasRectTransform =  canvas.GetComponent<RectTransform>();
        
        if (lineRenderer != null) return;
        
        lineRenderer = GetComponent<LineRenderer>();
        if (!lineRenderer)
        {
            lineRenderer = new GameObject("LineRenderer", typeof(RectTransform), typeof(LineRenderer)).GetComponent<LineRenderer>();
            lineRenderer.transform.SetParent(canvas.transform, false);
        }

        lineRenderer.useWorldSpace = false;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.sortingOrder = 1000;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(gameObject);
            EditorUtility.SetDirty(lineRenderer);
        }
    }
    
    // private void RenderEditorLines()
    // {
    //     bool selected = Selection.activeGameObject == gameObject || Selection.activeGameObject == lineRenderer.gameObject;
    //     lineRenderer.enabled = selected;
    //
    //     if (!selected) return;
    // }

    private void OnValidate()
    {
        lineRenderer.widthMultiplier = lineWidth;
        DrawAtPosition();
    }

    private void DrawAroundScreen()
    {
        Vector2 size = canvasRectTransform.rect.size;
        lineRenderer.positionCount = 4;
        lineRenderer.SetPosition(0, new Vector3(lineRenderer.startWidth/2, lineRenderer.startWidth/2));
        lineRenderer.SetPosition(1, new Vector3(size.x - lineRenderer.startWidth/2, lineRenderer.startWidth/2));
        lineRenderer.SetPosition(2, new Vector3(size.x - lineRenderer.startWidth/2, size.y - lineRenderer.startWidth/2));
        lineRenderer.SetPosition(3, new Vector3(lineRenderer.startWidth/2, size.y - lineRenderer.startWidth/2));
    }

    private void DrawAtPosition()
    {
        Debug.Log("Running");
        Vector2 size = canvasRectTransform.rect.size;
        lineRenderer.positionCount = 4;
        lineRenderer.SetPosition(0, new Vector3(spawnPos.x - spawnSize.x/2, spawnPos.y - spawnSize.y/2));
        lineRenderer.SetPosition(1, new Vector3(spawnPos.x + spawnSize.x/2, spawnPos.y - spawnSize.y/2));
        lineRenderer.SetPosition(2, new Vector3(spawnPos.x + spawnSize.x/2, spawnPos.y + spawnSize.y/2));
        lineRenderer.SetPosition(3, new Vector3(spawnPos.x - spawnSize.x/2, spawnPos.y + spawnSize.y/2));
    }
#endif
}