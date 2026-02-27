using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class TurnPlayerSelector : MonoBehaviour
{
    [Header("Turn Dropdowns")]
    public TMP_Dropdown[] dropdowns;

    [Header("Sprites selectionables")]
    public Sprite emptySprite;
    public Sprite[] listSprite;

    private bool isUpdating = false;

    void Start()
    {
        if(dropdowns == null) return;
        if(listSprite == null) return;

        foreach(var dd in dropdowns)
        {
            dd.onValueChanged.AddListener(delegate {RefreshDropdownState(); });
        }
        
        RefreshDropdownState();
    }

    private void RefreshDropdownState()
    {
        if(isUpdating) return;
        isUpdating = true;

        HashSet<Sprite> takenSprites = new HashSet<Sprite>();

        foreach(var dd in dropdowns)
        {
            Sprite selected = GetSelectedSprite(dd);
            if(selected != null && selected != emptySprite)
            {
                takenSprites.Add(selected);
            }
        }

        foreach(var dd in dropdowns)
        {
            RebuildDropdownOptions(dd, takenSprites);
        }
        
        isUpdating = false;
    }

    private void RebuildDropdownOptions(TMP_Dropdown targetDD, HashSet<Sprite> takenSprites)
    {
        Sprite currentSelectionBeforeRebuild = GetSelectedSprite(targetDD);

        List<TMP_Dropdown.OptionData> newOptions = new List<TMP_Dropdown.OptionData>();
        newOptions.Add(new TMP_Dropdown.OptionData(emptySprite));

        foreach(Sprite sprite in listSprite)
        {
            bool isTaken = takenSprites.Contains(sprite);
            bool isCurrentSelection = (sprite == currentSelectionBeforeRebuild);

            if(!isTaken || isCurrentSelection)
            {
                newOptions.Add(new TMP_Dropdown.OptionData(string.Empty, sprite, Color.white));
            }
        }

        targetDD.ClearOptions();
        targetDD.AddOptions(newOptions);

        int newIndexToSelect = 0;
        if(currentSelectionBeforeRebuild != null && currentSelectionBeforeRebuild != emptySprite)
        {
            for(int i = 1; i < targetDD.options.Count; i++)
            {
                if(targetDD.options[i].image == currentSelectionBeforeRebuild)
                {
                    newIndexToSelect = i;
                    break;
                }
            }
        }

        targetDD.SetValueWithoutNotify(newIndexToSelect);
        targetDD.RefreshShownValue();
    }

    private Sprite GetSelectedSprite(TMP_Dropdown dd)
    {
        if(dd.options.Count == 0) return null;
        int index = Mathf.Clamp(dd.value, 0, dd.options.Count - 1);
        return dd.options[index].image;
    }

}
