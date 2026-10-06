using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform target;

    public float height = 80f;

    void LateUpdate()
    {
        if (target == null)
            return;

        transform.position =
            new Vector3(
                target.position.x,
                height,
                target.position.z
            );
    }
}