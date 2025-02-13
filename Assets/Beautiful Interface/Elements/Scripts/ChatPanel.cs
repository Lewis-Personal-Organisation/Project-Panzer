using UnityEngine;
using UnityEngine.UI;

namespace Interface.Elements.Scripts
{
    public class ChatPanel : BasePanel
    {
        [Tooltip("The scroll rect component of the chat panel")]
        public ScrollRect scrollRect;
        
        [Tooltip("The display item to spawn in the list")]
        public GameObject textItem;
        
        [Tooltip("The parent anchor for all the items in the list")]
        public Transform textsAnchor;

        [Tooltip("The input field for the chat")]
        public InputField inputText;

        [Tooltip("The indicators to enable when we recieve a new message")] 
        public GameObject[] indicators;
        
        public delegate void SendChatMessage(string message);
        
        /// <summary>
        /// Callback activated when a message is sent by this client
        /// </summary>
        public SendChatMessage OnMessage;
        

        private void Start()
        {
            ShowIndicator(false);
        }
        
        
        protected override void Update()
        {
            base.Update();
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                if (inputText.text.Length > 0)
                {
                    Send(inputText.text);
                }

            if (IsShowing) ShowIndicator(false);
        }
        
        /// <summary>
        /// Show indicator that a message has been received, but not opened
        /// </summary>
        /// <param name="show"></param>
        public void ShowIndicator(bool show)
        {
            if (indicators == null || indicators.Length == 0)
                return;
            
            foreach (var indicator in indicators)
            {
                indicator.SetActive(show);
            }
        }


        public void Send()
        {
            Send(inputText.text);
        }
        
        private void Send(string message)
        {
            inputText.text = "";
            inputText.ActivateInputField();
            OnMessage?.Invoke(message);
        }
        
        
        /// <summary>
        /// Instantiate the Text Item in the list
        /// </summary>
        /// <returns>The spawned gameobject</returns>
        public GameObject AddMessage()
        {
            var item = Instantiate(textItem, textsAnchor);
            if (!IsShowing) ShowIndicator(true);
            return item;
        }

        /// <summary>
        /// Instantiate the Text Item in the list
        /// <para>
        /// This only works if the Item has a Text component
        /// </para>
        /// </summary>
        /// <param name="title">The sender</param>
        /// <param name="message">The message</param>
        public void AddMessage(string title, string message)
        {
            var text = "<b>" + title + ":</b>  " + message;
            var item = Instantiate(textItem, textsAnchor).GetComponent<Text>();
            if (!item) return;
            item.text = text;
            ScrollToBottom();
        }


        public void ScrollToTop()
        {
            if (scrollRect) scrollRect.normalizedPosition = new Vector2(0, 1);
        }
        
        public void ScrollToBottom()
        {
            if (scrollRect) scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }
}