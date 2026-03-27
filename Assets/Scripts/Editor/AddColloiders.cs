using UnityEngine;
using UnityEditor;

public class AddColliders : MonoBehaviour
{
    [MenuItem("Engine Tools/Add Mesh Colliders")]
    static void AddMeshColliders()
    {
        GameObject root = GameObject.Find("BarathPoC");
        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.GetComponent<MeshCollider>() == null)
            {
                MeshCollider col = mf.gameObject.AddComponent<MeshCollider>();
                col.sharedMesh = mf.sharedMesh;
            }
        }

        Debug.Log("✅ Added MeshColliders to " + meshFilters.Length + " parts.");
    }
}