using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameplayUI : Singleton<GameplayUI>
{
    [SerializeField] private TextMeshProUGUI scoresText;
    
    
    private new void Awake()
    {
        base.Awake();
    }

    public void UpdateScores()
    {
        if (GameplayNetworkManager.Instance == null)
            return;

        List<PlayerAvatar> playerAvatars = GameplayNetworkManager.Instance.playerAvatars;

        scoresText.text =
            string.Join("\n",
                playerAvatars.Select(playerAvatar => $"{playerAvatar.name}: {playerAvatar.score}").ToArray());
    }
}
