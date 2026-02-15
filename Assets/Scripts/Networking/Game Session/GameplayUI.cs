using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GameplayUI : Singleton<GameplayUI>
{
    [SerializeField] private TextMeshProUGUI scoresText;

    public float timer;
    public float maxtime;
    
    private new void Awake()
    {
        base.Awake();
    }

    public void UpdateScores()
    {
        if (GameplayNetworkManager.Instance == null)
            return;

        List<PlayerAvatar> playerAvatars = GameplayNetworkManager.Instance.playerAvatars;

        scoresText.text = string.Join("\n", playerAvatars.Select(playerAvatar => $"{playerAvatar.name}: {playerAvatar.score}").ToArray());
    }
}
