using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class FadedBackgroundUI : Panel
{
    [SerializeField] private Image fadeImage;
    public bool fadeComplete = false;

    
    public FadedBackgroundUI Fade(float start, float target, float speed, UnityAction onComplete = null)
    {
        fadeComplete = false;
        StopAllCoroutines();
        
        Color color = fadeImage.color;
        color.a = start;
        fadeImage.color = color;
        UIManager.PushPanel(this);
        
        StartCoroutine(Animate(target, speed, onComplete));
        return this;
    }

    private IEnumerator Animate(float target, float speed, UnityAction onComplete)
    { 
        Color color = fadeImage.color;
        
        while (!Mathf.Approximately(fadeImage.color.a, target))
        {
            color.a = Mathf.MoveTowards(color.a, target, speed * Time.deltaTime);
            fadeImage.color = color;
            yield return null;
        }
        
        onComplete?.Invoke();

        fadeComplete = true;
    }
}
