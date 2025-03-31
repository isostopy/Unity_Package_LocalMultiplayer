using System.Collections.Generic;
using UnityEngine;

public class MaterialsManager : MonoBehaviour
{
    [SerializeField] SelectableGroup[] selectableGroups;
    [SerializeField] SelectableMaterial[] selectableMaterials;
    Dictionary<string, Material> materials = new Dictionary<string, Material>();
    Dictionary<string, GameObject[]> groups = new Dictionary<string, GameObject[]>();

    private void Start()
    {
        foreach (SelectableGroup group in selectableGroups)
        {
            groups.Add(group.groupID, group.elements);
        }

        foreach (SelectableMaterial mat in selectableMaterials)
        {
            materials.Add(mat.materialID, mat.material);
        }
    }

    public void ChangeMaterials(string groupID, string materialID)
    {
        Material retrievedMaterial = materials[materialID];
        GameObject[] retrievedGroup = groups[groupID];

        foreach (GameObject element in retrievedGroup)
        {
            element.GetComponent<Renderer>().material = retrievedMaterial;
        }

    }


    [System.Serializable]
    public class SelectableMaterial
    {
        public string materialID;
        public Material material;
    }

    [System.Serializable]
    public class SelectableGroup
    {
        public string groupID;
        public GameObject[] elements;
    }
}
