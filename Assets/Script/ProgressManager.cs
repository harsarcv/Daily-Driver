using UnityEngine;

public class ProgresManager : MonoBehaviour
{
    public static ProgresManager Instance;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        // TEKAN R UNTUK RESET
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetSemuaProgress();
        }
    }
    
    public bool SemuaLevelOptimal()
    {
        for (int i = 1; i <= 5; i++)
        {
            if (!PlayerPrefs.HasKey($"Level{i}_Optimal"))
                return false;
        }
        return true;
    }
    
    public void TandaiLevelOptimal(int levelIndex)
    {
        PlayerPrefs.SetInt($"Level{levelIndex}_Optimal", 1);
        PlayerPrefs.Save();
        Debug.Log($"✅ Level {levelIndex} optimal!");
    }
    
    public void ResetSemuaProgress()
    {
        for (int i = 1; i <= 5; i++)
        {
            PlayerPrefs.DeleteKey($"Level{i}_Optimal");
        }
        PlayerPrefs.Save();
        Debug.Log("✅ SEMUA PROGRES DI-RESET!");
        Debug.Log("🔒 Tombol H sekarang terkunci lagi");
    }
}