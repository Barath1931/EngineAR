using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using UnityEngine.Android;

public class EngineAssistant : MonoBehaviour
{
    [Header("UI References")]
    public GameObject chatPanel;
    public GameObject toggleChatButton;
    public Transform chatContent;
    public ScrollRect scrollRect;
    public TMP_InputField inputField;
    public TextMeshProUGUI voiceButtonText;

    [Header("Groq Settings")]
    private string groqApiKey = "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";
    private string groqUrl = "https://api.groq.com/openai/v1/chat/completions";
    private string model = "llama-3.1-8b-instant";

    private bool isChatOpen = false;
    private GameObject loadingBubble;
    private bool isListening = false;
    private bool permissionGranted = false;
    private bool permissionRequested = false;

    private string systemPrompt =
        "You are an expert aircraft turbofan engine instructor explaining the CFM56 engine used in Boeing 737 aircraft. Give clear, short explanations suitable for training students inspecting engine components in VR.";

    void Start()
    {
        if (chatPanel != null)
            chatPanel.SetActive(false);

        Debug.Log("InputField: " + (inputField != null ? "OK" : "NULL"));
        Debug.Log("ChatContent: " + (chatContent != null ? "OK" : "NULL"));
        Debug.Log("ScrollRect: " + (scrollRect != null ? "OK" : "NULL"));
        Debug.Log("ChatPanel: " + (chatPanel != null ? "OK" : "NULL"));

#if UNITY_ANDROID && !UNITY_EDITOR
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            permissionGranted = true;
            Debug.Log("Mic permission already granted on Start");
        }
        else
        {
            StartCoroutine(RequestMicPermission());
        }
#else
        permissionGranted = true;
#endif
    }

    // ─── MIC PERMISSION ────────────────────────────────────────────
#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator RequestMicPermission()
    {
        if (permissionRequested)
            yield break;

        permissionRequested = true;

        yield return new WaitForSeconds(1f);

        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            permissionGranted = true;
            Debug.Log("Mic permission already granted");
            yield break;
        }

        Debug.Log("Requesting microphone permission...");

        bool finished = false;

        var callbacks = new PermissionCallbacks();

        callbacks.PermissionGranted += (perm) =>
        {
            permissionGranted = true;
            finished = true;
            Debug.Log("Mic permission GRANTED: " + perm);
        };

        callbacks.PermissionDenied += (perm) =>
        {
            permissionGranted = false;
            finished = true;
            Debug.Log("Mic permission DENIED: " + perm);
        };

        callbacks.PermissionDeniedAndDontAskAgain += (perm) =>
        {
            permissionGranted = false;
            finished = true;
            Debug.Log("Mic permission denied permanently: " + perm);
        };

        Permission.RequestUserPermission(Permission.Microphone, callbacks);

        float timeout = 30f;
        float elapsed = 0f;

        while (!finished && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!finished)
        {
            permissionGranted = Permission.HasUserAuthorizedPermission(Permission.Microphone);
            Debug.Log("Permission request timed out. Granted: " + permissionGranted);
        }

        permissionRequested = false;
    }
