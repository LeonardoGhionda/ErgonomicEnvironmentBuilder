using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class HM_ShowBody : HM_Toggle
{

    [SerializeField] GameObject bodyPrefab = null;
    GameObject body = null;


    public override void OnClick()
    {
        if (body == null)
        {
            body =
                Instantiate(bodyPrefab, 
                DependencyProvider.CurrentCamera.transform.position, 
                Quaternion.identity);

            body.GetComponent<NetworkObject>().enabled = false;
            body.GetComponent<NetworkTransform>().enabled = false;

            body.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToList().ForEach(m => m.enabled = false);
            body.GetComponentsInChildren<VibrateOnCollision>(true).ToList().ForEach(m => m.enabled = false);

            Managers.Get<StateManager>().OnStateChanged += DestroyBody;
        }
        else
        {
            Destroy(body);
            body = null;
            Managers.Get<StateManager>().OnStateChanged -= DestroyBody;
        }

        _state = body == null; // the opposite of the current state, since the state is toggled after this function is called
        base.OnClick();
    }


    private void DestroyBody()
    {
        if (body != null)
        {
            Destroy(body);
            body = null;
        }
    }   
}
