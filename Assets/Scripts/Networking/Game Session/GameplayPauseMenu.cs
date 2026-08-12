using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Button = UnityEngine.UI.Button;

public class GameplayPauseMenu : Panel
{
    public bool showRepositionOption = false;
    public bool cachedReposOption = false;

    [SerializeField] private Button quitButton;
    [SerializeField] private Button repositionPlayerButton;
    [SerializeField] private Button closeButton;
    

    private void Awake()
    {
        quitButton.onClick.AddListener(QuitGame);
        closeButton.onClick.AddListener(() => TogglePanels(false));
        repositionPlayerButton.onClick.AddListener(() =>
        {
            if (GameplayUI.RepositionUI.isFading)
                return;
            
            TogglePanels(false);
            VehicleController.Instance.stuckManager.UnstickPlayer();
        });
    }

    [Button(ButtonSizes.Medium)]
    private void TestGameQuit()
    {
        quitButton.onClick?.Invoke();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.CapsLock) && !GameplayUI.RepositionUI.isFading)
        {
            TogglePanels(invertedState);
        }
    }
    
    public override void TogglePanels(bool activeState)
    {
        if (panel.activeSelf == activeState)
            return;

        panel.SetActive(activeState);
		
        Cursor.lockState = activeState ? CursorLockMode.Confined : CursorLockMode.Locked;
        Cursor.visible = activeState;
        
        // Disable when closing, so we can't reposition again straight after a reposition
        repositionPlayerButton.gameObject.SetActive(cachedReposOption);
        
        if (activeState)
        {
            cachedReposOption = showRepositionOption;
            VehicleController.Instance.DisableSoft();
            onPushAction.Invoke();
        }
        else
        {
            VehicleController.Instance.EnableSoft();
            onPopAction.Invoke();
        }
    }

    private void QuitGame()
    {
        VehicleController.Instance.Destroy();

        #if UNITY_EDITOR
        quitButton.onClick.AddListener(EditorApplication.ExitPlaymode);
        #else
		quitButton.onClick.AddListener(Application.Quit);
        #endif
    }
}
