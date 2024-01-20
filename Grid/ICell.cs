using System.Numerics;
using UnityEngine;

public interface ICell
{
    public CellType Type { get; set; }
    public Vector3Int Position { get; set; }
}