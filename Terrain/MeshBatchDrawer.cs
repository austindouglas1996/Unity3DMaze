using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A helper class to help with drawing large batches of entities. Items like grass, flowers, rocks rendering will increase using an instance like this.
/// Objects along with their respected LOD groups are batched into groups. The groups are then rendered directly to the GPU jumping over Unity rendering 
/// system which greatly improves performance with large batch jobs with batches going into the hundred thousands, like with rendering grass in a field.
/// </summary>
public class MeshBatchDrawer
{
    private const int MAX_BATCH_SIZE = 1023; // Unity's instance batch limit

    private List<MeshLOD> MeshLODs = new List<MeshLOD>();
    private List<List<Matrix4x4>>[] InstancesLOD;

    private List<Bounds>[] BatchBounds;
    private Vector3 LastFollowerPosition;

    /// <summary>
    /// Initialize a new instance of the <see cref="MeshBatchDrawer"/>.
    /// </summary>
    /// <param name="go"></param>
    /// <param name="follower"></param>
    public MeshBatchDrawer(GameObject go, Camera follower)
    {
        this.MeshLODs = MeshLOD.Extract(go);
        this.Follower = follower;
        this.LastFollowerPosition = follower.transform.position;

        this.InitializeLODInstances();
    }

    /// <summary>
    /// The camera object the LOD objects should be chosen based on. Something like <see cref="Camera.main"/>
    /// </summary>
    public Camera Follower
    {
        get { return this._Follower; }
        set { this._Follower = value; UpdateLODs(); }
    }
    private Camera _Follower;

    /// <summary>
    /// Gets or sets the material to override LOD materials. This is a helpful function if you want to change the default
    /// material of each LOD group to something specific.
    /// </summary>
    public Material MaterialOverride
    {
        get { return this._MaterialOverride; }
        set { this._MaterialOverride = value; }
    }
    private Material _MaterialOverride;

    /// <summary>
    /// Gets or sets the distance before the LOD batches are updated to reflect the followers viewing. The higher this value
    /// the less objects will be updated.
    /// </summary>
    public float FollowerDistanceToUpdate
    {
        get { return _FollowerDistanceToUpdate; }
        set { _FollowerDistanceToUpdate = value; UpdateLODs(); }
    }
    private float _FollowerDistanceToUpdate = 25f;

    /// <summary>
    /// Add a new position into the batch.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="scale"></param>
    public void Add(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        float distanceToFollower = Vector3.Distance(position, Follower.transform.position);
        int lodIndex = GetLODIndex(distanceToFollower);

        // Add to the appropriate LOD batch
        var batchList = InstancesLOD[lodIndex];
        var currentBatch = batchList[batchList.Count - 1];

        if (currentBatch.Count >= MAX_BATCH_SIZE)
        {
            batchList.Add(new List<Matrix4x4>()); // Start a new batch
            BatchBounds[lodIndex].Add(new Bounds(position, Vector3.one * 5f)); // Initialize bounds for the new batch
            currentBatch = batchList[batchList.Count - 1];
        }

        currentBatch.Add(Matrix4x4.TRS(position, rotation, scale));

        // Update the batch's bounding box
        Bounds bounds = BatchBounds[lodIndex][batchList.Count - 1];
        bounds.Encapsulate(position);
        BatchBounds[lodIndex][batchList.Count - 1] = bounds;
    }

    /// <summary>
    /// Update and render the mesh instances. This method should be called every update frmae.
    /// </summary>
    public void Update()
    {
        // Has the follower travelled far enough we should re-evaluate our positions?
        if (Vector3.Distance(Follower.transform.position, LastFollowerPosition) >= FollowerDistanceToUpdate)
        {
            this.UpdateLODs();
        }

        this.RenderInstances();
    }

    /// <summary>
    /// Initialize the collections of each LOD group. This could probably be deleted and put in the constructor. 
    /// </summary>
    private void InitializeLODInstances()
    {
        InstancesLOD = new List<List<Matrix4x4>>[MeshLODs.Count];
        BatchBounds = new List<Bounds>[MeshLODs.Count];

        for (int i = 0; i < InstancesLOD.Length; i++)
        {
            InstancesLOD[i] = new List<List<Matrix4x4>>();
            InstancesLOD[i].Add(new List<Matrix4x4>()); // Initialize first batch

            BatchBounds[i] = new List<Bounds>();
            BatchBounds[i].Add(new Bounds(Vector3.zero, Vector3.one * 5f)); // Initialize bounds for the first batch
        }
    }

    /// <summary>
    /// If the <see cref="Follower"/> has walked some distance away should we update the LOD index of each position.
    /// </summary>
    private void UpdateLODs()
    {
        Vector3 cameraPos = Follower.transform.position;

        for (int i = 0; i < InstancesLOD.Length; i++)
        {
            var batches = InstancesLOD[i];
            var boundsList = BatchBounds[i]; // Bounds for current LOD level

            for (int j = 0; j < batches.Count; j++)
            {
                if (batches[j].Count == 0) continue; // Skip empty batches

                Bounds batchBound = boundsList[j]; // Get batch bounds
                float distanceToCamera = Vector3.Distance(batchBound.center, cameraPos);
                int newLodIndex = GetLODIndex(distanceToCamera);

                if (newLodIndex != i) // LOD needs to change
                {
                    // Move entire batch to new LOD level
                    InstancesLOD[newLodIndex].Add(batches[j]);
                    BatchBounds[newLodIndex].Add(batchBound);

                    // Remove from the old LOD level
                    batches.RemoveAt(j);
                    boundsList.RemoveAt(j);
                    j--; // Adjust index after removal
                }
            }
        }
    }

    /// <summary>
    /// Render the instances of each active batch using <see cref="Graphics.DrawMeshInstanced(Mesh, int, Material, List{Matrix4x4})"/>. This method is a bit more
    /// efficent than native Unity rendering as we will automatically use a FrustumPlane to determine what should be rendered.
    /// </summary>
    private void RenderInstances()
    {
        if (Camera.main == null) return;

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        for (int i = 0; i < InstancesLOD.Length; i++)
        {
            var batches = InstancesLOD[i];
            var boundsList = BatchBounds[i];

            for (int j = 0; j < batches.Count; j++)
            {
                if (batches[j].Count == 0) continue;

                // Test the batch's bounding box against the camera's frustum
                if (GeometryUtility.TestPlanesAABB(frustumPlanes, boundsList[j]))
                {
                    Graphics.DrawMeshInstanced(MeshLODs[i].Mesh, 0, MaterialOverride ?? MeshLODs[i].Mat, batches[j]);
                }
            }
        }
    }

    /// <summary>
    /// Retrieve the LOD index based on the distance.
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    private int GetLODIndex(float distance)
    {
        int lodCount = MeshLODs.Count;
        if (lodCount == 1) return 0; // No LODs available

        float step = 100f / lodCount;
        for (int i = 0; i < lodCount; i++)
        {
            if (distance < step * (i + 1)) 
                return i;
        }

        return lodCount - 1; // Default to lowest LOD
    }
}