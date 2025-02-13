using Interface.Elements.Scripts;
using UnityEngine;

namespace Interface.Demo.Scripts
{
    /// <summary>
    /// Used for testing the ChatPanel object
    /// </summary>
    public class ChatTester : MonoBehaviour
    {
        private ChatPanel panel;

        private void Start()
        {
            panel = GetComponent<ChatPanel>();
            panel.OnMessage += OnMessage;
        }

        private void OnMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Notification.Show("CANNOT SEND CHAT", "MESSAGE CANNOT BE EMPTY");
                return;
            }
            panel.AddMessage("YOU", message.ToUpper());
        }
    }
}