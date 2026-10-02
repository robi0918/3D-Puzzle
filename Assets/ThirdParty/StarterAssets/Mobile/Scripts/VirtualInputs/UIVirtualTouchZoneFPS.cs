using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class UIVirtualTouchZoneFPS : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [System.Serializable]
    public class Event : UnityEvent<Vector2> { }

    [Header("Rect References")]
    public RectTransform containerRect;
    public RectTransform handleRect; // Biasanya trackpad FPS transparan/tanpa handle

    [Header("Settings")]
    public float magnitudeMultiplier = 1f;
    public bool invertXOutputValue;
    public bool invertYOutputValue;

    // Menyimpan posisi pointer di frame sebelumnya
    private Vector2 lastPointerPosition;
    private Vector2 currentPointerPosition;

    [Header("Output")]
    public Event touchZoneOutputEvent;

    void Start()
    {
        SetupHandle();
    }

    private void SetupHandle()
    {
        if (handleRect)
        {
            SetObjectActiveState(handleRect.gameObject, false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Ambil posisi awal sentuhan
        RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, eventData.position, eventData.pressEventCamera, out lastPointerPosition);

        if (handleRect)
        {
            SetObjectActiveState(handleRect.gameObject, true);
            UpdateHandleRectPosition(lastPointerPosition);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, eventData.position, eventData.pressEventCamera, out currentPointerPosition);

        // Logika Trackpad: Hitung perubahan posisi dari frame sebelumnya
        Vector2 positionDelta = currentPointerPosition - lastPointerPosition;

        // SIMPAN posisi sekarang untuk digunakan sebagai acuan di frame berikutnya
        lastPointerPosition = currentPointerPosition;

        // NORMALISASI DPI: Membagi dengan DPI asli HP agar geseran 1 cm di semua HP menghasilkan nilai yang sama
        float dpiScale = Screen.dpi > 0 ? Screen.dpi : 300f;
        Vector2 normalizedDelta = (positionDelta / dpiScale) * 100f; // Perkalian 100f untuk menormalkan angka

        // Terapkan filter inversi jika aktif
        Vector2 outputPosition = ApplyInversionFilter(normalizedDelta);

        // Kirim output delta
        OutputPointerEventValue(outputPosition * magnitudeMultiplier);

        if (handleRect)
        {
            UpdateHandleRectPosition(currentPointerPosition);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        lastPointerPosition = Vector2.zero;
        currentPointerPosition = Vector2.zero;

        // Kirim nilai zero agar kamera langsung berhenti saat dilepas
        OutputPointerEventValue(Vector2.zero);

        if (handleRect)
        {
            SetObjectActiveState(handleRect.gameObject, false);
            UpdateHandleRectPosition(Vector2.zero);
        }
    }

    void OutputPointerEventValue(Vector2 pointerPosition)
    {
        touchZoneOutputEvent.Invoke(pointerPosition);
    }

    void UpdateHandleRectPosition(Vector2 newPosition)
    {
        handleRect.anchoredPosition = newPosition;
    }

    void SetObjectActiveState(GameObject targetObject, bool newState)
    {
        targetObject.SetActive(newState);
    }

    Vector2 ApplyInversionFilter(Vector2 position)
    {
        if (invertXOutputValue) position.x = -position.x;
        if (invertYOutputValue) position.y = -position.y;
        return position;
    }
}