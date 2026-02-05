using Dummiesman;
using Unity.VisualScripting;
using UnityEngine;

public class HM_SpawnModel : HM_Base
{
    public string modelFullPath;

    public override void OnClick()
    {
        base.OnClick();
        if (string.IsNullOrEmpty(modelFullPath))
        {
            Debug.LogError("Model card is missing path to the model");
            return;
        }

        OBJLoader loader = new();
        GameObject obj = loader.Load(modelFullPath);
        obj.name = $"[P] {obj.name}";
        obj.transform.SetParent(GameObject.Find("Objects Container").transform);
        Camera cam = _deps.player.GetComponentInChildren<Camera>();
        obj.transform.localPosition = cam.transform.position + cam.transform.forward * 4f;
        obj.AddComponent<InteractableParent>().Path = modelFullPath;

        foreach (Transform child in obj.transform)
        {
            RoomsUtility.SetUpVrObject(child, _deps.selection);
            child.AddComponent<InteractableObject>();
        }

        // Interactable parent and object are necessary to make them savable (see Rooms Utility)

        // Close HM on obj spawn
        _deps.handMenu.Show(false);
    }
}
