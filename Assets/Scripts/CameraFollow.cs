using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 3.5f, -7f);
    public float positionDamping = 5f;
    public float rotationDamping = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Follow Position smoothly
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * positionDamping);

        // Follow Rotation smoothly
        Vector3 lookPoint = target.position + target.forward * 10f;
        Quaternion desiredRotation = Quaternion.LookRotation(lookPoint - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationDamping);
    }
}
