using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class CellDirectionalGroup : IEnumerable<Cell>
{
    private List<Cell> cells = new List<Cell>();

    public CellDirectionalGroup(List<Cell> cells)
    {
        this.cells = cells;
    }

    public List<Cell> Group { get => cells; }

    public Cell Up { get => TryGet(0); }
    public Cell Down { get => TryGet(2); }
    public Cell Left { get => TryGet(3); }
    public Cell Right { get => TryGet(1); }
    public Cell UpRight { get => TryGet(4); }
    public Cell UpLeft { get => TryGet(5); }
    public Cell DownRight { get => TryGet(6); }
    public Cell DownLeft { get => TryGet(7); }

    public IEnumerator<Cell> GetEnumerator()
    {
        return Group.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Group.GetEnumerator();
    }
    private Cell TryGet(int id)
    {
        if (cells.Count > id)
            return cells[id];

        return null;
    }
}