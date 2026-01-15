using UnityEngine;

/// <summary>
/// Helper component for setting up mobile input UI.
/// Attach to a Canvas and use the CreateMobileControls context menu to generate UI.
///
/// Setup:
/// 1. Create a Canvas (Screen Space - Overlay)
/// 2. Add this component
/// 3. Right-click component > Create Mobile Controls
/// 4. The joystick and jump button will be created with On-Screen components
/// </summary>
public class MobileInputUI : MonoBehaviour
{
#if UNITY_EDITOR
    [ContextMenu("Create Mobile Controls")]
    private void CreateMobileControls()
    {
        // This is just documentation - actual setup should be done in Unity Editor
        Debug.Log(@"
To set up mobile controls manually:

1. VIRTUAL JOYSTICK:
   - Create UI > Image (name it 'JoystickBackground')
   - Position bottom-left, size ~150x150
   - Add child Image (name it 'JoystickHandle')
   - On JoystickHandle, add component: On-Screen Stick
   - Set Control Path to: <Gamepad>/leftStick
   - Set Movement Range to ~50

2. JUMP BUTTON:
   - Create UI > Button (name it 'JumpButton')
   - Position bottom-right
   - Add component: On-Screen Button
   - Set Control Path to: <Gamepad>/buttonSouth

The On-Screen components automatically feed into the Input System,
so your PlayerController will receive the input without any extra code.
");
    }
#endif
}
