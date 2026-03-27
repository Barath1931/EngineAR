using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

public class ObjectScanner : MonoBehaviour
{
    [Header("UI References")]
    public GameObject bottomSheet;
    public TextMeshProUGUI detectedObjectText;
    public TextMeshProUGUI matchedComponentText;
    public TextMeshProUGUI descriptionText;
    public Button yesButton;
    public Button noButton;
    public Button closeButton;
    public GameObject scanningIndicator;

    [Header("Engine Reference")]
    public EngineInspector engineInspector;

    [Header("Engine Startup Controller")]
    public EngineStartupController engineStartupController;

    [Header("Groq Settings")]
    public string groqApiKey = "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";
    private string groqUrl = "https://api.groq.com/openai/v1/chat/completions";

    private Camera arCamera;
    private string currentMatchedComponent = "";
    private bool isScanning = false;

    private readonly Dictionary<string, string[]> engineComponentMap =
        new Dictionary<string, string[]>()
    {
        { "fan blade",                  new[] { "Fan Blade",                  "Single-stage wide-chord fan blade. Generates ~80% of total CFM56 thrust by accelerating bypass air." } },
        { "fan disk",                   new[] { "Fan Disk",                   "Titanium rotor disk that holds all fan blades. Rotates at ~5,000 RPM at takeoff." } },
        { "fan case",                   new[] { "Fan Case",                   "Outer casing surrounding the fan. Contains blade-off events and shapes bypass duct airflow." } },
        { "spinner",                    new[] { "Spinner (Nose Cone)",        "Conical nose cone mounted on the fan hub. Guides inlet airflow smoothly into the fan." } },
        { "inlet cowl",                 new[] { "Inlet Cowl",                 "Aerodynamic nacelle lip that captures and conditions incoming air before the fan." } },
        { "fan outlet guide vane",      new[] { "Fan Outlet Guide Vane",      "Straightens swirling fan exit airflow before it enters the bypass duct." } },
        { "booster rotor",              new[] { "Booster Rotor (LPC)",        "Three-stage low-pressure compressor rotor. Pre-compresses core airflow before the HPC." } },
        { "booster blade",              new[] { "Booster Blade",              "LPC rotor blade that adds the first stages of compression to core airflow." } },
        { "booster stator vane",        new[] { "Booster Stator Vane",        "Fixed vane between booster rotor stages. Redirects airflow at optimal angle into each stage." } },
        { "lpc disk",                   new[] { "LPC Disk",                   "Disk carrying the booster blades of the low-pressure compressor." } },
        { "hpc rotor blade",            new[] { "HPC Rotor Blade",            "One of nine HPC stages. Progressively compresses air to ~30:1 pressure ratio." } },
        { "hpc stator vane",            new[] { "HPC Stator Vane",            "Fixed vane between HPC rotor stages. Diffuses and redirects high-speed compressed air." } },
        { "hpc disk",                   new[] { "HPC Disk",                   "Forged disk that carries HPC blades at each compression stage." } },
        { "variable stator vane",       new[] { "Variable Stator Vane (VSV)", "Adjustable HPC inlet vane. Optimises airflow angle across the operating range to prevent surge." } },
        { "variable bleed valve",       new[] { "Variable Bleed Valve (VBV)", "Valve that bleeds excess LPC air during low-power operation to prevent compressor surge." } },
        { "hpc front frame",            new[] { "HPC Front Frame",            "Structural frame at the front of the HPC. Carries compressor loads and houses VSV actuators." } },
        { "hpc rear frame",             new[] { "HPC Rear Frame",             "Diffuser case at the HPC exit. Decelerates airflow before entering the combustor." } },
        { "compressor discharge pressure tube", new[] { "CDP Tube",           "Senses high-pressure compressor exit pressure for FADEC fuel scheduling." } },
        { "combustor liner",            new[] { "Combustor Liner",            "Annular liner inside the combustion chamber. Contains and shapes the flame at ~2000°C." } },
        { "combustor dome",             new[] { "Combustor Dome",             "Front face of the combustor. Holds the fuel nozzles and creates the primary recirculation zone." } },
        { "fuel nozzle",                new[] { "Fuel Nozzle",                "Atomises and injects aviation fuel into the combustor at precise flow rates." } },
        { "igniter plug",               new[] { "Igniter Plug",               "High-energy spark plug that ignites the fuel-air mixture during engine start." } },
        { "ignition exciter",           new[] { "Ignition Exciter",           "Generates 20,000V pulses to fire the igniter plugs during start and relight." } },
        { "combustion casing",          new[] { "Combustion Casing",          "Outer pressure vessel surrounding the combustor liner. Handles extreme pressure and temperature." } },
        { "combustor transition duct",  new[] { "Transition Duct",            "Directs hot combustor exit gas into the HP turbine nozzle guide vanes." } },
        { "hpt nozzle guide vane",      new[] { "HPT Nozzle Guide Vane",      "First fixed vane after the combustor. Accelerates and angles hot gas onto HPT rotor blades." } },
        { "hpt rotor blade",            new[] { "HPT Rotor Blade",            "Single-crystal superalloy blade in the HP turbine. Extracts energy from ~1,400°C combustion gas." } },
        { "hpt disk",                   new[] { "HPT Disk",                   "Nickel superalloy disk carrying HPT blades. Rotates at ~14,500 RPM." } },
        { "hpt shroud",                 new[] { "HPT Blade Outer Air Seal",   "Abradable ring around the HPT rotor tips. Minimises tip clearance to maximise efficiency." } },
        { "hpt rear shaft",             new[] { "HPT Rear Shaft",             "Connects the HP turbine disk to the HPC. Transmits power to drive the compressor." } },
        { "turbine rear frame",         new[] { "Turbine Rear Frame (TRF)",   "Structural frame behind the LPT. Carries engine loads and houses the rear bearing." } },
        { "lpt rotor blade",            new[] { "LPT Rotor Blade",            "Four-stage LPT blade that extracts remaining energy from exhaust gas to drive the fan." } },
        { "lpt nozzle vane",            new[] { "LPT Nozzle Vane",            "Fixed stator vane between LPT rotor stages. Accelerates gas flow onto next rotor stage." } },
        { "lpt disk",                   new[] { "LPT Disk",                   "Disk carrying LPT blades at each of the four low-pressure turbine stages." } },
        { "lpt shaft",                  new[] { "LPT Shaft",                  "Long shaft connecting the LP turbine to the fan and booster. Runs inside the HP shaft." } },
        { "lpt conical support",        new[] { "LPT Conical Support",        "Structural cone that supports the LPT rotor assembly and transfers loads to the TRF." } },
        { "exhaust case",               new[] { "Exhaust Case",               "Nozzle at the rear of the engine. Accelerates and expels exhaust gas to generate thrust." } },
        { "accessory gearbox",          new[] { "Accessory Gearbox (AGB)",    "Bevel gear-driven box on the engine core. Powers the fuel pump, oil pump, hydraulic pump and IDG." } },
        { "fuel pump",                  new[] { "Fuel Pump",                  "High-pressure gear pump in the AGB. Delivers pressurised fuel to the FADEC metering valve." } },
        { "fuel metering unit",         new[] { "Fuel Metering Unit (FMU)",   "FADEC-controlled valve that meters precise fuel flow to the combustor nozzles." } },
        { "oil pump",                   new[] { "Oil Pump",                   "Pressure and scavenge pump. Supplies clean oil to bearings and scavenges it back to the tank." } },
        { "oil filter",                 new[] { "Oil Filter",                 "Removes metallic debris and contaminants from engine oil before it reaches the bearings." } },
        { "oil cooler",                 new[] { "Oil Cooler (ACOC)",          "Air-cooled oil cooler. Uses fan bypass air to cool hot scavenged oil." } },
        { "magnetic chip detector",     new[] { "Magnetic Chip Detector",     "Magnetic plug in the oil system. Attracts metallic debris — early warning of internal wear." } },
        { "hydraulic pump",             new[] { "Hydraulic Pump",             "Engine-driven pump providing hydraulic pressure for aircraft flight control systems." } },
        { "integrated drive generator", new[] { "IDG (Generator)",            "Constant-speed generator driven by the AGB. Produces 115V AC electrical power for the aircraft." } },
        { "starter",                    new[] { "Air Turbine Starter",        "Pneumatic turbine that spins the HP spool to motoring speed before light-off." } },
        { "fadec",                      new[] { "FADEC",                      "Full Authority Digital Engine Control. Dual-channel computer controlling all engine parameters." } },
        { "fadec ecu",                  new[] { "FADEC ECU",                  "Electronic Control Unit of the FADEC system. Processes sensor data and sends actuator commands." } },
        { "no.1 bearing",               new[] { "No.1 Bearing",               "Ball bearing at the fan hub. Carries fan/LPC axial and radial loads." } },
        { "no.3 bearing",               new[] { "No.3 Bearing",               "Roller bearing supporting the HP spool at the HPC front. Handles radial loads." } },
        { "no.4 bearing",               new[] { "No.4 Bearing",               "Ball bearing behind the HPT disk. Carries HP spool axial thrust loads." } },
        { "no.5 bearing",               new[] { "No.5 Bearing",               "Roller bearing at the rear of the LP shaft. Supports LPT and fan shaft." } },
        { "carbon seal",                new[] { "Carbon Seal",                "Face seal around each bearing compartment. Prevents oil leaking into the gas path." } },
        { "labyrinth seal",             new[] { "Labyrinth Seal",             "Non-contacting air seal between rotating and static structures. Controls secondary airflows." } },
        { "hpt air seal",               new[] { "HPT Air Seal",               "Seals cooling air inside the HPT disk cavity to prevent hot gas ingestion." } },
        { "thrust reverser",            new[] { "Thrust Reverser",            "Cascade-type reverser that deflects fan exhaust forward to decelerate the aircraft on landing." } },
        { "thrust reverser actuator",   new[] { "Thrust Reverser Actuator",   "Hydraulic actuator that deploys and stows the thrust reverser blocker doors." } },
        { "nacelle cowl",               new[] { "Nacelle Cowl",               "Aerodynamic fairing surrounding the engine. Reduces drag and provides maintenance access." } },
        { "core cowl",                  new[] { "Core Cowl",                  "Inner cowl panel giving access to the core engine for borescope inspections." } },
        { "bifurcation duct",           new[] { "Bifurcation Duct",           "Splits bypass air around the pylon. Directs airflow cleanly past the engine mount." } },
        { "pylon",                      new[] { "Engine Pylon",               "Structural attachment strut that connects the engine to the aircraft wing." } },
        { "engine mount",               new[] { "Engine Mount",               "Forward and rear fittings attaching the engine to the pylon. Transfers all engine loads to the airframe." } },
        { "egt thermocouple",           new[] { "EGT Thermocouple",           "Exhaust Gas Temperature sensor in the LPT exit plane. Primary overtemperature protection sensor." } },
        { "n1 speed sensor",            new[] { "N1 Speed Sensor",            "Magnetic pickup on the fan shaft. Measures low-pressure spool RPM for FADEC and display." } },
        { "n2 speed sensor",            new[] { "N2 Speed Sensor",            "Magnetic pickup on the HP spool. Measures high-pressure spool RPM." } },
        { "oil pressure transducer",    new[] { "Oil Pressure Transducer",    "Measures oil system pressure and sends signal to FADEC and cockpit indication." } },
        { "vibration sensor",           new[] { "Vibration Sensor (VBSV)",    "Accelerometer on the fan frame and core. Monitors engine vibration for imbalance detection." } },
        { "total air temperature probe",new[] { "TAT Probe",                  "Measures total inlet air temperature. Used by FADEC for thrust and fuel calculations." } },
        { "borescope port",             new[] { "Borescope Port",             "Access port on the engine casing allowing optical inspection of internal components without disassembly." } },
    };

