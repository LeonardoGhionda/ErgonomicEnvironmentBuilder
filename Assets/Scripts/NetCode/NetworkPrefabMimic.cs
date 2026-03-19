using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkPrefabMimic : NetworkBehaviour
{
    private bool _init = false;
    private Transform _target;

    public NetworkVariable<FixedString128Bytes> TargetID = new(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public void InitHostSide(Interactable target)
    {
        if (!IsServer) return;

        _target = target.transform;
        TargetID.Value = target.ID;
        _init = true;
    }

    private void TryInitClientSide()
    {
        if (_init || TargetID.Value == "") return;

        Interactable foundInteractable = Interactable.FindByID(TargetID.Value.ToString());

        if (foundInteractable != null)
        {
            _target = foundInteractable.transform;
            _init = true;
        }
    }

    private void Update()
    {
        if (!_init)
        {
            if (!IsServer)
            {
                TryInitClientSide();
            }
            return;
        }

        if (_target == null) return;

        if (IsServer)
        {
            transform.SetPositionAndRotation(_target.position, _target.rotation);
        }
        else
        {
            _target.SetPositionAndRotation(transform.position, transform.rotation);
        }
    }
}