using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARPlacementController : MonoBehaviour
{
    [Header("References")]
    public GameObject engineObject;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    [Header("UI")]
    public GameObject placementUI;
    public GameObject inspectionUI;
    public TMPro.TextMeshProUGUI placementHintText;
    public GameObject placeButton;

    private bool enginePlaced = false;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        engineObject.SetActive(false);
        placementUI.SetActive(true);
        inspectionUI.SetActive(false);

        // Show place button immediately
        if (placeButton != null)
            placeButton.SetActive(true);

        UpdateHintText("Point where you want the engine and press Place");
    }

    void Update()
    {
        // Nothing needed - button handles placement
    }

    // Called by Place Button
    public void OnPlaceButtonPressed()
    {
        // Place engine 1.5 meters in front of camera
        Vector3 placePos = Camera.main.transform.position +
                           Camera.main.transform.forward * 1.5f;

        Quaternion placeRot = Quaternion.Euler(
            0,
            Camera.main.transform.eulerAngles.y,
            0);

        PlaceEngine(new Pose(placePos, placeRot));
    }

    void PlaceEngine(Pose pose)
    {
        engineObject.transform.position = pose.position;
        engineObject.transform.rotation = pose.rotation;
        engineObject.SetActive(true);
        enginePlaced = true;

        placementUI.SetActive(false);
        inspectionUI.SetActive(true);

        if (planeManager != null)
            planeManager.enabled = false;

        Debug.Log("✅ Engine placed!");
    }

    void UpdateHintText(string text)
    {
        if (placementHintText != null)
            placementHintText.text = text;
    }

    public void OnRepositionPressed()
    {
        enginePlaced = false;
        engineObject.SetActive(false);
        placementUI.SetActive(true);
        inspectionUI.SetActive(false);

        if (placeButton != null)
            placeButton.SetActive(true);

        UpdateHintText("Point where you want the engine and press Place");
    }
}