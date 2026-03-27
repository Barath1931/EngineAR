using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EngineStartupController : MonoBehaviour
{
    [Header("Rotating Parts")]
    public Transform fanBlades;
    public Transform compressorStages;
    public Transform turbineBlades;

    [Header("Rotation Axes")]
    public Vector3 fanAxis = Vector3.forward;
    public Vector3 compressorAxis = Vector3.forward;
    public Vector3 turbineAxis = Vector3.forward;

    [Header("UI")]
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI statusText;
    public Button closeButton;

    [Header("Settings")]
    public int totalComponents = 1;

    [Header("Particle Controller")]
    public EngineParticleController particleController;

    private readonly float[] n1RPM     = { 680f,  2880f,  3200f  };
    private readonly float[] n2RPM     = { 3200f, 11700f, 13000f };
    private readonly string[] modeNames = { "Engine Start\n680 RPM", "Cruise\n2,880 RPM", "Max Thrust\n3,200 RPM" };

    private float currentN1 = 0f;
    private float currentN2 = 0f;
    private float targetN1  = 0f;
    private float targetN2  = 0f;

    private int scannedComponents = 0;
    private bool isRunning = false;
    private float rampSpeed = 120f;

    private GameObject engineControlPanel;

    void Start()
    {
        engineControlPanel = GameObject.Find("EngineControlPanel");

        if (engineControlPanel == null)
            Debug.LogError("[EngineStartup] EngineControlPanel NOT FOUND in scene!");
        else
            Debug.Log("[EngineStartup] EngineControlPanel found: " + engineControlPanel.name);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClosePressed);

        HidePanel();
        UpdateCounterText();
    }

    void Update()
    {
        if (!isRunning) return;

        currentN1 = Mathf.MoveTowards(currentN1, targetN1, rampSpeed * Time.deltaTime);
        currentN2 = Mathf.MoveTowards(currentN2, targetN2, rampSpeed * Time.deltaTime);

        float n1Deg = (currentN1 / 60f) * 360f;
        float n2Deg = (currentN2 / 60f) * 360f;

        if (fanBlades != null)
            fanBlades.Rotate(fanAxis, n1Deg * Time.deltaTime, Space.Self);
        if (compressorStages != null)
            compressorStages.Rotate(compressorAxis, n2Deg * Time.deltaTime, Space.Self);
        if (turbineBlades != null)
            turbineBlades.Rotate(turbineAxis, n2Deg * Time.deltaTime, Space.Self);

        if (particleController != null)
        {
            float n1Fraction = currentN1 / 3200f;
            particleController.UpdateIntensity(n1Fraction);
        }
    }

    public void NotifyComponentScanned()
    {
        scannedComponents++;
        scannedComponents = Mathf.Clamp(scannedComponents, 0, totalComponents);
        UpdateCounterText();

        if (scannedComponents >= totalComponents)
        {
            Debug.Log("[EngineStartup] reached — showing panel");
            ShowPanel();
        }
    }

    public void ShowPanel()
    {
        if (engineControlPanel == null)
            engineControlPanel = GameObject.Find("EngineControlPanel");

        if (engineControlPanel != null)
        {
            engineControlPanel.SetActive(true);

            CanvasGroup cg = engineControlPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha          = 1f;
                cg.interactable   = true;
                cg.blocksRaycasts = true;
            }

            if (statusText != null)
                statusText.text = "Engine Ready";
        }
        else
        {
            Debug.LogError("[EngineStartup] Still can't find EngineControlPanel!");
        }
    }

    private void HidePanel()
    {
        if (engineControlPanel != null)
            engineControlPanel.SetActive(false);
    }

    private void UpdateCounterText()
    {
        if (counterText != null)
            counterText.text = "Components: " + scannedComponents + "/" + totalComponents;
    }

    public void SetMode(int modeIndex)
    {
        targetN1  = n1RPM[modeIndex];
        targetN2  = n2RPM[modeIndex];
        isRunning = true;

        if (statusText != null)
            statusText.text = modeNames[modeIndex];

        if (particleController != null)
        {
            float n1Fraction = targetN1 / 3200f;
            particleController.OnEngineStarted(n1Fraction);
        }
    }

    public void OnStartModePressed()  => SetMode(0);
    public void OnCruiseModePressed() => SetMode(1);
    public void OnMaxModePressed()    => SetMode(2);

    public void OnStopPressed()
    {
        targetN1 = 0f;
        targetN2 = 0f;
        StartCoroutine(StopWhenSpunDown());
    }

    public void OnClosePressed()
    {
        // Stop engine first if running
        if (isRunning)
        {
            targetN1 = 0f;
            targetN2 = 0f;
            currentN1 = 0f;
            currentN2 = 0f;
            isRunning = false;
        }

        if (particleController != null)
            particleController.OnEngineStopped();

        HidePanel();
    }

    private IEnumerator StopWhenSpunDown()
    {
        while (currentN1 > 1f || currentN2 > 1f)
            yield return null;

        currentN1 = 0f;
        currentN2 = 0f;
        isRunning = false;

        // Show RPM as 0 when stopped
        if (statusText != null)
            statusText.text = "Engine Off\n0 RPM";

        if (particleController != null)
            particleController.OnEngineStopped();
    }

    public void ResetCounter()
    {
        scannedComponents = 0;
        HidePanel();
        UpdateCounterText();
    }
}