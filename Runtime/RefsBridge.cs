using UnityEngine;

public class RefsBridge : MonoBehaviour
{
    public void ChangeMaterials(string groupID, string materialID)
    {
        FindFirstObjectByType<MaterialsManager>().ChangeMaterials(groupID, materialID);
    }
}
