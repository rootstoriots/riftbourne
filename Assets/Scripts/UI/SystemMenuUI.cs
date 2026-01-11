using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Riftbourne.UI
{
    /// <summary>
    /// System menu UI component.
    /// Opens when Escape is pressed (if status menu is closed).
    /// Contains Save, Load, Settings, and Quit options.
    /// </summary>
    public class SystemMenuUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject systemMenuPanel;

        [Header("Menu Buttons")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button closeButton;

        [Header("Save/Load UI")]
        [SerializeField] private SaveLoadUI saveLoadUI;

        private PlayerInputActions inputActions;
        private float previousTimeScale = 1f;
        private bool isOpen = false;
        private float lastOpenTime = 0f;
        private const float OPEN_COOLDOWN = 0.1f; // Prevent immediate closing after opening

        private void Awake()
        {
            inputActions = new PlayerInputActions();
            
            if (systemMenuPanel != null)
            {
                systemMenuPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            inputActions?.Gameplay.Enable();
        }

        private void OnDisable()
        {
            inputActions?.Gameplay.Disable();
        }

        private void Update()
        {
            // Handle Escape key - only if system menu is open
            // If closed, StatusMenuUI will handle opening it
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // Prevent closing immediately after opening (within cooldown period)
                if (isOpen && Time.unscaledTime - lastOpenTime > OPEN_COOLDOWN)
                {
                    CloseMenu();
                }
            }
        }

        /// <summary>
        /// Open the system menu.
        /// </summary>
        public void OpenMenu()
        {
            if (systemMenuPanel == null)
            {
                Debug.LogError("SystemMenuUI: systemMenuPanel is null!");
                return;
            }

            if (isOpen) return;

            systemMenuPanel.SetActive(true);
            isOpen = true;
            lastOpenTime = Time.unscaledTime; // Record when menu was opened
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // Show main menu buttons, hide save/load UI
            if (saveLoadUI != null)
            {
                saveLoadUI.Hide();
            }

            Debug.Log("SystemMenuUI: System menu opened");
        }

        /// <summary>
        /// Close the system menu.
        /// </summary>
        public void CloseMenu()
        {
            if (systemMenuPanel == null || !isOpen) return;

            systemMenuPanel.SetActive(false);
            isOpen = false;
            Time.timeScale = previousTimeScale;

            // Hide save/load UI if visible
            if (saveLoadUI != null)
            {
                saveLoadUI.Hide();
            }

            Debug.Log("SystemMenuUI: System menu closed");
        }

        /// <summary>
        /// Toggle the system menu.
        /// </summary>
        public void ToggleMenu()
        {
            if (isOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }

        /// <summary>
        /// Check if the menu is open.
        /// </summary>
        public bool IsOpen => isOpen;

        private void Start()
        {
            // Setup button listeners
            if (saveButton != null)
            {
                saveButton.onClick.AddListener(OnSaveButtonClicked);
            }

            if (loadButton != null)
            {
                loadButton.onClick.AddListener(OnLoadButtonClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseMenu);
            }
        }

        private void OnSaveButtonClicked()
        {
            Debug.Log("SystemMenuUI: Save button clicked");
            if (saveLoadUI != null)
            {
                saveLoadUI.ShowSavePanel();
            }
        }

        private void OnLoadButtonClicked()
        {
            Debug.Log("SystemMenuUI: Load button clicked");
            if (saveLoadUI != null)
            {
                saveLoadUI.ShowLoadPanel();
            }
        }

        private void OnSettingsButtonClicked()
        {
            Debug.Log("SystemMenuUI: Settings button clicked");
            // TODO: Implement settings panel
        }

        private void OnQuitButtonClicked()
        {
            Debug.Log("SystemMenuUI: Quit button clicked");
            // TODO: Show confirmation dialog
            Application.Quit();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}
