using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class ItemSway : MonoBehaviour
{
    [SerializeField] private float swayAmount = 0.1f;
    [SerializeField] private float swaySpeed = 2f;

    private Vector3 SwayOffset = Vector3Int.zero;
    private Quaternion SwayRotation = Quaternion.identity;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalrotation;

    public void ClearSway()
    {
        this.SwayOffset = Vector3.zero;
        this.SwayRotation = Quaternion.identity;
    }

    public void SetSway(Vector3Int currentPos, Vector3Int destinationPos)
    {
        // Calculate the direction of movement
        Vector3 direction = (destinationPos - currentPos);
        direction = direction.normalized;

        // Calculate the sway offset using a sinusoidal pattern
        float sway = Mathf.Sin(Time.time * swaySpeed) * swayAmount;

        // Calculate the sway offset in the direction of movement
        SwayOffset = new Vector3(0, sway, 0);

        float rotationSway = sway * 50f;

        // Calculate the sway rotation (opposite direction of movement)
        SwayRotation = Quaternion.Euler(new Vector3(-rotationSway * direction.x, rotationSway * direction.y, rotationSway * direction.z));
    }

    private void Start()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalrotation = transform.localRotation;
    }

    private void Update()
    {
        ApplySway();
    }

    private void ApplySway()
    {
        transform.localPosition = originalLocalPosition + SwayOffset;
        transform.rotation = SwayRotation;
    }
}