    private readonly HashSet<string> normalObjects = new HashSet<string>()
    {
        "keyboard", "mouse", "monitor", "phone", "laptop", "tablet",
        "charger", "headphones", "earphones", "speaker", "microphone",
        "camera", "remote control", "calculator", "printer", "router",
        "usb drive", "hard drive", "power bank", "smartwatch", "television",
        "projector", "webcam", "tripod", "extension cord", "power strip",
        "pen", "pencil", "notebook", "paper", "book", "folder", "binder",
        "sticky note", "clipboard", "calendar", "envelope", "rubber band",
        "paper clip", "stapler", "tape", "glue", "marker", "highlighter",
        "eraser", "ruler", "scissors", "box",
        "cup", "mug", "bottle", "glass", "plate", "bowl", "tray",
        "kettle", "thermos", "lunchbox", "cutting board", "grater",
        "peeler", "can opener", "bottle opener", "whisk", "spatula",
        "ladle", "tongs", "spoon", "fork", "knife",
        "apple", "banana", "orange", "bread", "sandwich", "pizza",
        "burger", "egg", "chocolate", "biscuit", "chips", "coffee cup",
        "chair", "table", "desk", "shelf", "lamp", "clock", "watch",
        "photo frame", "vase", "candle", "tissue box", "bin", "basket",
        "hanger", "door handle", "key",
        "shoe", "bag", "wallet", "sunglasses", "hat", "belt", "umbrella",
        "glove", "scarf", "button", "zipper",
        "hammer", "wrench", "pliers", "screwdriver", "drill", "saw",
        "tape measure", "clamp", "sandpaper", "paintbrush",
        "bolt", "screw", "nut", "washer", "nail", "spring", "wire", "cable",
        "pipe", "tube", "hose", "funnel", "tin", "coin", "battery",
        "magnet", "mirror", "lens", "lighter", "torch",
        "toothbrush", "comb", "razor", "soap", "shampoo bottle", "towel",
        "lock", "rope", "sponge", "brush", "fan",
    };

