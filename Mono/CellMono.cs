using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellMono : MonoBehaviour
{
    /// <summary>
    /// The GameObject representing the actual door within the game world.
    /// This is the physical door that players can interact with.
    /// </summary>
    [SerializeField] public Vector3Int Position;
    [SerializeField] public CellType Type;
    [SerializeField] public RoomMono Room;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
