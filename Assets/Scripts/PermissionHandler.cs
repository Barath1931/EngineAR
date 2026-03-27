using UnityEngine;
using UnityEngine.Android;
using System.Collections;

public class PermissionHandler : MonoBehaviour
{
    public GameObject permissionDeniedPanel;

    void Start()
    {
        // This forces Unity to auto-declare RECORD_AUDIO in the manifest
        var _ = Microphone.devices;

        StartCoroutine(RequestPermissions());
    }

    IEnumerator RequestPermissions()
    {
        yield return new WaitForSeconds(1f); // Increased from 0.3f for Android 13+

        // Camera
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            yield return new WaitForSeconds(2f);
        }

        // Microphone
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            yield return new WaitForSeconds(2f);
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            if (permissionDeniedPanel != null)
                permissionDeniedPanel.SetActive(true);
        }
    }
}