using Interface.Elements.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Interface.Demo.Scripts
{
    public class Profile : MonoBehaviour
    {
        [SerializeField] private string username = "Username";
        [SerializeField] private int level = 4;
        [SerializeField] private int currentXP = 500;
        [SerializeField] private int maxXP = 1000;
        [SerializeField] private int rank = 4;
        [SerializeField] private int matches = 120;
        [SerializeField] private int kills = 1500;
        [SerializeField] private int deaths = 800;
        [SerializeField] private int headshots = 400;

        [Space] public Text usernameText;
        public Text levelText;
        public Text xpText;
        public Slider xpSlider;
        public PlayerRank playerRank;
        public Text matchesText;
        public Text killsText;
        public Text deathsText;
        public Text headshotsText;

        private void Start()
        {
            usernameText.text = username;
            levelText.text = "Level " + level;

            xpText.text = currentXP + " / " + maxXP;
            xpSlider.value = currentXP / (float) maxXP;

            playerRank.myRank = rank;
            playerRank.FillItems();

            matchesText.text = matches.ToString();

            killsText.text = kills.ToString();

            deathsText.text = deaths.ToString();

            headshotsText.text = headshots.ToString();
        }

        public void Focus()
        {
            playerRank.SnapTo();

            if (matchesText.GetType() == typeof(TextUI))
                ((TextUI) matchesText).StartAnimation();

            if (killsText.GetType() == typeof(TextUI))
                ((TextUI) killsText).StartAnimation();

            if (deathsText.GetType() == typeof(TextUI))
                ((TextUI) deathsText).StartAnimation();

            if (headshotsText.GetType() == typeof(TextUI))
                ((TextUI) headshotsText).StartAnimation();
        }
    }
}