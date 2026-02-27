using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Comportamiento del scroll
[RequireComponent(typeof(ScrollRect))]
public class ScrollToSelected : MonoBehaviour
{
    public float scrollSpeed = 10f; // Velocidad del ajuste automático
    private ScrollRect m_ScrollRect;
    private RectTransform m_RectTransform;
    private RectTransform m_ContentRectTransform;

    void Awake()
    {
        m_ScrollRect = GetComponent<ScrollRect>();
        m_RectTransform = GetComponent<RectTransform>();
        
        // Aseguramos que el scrollrect tenga contenido asignado
        if (m_ScrollRect.content != null)
        {
            m_ContentRectTransform = m_ScrollRect.content;
        }
        else
        {
            Debug.LogError("ScrollToSelected: El ScrollRect no tiene 'Content' asignado.");
            enabled = false;
        }
    }

    void Update()
    {
        // Objeto seleccionado actualmente por el sistema de eventos
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null) return;
        
        // Verificamos si el objeto seleccionado es hijo del contenido de este ScrollRect
        if (selected.transform.IsChildOf(m_ContentRectTransform))
        {
            // Si es hijo, procedemos a asegurar que sea visible
            RectTransform selectedRectTransform = selected.GetComponent<RectTransform>();
            if (selectedRectTransform != null)
            {
                EnsureVisible(selectedRectTransform);
            }
        }
    }

    private void EnsureVisible(RectTransform target)
    {
        // Calculamos los límites del viewport y del objeto objetivo en el espacio local del viewport
        Vector3[] viewportCorners = new Vector3[4];
        m_RectTransform.GetLocalCorners(viewportCorners);
        Rect viewportRect = new Rect(viewportCorners[0], viewportCorners[2] - viewportCorners[0]);

        Vector3[] targetCorners = new Vector3[4];
        target.GetWorldCorners(targetCorners);
        
        // Convertimos las esquinas del mundo del objetivo al espacio local del viewport para comparar
        for (int i = 0; i < 4; i++)
        {
            targetCorners[i] = m_RectTransform.InverseTransformPoint(targetCorners[i]);
        }
        Rect targetRect = new Rect(targetCorners[0], targetCorners[2] - targetCorners[0]);

        Vector2 delta = Vector2.zero;

        // Lógica para movimiento Vertical
        if (m_ScrollRect.vertical)
        {
            // Si el objetivo está por debajo del borde inferior
            if (targetRect.yMin < viewportRect.yMin)
            {
                delta.y = viewportRect.yMin - targetRect.yMin;
            }
            // Si el objetivo está por encima del borde superior
            else if (targetRect.yMax > viewportRect.yMax)
            {
                delta.y = viewportRect.yMax - targetRect.yMax;
            }
        }

        // Lógica para movimiento Horizontal 
        if (m_ScrollRect.horizontal)
        {
            if (targetRect.xMin < viewportRect.xMin)
            {
                delta.x = viewportRect.xMin - targetRect.xMin;
            }
            else if (targetRect.xMax > viewportRect.xMax)
            {
                delta.x = viewportRect.xMax - targetRect.xMax;
            }
        }

        // Si hay una diferencia (delta), movemos el contenido
        if (delta != Vector2.zero)
        {
            // Movemos el contenido suavemente hacia la nueva posición
            m_ContentRectTransform.anchoredPosition += delta;
        }
    }
}