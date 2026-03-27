using UnityEngine;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;

public class PlatformSwitcher : MonoBehaviour
{
    [Header("AR Camera (Android Mobile)")]
    public GameObject arSession;           // drag your AR Session GameObject here
    public GameObject xrOrigin;            // drag your XR Origin GameObject here

    [Header("Quest Camera")]
    public GameObject ovrCameraRig;        // drag your OVRCameraRig GameObject here

    [Header("UI Canvas")]
    public Canvas mainCanvas;              // drag your main UI Canvas here

    void Awake()
    {
        StartCoroutine(DetectAndSwitch());
    }

    IEnumerator DetectAndSwitch()
    {
        // Wait one frame for XR to initialise
        yield return null;

        bool isQuest = IsRunningOnQuest();

        Debug.Log("[PlatformSwitcher] Running on Quest: " + isQuest);

        if (isQuest)
        {
            // ── QUEST MODE ────────────────────────────────────────────
            // Disable AR Foundation camera
            if (arSession != null)  arSession.SetActive(false);
            if (xrOrigin != null)   xrOrigin.SetActive(false);

            // Enable OVR camera
            if (ovrCameraRig != null) ovrCameraRig.SetActive(true);

            // Set canvas to World Space so it floats in front of user
            if (mainCanvas != null)
            {
                mainCanvas.renderMode = RenderMode.WorldSpace;

                // Position canvas 1 metre in front, at eye height
                mainCanvas.transform.position = new Vector3(0f, 1.5f, 1f);
                mainCanvas.transform.rotation = Quaternion.identity;
                mainCanvas.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

                // Assign OVR center eye camera as canvas event camera
                Camera ovrCam = ovrCameraRig
                    .GetComponentInChildren<Camera>();
                if (ovrCam != null)
                    mainCanvas.worldCamera = ovrCam;
            }
        }
        else
        {
            // ── ANDROID MOBILE MODE ───────────────────────────────────
            // Enable AR Foundation camera
            if (arSession != null)  arSession.SetActive(true);
            if (xrOrigin != null)   xrOrigin.SetActive(true);

            // Disable OVR camera
            if (ovrCameraRig != null) ovrCameraRig.SetActive(false);

            // Keep canvas in Screen Space Overlay for mobile
            if (mainCanvas != null)
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
    }

    bool IsRunningOnQuest()
    {
        // Check if any XR display device is active (Quest, Quest 2, Quest 3 etc)
        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);

        foreach (var display in displays)
        {
            if (display.running)
            {
                Debug.Log("[PlatformSwitcher] XR Display found and running.");
                return true;
            }
        }

        // Fallback — check device name
        string deviceName = SystemInfo.deviceModel.ToLower();
        if (deviceName.Contains("quest"))
        {
            Debug.Log("[PlatformSwitcher] Quest detected via device name: " + deviceName);
            return true;
        }

        return false;
    }
}