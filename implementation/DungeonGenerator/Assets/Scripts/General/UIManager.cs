using UnityEngine;
using UnityEngine.UI;

namespace General
{
    public class UIManager : MonoBehaviour
    {
        public GameObject textWindow;
        public Text textInWindow;
        
        public void ToggleTextWindow(bool isActive)
        {
            textWindow.SetActive(isActive);
        }

        public void SetText(string displayText)
        {
            textInWindow.text = displayText;
        }
    }
}