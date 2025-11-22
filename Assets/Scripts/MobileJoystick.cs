using UnityEngine;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform handle;
    public float radius = 100f;
    public bool smoothReturn = true;
    public float returnSpeed = 8f;

    Vector2 input;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out pos
        );

        pos = Vector2.ClampMagnitude(pos, radius);
        handle.anchoredPosition = pos;
        input = pos / radius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        if (smoothReturn)
            StartCoroutine(ReturnHandle());
        else
            handle.anchoredPosition = Vector2.zero;
    }

    System.Collections.IEnumerator ReturnHandle()
    {
        while (handle.anchoredPosition.magnitude > 0.1f)
        {
            handle.anchoredPosition = Vector2.Lerp(
                handle.anchoredPosition,
                Vector2.zero,
                Time.deltaTime * returnSpeed
            );
            yield return null;
        }
        handle.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Returns joystick movement direction (-1 to 1)
    /// </summary>
    public Vector2 Direction()
    {
        return input;
    }
}
