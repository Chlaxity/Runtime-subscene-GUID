using UnityEngine;
using UnityEngine.UI;

namespace LobbySample
{
    public class PlayerSlotUI : MonoBehaviour
    {
        public Text nameUI;
        public Image readyUI;
        
        public void UpdateInformation(PlayerSlot playerSlot)
        {
            nameUI.text = playerSlot.name;
            readyUI.enabled = playerSlot.ready;
        }

        public void EmptyInformation()
        {
            nameUI.text = "Empty";
            readyUI.enabled = false;
        }
    }
}