using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InteractableMinimap : MonoBehaviour
{
    [Header("References")]
    public RectTransform minimapPanel;
    public Camera minimapCamera;
    public Transform player;
    
    [Header("Size Settings")]
    public KeyCode toggleKey = KeyCode.M;
    public Vector2 smallSize = new Vector2(250, 250);
    public Vector2 largeSize = new Vector2(800, 800);
    public Vector2 smallPosition = new Vector2(-20, 20);
    
    [Header("Camera Follow Settings")]
    public float followHeight = 40f;
    public Vector3 staticCameraPosition = new Vector3(0, 50, 0);
    
    [Header("Camera Zoom Settings")]
    public float smallCameraZoom = 35f;
    public float largeCameraZoom = 50f;
    
    [Header("Camera Rotation")]
    public Vector3 cameraRotation = new Vector3(90, 0, 0);
    
    [Header("Rotate Minimap with Player")]
    public bool rotateWithPlayer = true;
    
    [Header("=== SLOW MOTION SETTINGS ===")]
    public float slowMotionSpeed = 0.3f;
    public float transitionSpeed = 5f;
    
    public NotifikasiPaket notifikasiSystem;
    
    private bool isLarge = false;
    private bool isFollowing = true;
    private float targetTimeScale = 1f;
    private float currentTimeScale = 1f;
    public bool isPaused = false;
    
    void Awake()
    {
        if (minimapPanel == null)
            minimapPanel = GetComponent<RectTransform>();
        
        if (minimapCamera == null)
            minimapCamera = GetComponent<Camera>();
        
        if (notifikasiSystem == null)
            notifikasiSystem = FindFirstObjectByType<NotifikasiPaket>();
    }
    
    void Start()
    {
        SetSmallMode();
        
        currentTimeScale = 1f;
        targetTimeScale = 1f;
        Time.timeScale = 1f;
        
        // Delay sedikit biar semua icon selesai dibuat
        StartCoroutine(HideNodeIconsAfterDelay());
    }
    
    
    IEnumerator HideNodeIconsAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        // Sembunyikan semua icon NODE, tapi PLAYER ICON tetap muncul
        SetNodeIconsActive(false);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMinimap();
        }
        
        bool isNotifikasiAktif = notifikasiSystem != null && notifikasiSystem.IsNotifikasiAktif();

        if (!isNotifikasiAktif && !isPaused)
        {
            currentTimeScale = Mathf.Lerp(currentTimeScale, targetTimeScale, Time.unscaledDeltaTime * transitionSpeed);
            Time.timeScale = currentTimeScale;
        }
        
        UpdateCamera();
    }
    
    void ToggleMinimap()
    {
        if (isLarge)
        {
            SetSmallMode();
            targetTimeScale = 1f;
        }
        else
        {
            SetLargeMode();
            targetTimeScale = slowMotionSpeed;
        }
    }
    
    void SetSmallMode()
    {
        if (minimapPanel == null) return;
        
        isLarge = false;
        isFollowing = true;
        
        minimapPanel.anchorMin = new Vector2(1, 0);
        minimapPanel.anchorMax = new Vector2(1, 0);
        minimapPanel.pivot = new Vector2(1, 0);
        minimapPanel.sizeDelta = smallSize;
        minimapPanel.anchoredPosition = smallPosition;
        
        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = smallCameraZoom;
            minimapCamera.transform.rotation = Quaternion.Euler(cameraRotation);
        }
        
        // Mode Kecil: SEMBUNYIKAN NODE ICON, PLAYER ICON TETAP MUNCUL
        SetNodeIconsActive(false);
    }
    
    void SetLargeMode()
    {
        if (minimapPanel == null) return;
        
        isLarge = true;
        isFollowing = false;
        
        minimapPanel.anchorMin = new Vector2(0.5f, 0.5f);
        minimapPanel.anchorMax = new Vector2(0.5f, 0.5f);
        minimapPanel.pivot = new Vector2(0.5f, 0.5f);
        minimapPanel.sizeDelta = largeSize;
        minimapPanel.anchoredPosition = Vector2.zero;
        
        if (minimapCamera != null)
        {
            minimapCamera.transform.position = staticCameraPosition;
            minimapCamera.orthographicSize = largeCameraZoom;
            minimapCamera.transform.rotation = Quaternion.Euler(cameraRotation);
        }
        
        // Mode Besar: MUNCULKAN SEMUA ICON (NODE + PLAYER)
        SetAllIconsActive(true);
    }
    
    // 🔥 Method: Sembunyikan hanya NODE icon (bukan player icon)
    void SetNodeIconsActive(bool active)
    {
        if (minimapPanel == null) return;
        
        foreach (RectTransform child in minimapPanel)
        {
            MinimapIcon icon = child.GetComponent<MinimapIcon>();
            if (icon != null)
            {
                // 🔥 Cek apakah ini player icon atau node icon
                // Player icon biasanya namanya "Icon_Player"
                // Atau kita cek dari targetNode (player icon targetnya adalah Transform player)
                bool isPlayerIcon = icon.IsPlayerIcon();
                
                // Kalau player icon, tetap aktif. Kalau node icon, ikuti parameter
                if (isPlayerIcon)
                {
                    icon.gameObject.SetActive(true); // Player icon SELALU aktif
                }
                else
                {
                    icon.gameObject.SetActive(active); // Node icon mengikuti parameter
                }
            }
        }
    }
    
    // Method untuk show/hide SEMUA icon (termasuk player)
    void SetAllIconsActive(bool active)
    {
        if (minimapPanel == null) return;
        
        foreach (RectTransform child in minimapPanel)
        {
            MinimapIcon icon = child.GetComponent<MinimapIcon>();
            if (icon != null)
            {
                icon.gameObject.SetActive(active);
            }
        }
    }
    
    void UpdateCamera()
    {
        if (minimapCamera == null) return;
        if (player == null) return;
        
        if (isFollowing)
        {
            minimapCamera.transform.position = new Vector3(
                player.position.x,
                followHeight,
                player.position.z
            );
            
            if (rotateWithPlayer)
            {
                float playerAngle = player.eulerAngles.y;
                minimapCamera.transform.rotation = Quaternion.Euler(cameraRotation.x, playerAngle, cameraRotation.z);
            }
            else
            {
                minimapCamera.transform.rotation = Quaternion.Euler(cameraRotation);
            }
            
            minimapCamera.orthographicSize = smallCameraZoom;
        }
        else
        {
            minimapCamera.orthographicSize = largeCameraZoom;
            minimapCamera.transform.rotation = Quaternion.Euler(cameraRotation);
        }
    }
    
    public bool IsLarge() => isLarge;
    public bool IsFullscreen() => isLarge;
    
    public void SetSmallCameraZoom(float zoom)
    {
        smallCameraZoom = zoom;
        if (!isLarge && minimapCamera != null)
        {
            minimapCamera.orthographicSize = smallCameraZoom;
        }
    }
    
    public void SetLargeCameraZoom(float zoom)
    {
        largeCameraZoom = zoom;
        if (isLarge && minimapCamera != null)
        {
            minimapCamera.orthographicSize = largeCameraZoom;
        }
    }
    
    public void SetSlowMotionSpeed(float speed)
    {
        slowMotionSpeed = Mathf.Clamp(speed, 0.05f, 0.9f);
        if (isLarge)
        {
            targetTimeScale = slowMotionSpeed;
        }
    }
    
    public void ForceCloseMinimap()
    {
        if (isLarge)
        {
            ToggleMinimap();
        }
    }
    
    public void ForceOpenMinimap()
    {
        if (!isLarge)
        {
            ToggleMinimap();
        }
    }
    
    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
    
    void OnApplicationPause(bool pause)
    {
        if (pause && isLarge)
        {
            Time.timeScale = 1f;
        }
    }
}