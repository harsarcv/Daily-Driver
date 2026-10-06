using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameSettings : MonoBehaviour
{
    [Header("Loading Settings")]
    public float loadingDelay = 4f;
    public GameObject loadingScreen;
    public GameObject[] disableOnStart;
    
    [Header("Loading UI")]
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI statusText;
    public Slider progressBar;
    
    [Header("Loading Messages")]
    public string[] loadingMessages = new string[]
    {
        "Memuat aset jalan raya",
        "Membangun infrastruktur node",
        "Menyiapkan kendaraan NPC",
        "Mengaktifkan sistem lampu lalu lintas",
        "Mengatur rute optimal AI",
        "Menghidupkan mesin kendaraan",
        "Memuat peta dunia",
        "Hampir selesai, bersiap mengemudi"
    };
    
    private bool gameStarted = false;
    
    void Start()
    {
        Application.targetFrameRate = 60;
        StartCoroutine(LoadingSequence());
    }
    
    IEnumerator LoadingSequence()
    {
        // Nonaktifkan objek game
        foreach (GameObject obj in disableOnStart)
        {
            if (obj != null) obj.SetActive(false);
        }
        
        // Aktifkan loading screen
        if (loadingScreen != null)
            loadingScreen.SetActive(true);
        
        // Reset progress
        if (progressBar != null)
            progressBar.value = 0f;
        
        if (progressText != null)
            progressText.text = "0%";
        
        if (statusText != null)
            statusText.text = "INITIALIZING...";
        
        float elapsedTime = 0f;
        int messageIndex = 0;
        float messageTimer = 0f;
        float messageInterval = loadingDelay / loadingMessages.Length;
        
        // Coroutine untuk animasi titik-titik yang terus berjalan
        Coroutine dotCoroutine = StartCoroutine(AnimateDots());
        
        while (elapsedTime < loadingDelay)
        {
            elapsedTime += Time.deltaTime;
            messageTimer += Time.deltaTime;
            
            // Update progress bar dengan efek smooth
            float progress = elapsedTime / loadingDelay;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            if (progressBar != null)
                progressBar.value = smoothProgress;
            
            if (progressText != null)
                progressText.text = $"{(progress * 100f):F0}%";
            
            // Ganti teks loading
            if (messageTimer >= messageInterval && messageIndex < loadingMessages.Length - 1)
            {
                messageTimer = 0f;
                messageIndex++;
                
                if (loadingText != null)
                {
                    loadingText.text = loadingMessages[messageIndex];
                }
            }
            
            // Update teks awal
            if (loadingText != null && messageIndex == 0)
            {
                loadingText.text = loadingMessages[0];
            }
            
            yield return null;
        }
        
        // Stop animasi titik
        StopCoroutine(dotCoroutine);
        
        // Selesai loading - efek final
        if (loadingText != null)
        {
            loadingText.text = "Selesai! Memulai permainan...";
        }
        
        if (progressBar != null)
            progressBar.value = 1f;
        
        if (progressText != null)
            progressText.text = "100%";
        
        if (statusText != null)
            statusText.text = "SELAMAT BERGABUNG!";
        
        yield return new WaitForSeconds(0.8f);
        
        // Tutup loading screen
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
        
        // Aktifkan game
        foreach (GameObject obj in disableOnStart)
        {
            if (obj != null) obj.SetActive(true);
        }
        
        gameStarted = true;
    }
    
    IEnumerator AnimateDots()
    {
        if (loadingText == null) yield break;
        
        int dotCount = 0;
        string baseText = loadingText.text;
        
        while (true)
        {
            dotCount = (dotCount + 1) % 4;
            string dots = new string('.', dotCount);
            
            // Ambil teks tanpa titik sebelumnya
            string cleanText = baseText;
            foreach (var message in loadingMessages)
            {
                if (loadingText.text.StartsWith(message))
                {
                    cleanText = message;
                    break;
                }
            }
            
            loadingText.text = cleanText + dots;
            yield return new WaitForSeconds(0.3f);
        }
    }
    
    public bool IsGameStarted()
    {
        return gameStarted;
    }
}