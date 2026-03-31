using System.Linq;
using UnityEngine;

public class HM_ModelMenu : HM_Group
{
    [SerializeField] HM_SpawnModel HMEntryTemplate;

    public override void OnClick()
    {

        // menu open changes when base.OnClick is called so now is reversed
        if (!_isMenuOpen)
        {
            // Generates model entris based on the currently saved 
            System.Collections.Generic.List<HM_SpawnModel> modelsCards = ModelButtonGenerator.VRInit(HMEntryTemplate);

            // Add to group
            Group.AddRange(modelsCards.Select(mb => mb.GetComponent<HM_Base>()));
        }
        else
        {
            foreach (HM_Base item in Group)
            {
                Destroy(item.gameObject);
            }
            Group.Clear();
        }

        base.OnClick();
    }
}
