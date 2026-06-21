using System.Linq;
using TMPro;
using UnityEngine;

public class GameplayUI : Singleton<GameplayUI>
{
    [SerializeField] private TextMeshProUGUI scoresText;

    [SerializeField] private GameplayCountdown countdownGroup;
    public static GameplayCountdown CountdownGroup => Instance.countdownGroup;
    
    [SerializeField] private RepositionUI repositionUI;
    public static RepositionUI RepositionUI => Instance.repositionUI;

    [SerializeField] private GameplayNotifications notifications;
    public static GameplayNotifications Notifications => Instance.notifications;
    
    [SerializeField] private GameplayPauseMenu pauseMenu;
    public static GameplayPauseMenu PauseMenu => Instance.pauseMenu;
    
    
    private new void Awake()
    {
        base.Awake();
    }
  
    public void UpdateScores()
    {
        if (GameplayNetworkManager.Instance == null)
            return;

        System.Collections.Generic.List<PlayerAvatar> playerAvatars = GameplayNetworkManager.Instance.playerAvatars;

        scoresText.text = string.Join("\n", playerAvatars.Select(playerAvatar => $"{playerAvatar.name}: {playerAvatar.score}").ToArray());
    }
}
