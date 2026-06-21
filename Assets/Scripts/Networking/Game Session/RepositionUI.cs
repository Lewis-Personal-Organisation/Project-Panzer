using System.Collections;
using UnityEngine;

public class RepositionUI : Panel
{
    [SerializeField] private CanvasGroup repositionCanvasGroup;
    private Coroutine canvasFadeCoroutine;
    public bool isFading => canvasFadeCoroutine != null;
    public float fadeDuration;
    
    
    public void FadeForPlayerReposition(Vector3 safePosition, Quaternion safeRotation)
    {
        if (canvasFadeCoroutine == null)
        {
            StartCoroutine(RepositionPlayerRoutine(safePosition, safeRotation));
        }
    }
    
    private IEnumerator RepositionPlayerRoutine(Vector3 safePosition, Quaternion safeRotation)
    {
        VehicleController.Instance.stuckManager.stuckTime = 0;
        VehicleController.Instance.defence.Disable();
        
        repositionCanvasGroup.gameObject.SetActive(true);
        canvasFadeCoroutine = StartCoroutine(FadeCanvas(fadeDuration, 1F));

        yield return new WaitUntil(() => canvasFadeCoroutine == null);
        
        VehicleController.Instance.hullRigidbody.MovePosition(safePosition);
        VehicleController.Instance.hullRigidbody.MoveRotation(safeRotation);
        VehicleController.Instance.EnableSoft();
        VehicleController.Instance.defence.Enable();
        
        canvasFadeCoroutine = StartCoroutine(FadeCanvas(fadeDuration, 0F));
        
        yield return new WaitUntil(() => canvasFadeCoroutine == null);
        
        repositionCanvasGroup.gameObject.SetActive(false);
    }
    
    private IEnumerator FadeCanvas(float duration, float targetAlpha)
    {
        float startAlpha = repositionCanvasGroup.alpha;
        float rate = 1F / duration;
        float progress = 0F;
        Debug.Log($"Fade: {startAlpha}, {targetAlpha}");

        while (progress < 1F)
        {
            repositionCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            progress += rate * Time.deltaTime;
            Debug.Log($"Fade: {progress}");
            yield return null;
        }
        
        repositionCanvasGroup.alpha = targetAlpha;
        canvasFadeCoroutine = null;
    }
}
