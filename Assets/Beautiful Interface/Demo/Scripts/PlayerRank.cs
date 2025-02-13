using System;
using System.Collections.Generic;
using System.Linq;
using Interface.Elements.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Interface.Demo.Scripts
{
    public class PlayerRank : MonoBehaviour
    {
        private Transform rankTransform;
        
        public int myRank;
        public GameObject rankItem;
        public RectTransform anchor;
        public ScrollRect scrollRect;

        public List<Rank> Ranks = new List<Rank>
        {
            new Rank
            {
                ID = 0,
                Name = "Silver"
            },
            new Rank
            {
                ID = 1,
                Name = "Silver Elite"
            },
            new Rank
            {
                ID = 2,
                Name = "Gold"
            },
            new Rank
            {
                ID = 3,
                Name = "Master"
            },
            new Rank
            {
                ID = 4,
                Name = "Legendary"
            },
            new Rank
            {
                ID = 5,
                Name = "Global Elite"
            },
            
        };
        

        public Rank GetRank(int rankID)
        {
            foreach (var rank in Ranks.Where(rank => rank.ID == rankID))
            {
                return rank;
            }
            throw new IndexOutOfRangeException("Could not find rank " + rankID);
        }

        
        /// <summary>
        /// Displays all the ranks (Not used anymore)
        /// </summary>
        public void FillItems()
        {
            // Destroy previous objects
            foreach (Transform child in anchor)
            {
                GameObject o;
                (o = child.gameObject).SetActive(false);
                Destroy(o);
            }

            foreach (var rank in Ranks)
            {
                var item = Instantiate(rankItem, anchor).GetComponent<ButtonUI>();
                if (!item) continue;
                
                var isCurrent = rank.ID == myRank;

                ((Image) item.normalStates[1].image).color = isCurrent ? Color.white : Color.clear;
                ((Text) item.normalStates[3].image).text = rank.Name;

                var image = (Image) item.normalStates[2].image;
                image.sprite = rank.Icon;
                    
                // If rank is not yet unlocked, then grey out
                if (myRank < rank.ID) image.color = Color.grey;

                // Snap to view the current rank
                if (isCurrent)
                {
                    rankTransform = item.transform;
                    SnapTo(rankTransform);
                }
            }
        }

        public void SnapTo()
        {
            SnapTo(rankTransform);
        }

        /// <summary>
        /// Snaps horizontally to a particular target in a list
        /// </summary>
        /// <param name="target"></param>
        public void SnapTo(Transform target)
        {
            Canvas.ForceUpdateCanvases();

            if (!anchor || !scrollRect || !target) return;

            var contentPos = anchor.anchoredPosition;
            var desiredPos = (Vector2) scrollRect.transform.InverseTransformPoint(anchor.position)
                             - (Vector2) scrollRect.transform.InverseTransformPoint(target.position);

            anchor.anchoredPosition = new Vector2(desiredPos.x, contentPos.y);
        }
        
        
        
        /// <summary>
        /// The player ranking
        /// </summary>
        [Serializable]
        public class Rank
        {
            public int ID;
            public string Name;
            public Sprite Icon;
        }
    }
    
    
}