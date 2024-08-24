using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;

public class PathTrigger : MonoBehaviour
{
    /// <summary>
    /// The player that is involved with this path trigger.
    /// </summary>
    [SerializeField] private GameObject Player;

    /// <summary>
    /// Position of the last tile the player interacted with.
    /// </summary>
    public Vector3Int Position;

    /// <summary>
    /// Handles trigger enter.
    /// </summary>
    /// <param name="other"></param>
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Path")
        {
            Position = other.transform.position.RoundToInt();
        }
    }
}
