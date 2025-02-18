using UnityEngine;

public class MeshSwitcher : MonoBehaviour
{
    [Range(0, 9)] public float slider;
    private GameObject[] meshes;

    void Start()
    {
        FindChildMeshes();
        UpdateMesh(slider);
    }

    void FindChildMeshes()
    {
        int count = transform.childCount;
        meshes = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            meshes[i] = transform.GetChild(i).gameObject;
        }
    }

    void UpdateMesh(float value)
    {
        if (meshes == null || meshes.Length == 0) return;

        int index = Mathf.RoundToInt(value);
        index = Mathf.Clamp(index, 0, meshes.Length - 1);

        for (int i = 0; i < meshes.Length; i++)
        {
            meshes[i].SetActive(i == index);
        }
    }

    void OnValidate()
    {
        FindChildMeshes();
        UpdateMesh(slider);
    }
}
