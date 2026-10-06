using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class DeliveryManualAI : MonoBehaviour
{
    [Header("=== PENGATURAN ===")]
    public int totalPaket = 3;
    public float batasWaktu = 180f;
    
    [Header("=== DETECTION RADIUS ===")]
    public float nodeRadius = 8f;
    
    [Header("=== REFERENSI ===")]
    public Transform player;
    public Pathfinding pathfinding;
    public List<Node> daftarTujuan;
    
    [Header("=== NOTIFIKASI ===")]
    public NotifikasiPaket notifikasiSystem;
    
    [Header("=== UI ===")]
    public TextMeshProUGUI teksMode;
    public TextMeshProUGUI teksDaftarPaket;
    public TextMeshProUGUI teksProgress;
    public TextMeshProUGUI teksTimer;
    public TextMeshProUGUI teksJarakTempuh;
    public TextMeshProUGUI teksJarakSebenarnya;
    public TextMeshProUGUI teksJarakOptimalAI;
    public GameObject panelGameOver;
    public GameObject panelMenangManual;
    public GameObject panelMenangAI;
    public GameObject panelHasilSkripsi;
    public GameObject tombolLihatSkripsi;
    
    [Header("=== AI SETTINGS ===")]
    public float pengurangPerPaket = 10f;
    public float toleransiJarakAI = 20f;
    
    [Header("=== MINIMAP ===")]
    public GameObject prefabIconMinimap;
    public Transform canvasMinimap;
    
    [Header("=== PLAYER ICON ===")]
    public bool tampilkanPlayerIcon = true;
    public Color playerIconColor = Color.black;
    
    // ===== MODE =====
    private enum GameMode { Manual, AI }
    private GameMode currentMode = GameMode.Manual;
    
    // ===== VARIABEL INTERNAL =====
    private List<Node> paketBelumTerkirim = new List<Node>();
    private List<Node> paketTerkirim = new List<Node>();
    private int jumlahTerkirim = 0;
    private float waktuTersisa;
    public bool gameAktif = true;
    private Node tujuanSekarang = null;
    private int currentAIIndex = 0;
    private bool tombolHTerkunci = false;
    private bool menungguNotifikasi = false;
    
    private List<string> logUrutanPlayer = new List<string>();
    private List<MinimapIcon> semuaIcon = new List<MinimapIcon>();
    private MinimapIcon playerIconScript;
    
    // ===== REKOMENDASI AI =====
    private List<Node> rekomendasiUrutan = new List<Node>();
    private List<Node> urutanAIAwal = new List<Node>();
    
    // ===== JARAK =====
    private float totalJarakTempuh = 0f;
    private Vector3 lastFramePosition;
    private Vector3 posisiAwalPlayer;
    private float jarakOptimalAI = 0f;
    private float jarakAISetelahPengurang = 0f;
    private float totalPengurang = 0f;
    
    // ===== DATA PERBANDINGAN =====
    private string dataPerbandingan;
    private float selisihJarak;
    private float jarakPlayer;
    private float jarakOptimal;
    private string urutanPlayerStr;
    private string urutanOptimalStr;
    private int levelIndex;
    
    void Start()
    {
        string namaScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        levelIndex = int.Parse(namaScene.Replace("Level", ""));
        
        waktuTersisa = batasWaktu;
        gameAktif = true;
        menungguNotifikasi = false;
        paketBelumTerkirim = new List<Node>(daftarTujuan);
        paketTerkirim = new List<Node>();
        lastFramePosition = player.position;
        
        posisiAwalPlayer = player.position;
        
        totalJarakTempuh = 0f;
        currentAIIndex = 0;
        currentMode = GameMode.Manual;
        tombolHTerkunci = false;
        
        totalPengurang = totalPaket * pengurangPerPaket;
        
        HitungRekomendasiUrutanDariPosisi(posisiAwalPlayer);
        urutanAIAwal = new List<Node>(rekomendasiUrutan);
        
        jarakAISetelahPengurang = Mathf.Max(0f, jarakOptimalAI - totalPengurang);
        
        // Sembunyikan panel
        if (panelGameOver != null) panelGameOver.SetActive(false);
        if (panelMenangManual != null) panelMenangManual.SetActive(false);
        if (panelMenangAI != null) panelMenangAI.SetActive(false);
        if (panelHasilSkripsi != null) panelHasilSkripsi.SetActive(false);
        if (tombolLihatSkripsi != null) tombolLihatSkripsi.SetActive(false);
        
        BuatIconMinimap();
        BuatPlayerIcon();
        UpdateUI();
        UpdateModeUI();
        UpdateJarakUI();
        UpdateJarakPerbandinganUI();
    }
    
    void UpdateJarakUI()
    {
        if (teksJarakTempuh != null)
        {
            teksJarakTempuh.text = $"{totalJarakTempuh:F1} m";
        }
    }
    
    void UpdateJarakPerbandinganUI()
    {
        if (teksJarakSebenarnya != null)
        {
            teksJarakSebenarnya.text = $"JARAK ANDA: {totalJarakTempuh:F1} m";
        }
        
        if (teksJarakOptimalAI != null)
        {
            teksJarakOptimalAI.text = $"JARAK OPTIMAL: {jarakAISetelahPengurang:F1} m";
        }
    }
    
    void HitungRekomendasiUrutanDariPosisi(Vector3 posisiAwal)
    {
        if (paketBelumTerkirim.Count == 0) return;
        
        List<Node> urutanTerbaik = new List<Node>();
        float jarakTerbaik = Mathf.Infinity;
        
        List<List<Node>> semuaUrutan = GetPermutasi(paketBelumTerkirim);
        foreach (List<Node> urutan in semuaUrutan)
        {
            float total = HitungTotalJarakDariPosisi(urutan, posisiAwal);
            if (total < jarakTerbaik)
            {
                jarakTerbaik = total;
                urutanTerbaik = urutan;
            }
        }
        
        rekomendasiUrutan = urutanTerbaik;
        currentAIIndex = 0;
        jarakOptimalAI = jarakTerbaik;
    }
    
    void HitungRekomendasiUrutan()
    {
        HitungRekomendasiUrutanDariPosisi(posisiAwalPlayer);
        jarakAISetelahPengurang = Mathf.Max(0f, jarakOptimalAI - totalPengurang);
        UpdateJarakPerbandinganUI();
    }
    
    float HitungTotalJarakDariPosisi(List<Node> urutan, Vector3 posisiAwal)
    {
        if (urutan.Count == 0) return 0f;
        
        float total = 0f;
        Vector3 posisi = posisiAwal;
        
        foreach (Node node in urutan)
        {
            float jarak = HitungJarakViaPathfinding(posisi, node.transform.position);
            total += jarak;
            posisi = node.transform.position;
        }
        return total;
    }
    
    float HitungJarakViaPathfinding(Vector3 start, Vector3 end)
    {
        if (pathfinding == null)
        {
            return Vector3.Distance(start, end);
        }
        
        Node startNode = GetNearestNode(start);
        Node endNode = GetNearestNode(end);
        
        if (startNode == null || endNode == null)
            return Vector3.Distance(start, end);
        
        if (startNode == endNode)
            return Vector3.Distance(start, end);
        
        float jarakNode = pathfinding.GetDistanceBetweenNodes(startNode, endNode);
        
        if (jarakNode < 0f)
            return Vector3.Distance(start, end);
        
        float jarakAwal = Vector3.Distance(start, startNode.transform.position);
        float jarakAkhir = Vector3.Distance(end, endNode.transform.position);
        
        return jarakAwal + jarakNode + jarakAkhir;
    }
    
    Node GetNearestNode(Vector3 position)
    {
        if (pathfinding == null || pathfinding.allNodes == null || pathfinding.allNodes.Count == 0)
            return null;
        
        Node nearest = null;
        float nearestDist = Mathf.Infinity;
        
        foreach (Node node in pathfinding.allNodes)
        {
            if (node == null) continue;
            float dist = Vector3.Distance(position, node.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = node;
            }
        }
        return nearest;
    }
    
    void UpdateModeUI()
    {
        if (teksMode != null)
        {
            if (currentMode == GameMode.Manual)
            {
                teksMode.text = "MODE : MANUAL";
            }
            else
            {
                teksMode.text = "MODE : AI";
            }
        }
    }
    
    void BuatIconMinimap()
    {
        if (prefabIconMinimap == null || canvasMinimap == null || daftarTujuan == null || daftarTujuan.Count == 0)
            return;
        
        foreach (Node node in daftarTujuan)
        {
            if (node == null) continue;
            
            GameObject iconObj = Instantiate(prefabIconMinimap, canvasMinimap);
            iconObj.name = "Icon_" + node.name;
            
            MinimapIcon iconScript = iconObj.GetComponent<MinimapIcon>();
            if (iconScript != null)
            {
                iconScript.targetNode = node.transform;
                semuaIcon.Add(iconScript);
            }
        }
    }
    
    void BuatPlayerIcon()
    {
        if (!tampilkanPlayerIcon || prefabIconMinimap == null || canvasMinimap == null || player == null)
            return;
        
        GameObject playerIconObj = Instantiate(prefabIconMinimap, canvasMinimap);
        playerIconObj.name = "Icon_Player";
        
        playerIconScript = playerIconObj.GetComponent<MinimapIcon>();
        if (playerIconScript != null)
        {
            playerIconScript.targetNode = player;
            playerIconScript.SetAsPlayerIcon(playerIconColor);
        }
    }
    
    void Update()
    {
        if (!gameAktif) return;
        if (menungguNotifikasi) return;
        
        waktuTersisa -= Time.deltaTime;
        if (waktuTersisa <= 0)
        {
            GameOver("WAKTU HABIS!");
            return;
        }
        
        float jarakPerFrame = Vector3.Distance(player.position, lastFramePosition);
        if (jarakPerFrame > 0.001f)
        {
            totalJarakTempuh += jarakPerFrame;
        }
        lastFramePosition = player.position;
        
        UpdateJarakUI();
        UpdateJarakPerbandinganUI();
        
        // Tombol H untuk pindah mode
        if (Input.GetKeyDown(KeyCode.H) && !tombolHTerkunci)
        {
            // CEK SEMUA LEVEL SUDAH OPTIMAL?
            if (ProgresManager.Instance != null && !ProgresManager.Instance.SemuaLevelOptimal())
            {
                Debug.Log("🔒 Belum semua level optimal!");
                return; // LANGSUNG KELUAR
            }
            
            // Kalo udah semua, baru bisa ganti mode
            if (currentMode == GameMode.Manual)
            {
                currentMode = GameMode.AI;
                HitungRekomendasiUrutan();
                StartAIMode();
                tombolHTerkunci = true;
            }
            else
            {
                currentMode = GameMode.Manual;
                tujuanSekarang = null;
                if (pathfinding != null) pathfinding.SetTargetNode(null);
                tombolHTerkunci = true;
            }
            UpdateModeUI();
        }
        
        // Mode Manual: Pilih target dengan angka
        if (currentMode == GameMode.Manual)
        {
            int indexPaket = 0;
            for (int i = 0; i < daftarTujuan.Count && indexPaket < 9; i++)
            {
                Node node = daftarTujuan[i];
                if (node == null) continue;
                if (paketTerkirim.Contains(node)) continue;
                
                if (Input.GetKeyDown(KeyCode.Alpha1 + indexPaket))
                {
                    PilihPaketManual(node);
                }
                indexPaket++;
            }
        }
        
        // Cek apakah player sampai di target
        if (tujuanSekarang != null)
        {
            float jarak = Vector3.Distance(player.position, tujuanSekarang.transform.position);
            
            if (jarak < nodeRadius)
            {
                logUrutanPlayer.Add(tujuanSekarang.name);
                KirimPaket(tujuanSekarang);
                tujuanSekarang = null;
                
                if (currentMode == GameMode.AI)
                {
                    StartAIMode();
                }
                
                UpdateJarakUI();
                UpdateUI();
            }
        }
        
        UpdateUI();
    }
    
    void StartAIMode()
    {
        for (int i = 0; i < rekomendasiUrutan.Count; i++)
        {
            Node node = rekomendasiUrutan[i];
            if (node == null) continue;
            
            if (!paketTerkirim.Contains(node))
            {
                tujuanSekarang = node;
                currentAIIndex = i;
                
                if (pathfinding != null)
                {
                    pathfinding.SetTargetNode(node);
                }
                
                foreach (MinimapIcon icon in semuaIcon)
                {
                    if (icon.targetNode == node.transform)
                        icon.SetAsTarget();
                    else if (paketBelumTerkirim.Contains(icon.targetNode.GetComponent<Node>()))
                        icon.SetAsNormal();
                }
                
                return;
            }
        }
        
        tujuanSekarang = null;
    }
    
    public void PilihPaketManual(Node paket)
    {
        if (paketTerkirim.Contains(paket) || !paketBelumTerkirim.Contains(paket))
            return;
        
        tombolHTerkunci = true;
        tujuanSekarang = paket;
        
        if (pathfinding != null)
        {
            pathfinding.SetTargetNode(paket);
        }
        
        foreach (MinimapIcon icon in semuaIcon)
        {
            if (icon.targetNode == paket.transform)
                icon.SetAsTarget();
            else if (paketBelumTerkirim.Contains(icon.targetNode.GetComponent<Node>()))
                icon.SetAsNormal();
        }
        
        UpdateJarakUI();
    }
    
    void KirimPaket(Node paket)
    {
        paketBelumTerkirim.Remove(paket);
        paketTerkirim.Add(paket);
        jumlahTerkirim++;
        waktuTersisa += 10f;
        
        foreach (MinimapIcon icon in semuaIcon)
        {
            if (icon.targetNode == paket.transform)
                icon.SetAsDelivered();
        }
        Time.timeScale = 0f;

        bool semuaTerkirim = (jumlahTerkirim >= totalPaket);
        
        if (notifikasiSystem != null)
        {
            menungguNotifikasi = true;
            gameAktif = false;
            notifikasiSystem.TampilkanNotifikasiPaket(
                paket.name,
                jumlahTerkirim,
                totalPaket,
                totalJarakTempuh,
                semuaTerkirim
            );
        }
        else
        {
            if (semuaTerkirim)
            {
                Menang();
            }
            else
            {
                UpdateUI();
            }
        }
    }
    
    // ===== DIPANGGIL OLEH NOTIFIKASI SAAT OK DIKLIK =====
    public void LanjutkanGame()
    {
        menungguNotifikasi = false;
        gameAktif = true;
        UpdateUI();
    }
    
    public void LanjutkanKeMenang()
    {
        menungguNotifikasi = false;
        Menang();
    }
    
    List<List<Node>> GetPermutasi(List<Node> nodes)
    {
        List<List<Node>> hasil = new List<List<Node>>();
        if (nodes.Count == 1)
        {
            hasil.Add(new List<Node>(nodes));
            return hasil;
        }
        
        for (int i = 0; i < nodes.Count; i++)
        {
            Node current = nodes[i];
            List<Node> sisa = new List<Node>(nodes);
            sisa.RemoveAt(i);
            
            List<List<Node>> sisaPermutasi = GetPermutasi(sisa);
            foreach (List<Node> perm in sisaPermutasi)
            {
                List<Node> newPerm = new List<Node> { current };
                newPerm.AddRange(perm);
                hasil.Add(newPerm);
            }
        }
        return hasil;
    }
    
    void UpdateUI()
    {
        if (teksDaftarPaket != null)
        {
            string daftar = "LIST PAKET :\n";
            
            for (int i = 0; i < daftarTujuan.Count; i++)
            {
                Node node = daftarTujuan[i];
                if (node == null) continue;
                
                bool sudahTerkirim = paketTerkirim.Contains(node);
                bool adalahTujuan = (tujuanSekarang == node);   
                
                string status = sudahTerkirim ? "[o]" : (adalahTujuan ? "[>]" : "[ ]");
                
                daftar += $" {status} {node.name}\n";
            }
            teksDaftarPaket.text = daftar;
        }
        
        if (teksProgress != null)
            teksProgress.text = $"{jumlahTerkirim}/{totalPaket}";
        
        if (teksTimer != null)
        {
            int menit = Mathf.FloorToInt(waktuTersisa / 60);
            int detik = Mathf.FloorToInt(waktuTersisa % 60);
            teksTimer.text = $"{menit:00}:{detik:00}";
            
            if (waktuTersisa < 30f)
                teksTimer.color = Color.red;
            else if (waktuTersisa < 60f)
                teksTimer.color = Color.yellow;
            else
                teksTimer.color = Color.white;
        }
        
        UpdateJarakUI();
        UpdateJarakPerbandinganUI();
    }
    
    // ===== ===== ===== ===== ===== ===== ===== ===== =====
    // ===== STATUS & PERBANDINGAN =====
    // ===== ===== ===== ===== ===== ===== ===== ===== =====
    
    private bool IsPlayerFollowingAIOrder()
    {
        if (logUrutanPlayer.Count == 0)
            return false;
        
        List<string> urutanAINama = urutanAIAwal.Select(n => n.name).ToList();
        
        if (logUrutanPlayer.Count != urutanAINama.Count)
            return false;
        
        for (int i = 0; i < logUrutanPlayer.Count; i++)
        {
            if (logUrutanPlayer[i] != urutanAINama[i])
                return false;
        }
        
        return true;
    }
    
    private bool IsPlayerBetterThanAI()
    {
        return totalJarakTempuh < jarakAISetelahPengurang;
    }
    
    private bool IsPlayerFollowingAIPath()
    {
        return selisihJarak <= toleransiJarakAI;
    }
    
    private float GetUrutanSimilarity()
    {
        if (logUrutanPlayer.Count == 0 || urutanAIAwal.Count == 0)
            return 0f;
        
        int sama = 0;
        int total = Mathf.Min(logUrutanPlayer.Count, urutanAIAwal.Count);
        
        for (int i = 0; i < total; i++)
        {
            if (logUrutanPlayer[i] == urutanAIAwal[i].name)
                sama++;
        }
        
        return (float)sama / total * 100f;
    }
    
    private void GetStatusMenang(out string status, out string detail)
    {
        bool mengikutiUrutanAI = IsPlayerFollowingAIOrder();
        bool lebihBaikDariAI = IsPlayerBetterThanAI();
        bool mengikutiJalurAI = IsPlayerFollowingAIPath();
        float similarity = GetUrutanSimilarity();
        
        status = "";
        detail = "";
        
        // ============================================================
        // PRIORITAS 1: LEBIH BAIK DARI AI (JARAK LEBIH PENDEK)
        // ============================================================
        if (lebihBaikDariAI)
        {
            status = "<color=#4CAF50>MENGGUNAKAN JALUR OPTIMAL DENGAN BAIK</color>";
            detail = string.Format("SELISIH JARAK : {0:F1} m (lebih pendek)", -selisihJarak);
        }
        
        // ============================================================
        // PRIORITAS 2: DALAM TOLERANSI JARAK (≤ 20m)
        // ============================================================
        else if (mengikutiJalurAI)
        {
            if (mengikutiUrutanAI)
            {
                status = "<color=#4CAF50>MENGGUNAKAN JALUR OPTIMAL</color>";
                detail = string.Format("SELISIH JARAK : {0:F1} m", selisihJarak);
            }
            else if (similarity >= 80f)
            {
                status = string.Format("<color=#4CAF50>URUTAN {0:F0}% SAMA DENGAN AI</color>", similarity);
                detail = string.Format("SELISIH JARAK : {0:F1} m", selisihJarak);
            }
            else
            {
                status = "<color=#4CAF50>JARAK SANGAT PRESISI</color>";
                detail = string.Format("SELISIH JARAK : {0:F1} m", selisihJarak);
            }
        }
        
        // ============================================================
        // PRIORITAS 3: MELEBIHI TOLERANSI (> 20m)
        // ============================================================
        else
        {
            if (mengikutiUrutanAI)
            {
                status = "<color=#F44336>URUTAN SAMA TAPI JALUR KURANG EFISIEN</color>";
                detail = string.Format("SELISIH JARAK : {0:F1} m (MELEBIHI TOLERANSI)", selisihJarak);
            }
            else if (similarity >= 80f)
            {
                status = string.Format("<color=#F44336>URUTAN {0:F0}% SAMA TAPI JALUR KURANG EFISIEN</color>", similarity);
                detail = string.Format("SELISIH JARAK : {0:F1} m (MELEBIHI TOLERANSI)", selisihJarak);
            }
            else if (selisihJarak <= 20f)  // Sebenarnya ini tidak akan terpakai karena sudah di filter di atas
            {
                status = "<color=#F44336>PERLU TINGKATKAN STRATEGI URUTAN</color>";
                detail = string.Format("SELISIH JARAK : {0:F1} m", selisihJarak);
            }
            else
            {
                status = "<color=#F44336>PERLU BELAJAR STRATEGI PENGIRIMAN</color>";
                detail = string.Format("SELISIH JARAK : {0:F1} m", selisihJarak);
            }
        }
    }
    
    void HitungDataPerbandingan()
    {
        List<Node> urutanOptimal = new List<Node>(urutanAIAwal);
        if (urutanOptimal.Count == 0) urutanOptimal = new List<Node>(rekomendasiUrutan);
        
        jarakOptimal = jarakAISetelahPengurang;
        jarakPlayer = totalJarakTempuh;
        selisihJarak = jarakPlayer - jarakOptimal;
        
        urutanPlayerStr = string.Join(" -> ", logUrutanPlayer);
        urutanOptimalStr = string.Join(" -> ", urutanOptimal.Select(n => n.name));
        
        if (logUrutanPlayer.Count == 0) urutanPlayerStr = "Belum ada";
        if (string.IsNullOrEmpty(urutanOptimalStr)) urutanOptimalStr = "Belum ada";
        
        bool mengikutiUrutanAI = IsPlayerFollowingAIOrder();
        bool lebihBaikDariAI = IsPlayerBetterThanAI();
        bool mengikutiJalurAI = IsPlayerFollowingAIPath();
        float similarity = GetUrutanSimilarity();
        
        if (currentMode == GameMode.AI)
        {
            dataPerbandingan = 
                "====================================\n" +
                "          HASIL AI\n" +
                "====================================\n\n" +
                string.Format("URUTAN: {0}\n", urutanOptimalStr) +
                string.Format("JARAK: {0:F1} m\n", jarakOptimal) +
                "====================================\n\n" +
                "[SPACE UNTUK RESTART]";
        }
        else
        {
            float persentase = 0f;
            if (jarakOptimal > 0) persentase = (selisihJarak / jarakOptimal) * 100f;
            
            string status = "";
            string statusDeskripsi = "";
            
            if (mengikutiUrutanAI)
            {
                status = "<color=#4CAF50>MENGGUNAKAN JALUR OPTIMAL</color>";
                statusDeskripsi = string.Format("SELISIH JARAK : {0:F1} m", selisihJarak);
            }
            else if (lebihBaikDariAI)
            {
                status = "<color=#4CAF50>ANDA LEBIH BAIK DARI AI</color>";
                statusDeskripsi = string.Format("SELISIH JARAK : {0:F1} m (lebih pendek)", -selisihJarak);
            }
            else if (similarity >= 80f && mengikutiJalurAI)
            {
                status = string.Format("<color=#4CAF50>URUTAN {0:F0}% SAMA DENGAN AI</color>", similarity);
                statusDeskripsi = string.Format("SELISIH JARAK : {0:F1} m", selisihJarak);
            }
            else if (similarity >= 80f && !mengikutiJalurAI)
            {
                status = string.Format("<color=#F44336>URUTAN {0:F0}% SAMA TAPI JALUR KURANG EFISIEN</color>", similarity);
                statusDeskripsi = string.Format("SELISIH JARAK : {0:F1} m (melebihi toleransi)", selisihJarak);
            }
            else if (selisihJarak <= 5f)
            {
                status = "<color=#4CAF50>JARAK SANGAT PRESISI</color>";
                statusDeskripsi = string.Format("SELISIH JARAK : {0:F1} m", selisihJarak);
            }
            else if (selisihJarak <= 20f)
            {
                status = "<color=#F44336>PERLU TINGKATKAN STRATEGI URUTAN</color>";
                statusDeskripsi = string.Format("SELISIH JARAK : {0:F1} m", selisihJarak);
            }
            else
            {
                status = "<color=#F44336>PERLU BELAJAR STRATEGI PENGIRIMAN</color>";
                statusDeskripsi = string.Format("SELISIH JARAK : {0:F1} m", selisihJarak);
            }
            
            dataPerbandingan = 
                "DATA ANDA\n" +
                string.Format("URUTAN : {0}\n", urutanPlayerStr) +
                string.Format("JARAK : {0:F1} m\n\n", jarakPlayer) +
                "REKOMENDASI AI\n" +
                string.Format("URUTAN : {0}\n", urutanOptimalStr) +
                string.Format("JARAK : {0:F1} m\n\n", jarakOptimal) +
                "KESIMPULAN\n" +
                string.Format("SELISIH JARAK : {0:F1} m\n", selisihJarak) +
                string.Format("PERSENTASE : {0:F0}%\n", Mathf.Abs(persentase)) +
                string.Format("KESAMAAN URUTAN : {0:F0}%\n", similarity);
        }
    }
    
    void Menang()
    {
        gameAktif = false;
        
        HitungDataPerbandingan();
        // CEK OPTIMAL?
        float selisih = totalJarakTempuh - jarakAISetelahPengurang;
        if ((selisih <= toleransiJarakAI || IsPlayerFollowingAIOrder()) && ProgresManager.Instance != null)
        {
            ProgresManager.Instance.TandaiLevelOptimal(levelIndex);
        }
        string status, detail;
        GetStatusMenang(out status, out detail);
        
        if (currentMode == GameMode.AI)
        {
            if (panelMenangAI != null)
            {
                TextMeshProUGUI teksMenangAI = panelMenangAI.GetComponentInChildren<TextMeshProUGUI>();
                if (teksMenangAI != null)
                {
                    teksMenangAI.text = string.Format(
                        "KAMU BERHASIL DENGAN BANTUAN AI\n" +
                        "{4}\n\n" +
                        "JARAK ANDA : {2:F1} m\n" +
                        "JARAK OPTIMAL : {3:F1} m\n" +
                        "{5}\n\n" +
                        "{0}/{1} PAKET TERKIRIM\n",
                        jumlahTerkirim, totalPaket, totalJarakTempuh, jarakAISetelahPengurang, status, detail
                    );
                }
                panelMenangAI.SetActive(true);
                if (panelMenangManual != null) panelMenangManual.SetActive(false);
            }
        }
        else
        {
            if (panelMenangManual != null)
            {
                TextMeshProUGUI teksMenangManual = panelMenangManual.GetComponentInChildren<TextMeshProUGUI>();
                if (teksMenangManual != null)
                {
                    teksMenangManual.text = string.Format(
                        "SELAMAT SUDAH MENYELESAIKAN!\n" +
                        "{4}\n\n" +
                        "JARAK ANDA : {2:F1} m\n" +
                        "JARAK OPTIMAL : {3:F1} m\n" +
                        "{5}\n\n" +
                        "{0}/{1} PAKET TERKIRIM\n",
                        jumlahTerkirim, totalPaket, totalJarakTempuh, jarakAISetelahPengurang, status, detail
                    );
                }
                panelMenangManual.SetActive(true);
                if (panelMenangAI != null) panelMenangAI.SetActive(false);
            }
        }
        
        if (tombolLihatSkripsi != null) 
        {
            tombolLihatSkripsi.SetActive(true);
        }
        
        Time.timeScale = 0f;
    }
    
    public void TampilkanPanelSkripsi()
    {
        if (panelHasilSkripsi != null)
        {
            TextMeshProUGUI teksHasil = panelHasilSkripsi.GetComponentInChildren<TextMeshProUGUI>();
            if (teksHasil != null) teksHasil.text = dataPerbandingan;
            panelHasilSkripsi.SetActive(true);
        }
    }
    
    public void GameOver(string alasan)
    {
        gameAktif = false;
        if (panelGameOver != null)
        {
            TextMeshProUGUI teksGO = panelGameOver.GetComponentInChildren<TextMeshProUGUI>();
            if (teksGO != null) teksGO.text = string.Format("\n{0}\n{1}/{2} PAKET TERKIRIM", alasan, jumlahTerkirim, totalPaket);
            panelGameOver.SetActive(true);
        }
        Time.timeScale = 0f;
    }
    
    public Node GetTujuanSekarang() { return tujuanSekarang; }
    public List<Node> GetPaketBelumTerkirim() { return paketBelumTerkirim; }
    public int GetJumlahTerkirim() { return jumlahTerkirim; }
    public float GetWaktuTersisa() { return waktuTersisa; }
    public float GetTotalJarakTempuh() { return totalJarakTempuh; }
    public bool IsAIModeActive() { return currentMode == GameMode.AI; }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
}