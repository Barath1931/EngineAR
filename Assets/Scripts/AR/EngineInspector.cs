using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EngineInspector : MonoBehaviour
{
    [Header("Engine Setup")]
    public GameObject engineRoot;

    [Header("UI References")]
    public GameObject nextButton;
    public GameObject backButton;
    public GameObject infoPanel;
    public GameObject showPanelButton;
    public TMPro.TextMeshProUGUI componentNameText;
    public TMPro.TextMeshProUGUI componentDescText;
    public TMPro.TextMeshProUGUI counterText;

    [Header("AR Camera")]
    public Camera arCamera;

    [Header("Engine Startup Controller")]
    public EngineStartupController engineStartupController;

    private List<GameObject> components = new List<GameObject>();
    private int currentIndex = -1;
    private Dictionary<int, string[]> componentInfo = new Dictionary<int, string[]>();
    private bool infoPanelManuallyClosed = false;

    // Touch & Gesture
    private Vector2 lastTouchPos;
    private float lastPinchDistance;
    private bool isDragging = false;

    void Start()
    {
        foreach (Transform child in engineRoot.transform)
            components.Add(child.gameObject);

        foreach (GameObject comp in components)
            comp.SetActive(false);

        SetupComponentInfo();
        infoPanel.SetActive(false);

        if (showPanelButton != null)
            showPanelButton.SetActive(false);

        if (counterText != null)
            counterText.text = "0 / " + components.Count;

        Debug.Log("Engine Inspector Ready. Total components: " + components.Count);
    }

    void Update()
    {
        HandleTouchInput();
        HandleComponentClick();
        // HandleControllerInput(); // Quest only — re-enable when building for Quest
    }

    // ─── TOUCH GESTURES ────────────────────────────────────────────

    void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (IsTouchingUI(touch.position)) return;

            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPos = touch.position;
                isDragging = true;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector2 delta = touch.position - lastTouchPos;
                engineRoot.transform.Rotate(arCamera.transform.up, -delta.x * 0.3f, Space.World);
                engineRoot.transform.Rotate(arCamera.transform.right, delta.y * 0.3f, Space.World);
                lastTouchPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                isDragging = false;
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float currentPinchDistance = Vector2.Distance(t0.position, t1.position);

            if (t1.phase == TouchPhase.Began)
            {
                lastPinchDistance = currentPinchDistance;
            }
            else if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
            {
                float pinchDelta = currentPinchDistance - lastPinchDistance;
                float scaleFactor = 1 + pinchDelta * 0.001f;
                engineRoot.transform.localScale *= scaleFactor;

                float clampedScale = Mathf.Clamp(engineRoot.transform.localScale.x, 0.01f, 5f);
                engineRoot.transform.localScale = Vector3.one * clampedScale;

                Vector2 t0Prev = t0.position - t0.deltaPosition;
                Vector2 t1Prev = t1.position - t1.deltaPosition;
                Vector2 prevMid = (t0Prev + t1Prev) / 2f;
                Vector2 currMid = (t0.position + t1.position) / 2f;
                Vector2 moveDelta = currMid - prevMid;

                engineRoot.transform.position += new Vector3(moveDelta.x * 0.001f, moveDelta.y * 0.001f, 0f);
                lastPinchDistance = currentPinchDistance;
            }
        }
    }

    // ─── CLICK COMPONENT TO SEE DETAILS ───────────────────────────

    void HandleComponentClick()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (IsTouchingUI(touch.position)) return;

                Ray ray = arCamera.ScreenPointToRay(touch.position);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    Transform hitTransform = hit.transform;
                    for (int i = 0; i < components.Count; i++)
                    {
                        if (components[i] == hitTransform.gameObject ||
                            hitTransform.IsChildOf(components[i].transform))
                        {
                            currentIndex = i;
                            infoPanelManuallyClosed = false;
                            ShowInfoPanel(i);
                            UpdateCounter();
                            Debug.Log("Clicked component: " + i);
                            break;
                        }
                    }
                }
            }
        }
    }

    bool IsTouchingUI(Vector2 screenPos)
    {
        if (screenPos.y < 200f) return true;
        if (infoPanel.activeSelf && screenPos.x < 450f) return true;
        return false;
    }

    // ─── NEXT ──────────────────────────────────────────────────────

    public void OnNextPressed()
    {
        if (currentIndex >= components.Count - 1)
        {
            Debug.Log("All components placed — showing engine control panel.");
            if (engineStartupController != null)
                engineStartupController.ShowPanel();
            return;
        }

        currentIndex++;
        components[currentIndex].SetActive(true);
        StartCoroutine(AnimateAppear(components[currentIndex]));
        UpdateCounter();

        if (componentInfo.ContainsKey(currentIndex))
        {
            componentNameText.text = componentInfo[currentIndex][0];
            componentDescText.text = componentInfo[currentIndex][1];
        }
        else
        {
            componentNameText.text = "Component " + currentIndex;
            componentDescText.text = "Part of the CFM56 turbofan engine assembly.";
        }

        if (!infoPanelManuallyClosed)
        {
            infoPanel.SetActive(true);
            if (showPanelButton != null)
                showPanelButton.SetActive(false);
        }
    }

    // ─── BACK ──────────────────────────────────────────────────────

    public void OnBackPressed()
    {
        if (currentIndex < 0)
        {
            Debug.Log("Already at beginning.");
            return;
        }

        components[currentIndex].SetActive(false);
        currentIndex--;
        UpdateCounter();

        if (currentIndex >= 0)
        {
            if (componentInfo.ContainsKey(currentIndex))
            {
                componentNameText.text = componentInfo[currentIndex][0];
                componentDescText.text = componentInfo[currentIndex][1];
            }
            else
            {
                componentNameText.text = "Component " + currentIndex;
                componentDescText.text = "Part of the CFM56 turbofan engine assembly.";
            }

            if (!infoPanelManuallyClosed)
            {
                infoPanel.SetActive(true);
                if (showPanelButton != null)
                    showPanelButton.SetActive(false);
            }
        }
        else
        {
            infoPanel.SetActive(false);
            if (showPanelButton != null)
                showPanelButton.SetActive(false);
        }
    }

    // ─── INFO PANEL ────────────────────────────────────────────────

    void ShowInfoPanel(int index)
    {
        infoPanel.SetActive(true);

        if (showPanelButton != null)
            showPanelButton.SetActive(false);

        if (componentInfo.ContainsKey(index))
        {
            componentNameText.text = componentInfo[index][0];
            componentDescText.text = componentInfo[index][1];
        }
        else
        {
            componentNameText.text = "Component " + index;
            componentDescText.text = "Part of the CFM56 turbofan engine assembly.";
        }
    }

    public void OnHidePanelPressed()
    {
        infoPanelManuallyClosed = true;
        infoPanel.SetActive(false);
        if (showPanelButton != null)
            showPanelButton.SetActive(true);
    }

    public void OnShowPanelPressed()
    {
        infoPanelManuallyClosed = false;
        if (currentIndex >= 0)
            ShowInfoPanel(currentIndex);
    }

    void UpdateCounter()
    {
        if (counterText != null)
            counterText.text = Mathf.Max(currentIndex + 1, 0) + " / " + components.Count;
    }

    // ─── ANIMATE ───────────────────────────────────────────────────

    IEnumerator AnimateAppear(GameObject obj)
    {
        Transform meshChild = obj.transform.childCount > 0
            ? obj.transform.GetChild(0)
            : obj.transform;

        Vector3 originalScale = meshChild.localScale;
        meshChild.localScale = Vector3.zero;

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = 1 - Mathf.Pow(1 - t, 3);
            meshChild.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            yield return null;
        }

        meshChild.localScale = originalScale;
    }

    // ─── COMPONENT INFO ────────────────────────────────────────────

    void SetupComponentInfo()
    {
        componentInfo[0]  = new string[] { "LP Compressor Rotor",        "Low-pressure compressor rotor. First stage of core compression, driven by the LP turbine via the LP shaft." };
        componentInfo[1]  = new string[] { "Compressor Blades Set 1",    "First set of LP compressor rotor blades. Accelerates and compresses incoming core airflow." };
        componentInfo[2]  = new string[] { "Compressor Blades Set 2",    "Second set of LP compressor rotor blades. Continues core airflow compression before the HP stages." };
        componentInfo[3]  = new string[] { "HP Shaft",                   "High-pressure shaft connecting the HP compressor to the HP turbine. Rotates at up to 14,500 RPM." };
        componentInfo[4]  = new string[] { "Compressor Blade Lock",      "Retention device that locks compressor blades into their disk slots and prevents axial movement." };
        componentInfo[5]  = new string[] { "Fan Blade",                  "Single-stage wide-chord fan blade. Generates ~80% of total CFM56 thrust by accelerating bypass air." };
        componentInfo[6]  = new string[] { "Fuel Pipes Set 1",           "First set of high-pressure fuel delivery pipes routing fuel from the pump to the combustor nozzles." };
        componentInfo[7]  = new string[] { "Compressor Blade Mount",     "Structural mount that secures compressor blade assemblies to the rotor disk at each stage." };
        componentInfo[8]  = new string[] { "Blade Root",                 "The root section of a compressor or fan blade that slots into the disk and transfers centrifugal loads." };
        componentInfo[9]  = new string[] { "Fuel Control Unit",          "Hydromechanical unit that meters fuel flow based on FADEC commands and engine operating conditions." };
        componentInfo[10] = new string[] { "Fuel Pump",                  "High-pressure gear pump driven by the accessory gearbox. Delivers pressurised fuel to the FADEC metering valve." };
        componentInfo[11] = new string[] { "Heat Exchanger",             "Air-cooled heat exchanger that cools engine oil and fuel using bypass air before combustion." };
        componentInfo[12] = new string[] { "Fuel Nozzles Set 1",         "First set of atomising fuel nozzles in the combustor dome. Sprays fine fuel mist for efficient combustion." };
        componentInfo[13] = new string[] { "Fuel Injectors Set 1",       "First set of fuel injectors that introduce precisely metered fuel into the primary combustion zone." };
        componentInfo[14] = new string[] { "Compressor Blades",          "Multi-stage HP compressor blades that progressively compress air to a ~30:1 pressure ratio." };
        componentInfo[15] = new string[] { "Fan Spinner Hub",            "Central hub of the fan spinner assembly. Mounts to the fan shaft and supports the nose cone." };
        componentInfo[16] = new string[] { "Fan Hub",                    "Main structural hub of the fan rotor. Carries all fan blades and transmits torque from the LP shaft." };
        componentInfo[17] = new string[] { "Gearbox",                    "Accessory gearbox driven by the HP spool. Powers engine accessories including fuel pump, oil pump and IDG." };
        componentInfo[18] = new string[] { "Fan Spinner Hub (Outer)",    "Outer section of the fan spinner hub assembly. Provides aerodynamic fairing over the fan shaft." };
        componentInfo[19] = new string[] { "Fan Stator Vanes",           "Fixed vanes behind the fan rotor. Straighten swirling bypass airflow before it enters the bypass duct." };
        componentInfo[20] = new string[] { "Fan Inlet Lip",              "Aerodynamic inlet lip at the front of the nacelle. Captures and conditions incoming air before the fan." };
        componentInfo[21] = new string[] { "Compressor Disc",            "Forged titanium or steel disc that carries compressor blades at each compression stage." };
        componentInfo[22] = new string[] { "Shaft Coupling",             "Mechanical coupling connecting the LP and HP shaft assemblies. Transfers torque between spools." };
        componentInfo[23] = new string[] { "HP Compressor Rotor",        "Nine-stage high-pressure compressor rotor. Compresses air to ~30:1 pressure ratio before combustion." };
        componentInfo[24] = new string[] { "Combustor Casing",           "Outer pressure vessel surrounding the combustor liner. Handles extreme pressure and temperature loads." };
        componentInfo[25] = new string[] { "HP Compressor Stator",       "Fixed stator vane assembly between HP compressor rotor stages. Diffuses and redirects compressed airflow." };
        componentInfo[26] = new string[] { "LP Compressor Casing",       "Structural casing surrounding the LP compressor stages. Carries loads and houses stator vane assemblies." };
        componentInfo[27] = new string[] { "Turbine Nozzle Guide Vane",  "First fixed vane after the combustor. Accelerates and angles hot gas onto HP turbine rotor blades." };
        componentInfo[28] = new string[] { "Turbine Shaft",              "LP turbine shaft connecting the LP turbine to the fan and booster. Runs concentrically inside the HP shaft." };
        componentInfo[29] = new string[] { "Bearing Housing",            "Structural housing that supports and locates the main engine bearings under all operating loads." };
        componentInfo[30] = new string[] { "Fuel Pipes Set 2",           "Second set of fuel delivery pipes distributing fuel to additional combustor nozzle positions." };
        componentInfo[31] = new string[] { "Starter Motor",              "Air turbine starter that spins the HP spool to motoring speed before ignition and light-off." };
        componentInfo[32] = new string[] { "Accessory Gearbox",          "Bevel gear-driven box on the engine core. Powers the fuel pump, oil pump, hydraulic pump and IDG." };
        componentInfo[33] = new string[] { "Exhaust Mixer",              "Mixes core exhaust gas with bypass air at the nozzle exit to reduce jet noise and improve efficiency." };
        componentInfo[34] = new string[] { "Thrust Reverser Bucket",     "Cascade-type reverser that deflects fan exhaust forward to decelerate the aircraft on landing." };
        componentInfo[35] = new string[] { "HP Turbine Rotor",           "Single-crystal superalloy rotor in the HP turbine. Extracts energy from ~1,400°C combustion gas." };
        componentInfo[36] = new string[] { "HP Turbine Stator",          "Fixed stator vane ring in the HP turbine. Accelerates and angles hot gas onto the HP rotor blades." };
        componentInfo[37] = new string[] { "Fuel Nozzles Set 2",         "Second set of fuel nozzles providing additional fuel atomisation points around the combustor annulus." };
        componentInfo[38] = new string[] { "Compressor Hub",             "Central hub of the compressor rotor assembly. Carries all compressor discs and blade stages." };
        componentInfo[39] = new string[] { "Compressor Rotor",           "Complete HP compressor rotor assembly rotating at up to 14,500 RPM to compress core airflow." };
        componentInfo[40] = new string[] { "Fan Rotor",                  "Complete fan rotor assembly including hub and all fan blades. Driven by the LP turbine via the LP shaft." };
        componentInfo[41] = new string[] { "Fan Outlet Guide Vanes",     "Fixed vanes at the fan exit. Straighten fan exit airflow before it enters the bypass duct." };
        componentInfo[42] = new string[] { "Engine Mount",               "Forward and rear fittings attaching the engine to the pylon. Transfers all engine loads to the airframe." };
        componentInfo[43] = new string[] { "Fuel Pipes Set 3",           "Third set of fuel pipes completing the fuel distribution network around the combustor." };
        componentInfo[44] = new string[] { "Connectors",                 "Electrical and pneumatic connectors linking engine systems to aircraft harnesses and bleed air ducting." };
        componentInfo[45] = new string[] { "LP Compressor Stator",       "Fixed stator vane assembly between LP compressor rotor stages. Redirects airflow at optimal angle." };
        componentInfo[46] = new string[] { "Wiring Harness",             "Engine wiring loom routing electrical signals between FADEC, sensors and actuators around the engine." };
        componentInfo[47] = new string[] { "Ignition Leads",             "High-voltage ignition cables connecting the ignition exciter to the igniter plugs in the combustor." };
        componentInfo[48] = new string[] { "Fuel Injectors Set 2",       "Second set of fuel injectors providing additional fuel injection points in the combustion zone." };
        componentInfo[49] = new string[] { "Combustor Dome",             "Front face of the combustor. Holds the fuel nozzles and creates the primary recirculation zone." };
        componentInfo[50] = new string[] { "Combustor Fittings",         "Structural fittings and brackets that mount and locate the combustor liner within the casing." };
        componentInfo[51] = new string[] { "Ignitor Plug",               "High-energy spark plug that ignites the fuel-air mixture during engine start and relight." };
    }

    // ─── HIGHLIGHT BY NAME ─────────────────────────────────────────

    public void HighlightComponentByName(string componentName)
    {
        foreach (GameObject comp in components)
        {
            Renderer r = comp.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                r.material.color = Color.yellow;
                StartCoroutine(ResetColorAfterDelay(r, 3f));
            }
        }
    }

    IEnumerator ResetColorAfterDelay(Renderer r, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (r != null)
            r.material.color = Color.white;
    }

    // ─── CONTROLLER INPUT (Quest only — uncomment when building for Quest) ────

    /*
    void HandleControllerInput()
    {
        Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        if (rightStick.sqrMagnitude > 0.01f)
        {
            engineRoot.transform.Rotate(Vector3.up,   -rightStick.x * 2f, Space.World);
            engineRoot.transform.Rotate(Vector3.right,  rightStick.y * 2f, Space.World);
        }

        float rightTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        if (rightTrigger > 0.1f)
        {
            float scaleFactor = 1 + rightTrigger * 0.01f;
            engineRoot.transform.localScale *= scaleFactor;
            float clamped = Mathf.Clamp(engineRoot.transform.localScale.x, 0.01f, 5f);
            engineRoot.transform.localScale = Vector3.one * clamped;
        }

        float leftTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        if (leftTrigger > 0.1f)
        {
            float scaleFactor = 1 - leftTrigger * 0.01f;
            engineRoot.transform.localScale *= scaleFactor;
            float clamped = Mathf.Clamp(engineRoot.transform.localScale.x, 0.01f, 5f);
            engineRoot.transform.localScale = Vector3.one * clamped;
        }

        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            OnNextPressed();

        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            OnBackPressed();
    }
    */
}