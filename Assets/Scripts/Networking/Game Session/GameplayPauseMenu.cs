using UnityEngine;

public class GameplayPauseMenu : Panel
{
    public bool showRepositionOption = false;
    public bool cachedReposOption = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !GameplayUI.RepositionUI.isFading)
        {
            TogglePanels(invertedState);
            
            // Capture the current state of the reposition option
            if (panel.activeSelf)
            {
                cachedReposOption = showRepositionOption;
                VehicleController.Instance.DisableSoft();
            }
            else
            {
                VehicleController.Instance.EnableSoft();
            }
            
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.R) && cachedReposOption && !GameplayUI.RepositionUI.isFading)
        {
            TogglePanels(false);
            VehicleController.Instance.stuckManager.Unstick();
        }
    }
}
