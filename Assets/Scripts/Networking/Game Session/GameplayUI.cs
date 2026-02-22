using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GameplayUI : Singleton<GameplayUI>
{
    [SerializeField] private TextMeshProUGUI scoresText;

    public float timer;
    public float maxtime;

    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject countdownTimer;
    [SerializeField] private GameObject animatedGameObject;
    
    
    private new void Awake()
    {
        base.Awake();
    }

    public void SetCountdownTimer(string time)
    {
        countdownText.text = time;
    }

    public void ToggleCountdownTimer(bool show)
    {
        countdownTimer.SetActive(show);
    }

    public void ToggleWaitAnimation(bool show)
    {
        animatedGameObject.SetActive(show);
    }
    
    public void UpdateScores()
    {
        if (GameplayNetworkManager.Instance == null)
            return;

        List<PlayerAvatar> playerAvatars = GameplayNetworkManager.Instance.playerAvatars;

        scoresText.text = string.Join("\n", playerAvatars.Select(playerAvatar => $"{playerAvatar.name}: {playerAvatar.score}").ToArray());
    }
}
