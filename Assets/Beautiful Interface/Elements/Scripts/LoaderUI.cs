using System;
using System.Collections;
using ElRaccoone.Tweens;
using ElRaccoone.Tweens.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Interface.Elements.Scripts
{
    public class LoaderUI : MonoBehaviour
    {
        /// <summary>
        /// Starting positions for loader icon
        /// </summary>
        private Vector3[] iconPosits;
        
        [Tooltip("The circle loader image")]
        [DrawIf("type", LoaderType.Circle, ComparisonType.Equals)]
        public Image image;

        [Tooltip("The Logo used to animate")]
        [DrawIf("type", LoaderType.Logo, ComparisonType.Equals)]
        public Sprite logoIcon;

        [Tooltip("The Logo used to animate")]
        [DrawIf("type", LoaderType.Logo, ComparisonType.Equals)]
        public LoaderDirection loaderDirection;
        
        [Tooltip("Display the loading text")]
        public Text text;

        [DrawIf("type", LoaderType.Line, ComparisonType.Equals)]
        [Tooltip("Left part of line loader")]
        public Image leftLine;

        [DrawIf("type", LoaderType.Line, ComparisonType.Equals)]
        [Tooltip("Left part of line loader")]
        public Image rightLine;

        [DrawIf("type", LoaderType.Logo, ComparisonType.Equals)]
        [Tooltip("Used for Logo loader")]
        public Image[] logoParts;
        
        [Tooltip("Start playing immediately")]
        public bool startPlaying;

        [Tooltip("The type of loader")]
        public LoaderType type;


        private void Start()
        {
            PlayAnimation(startPlaying);

            iconPosits = new Vector3[logoParts.Length];
            for (var i = 0; i < iconPosits.Length; i++)
            {
                iconPosits[i] = logoParts[i].transform.localPosition;
                var iconImage = logoParts[i].transform.GetChild(0).GetComponent<Image>();
                iconImage.sprite = logoIcon;
            }
        }

        private void PlayAnimation(bool isPlaying)
        {
            if (!isPlaying)
            {
                StopAnimation();
            }
            else
            {
                PlayAnimation();
            }
        }

        public void PlayAnimation()
        {
            startPlaying = true;
            switch (type)
            {
                case LoaderType.Circle:
                    PlayCircle();
                    break;
                case LoaderType.Line:
                    PlayLine();
                    break;
                case LoaderType.Logo:
                    StartCoroutine(PlayLogo());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void StopAnimation()
        {
            startPlaying = false;
            switch (type)
            {
                case LoaderType.Circle:
                    StopCircle();
                    break;
                case LoaderType.Line:
                    StopLine();
                    break;
                case LoaderType.Logo:
                    StopLogo();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #region Circle Loader

        private void PlayCircle()
        {
            var clockwise = image.fillClockwise;
            const int duration = 1;
            if (text) text.color += Color.black;

            image.TweenImageFillAmount(1, duration).SetFrom(0).SetEase(EaseType.QuadIn)
                .SetOnComplete(() =>
                {
                    image.fillClockwise = !clockwise;
                    image.TweenImageFillAmount(0, duration).SetFrom(1).SetEase(EaseType.QuadOut)
                        .SetOnComplete(() =>
                        {
                            image.fillClockwise = clockwise;
                            PlayAnimation(startPlaying);
                        });
                });

            this.TweenValueFloat(360, 2,
                f => image.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, f)));
        }

        private void StopCircle()
        {
            image.fillAmount = 0;
            if (text) text.color -= Color.black;
        }

        #endregion
        

        #region Line Loader

        private void PlayLine()
        {
            const float duration = 0.8f;
            if (text) text.color += Color.black;

            leftLine.TweenImageFillAmount(0.5f, duration)
                .SetFrom(0)
                .SetEase(EaseType.QuadIn)
                .SetOnComplete(() =>
                {
                    leftLine.TweenImageFillAmount(0, duration)
                        .SetFrom(0.5f)
                        .SetEase(EaseType.QuadOut);
                });

            rightLine.TweenImageFillAmount(0.5f, duration)
                .SetFrom(0)
                .SetEase(EaseType.QuadIn)
                .SetOnComplete(() =>
                {
                    rightLine.TweenImageFillAmount(0, duration)
                        .SetFrom(0.5f)
                        .SetEase(EaseType.QuadOut)
                        .SetOnComplete(() => PlayAnimation(startPlaying));
                    
                    
                });
        }

        private void StopLine()
        {
            leftLine.fillAmount = 0;
            rightLine.fillAmount = 0;
            if (text) text.color -= Color.black;
        }

        #endregion
        

        #region Logo

        private IEnumerator PlayLogo()
        {
            const float duration = 0.5f;
            if (text) text.color += Color.black;


            for (var i = 0; i < logoParts.Length; i++)
            {
                var part = logoParts[i];
                part.color += Color.black;
                var yPos = iconPosits[i].y;
                var xPos = iconPosits[i].x;

                // Determine starting and ending position based on loaderDirection
                Vector3 startPos;
                Vector3 endPos;
                switch (loaderDirection)
                {
                    case LoaderDirection.TopToBottom:
                        startPos = new Vector3(xPos, yPos + 60);
                        endPos = new Vector3(xPos, -startPos.y);
                        break;
                    case LoaderDirection.BottomToTop:
                        startPos = new Vector3(xPos, yPos - 60);
                        endPos = new Vector3(xPos, -startPos.y);
                        break;
                    case LoaderDirection.LeftToRight:
                        startPos = new Vector3(xPos - 60, yPos);
                        endPos = new Vector3(-startPos.x, yPos);
                        break;
                    case LoaderDirection.RightToLeft:
                        startPos = new Vector3(xPos + 60, yPos);
                        endPos = new Vector3(-startPos.x, yPos);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var midPos = new Vector3(xPos, yPos);
                var iconImage = part.transform.GetChild(0).GetComponent<Image>();
                iconImage.sprite = logoIcon;
                iconImage.TweenGraphicAlpha(1, duration + 0.5f);
                part.TweenLocalPosition(midPos, duration + 1)
                    .SetFrom(startPos)
                    .SetEase(EaseType.BounceOut)
                    .SetOnComplete(() =>
                    {
                        iconImage.TweenValueFloat(1, 1, f => { }).SetOnComplete(() =>
                        {
                            iconImage.TweenGraphicAlpha(0, duration);
                            part.TweenLocalPosition(endPos, duration)
                                .SetEase(EaseType.QuadIn);
                        });
                    });

                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(logoParts.Length / 10f + 3);
            
            PlayAnimation(startPlaying);
        }

        private void StopLogo()
        {
            if (text) text.color -= Color.black;
            foreach (var part in logoParts)
            {
                part.color -= Color.black;
            }
        }

        #endregion
    }

    public enum LoaderType
    {
        Circle, Line, Logo
    }

    public enum LoaderDirection
    {
        TopToBottom, BottomToTop, LeftToRight, RightToLeft
    }
}