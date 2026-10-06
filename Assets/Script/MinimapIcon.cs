using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MinimapIcon : MonoBehaviour, IPointerClickHandler
{
    [Header("Target")]

    [Header("Reference Panel Size")]
    public Vector2 smallReferenceSize = new Vector2(350, 350);
    public Vector2 largeReferenceSize = new Vector2(1920, 1920);

    public Transform targetNode;
    
    [Header("Warna")]
    public Color warnaNormal = Color.green;
    public Color warnaTarget = Color.yellow;
    public Color warnaTerkirim = Color.gray;
    
    [Header("Ukuran Icon")]
    public float ukuranNormal = 20f;
    public float ukuranFullscreen = 40f;
    
    [Header("Teks")]
    public TextMeshProUGUI labelText;
    public float fontSizeNormal = 10f;
    public float fontSizeFullscreen = 18f;
    public float offsetYNormal = -25f;
    public float offsetYFullscreen = -40f;
    
    private Image iconImage;
    private RectTransform rectTransform;
    private RectTransform labelRectTransform;
    private Camera minimapCamera;
    private RectTransform minimapPanel;
    private DeliveryManualAI deliveryManager;
    private InteractableMinimap interactableMinimap;
    private bool isPlayerIcon = false;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        iconImage = GetComponent<Image>();
        
        if (labelText != null)
        {
            labelRectTransform = labelText.GetComponent<RectTransform>();
        }
        
        if (iconImage != null && !isPlayerIcon)
        {
            iconImage.color = warnaNormal;
        }
        
        deliveryManager = FindFirstObjectByType<DeliveryManualAI>();
        interactableMinimap = FindFirstObjectByType<InteractableMinimap>();
        
        CariMinimapCamera();
        CariMinimapPanel();
        
        if (labelText != null && targetNode != null && !isPlayerIcon)
        {
            labelText.text = targetNode.name;
        }
        
        // Icon pakai anchor TENGAH
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        UpdateSizeAndPosition();
    }
    
    void Update()
    {
        if (targetNode == null || minimapCamera == null || minimapPanel == null) return;
        
        // 🔥 Cek apakah icon aktif (mode besar)
        if (!gameObject.activeSelf) return;
        
        UpdatePosition();
    }
    
    // 🔥 Method untuk cek apakah ini player icon
    public bool IsPlayerIcon()
    {
        return isPlayerIcon;
    }
    
    public void UpdateSizeAndPosition()
    {
        if (rectTransform == null) return;
        
        bool isLarge = interactableMinimap != null && interactableMinimap.IsFullscreen();
        
        float size = isLarge ? ukuranFullscreen : ukuranNormal;
        rectTransform.sizeDelta = new Vector2(size, size);
        
        if (labelText != null && !isPlayerIcon)
        {
            labelText.fontSize = isLarge ? fontSizeFullscreen : fontSizeNormal;
        }
        
        if (labelRectTransform != null && !isPlayerIcon)
        {
            float offsetY = isLarge ? offsetYFullscreen : offsetYNormal;
            labelRectTransform.anchoredPosition = new Vector2(0, offsetY);
        }
    }
    
    void UpdatePosition()
    {
        Vector3 viewportPos = minimapCamera.WorldToViewportPoint(targetNode.position);

        bool isLarge = interactableMinimap != null && interactableMinimap.IsFullscreen();

        Vector2 referenceSize = isLarge
            ? largeReferenceSize
            : smallReferenceSize;

        float x = (viewportPos.x - 0.5f) * referenceSize.x;
        float y = (viewportPos.y - 0.5f) * referenceSize.y;

        rectTransform.anchoredPosition = new Vector2(x, y);

        if (iconImage != null)
        {
            iconImage.enabled = true;

            bool diDalamPanel =
                Mathf.Abs(x) < referenceSize.x * 0.5f &&
                Mathf.Abs(y) < referenceSize.y * 0.5f;

            Color c = iconImage.color;
            c.a = diDalamPanel ? 1f : 0.3f;
            iconImage.color = c;
        }

        if (labelText != null && !isPlayerIcon)
        {
            labelText.enabled = true;
        }
    }
    
    void CariMinimapCamera()
    {
        GameObject camObj = GameObject.FindGameObjectWithTag("MinimapCamera");
        if (camObj != null)
        {
            minimapCamera = camObj.GetComponent<Camera>();
            return;
        }
        
        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in cameras)
        {
            if (cam != Camera.main && cam.transform.eulerAngles.x > 80f)
            {
                minimapCamera = cam;
                return;
            }
        }
        
        minimapCamera = Camera.main;
    }
    
    void CariMinimapPanel()
    {
        RawImage raw = FindFirstObjectByType<RawImage>();
        if (raw != null)
        {
            minimapPanel = raw.GetComponent<RectTransform>();
            return;
        }
        
        GameObject panel = GameObject.Find("MinimapPanel");
        if (panel != null)
        {
            minimapPanel = panel.GetComponent<RectTransform>();
        }
        
        if (minimapPanel == null)
        {
            Image img = FindFirstObjectByType<Image>();
            if (img != null)
            {
                minimapPanel = img.GetComponent<RectTransform>();
            }
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetNode == null || isPlayerIcon) return;
        if (!gameObject.activeSelf) return;
        
        if (deliveryManager == null)
            deliveryManager = FindFirstObjectByType<DeliveryManualAI>();
        
        if (deliveryManager != null && !deliveryManager.IsAIModeActive())
        {
            Node node = targetNode.GetComponent<Node>();
            if (node != null && deliveryManager.GetPaketBelumTerkirim().Contains(node))
            {
                deliveryManager.PilihPaketManual(node);
            }
        }
    }
    
    public void SetAsTarget()
    {
        if (iconImage != null && !isPlayerIcon) iconImage.color = warnaTarget;
    }
    
    public void SetAsNormal()
    {
        if (iconImage != null && !isPlayerIcon) iconImage.color = warnaNormal;
    }
    
    public void SetAsDelivered()
    {
        if (iconImage != null && !isPlayerIcon) iconImage.color = warnaTerkirim;
    }
    
    public void SetAsPlayerIcon(Color warna)
    {
        isPlayerIcon = true;
        if (iconImage != null) iconImage.color = warna;
        if (labelText != null) labelText.enabled = false;
        UpdateSizeAndPosition();
    }
}