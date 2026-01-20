using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class ImmersiveEditorView : MonoBehaviour
{
    [SerializeField] HandMenuHandler handMenu;
    [SerializeField] ContinuousMoveProvider moveProvider;

    public void HandMenuActions(HandMenuInput input) => handMenu.ProcessInput(input);

    public void ToggleHandMenu()
    {
        // Enable/Disable controller manager based on hand menu state -> prevent input conflicts
        moveProvider.enabled = handMenu.gameObject.activeInHierarchy;
        // Toggle hand menu visibility
        handMenu.gameObject.SetActive(!handMenu.gameObject.activeInHierarchy);

    }
}