    private string BuildObjectList()
    {
        var all = new List<string>(engineComponentMap.Keys);
        all.AddRange(normalObjects);
        return string.Join(", ", all);
    }

    void Start()
    {
        arCamera = Camera.main;

        if (bottomSheet       != null) bottomSheet.SetActive(false);
        if (scanningIndicator != null) scanningIndicator.SetActive(false);

        if (yesButton   != null) yesButton.onClick.AddListener(OnYesPressed);
        if (noButton    != null) noButton.onClick.AddListener(OnNoPressed);
        if (closeButton != null) closeButton.onClick.AddListener(OnClosePressed);
    }

    public void OnScanPressed()
    {
        if (isScanning) return;
        StartCoroutine(CaptureAndScan());
    }

    IEnumerator CaptureAndScan()
    {
        isScanning = true;
        if (scanningIndicator != null) scanningIndicator.SetActive(true);

        yield return new WaitForEndOfFrame();

        Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();

        Texture2D resized = ResizeTexture(screenshot, 512, 512);
        Destroy(screenshot);

        byte[] imageBytes = resized.EncodeToJPG(70);
        Destroy(resized);
        string base64Image = System.Convert.ToBase64String(imageBytes);

        yield return StartCoroutine(SendToGroqVision(base64Image));

        if (scanningIndicator != null) scanningIndicator.SetActive(false);
        isScanning = false;
    }

