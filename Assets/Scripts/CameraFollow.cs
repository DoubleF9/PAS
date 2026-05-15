using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance = 6.0f;
    public float height = 1.5f;
    public float mouseSensitivity = 0.2f;
    public float smoothTime = 15f;

    private float currentX = 0f;
    private float currentY = 15f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;

        // Lock and hide the cursor so mouse delta drives the orbital camera
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float mouseX = 0;
        float mouseY = 0;

        if (Mouse.current != null)
        {
            mouseX = Mouse.current.delta.x.ReadValue();
            mouseY = Mouse.current.delta.y.ReadValue();
        }
        else
        {
            mouseX = Input.GetAxis("Mouse X");
            mouseY = Input.GetAxis("Mouse Y");
        }

        currentX += mouseX * mouseSensitivity;
        currentY -= mouseY * mouseSensitivity;
        currentY = Mathf.Clamp(currentY, -5f, 60f); // Limit up/down pitch

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        // Aim slightly above the car's pivot so the view sits over the roof, not the wheels
        Vector3 lookTarget = target.position + new Vector3(0, height, 0);
        Vector3 position = lookTarget - (rotation * Vector3.forward * distance);

        transform.rotation = rotation;
        transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * smoothTime);
    }
}