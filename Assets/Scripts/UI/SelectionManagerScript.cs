using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SelectionManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject engineOptionPanel;
    public GameObject loadingPanel;
    public TMPro.TextMeshProUGUI loadingText;

    void Start()
    {
        // Make sure loading panel is hidden at start
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        // Make sure engine panel is visible at start
        if (engineOptionPanel != null)
            engineOptionPanel.SetActive(true);
    }

    // Called when user taps CFM56 Engine button
    public void OnSelectCFM56()
    {
        StartCoroutine(LoadEngineScene("MainScene"));
    }

    IEnumerator LoadEngineScene(string sceneName)
    {
        engineOptionPanel.SetActive(false);
        loadingPanel.SetActive(true);
        loadingText.text = "Loading CFM56 Engine...";

        yield return new WaitForSeconds(0.5f);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        while (!op.isDone)
        {
            loadingText.text = "Loading... " +
                Mathf.RoundToInt(op.progress * 100) + "%";
            yield return null;
        }
    }
}