using Core;
using TMPro;
using UnityEngine;

/*
* This service provides a way to display floating text feedback for player actions. It creates temporary UI 
* elements that show messages with customizable appearance and behavior, such as duration and rise effect. The 
* service ensures that only one instance exists and can be accessed globally.
*/
public class ActionFeedbackService : MonoBehaviour, IActionFeedbackService
{
    private static ActionFeedbackService instance; // Singleton instance reference.

    /*
    * Accessor for the singleton instance. It ensures that an instance exists by searching the scene or creating a
    * new one if necessary. The instance is marked to persist across scene loads.
    * Exposes: - Instance: A globally accessible reference to the IActionFeedbackService implementation.
    * Requires: - The scene must allow for the creation of GameObjects if no instance is found. 
    * - The service relies on the presence of a Canvas in the scene to display feedback; if none is found, 
    * it will log messages to the console instead.
    */
    public static IActionFeedbackService Instance
    {
        get { return EnsureInstance(); }
    }

    /*
    * Resets the static instance reference when the game reloads to prevent stale references. This is important 
    *for editor play mode and ensures that a new instance will be created if needed.
    */
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    /*
    * Ensures that a singleton instance of ActionFeedbackService exists. It first checks if the static instance
    * reference is set, then searches the scene for an existing instance. If none is found, it creates a new 
    * GameObject and attaches the service component to it. The new instance is marked to not be destroyed on load.
     * Exposes: - None (this is an internal method used by the Instance property).
     * Requires: - The scene must allow for the creation of GameObjects if no instance is found. 
     * - The service relies on the presence of a Canvas in the scene to display feedback; if none is found, 
     * it will log messages to the console instead.
    */
    private static ActionFeedbackService EnsureInstance()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<ActionFeedbackService>();
        if (instance != null)
            return instance;

        GameObject go = new GameObject(nameof(ActionFeedbackService));
        instance = go.AddComponent<ActionFeedbackService>();
        DontDestroyOnLoad(go);
        return instance;
    }

    /*
    * Initializes the singleton instance. If another instance already exists, it destroys the new one to maintain
    * the singleton pattern. The surviving instance is marked to persist across scene loads.
     * Exposes: - None (this is an internal method called by Unity during the component's lifecycle).
     * Requires: - The scene must allow for the creation of GameObjects if no instance is found. 
     * - The service relies on the presence of a Canvas in the scene to display feedback; if none is found, 
     * it will log messages to the console instead. 
    */
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /*
    * Displays a floating text message with specified parameters. It creates a new GameObject with the necessary
    * components to show the message on a Canvas. If a preferred Canvas is provided and valid, it uses that; 
    *otherwise, it searches for any active Canvas in the scene. If no Canvas is found, it logs the message to the console instead.
     * Exposes: - ShowFeedback: A method to display a feedback message with customizable appearance and behavior.
     * Requires: - The scene must allow for the creation of GameObjects if no instance is found. 
     * - The service relies on the presence of a Canvas in the scene to display feedback; if none is found, 
     * it will log messages to the console instead. 
     * - The method parameters must be valid (e.g., non-negative duration and rise values) for proper behavior.
    */
    public void ShowFeedback(
        string message,
        bool richText,
        Canvas preferredCanvas,
        Vector2 anchor,
        Vector2 size,
        int fontSize,
        float durationSeconds,
        float risePixels,
        Color color,
        string objectName)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        // Attempt to find a valid canvas to display the feedback. If none is found, log the message instead.
        Canvas canvas = ResolveCanvas(preferredCanvas);
        if (canvas == null)
        {
            Debug.Log(message);
            return;
        }

        // Create a new GameObject for the feedback text and configure its components.
        GameObject go = new GameObject(
            string.IsNullOrWhiteSpace(objectName) ? "ActionFeedback" : objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            typeof(CanvasGroup),
            typeof(FloatingTextPopup));
        go.transform.SetParent(canvas.transform, false);
        
        // Configure the TextMeshProUGUI component with the provided parameters.
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.richText = richText;
        label.text = message;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = Mathf.Max(10, fontSize);
        label.color = color;

        // Configure the RectTransform to position the feedback according to the specified anchor and size.
        RectTransform rt = label.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        // Configure the FloatingTextPopup component to handle the display duration and rise effect.
        FloatingTextPopup popup = go.GetComponent<FloatingTextPopup>();
        popup.Configure(Mathf.Max(0.1f, durationSeconds), Mathf.Max(0f, risePixels));
    }

    /*
    * Resolves a valid Canvas to use for displaying feedback. It first checks if the preferred Canvas is valid and 
    * active. If not, it searches the scene for any active Canvas and returns the first one found. If no active 
    * Canvas is found, it returns null.
     * Exposes: - None (this is an internal method used by ShowFeedback to find a Canvas).
     * Requires: - The scene must contain at least one active Canvas for feedback to be displayed; otherwise, 
     * messages will be logged to the console instead. 
     * - The preferredCanvas parameter can be null or inactive, in which case the method will search for 
     * alternatives. 
     * - The method assumes that the scene is not excessively large with many Canvas objects, as it performs a \
     * search through all Canvas instances; in a very large scene, this could have performance implications.
    */
    private static Canvas ResolveCanvas(Canvas preferredCanvas)
    {
        if (preferredCanvas != null && preferredCanvas.isActiveAndEnabled)
            return preferredCanvas;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.isActiveAndEnabled)
                return canvas;
        }

        return null;
    }
}
