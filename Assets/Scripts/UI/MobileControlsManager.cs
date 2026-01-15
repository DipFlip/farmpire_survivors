using UnityEngine;
using UnityEngine.InputSystem;

public class MobileControlsManager : MonoBehaviour
{
    [SerializeField] private Canvas mobileControlsCanvas;

    [Header("Editor Settings")]
    [SerializeField] private bool showInEditor = false;

    private void Awake()
    {
        if (mobileControlsCanvas == null)
            mobileControlsCanvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        UpdateVisibility();

        // Re-check when devices change (e.g., tablet user connects keyboard)
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        bool showMobileControls = IsTouchDevice();
        mobileControlsCanvas.enabled = showMobileControls;
    }

    private bool IsTouchDevice()
    {
#if UNITY_EDITOR
        return showInEditor;
#else
        return Touchscreen.current != null;
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (mobileControlsCanvas == null)
            mobileControlsCanvas = GetComponent<Canvas>();

        if (mobileControlsCanvas != null)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (mobileControlsCanvas != null)
                    mobileControlsCanvas.enabled = showInEditor;
            };
        }
    }
#endif
}
