using UnityEngine;
using TMPro;

public class GameplayCountdown : Panel
{
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject countdownTimer;
    [SerializeField] private GameObject animatedGameObject;
    
    
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
}
