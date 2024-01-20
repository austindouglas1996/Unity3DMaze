using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class InteractiveEntityMono : MonoBehaviour
{
    [Tooltip("Name of the item.")]
    [SerializeField] public string Name;

    private void Start()
    {
    }
}