#endif

    // ─── TOGGLE CHAT ───────────────────────────────────────────────
    public void OnToggleChatPressed()
    {
        isChatOpen = !isChatOpen;
        chatPanel.SetActive(isChatOpen);

        if (toggleChatButton != null)
            toggleChatButton.SetActive(!isChatOpen);

        Debug.Log("Chat toggled: " + isChatOpen);
    }

    // ─── SEND MESSAGE ──────────────────────────────────────────────
    public void OnSendPressed()
    {
        Debug.Log("Send pressed");

        if (inputField == null)
        {
            Debug.LogError("InputField is NULL!");
            return;
        }

        string message = inputField.text.Trim();
        Debug.Log("Message: " + message);

        if (string.IsNullOrEmpty(message)) return;

        if (message == "Listening..." ||
            message == "Mic denied! Allow in phone Settings.")
            return;

        isListening = false;
        inputField.text = "";
        inputField.ForceLabelUpdate();

        if (voiceButtonText != null)
            voiceButtonText.text = "🎤";

        AddUserBubble(message);
        loadingBubble = AddLoadingBubble();
        StartCoroutine(SendToGroq(message));
    }

    // ─── VOICE BUTTON ──────────────────────────────────────────────
    public void OnVoiceButtonPressed()
    {
        Debug.Log("Voice pressed, permission: " + permissionGranted);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (isListening)
        {
            isListening = false;
            if (inputField != null) inputField.text = "";
            if (voiceButtonText != null) voiceButtonText.text = "🎤";
            return;
        }

        if (!permissionGranted)
        {
            permissionGranted = Permission.HasUserAuthorizedPermission(Permission.Microphone);
        }

        if (!permissionGranted)
        {
            if (inputField != null)
                inputField.text = "Mic permission needed!";

            if (!permissionRequested)
                StartCoroutine(RequestMicPermission());

            return;
        }

        StartCoroutine(ListenAndRecognize());
#else
        if (inputField != null)
            inputField.text = "What is the fan blade?";
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator ListenAndRecognize()
    {
        isListening = true;

        if (inputField != null) inputField.text = "Listening...";
        if (voiceButtonText != null) voiceButtonText.text = "🔴 Stop";

        bool done = false;
        string recognizedText = "";
        string errorText = "";

        AndroidJavaClass unityPlayer =
            new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity =
            unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        SpeechRecognitionListener listener =
            new SpeechRecognitionListener(gameObject.name);

        AndroidJavaClass speechRecognizerClass =
            new AndroidJavaClass("android.speech.SpeechRecognizer");

        AndroidJavaObject speechRecognizer = null;

        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            try
            {
                speechRecognizer =
                    speechRecognizerClass.CallStatic<AndroidJavaObject>(
                        "createSpeechRecognizer", activity);

                speechRecognizer.Call("setRecognitionListener", listener);

                AndroidJavaObject intent = new AndroidJavaObject(
                    "android.content.Intent",
                    "android.speech.action.RECOGNIZE_SPEECH");

                intent.Call<AndroidJavaObject>("putExtra",
                    "android.speech.extra.LANGUAGE_MODEL", "free_form");
                intent.Call<AndroidJavaObject>("putExtra",
                    "android.speech.extra.MAX_RESULTS", 1);
                intent.Call<AndroidJavaObject>("putExtra",
                    "android.speech.extra.LANGUAGE", "en-US");
                intent.Call<AndroidJavaObject>("putExtra",
                    "android.speech.extra.PARTIAL_RESULTS", true);

                speechRecognizer.Call("startListening", intent);
            }
            catch (System.Exception e)
            {
                errorText = "Error: " + e.Message;
                done = true;
            }
        }));

        float timer = 0f;

        while (!done && isListening && timer < 15f)
        {
            if (listener.HasResult)
            {
                recognizedText = listener.RecognizedText;
                done = true;
            }
            else if (listener.HasPartial && inputField != null)
            {
                inputField.text = listener.PartialText;
            }
            else if (listener.HasError)
            {
                errorText = listener.ErrorText;
                done = true;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (speechRecognizer != null)
        {
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try { speechRecognizer.Call("destroy"); } catch { }
            }));
        }

        isListening = false;

        if (voiceButtonText != null)
            voiceButtonText.text = "🎤";

        if (!string.IsNullOrEmpty(recognizedText))
        {
            if (inputField != null)
            {
                inputField.text = recognizedText;
                inputField.ForceLabelUpdate();
            }
        }
        else
        {
            string err = string.IsNullOrEmpty(errorText)
                ? "Timeout - try again"
                : errorText;

            if (inputField != null)
                inputField.text = err;
        }
    }
