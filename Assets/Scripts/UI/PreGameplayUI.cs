using UnityEngine;
using UnityEngine.Serialization;

public class PreGameplayUI : Singleton<PreGameplayUI>
{
    [Space(5)]
    [Header("Panels")]
    [SerializeField] private BackgroundUI background;
    [SerializeField] private MainMenuUI mainMenu;
    [SerializeField] private LobbySetupUI lobbySetup;
    [SerializeField] private LobbyUI lobby;
    [SerializeField] private TextInputUI textInputGroup;
    
    public static LobbySetupUI LobbySetupMenu => Instance.lobbySetup;
    public static LobbyUI Lobby => Instance.lobby;
    public static MainMenuUI MainMenu => Instance.mainMenu;
    public static TextInputUI TextInputGroup => Instance.textInputGroup;
    
    [Header("Shared Panels")]
    [SerializeField] private FadedBackgroundUI fadedBackground;
    [SerializeField] private LoadingIcon loadingIcon;
    [FormerlySerializedAs("notificationUI")]
    [SerializeField] private Notifs notifs;
    public static LoadingIcon LoadingIcon => Instance.loadingIcon;
    public static FadedBackgroundUI FadedBackgroundUI => Instance.fadedBackground;
    public static Notifs Notifs => Instance.notifs;
    
    
    private void Awake()
    {
        base.Awake();
        UIManager.Instance.SetInitialPanels(background, mainMenu);
    }
}
