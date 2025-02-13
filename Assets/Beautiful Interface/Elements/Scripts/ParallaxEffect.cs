using System;
using System.Collections.Generic;
using ElRaccoone.Tweens;
using UnityEngine;
using UnityEngine.UI;

namespace Interface.Elements.Scripts
{
    /// <summary>
    /// Animated parallax effect using the mouse cursor movements
    /// </summary>
    public class ParallaxEffect : MonoBehaviour
    {
        public bool play;
        public float delay = 5;
        public float fadeBackground = 1.5f;
        public float fadeForeground = 1;
        public float mouseSmooth = 50;
        public List<BackgroundSet> backgroundSets;

        private float smoothX;
        private float smoothY;
        
        private int current = -1;
        

        private void Start()
        {
            foreach (var set in backgroundSets)
            {
                Hide(set);
            }
            
            InvokeRepeating(nameof(Increment), 1, delay);
        }

        private void Update()
        {
            if (backgroundSets.Count == 0 || current < 0) return;
            
            var mouse = Input.mousePosition;
            var mouseX = mouse.x / Screen.width;
            var mouseY = mouse.y / Screen.height;
            for (var i = 0; i < backgroundSets[current].foregrounds.Count; i++)
            {
                var set = backgroundSets[current];
                var pos = set.initPositions[i];
                var curPos = set.foregrounds[i].transform.localPosition;
                var toX = Mathf.Lerp(pos.x - set.XRange, pos.x + set.XRange, mouseX);
                var toY = Mathf.Lerp(pos.y - set.YRange, pos.y + set.YRange, mouseY);

                var x = Mathf.SmoothDamp(curPos.x, toX, ref smoothX, mouseSmooth * Time.deltaTime);
                var y = Mathf.SmoothDamp(curPos.y, toY, ref smoothY, mouseSmooth * Time.deltaTime);

                set.foregrounds[i].transform.localPosition = new Vector3(x, y);
            }
        }

        private void Hide(BackgroundSet set)
        {
            var images = set.parent.transform.GetComponentsInChildren<Image>();
            set.foregrounds = new List<Image>();
            set.initPositions = new List<Vector3>();
            foreach (var image in images)
            {
                image.color -= Color.black;
                
                if (image.GetInstanceID() == set.parent.GetInstanceID())
                    continue;
                
                set.foregrounds.Add(image);
                set.initPositions.Add(image.transform.localPosition);
            }
        }

        private void Increment()
        {
            if (!play)
            {
                current = -1;
                return;
            }
            if (current >= 0)
            {
                backgroundSets[current].parent.TweenGraphicAlpha(0, fadeForeground);
                foreach (var foreground in backgroundSets[current].foregrounds)
                {
                    foreground.TweenGraphicAlpha(0, fadeBackground);
                }
            }

            this.TweenValueFloat(1, fadeForeground, f => { }).SetOnComplete(() =>
            {
                current++;
                if (current >= backgroundSets.Count)
                    current = 0;
                
                foreach (var foreground in backgroundSets[current].foregrounds)
                {
                    foreground.TweenGraphicAlpha(1, fadeBackground);
                }
            }).valueFrom = 0;

            
            this.TweenValueFloat(1, fadeBackground, f => { }).SetOnComplete(() =>
            {
                backgroundSets[current].parent.TweenGraphicAlpha(1, fadeBackground);
            }).valueFrom = 0;

        }
    }

    [Serializable]
    public class BackgroundSet
    {
        public Image parent;
        public float XRange;
        public float YRange;
        [HideInInspector] public List<Image> foregrounds;
        [HideInInspector] public List<Vector3> initPositions;
    }
}