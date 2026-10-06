using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NotifikasiPaket : MonoBehaviour
{
    [Header("=== UI NOTIFIKASI ===")]
    public GameObject panelNotifikasi;
    public TextMeshProUGUI teksNotifikasi;
    public Button tombolOK;
    
    [Header("=== REFERENSI GAME ===")]
    public DeliveryManualAI gameManager;
    
    private bool isNotifikasiAktif = false;
    private bool semuaTerkirim = false;
    
    void Start()
    {
        if (panelNotifikasi != null)
            panelNotifikasi.SetActive(false);
        
        if (tombolOK != null)
        {
            tombolOK.onClick.AddListener(OnOKClicked);
        }
    }
    
    public void TampilkanNotifikasiPaket(string namaPaket, int paketKe, int totalPaket, float jarak, bool semuaTerkirimPaket = false)
    {
        if (isNotifikasiAktif) return;
        
        isNotifikasiAktif = true;
        semuaTerkirim = semuaTerkirimPaket;
        
        // Freeze game
        if (gameManager != null)
        {
            gameManager.gameAktif = false;
        }
        
        // Tampilkan panel
        if (panelNotifikasi != null)
            panelNotifikasi.SetActive(true);
        
        // Set teks notifikasi
        if (teksNotifikasi != null)
        {
            if (semuaTerkirim)
                teksNotifikasi.text = $"Semua paket telah terkirim! Total paket: {totalPaket}";
            else
                teksNotifikasi.text = $"Paket {namaPaket} telah terkirim!";
        }
        
        // Tampilkan tombol OK
        if (tombolOK != null)
            tombolOK.gameObject.SetActive(true);
    }

    public bool IsNotifikasiAktif()
    {
        return isNotifikasiAktif;
    }
    
    public void OnOKClicked()
    {
        if (!isNotifikasiAktif) return;
        
        // Sembunyikan panel
        if (panelNotifikasi != null)
            panelNotifikasi.SetActive(false);
        
        if (tombolOK != null)
            tombolOK.gameObject.SetActive(false);
        
        isNotifikasiAktif = false;
        
        // Unfreeze game
        Time.timeScale = 1f;
        
        if (gameManager != null)
        {
            if (semuaTerkirim)
            {
                gameManager.LanjutkanKeMenang();
            }
            else
            {
                gameManager.LanjutkanGame();
            }
        }
    }
    
    public void TutupNotifikasi()
    {
        if (isNotifikasiAktif)
        {
            OnOKClicked();
        }
    }
}