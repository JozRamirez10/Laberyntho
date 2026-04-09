using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public abstract class UIBaseManager : MonoBehaviour
{
    protected GameObject lastSelectObject;

    protected virtual void Start()
    {
        Selectable[] selectables = FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach(Selectable sel in selectables)
        {
            if(sel.gameObject.GetComponent<UIHoverSelector>() == null)
            {
                sel.gameObject.AddComponent<UIHoverSelector>();
            }
        }
    }

    protected virtual void Update()
    {
        if(EventSystem.current == null) return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if(currentSelected == null && lastSelectObject != null)
        {
            if(lastSelectObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(lastSelectObject);
                currentSelected = lastSelectObject;
            }
        }

        if(currentSelected != null && currentSelected != lastSelectObject)
        {
            lastSelectObject = currentSelected;
            OnSelectionChanged(currentSelected);
        }
    }

    protected void ForceSelectButton(Selectable btnToSelect)
    {
        if(EventSystem.current != null && btnToSelect != null && btnToSelect.gameObject.activeInHierarchy && btnToSelect.interactable)
        {
            if(AudioManager.Instance != null) AudioManager.Instance.ignoreNextSelectSound = true;

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(btnToSelect.gameObject);
        }
    }

    protected virtual void OnSelectionChanged(GameObject selectedObject){ }

    public void ClearFocus()
    {
        lastSelectObject = null;
        if(EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void RestoreLastFocus()
    {
        if(lastSelectObject != null && lastSelectObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(lastSelectObject);
        }
    }
}

public class UIHoverSelector : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(InputManager.Instance != null && InputManager.Instance.isUsingMouseInput)
        {
            Selectable selectable = GetComponent<Selectable>();
            if(selectable != null && selectable.interactable)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }
    }
}
    