#endif

    // ─── GROQ API ──────────────────────────────────────────────────
    IEnumerator SendToGroq(string userMessage)
    {
        Debug.Log("Sending to Groq: " + userMessage);

        string jsonBody =
            "{\"model\":\"llama-3.1-8b-instant\",\"messages\":[{\"role\":\"system\",\"content\":\""
            + systemPrompt + "\"},{\"role\":\"user\",\"content\":\""
            + userMessage + "\"}]}";

        Debug.Log("JSON Body: " + jsonBody);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        UnityWebRequest request = new UnityWebRequest(groqUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + groqApiKey);

        yield return request.SendWebRequest();

        if (loadingBubble != null)
            Destroy(loadingBubble);

        Debug.Log("Groq code: " + request.responseCode);
        Debug.Log("Groq response: " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            string aiMessage = ParseGroqResponse(request.downloadHandler.text);
            AddAIBubble(aiMessage);
        }
        else
        {
            AddAIBubble("Error " + request.responseCode + ": " + request.error);
            Debug.LogError("Groq error: " + request.error);
        }
    }

    // ─── JSON ESCAPER ──────────────────────────────────────────────
    string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        StringBuilder sb = new StringBuilder();
        foreach (char c in text)
        {
            switch (c)
            {
                case '"':  sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n");  break;
                case '\r': sb.Append("\\r");  break;
                case '\t': sb.Append("\\t");  break;
                default:
                    if (c < 0x20)
                        sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    // ─── GROQ RESPONSE PARSER ──────────────────────────────────────
    string ParseGroqResponse(string json)
    {
        try
        {
            int idx = json.IndexOf("\"content\":");

            if (idx != -1)
            {
                int start = json.IndexOf("\"", idx + 10) + 1;
                int end = start;

                while (end < json.Length)
                {
                    if (json[end] == '"' && json[end - 1] != '\\')
                        break;
                    end++;
                }

                return json.Substring(start, end - start)
                    .Replace("\\n", "\n")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Parse error: " + e.Message);
        }

        return "Response error. Please try again.";
    }

    // ─── WHATSAPP STYLE BUBBLES ────────────────────────────────────
    void AddUserBubble(string message)
    {
        CreateBubble(message, true);
        ScrollToBottom();
    }

    void AddAIBubble(string message)
    {
        CreateBubble(message, false);
        ScrollToBottom();
    }

    GameObject AddLoadingBubble()
    {
        return CreateBubble("Thinking...", false);
    }

    GameObject CreateBubble(string message, bool isUser)
    {
        // Get actual width of chatContent at runtime so bubbles always fit
        RectTransform contentRect = chatContent as RectTransform;
        float panelWidth = contentRect != null ? contentRect.rect.width : 500f;
        float bubbleWidth = Mathf.Max(panelWidth - 40f, 200f);

        GameObject row = new GameObject(isUser ? "UserRow" : "AIRow");
        row.transform.SetParent(chatContent, false);

        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = isUser ?
            TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        rowLayout.padding = new RectOffset(8, 8, 4, 4);
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;

        ContentSizeFitter rowFitter = row.AddComponent<ContentSizeFitter>();
        rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement rowElement = row.AddComponent<LayoutElement>();
        rowElement.flexibleWidth = 1;

        GameObject bg = new GameObject("Bubble");
        bg.transform.SetParent(row.transform, false);

        Image img = bg.AddComponent<Image>();
        img.color = isUser ?
            new Color(0.07f, 0.53f, 0.25f, 1f) :
            new Color(0.15f, 0.15f, 0.18f, 1f);

        ContentSizeFitter bgFitter = bg.AddComponent<ContentSizeFitter>();
        bgFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        bgFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup bgLayout = bg.AddComponent<VerticalLayoutGroup>();
        bgLayout.padding = new RectOffset(16, 16, 10, 10);
        bgLayout.childForceExpandWidth = false;
        bgLayout.childForceExpandHeight = false;

        LayoutElement bgElement = bg.AddComponent<LayoutElement>();
        bgElement.flexibleWidth = 0;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(bg.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = 16;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        // Dynamic width based on actual panel size at runtime
        LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
        textLayout.preferredWidth = bubbleWidth;
        textLayout.flexibleWidth = 0;

        ContentSizeFitter textFitter = textObj.AddComponent<ContentSizeFitter>();
        textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return row;
    }

    void ScrollToBottom()
    {
        StartCoroutine(ScrollNextFrame());
    }

    IEnumerator ScrollNextFrame()
    {
        yield return new WaitForEndOfFrame();

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}