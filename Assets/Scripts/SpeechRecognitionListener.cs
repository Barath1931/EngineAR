using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
public class SpeechRecognitionListener : AndroidJavaProxy
{
    public bool HasResult { get; private set; }
    public bool HasError { get; private set; }
    public bool HasPartial { get; private set; }

    public string RecognizedText { get; private set; }
    public string ErrorText { get; private set; }
    public string PartialText { get; private set; }

    public SpeechRecognitionListener(string goName)
        : base("android.speech.RecognitionListener")
    {
        HasResult = false;
        HasError = false;
        HasPartial = false;
    }

    public void onResults(AndroidJavaObject results)
    {
        var matches = results.Call<AndroidJavaObject>(
            "getStringArrayList", "results_recognition");

        if (matches != null && matches.Call<int>("size") > 0)
        {
            RecognizedText = matches.Call<string>("get", 0);
            HasResult = true;
        }
    }

    public void onPartialResults(AndroidJavaObject partialResults)
    {
        var matches = partialResults.Call<AndroidJavaObject>(
            "getStringArrayList", "results_recognition");

        if (matches != null && matches.Call<int>("size") > 0)
        {
            PartialText = matches.Call<string>("get", 0);
            HasPartial = true;
        }
    }

    public void onError(int error)
    {
        ErrorText = "Error code: " + error;
        HasError = true;
    }

    public void onReadyForSpeech(AndroidJavaObject bundle) { }
    public void onBeginningOfSpeech() { }
    public void onRmsChanged(float rmsdB) { }
    public void onBufferReceived(byte[] buffer) { }
    public void onEndOfSpeech() { }
    public void onEvent(int eventType, AndroidJavaObject bundle) { }
}

#else
public class SpeechRecognitionListener : MonoBehaviour
{
    public SpeechRecognitionListener(string n) { }
}
#endif