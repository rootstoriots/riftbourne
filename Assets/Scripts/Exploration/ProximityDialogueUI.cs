using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Riftbourne.Exploration
{
    /// <summary>
    /// Controls a manually-created 2D subtitle UI for proximity dialogue.
    /// The UI should be set up in the scene with a Canvas, background Image, and Text component.
    /// This script just activates it and sets the text content.
    /// Supports both legacy Text and TextMeshProUGUI.
    /// </summary>
    public class ProximityDialogueUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The root GameObject for the dialogue UI (should contain Canvas, background, text).")]
        [SerializeField] private GameObject dialoguePanel;

        [Tooltip("Text component that displays the dialogue (TextMeshProUGUI or legacy Text).")]
        [SerializeField] private TextMeshProUGUI dialogueTextTMP;

        [Tooltip("Legacy Text component (use if not using TextMeshPro).")]
        [SerializeField] private Text dialogueTextLegacy;

        [Tooltip("Background image (optional, for styling).")]
        [SerializeField] private Image backgroundImage;

        [Header("Settings")]
        [Tooltip("Duration of fade in/out animation (in seconds).")]
        [Range(0.1f, 2f)]
        [SerializeField] private float fadeDuration = 0.5f;

        // Components
        private AudioSource audioSource;
        private CanvasGroup canvasGroup; // For fade animations

        // State
        private DialogueEntry currentEntry;
        private ProximityDialogueData dialogueData;
        private System.Action onFinishedCallback;
        private Coroutine fadeCoroutine;
        private Coroutine dialogueSequenceCoroutine;

        private void Awake()
        {
            // Ensure dialogue panel starts disabled
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        /// <summary>
        /// Show dialogue with the specified entry.
        /// </summary>
        public void ShowDialogue(DialogueEntry entry, ProximityDialogueData data, System.Action onFinished)
        {
            if (entry == null || data == null)
            {
                Debug.LogError("[ProximityDialogueUI] Cannot show dialogue - null parameters!");
                return;
            }

            currentEntry = entry;
            dialogueData = data;
            onFinishedCallback = onFinished;

            // Get formatted dialogue text (with speaker name if configured)
            string formattedText = data.GetFormattedDialogueText(entry);
            
            if (string.IsNullOrEmpty(formattedText))
            {
                formattedText = "[No Text]";
                Debug.LogWarning("[ProximityDialogueUI] Dialogue entry has empty text!");
            }

            // Set text content (support both TextMeshPro and legacy Text)
            bool textSet = false;
            
            if (dialogueTextTMP != null)
            {
                dialogueTextTMP.text = formattedText;
                textSet = true;
            }
            else if (dialogueTextLegacy != null)
            {
                dialogueTextLegacy.text = formattedText;
                textSet = true;
            }

            if (!textSet)
            {
                Debug.LogError("[ProximityDialogueUI] Neither DialogueTextTMP nor DialogueTextLegacy is assigned! Please assign one in the Inspector.");
            }

            // Activate the panel
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }
            else
            {
                Debug.LogError("[ProximityDialogueUI] DialoguePanel GameObject is not assigned!");
            }

            // Get or create CanvasGroup for fade animations
            if (canvasGroup == null)
            {
                canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
                }
            }

            // Get or create AudioSource
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f; // 2D sound
                }
            }

            // Start dialogue sequence
            dialogueSequenceCoroutine = StartCoroutine(DialogueSequence());
        }

        /// <summary>
        /// Hide the dialogue UI immediately.
        /// </summary>
        public void HideDialogue()
        {
            if (dialogueSequenceCoroutine != null)
            {
                StopCoroutine(dialogueSequenceCoroutine);
            }
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Coroutine that handles the full dialogue sequence: fade in, display, fade out.
        /// </summary>
        private IEnumerator DialogueSequence()
        {
            // Fade in
            yield return StartCoroutine(FadeIn());

            // Play audio if available
            if (currentEntry.audioClip != null && audioSource != null)
            {
                audioSource.clip = currentEntry.audioClip;
                audioSource.Play();
            }

            // Wait for display duration (or audio length if longer)
            float displayDuration = dialogueData.GetDisplayDuration(currentEntry);
            float waitDuration = displayDuration;
            if (currentEntry.audioClip != null && audioSource != null && audioSource.isPlaying)
            {
                waitDuration = Mathf.Max(displayDuration, currentEntry.audioClip.length);
            }
            yield return new WaitForSeconds(waitDuration);

            // Fade out
            yield return StartCoroutine(FadeOut());

            // Notify manager that dialogue finished
            onFinishedCallback?.Invoke();

            // Hide the panel
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        /// <summary>
        /// Fade in the dialogue UI.
        /// </summary>
        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = alpha;
                yield return null;
            }

            canvasGroup.alpha = 1f; // Ensure fully visible
        }

        /// <summary>
        /// Fade out the dialogue UI.
        /// </summary>
        private IEnumerator FadeOut()
        {
            if (canvasGroup == null) yield break;

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                canvasGroup.alpha = alpha;
                yield return null;
            }

            canvasGroup.alpha = 0f; // Ensure fully transparent
        }

        private void OnDestroy()
        {
            // Stop any playing audio
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // Stop coroutines if still running
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            if (dialogueSequenceCoroutine != null)
            {
                StopCoroutine(dialogueSequenceCoroutine);
            }
        }
    }
}