    IEnumerator SendToGroqVision(string base64Image)
    {
        string objectList = BuildObjectList();

        string prompt =
            "Look at this image and identify the main object. " +
            "Reply with ONLY a single word or short phrase from this list: " +
            objectList +
            ". If the object is not in the list, reply with the closest match from the list. " +
            "Reply with only the object name, nothing else.";

        string jsonBody =
            "{" +
                "\"model\": \"meta-llama/llama-4-scout-17b-16e-instruct\"," +
                "\"messages\": [{" +
                    "\"role\": \"user\"," +
                    "\"content\": [" +
                        "{\"type\": \"text\", \"text\": \"" + EscapeJson(prompt) + "\"}," +
                        "{\"type\": \"image_url\", \"image_url\": {" +
                            "\"url\": \"data:image/jpeg;base64," + base64Image + "\"" +
                        "}}" +
                    "]" +
                "}]," +
                "\"max_tokens\": 20" +
            "}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using UnityWebRequest request = new UnityWebRequest(groqUrl, "POST");
        request.uploadHandler   = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + groqApiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string detected = ParseResponse(request.downloadHandler.text).ToLower().Trim();
            Debug.Log("[ObjectScanner] Groq detected: " + detected);
            ProcessDetectedObject(detected);
        }
        else
        {
            Debug.LogError("[ObjectScanner] Groq error: " + request.error);
        }
    }

    void ProcessDetectedObject(string detected)
    {
        foreach (var key in engineComponentMap.Keys)
        {
            if (detected.Contains(key) || key.Contains(detected))
            {
                string[] data = engineComponentMap[key];
                currentMatchedComponent = data[0];

                ShowBottomSheet(
                    detected:    detected,
                    component:   data[0],
                    description: data[1],
                    showButtons: true
                );
                return;
            }
        }

        currentMatchedComponent = "";
        ShowBottomSheet(
            detected:    detected,
            component:   "",
            description: "",
            showButtons: false
        );
    }

    void ShowBottomSheet(string detected, string component, string description, bool showButtons)
    {
        if (detectedObjectText != null)
            detectedObjectText.text = "Detected: " + TitleCase(detected);

        if (matchedComponentText != null)
            matchedComponentText.text = showButtons ? "CFM56 Part: " + component : "";

        if (descriptionText != null)
            descriptionText.text = showButtons ? description : "";

        if (yesButton != null) yesButton.gameObject.SetActive(showButtons);
        if (noButton  != null) noButton.gameObject.SetActive(showButtons);

        if (bottomSheet != null)
        {
            bottomSheet.SetActive(true);
            StartCoroutine(SlideUp());
        }
    }

    IEnumerator SlideUp()
    {
        RectTransform rt = bottomSheet.GetComponent<RectTransform>();
        float h = rt.rect.height;
        rt.anchoredPosition = new Vector2(0, -h);
        float duration = 0.35f, elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rt.anchoredPosition = new Vector2(0, Mathf.Lerp(-h, 0f, t));
            yield return null;
        }
        rt.anchoredPosition = Vector2.zero;
    }

    IEnumerator SlideDown()
    {
        RectTransform rt = bottomSheet.GetComponent<RectTransform>();
        float h = rt.rect.height;
        float duration = 0.25f, elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rt.anchoredPosition = new Vector2(0, Mathf.Lerp(0f, -h, t));
            yield return null;
        }
        bottomSheet.SetActive(false);
        rt.anchoredPosition = Vector2.zero;
    }

    // ─── BUTTONS ──────────────────────────────────────────────────────────────

    void OnYesPressed()
    {
        if (engineInspector != null && !string.IsNullOrEmpty(currentMatchedComponent))
            engineInspector.HighlightComponentByName(currentMatchedComponent);

        if (engineStartupController != null)
            engineStartupController.NotifyComponentScanned();

        StartCoroutine(SlideDown());
    }

    void OnNoPressed()    => StartCoroutine(SlideDown());
    void OnClosePressed() => StartCoroutine(SlideDown());

    // ─── HELPERS ──────────────────────────────────────────────────────────────

    Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    string ParseResponse(string json)
    {
        try
        {
            int contentIndex = json.IndexOf("\"content\":");
            if (contentIndex != -1)
            {
                int start = json.IndexOf("\"", contentIndex + 10) + 1;
                int end   = json.IndexOf("\"", start);
                if (start > 0 && end > start)
                    return json.Substring(start, end - start).Trim();
            }
        }
        catch { }
        return "unknown";
    }

    string EscapeJson(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }

    string TitleCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var words = s.Split(' ');
        for (int i = 0; i < words.Length; i++)
            if (words[i].Length > 0)
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
        return string.Join(" ", words);
    }
}