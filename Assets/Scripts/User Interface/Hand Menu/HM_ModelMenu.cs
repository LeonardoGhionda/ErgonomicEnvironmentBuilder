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
            var modelsCards = ModelButtonGenerator.VRInit(HMEntryTemplate);

            // Add to group
            _group.AddRange(modelsCards.Select(mb => mb.GetComponent<HM_Base>()));
        }
        else
        {
            foreach (var item in _group)
            {
                Destroy(item.gameObject);
            }
            _group.Clear();
        }

        base.OnClick();
    }
}
