using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkPrefabMimic : NetworkBehaviour
{
    private bool _init = false;
    private Transform _target;

    public NetworkVariable<FixedString128Bytes> ID = new(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            ID.OnValueChanged += HandleModelPathChanged;

            if (!string.IsNullOrEmpty(ID.Value.ToString()))
            {
                InitClientSide(ID.Value.ToString());
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
        {
            ID.OnValueChanged -= HandleModelPathChanged;
        }
    }

    private void HandleModelPathChanged(FixedString128Bytes previousValue, FixedString128Bytes newValue)
    {
        if (!_init && !string.IsNullOrEmpty(newValue.ToString()))
        {
            InitClientSide(newValue.ToString());
        }
    }

    public void InitHostSide(Interactable target)
    {
        if (!IsServer) throw new Exception("This function should only be called from the server/host");

        _target = target.transform;
        ID.Value = target.ID;

        _init = true;
    }

    public void InitClientSide(string targetID)
    {
        if (IsServer) throw new Exception("This function should only be called from the client");
        if (string.IsNullOrEmpty(targetID)) Debug.LogError("ID is null or empty");

        Interactable foundInteractable = Interactable.FindByID(targetID);

        if (foundInteractable != null)
        {
            _target = foundInteractable.transform;
            _init = true;
        }
        else
        {
            Debug.LogError("Could not find Interactable with ID: " + targetID);
        }
    }

    public void MimicTarget()
    {
        transform.SetPositionAndRotation(_target.position, _target.rotation);
    }

    public void SetTargetTransform()
    {
        _target.SetPositionAndRotation(transform.position, transform.rotation);
    }

    private void Update()
    {
        if (!_init) return;

        if (IsServer)
        {
            MimicTarget();
        }
        else
        {
            SetTargetTransform();
        }
    }
}