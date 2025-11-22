using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class MobileJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    public RectTransform handle;              // assign child handle RectTransform

    [Header("Settings")]
    [Tooltip("If <= 0 the script will compute the max radius from the background / handle sizes.")]
    public float radius = 0f;
    [Tooltip("Extra padding so the handle does not exactly touch the background edge (pixels).")]
    public float edgePadding = 8f;
    public bool smoothReturn = true;
    public float returnSpeed = 10f;

    // runtime
    RectTransform bgRect;
    Vector2 input = Vector2.zero;
    float effectiveRadius = 1f;

    void Awake()
    {
        bgRect = GetComponent<RectTransform>();
        if (handle == null) Debug.LogError("MobileJoystick: assign 'handle' RectTransform in Inspector.");
        ComputeEffectiveRadius();
    }

    void OnValidate()
    {
        if (edgePadding < 0f) edgePadding = 0f;
        if (returnSpeed < 0f) returnSpeed = 0f;
        ComputeEffectiveRadius();
    }

    void ComputeEffectiveRadius()
    {
        if (bgRect == null) bgRect = GetComponent<RectTransform>();
        if (bgRect == null || handle == null) { effectiveRadius = Mathf.Max(1f, radius); return; }

        float halfBg = Mathf.Min(bgRect.rect.width, bgRect.rect.height) * 0.5f;
        float halfHandle = Mathf.Max(handle.rect.width, handle.rect.height) * 0.5f;

        float maxPossible = Mathf.Max(1f, halfBg - halfHandle - edgePadding);
        if (radius > 0.001f)
            effectiveRadius = Mathf.Min(radius, maxPossible);
        else
            effectiveRadius = maxPossible;

        if (effectiveRadius < 1f) effectiveRadius = 1f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (bgRect == null || handle == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(bgRect, eventData.position, eventData.pressEventCamera, out localPoint);

        Vector2 clamped = Vector2.ClampMagnitude(localPoint, effectiveRadius);
        handle.anchoredPosition = clamped;
        input = clamped / effectiveRadius; // normalized -1..1
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;
        if (smoothReturn)
            StartCoroutine(SmoothReturn());
        else
            handle.anchoredPosition = Vector2.zero;
    }

    System.Collections.IEnumerator SmoothReturn()
    {
        while (handle != null && handle.anchoredPosition.sqrMagnitude > 0.01f)
        {
            handle.anchoredPosition = Vector2.Lerp(handle.anchoredPosition, Vector2.zero, Time.deltaTime * returnSpeed);
            input = handle.anchoredPosition / Mathf.Max(1f, effectiveRadius);
            yield return null;
        }
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        input = Vector2.zero;
    }

    /// <summary>
    /// Direction in range [-1..1]
    /// </summary>
    public Vector2 Direction()
    {
        return input;
    }

    [ContextMenu("Recompute Radius")]
    public void RecomputeRadius()
    {
        ComputeEffectiveRadius();
    }
}
