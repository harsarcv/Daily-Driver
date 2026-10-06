using UnityEngine;

public class AIUnlockManager : MonoBehaviour
{
    [Header("Unlock AI")]
    public int gagalUntukUnlock = 5;

    [SerializeField] private int gagalBeruntun = 0;
    [SerializeField] private bool aiUnlocked = false;

    public void TambahGagal()
    {
        if (aiUnlocked) return;

        gagalBeruntun++;

        if (gagalBeruntun >= gagalUntukUnlock)
        {
            aiUnlocked = true;
            Debug.Log("AI Assistant berhasil dibuka!");
        }
    }

    public void ResetGagal()
    {
        gagalBeruntun = 0;
    }

    public bool IsUnlocked()
    {
        return aiUnlocked;
    }

    public int GetProgress()
    {
        return gagalBeruntun;
    }
}