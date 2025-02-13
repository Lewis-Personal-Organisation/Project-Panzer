using System.Collections;
using ElRaccoone.Tweens;
using ElRaccoone.Tweens.Core;
using Interface.Elements.Scripts;
using UnityEngine;

namespace Interface.Demo.Scripts
{
    public class LoadingScreen : BasePanel
    {
        public RectTransform titleMask;
        public LoaderUI[] loader;
        public ParallaxEffect parallaxEffect;
        public CanvasGroup menuCanvas;

        [Space] 
        
        public float loadingTime = 5;

        private IEnumerator Start()
        {
            cg.Show(0);
            menuCanvas.Hide(0);
            titleMask.sizeDelta = Vector2.zero;
            yield return new WaitForSeconds(1);

            // Reveal
            this.TweenValueFloat(450, 1.5f, f => titleMask.sizeDelta = new Vector2(f, 100))
                .SetEase(EaseType.ExpoOut)
                .SetOnComplete(() =>
                {
                    // Move up and play loader
                    titleMask.TweenLocalPositionY(50, 0.5f).SetEase(EaseType.ExpoOut);
                    parallaxEffect.play = true;
                    foreach (var loaderUI in loader) loaderUI.PlayAnimation();
                });

            
            yield return new WaitForSeconds(loadingTime - 1);

            cg.Hide(1);

            yield return new WaitForSeconds(1);
            
            menuCanvas.Show();
        }
    }
}