using UnityEngine;
using UnityEngine.InputSystem;

namespace Riftbourne.UI
{
    /// <summary>
    /// Simple test script to verify TAB key input is working.
    /// Attach this to ANY GameObject in the scene to test.
    /// </summary>
    public class StatusMenuUITest : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("[TEST] StatusMenuUITest script is running!");
            
            // Test Input System
            if (Keyboard.current != null)
            {
                Debug.Log("[TEST] Keyboard.current is available!");
            }
            else
            {
                Debug.LogError("[TEST] Keyboard.current is NULL - Input System not initialized!");
            }
        }
        
        private void Update()
        {
            // Test: Direct keyboard input using Input System
            if (Keyboard.current == null)
            {
                if (Time.frameCount % 60 == 0) // Log once per second
                {
                    Debug.LogWarning("[TEST] Keyboard.current is null in Update()");
                }
                return;
            }
            
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                Debug.Log("[TEST] TAB key detected via Keyboard.current!");
            }
            
            // Also test if any key is being pressed (to verify Input System works at all)
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                Debug.Log($"[TEST] Some key was pressed! TAB pressed: {Keyboard.current.tabKey.isPressed}");
            }
        }
    }
}
