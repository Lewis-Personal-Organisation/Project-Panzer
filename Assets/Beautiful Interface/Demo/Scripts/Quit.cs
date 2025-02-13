using Interface.Elements.Scripts;
using UnityEngine;

namespace Interface.Demo.Scripts
{
    public class Quit : MonoBehaviour
    {
        /// <summary>
        /// Exit notification ID. Used to hide the notification after response
        /// </summary>
        private int exitNotifID;
        
        public ButtonUI quitButton;

        private void Start()
        {
            quitButton.onClick.AddListener(Exit);
        }

        public void Exit()
        {
            var title = "Exit Game".ToUpper();
            var description = "Are you sure you want to exit".ToUpper();
            exitNotifID = Notification.Show(title, description, null,
                20, NotifPosition.MidCenter,
                NotifStyle.Rectangle, false, Color.clear, true,
                () => ExitResponse(true), () => ExitResponse(false), "EXIT");
        }
        
        /// <summary>
        /// Response to exit notification
        /// </summary>
        /// <param name="response"></param>
        private void ExitResponse(bool response)
        {
            Notification.BackgroundClicked();
            Notification.Destroy(exitNotifID);
            if (response)
            {
                Application.Quit();
            }
        }
    }
}