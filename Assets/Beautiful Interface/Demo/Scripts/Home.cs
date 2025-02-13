using System.Collections.Generic;
using ElRaccoone.Tweens;
using ElRaccoone.Tweens.Core;
using Interface.Elements.Scripts;
using UnityEngine;

namespace Interface.Demo.Scripts
{
    public class Home : MonoBehaviour
    {
        [Header("Maps")]
        public ButtonGroup mapSelection;

        [Header("Game Modes")]
        public ButtonGroup modeSelection;
        public GameObject modeSelector;
        public List<Vector2> selectorPositions;

        [Header("Start")] 
        public ButtonUI startButton;

        private void Start()
        {
            mapSelection.OnSelected += MapSelected;
            modeSelection.OnSelected += ModeSelected;
            
            startButton.onClick.AddListener(StartMatch);
        }

        private void MapSelected(IList<int> selectedindices)
        {
            
        }
        
        private void ModeSelected(IList<int> selectedindices)
        {
            modeSelector.TweenLocalPosition(selectorPositions[selectedindices[0]], 0.4f)
                .SetEase(EaseType.ExpoOut);
        }

        private void StartMatch()
        {
            Notification.Show("MATCHMAKING", "STARTING MATCH");
        }
    }
}