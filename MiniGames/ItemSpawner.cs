using System.Threading.Tasks;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] public int MinItems = 10;
    [SerializeField] public int MaxItems = 25;

    private Renderer objectRenderer;

    private async void Start()
    {
        await SpawnItems();
        Destroy(this.gameObject);
    }

    private async Task SpawnItems()
    {
        var itemsPrefabs = MazeResourceManager.Instance.Items;

        int items = Random.Range(MinItems, MaxItems);
        for (int i = 0; i < items; i++)
        {
            PocketableItem newObj = Instantiate(itemsPrefabs.FindAll(r => r.RequiresTwoHands == false).Random(), this.transform.localPosition, Quaternion.identity, this.transform);
            newObj.transform.localPosition = Vector3.zero;

            newObj.transform.parent = this.transform.parent;
        }
    }

    private Vector3 GetRandomPosition()
    {
        if (objectRenderer == null)
        {
            return Vector3.zero;
        }

        Bounds bounds = TransformHelper.BoundingBox(this.transform);

        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomY = Random.Range(bounds.min.y, bounds.max.y);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        return new Vector3(randomX, randomY, randomZ);
    }
}