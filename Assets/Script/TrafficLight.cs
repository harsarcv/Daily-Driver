using UnityEngine;
using System.Collections;

public class TrafficLight : MonoBehaviour
{
    public enum LightState { Red, Yellow, Green }

    [Header("Durasi")]
    public float greenTime = 10f;
    public float yellowTime = 2f;

    [Header("Urutan")]
    public int lampuKe = 0;
    public TrafficLight nextLamp;

    [Header("Visual - GameObject Lampu")]
    public GameObject redLight;
    public GameObject yellowLight;
    public GameObject greenLight;

    [Header("Material Abu-Abu")]
    public Material offMaterial;  // Material untuk lampu yang mati (abu-abu)

    private LightState currentState;
    private float timer;
    private bool isWaitingForGreen = false;

    // Simpan material asli setiap lampu
    private Material redOriginalMaterial;
    private Material yellowOriginalMaterial;
    private Material greenOriginalMaterial;

    void Start()
    {
        // Simpan material asli dari setiap lampu
        if (redLight != null)
        {
            Renderer renderer = redLight.GetComponent<Renderer>();
            if (renderer != null) redOriginalMaterial = renderer.material;
        }

        if (yellowLight != null)
        {
            Renderer renderer = yellowLight.GetComponent<Renderer>();
            if (renderer != null) yellowOriginalMaterial = renderer.material;
        }

        if (greenLight != null)
        {
            Renderer renderer = greenLight.GetComponent<Renderer>();
            if (renderer != null) greenOriginalMaterial = renderer.material;
        }

        // Pastikan semua lampu aktif (tidak di-disable)
        if (redLight != null) redLight.SetActive(true);
        if (yellowLight != null) yellowLight.SetActive(true);
        if (greenLight != null) greenLight.SetActive(true);

        // Set semua ke OFF (abu-abu) di awal
        SetAllLightsOff();

        if (lampuKe == 0)
        {
            SetState(LightState.Green);
            timer = greenTime;
        }
        else
        {
            SetState(LightState.Red);
            timer = (greenTime + yellowTime) * lampuKe;
        }
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0 && !isWaitingForGreen)
        {
            if (currentState == LightState.Green)
            {
                SetState(LightState.Yellow);
                timer = yellowTime;
            }
            else if (currentState == LightState.Yellow)
            {
                SetState(LightState.Red);
                timer = (greenTime + yellowTime) * 2;
                
                if (nextLamp != null)
                    nextLamp.StartGreenSequence();
            }
        }
    }

    public void StartGreenSequence()
    {
        if (currentState == LightState.Red && !isWaitingForGreen)
        {
            StartCoroutine(RedToGreen());
        }
    }

    IEnumerator RedToGreen()
    {
        isWaitingForGreen = true;
        
        SetState(LightState.Yellow);
        timer = yellowTime;
        
        yield return new WaitForSeconds(yellowTime);
        
        if (currentState == LightState.Yellow)
        {
            SetState(LightState.Green);
            timer = greenTime;
        }
        
        isWaitingForGreen = false;
    }

    void SetState(LightState state)
    {
        currentState = state;
        
        // MATIKAN SEMUA LAMPU (jadikan abu-abu)
        SetAllLightsOff();
        
        // NYALAKAN LAMPU YANG SESUAI
        switch (state)
        {
            case LightState.Red:
                SetLight(redLight, redOriginalMaterial);
                break;
            case LightState.Yellow:
                SetLight(yellowLight, yellowOriginalMaterial);
                break;
            case LightState.Green:
                SetLight(greenLight, greenOriginalMaterial);
                break;
        }
    }

    void SetAllLightsOff()
    {
        SetLight(redLight, offMaterial);
        SetLight(yellowLight, offMaterial);
        SetLight(greenLight, offMaterial);
    }

    void SetLight(GameObject lightObject, Material material)
    {
        if (lightObject == null) return;

        // Pastikan GameObject aktif
        if (!lightObject.activeSelf)
            lightObject.SetActive(true);

        // Ubah material
        Renderer renderer = lightObject.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.material = material;
        }
    }

    public bool IsRed() => currentState == LightState.Red || currentState == LightState.Yellow;
    public bool IsGreen() => currentState == LightState.Green;
}