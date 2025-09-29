using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Avoider : MonoBehaviour
{

    private NavMeshAgent agent = null;
    [SerializeField] private GameObject avoid = null;

    private RaycastHit Hit;

    public float speed = 1f;
    public float radius = 20f;
    public float size_x = 10f;
    public float size_y = 10f;
    public float size_z = 10f;


    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        avoid = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        Debug.DrawLine(transform.position, avoid.transform.position);

        if(Physics.Linecast(transform.position, avoid.gameObject.transform.position)!= true)
        {
            var sampler = new PoissonDiscSampler(size_x, size_y, size_z, radius);
            foreach (var point in sampler.Samples())
            {
                if(Physics.Linecast(point, avoid.gameObject.transform.position) != true)
                {
                    
                    agent.destination = point;
                }

                
            }
        }
        
    }
}

public class PoissonDiscSampler
{
    private const int k = 30;  // Maximum number of attempts before marking a sample as inactive.

    private readonly Rect rect;
    private readonly float radius2;  // radius squared
    private readonly float cellSize;
    private Vector3[,,] grid;
    private List<Vector3> activeSamples = new List<Vector3>();

    /// Create a sampler with the following parameters:
    ///
    /// width:  each sample's x coordinate will be between [0, width]
    /// height: each sample's y coordinate will be between [0, height]
    /// radius: each sample will be at least `radius` units away from any other sample, and at most 2 * `radius`.
    public PoissonDiscSampler(float width, float height, float depth, float radius)
    {
        rect = new Rect(0, 0, width, height);
        radius2 = radius * radius;
        cellSize = radius / Mathf.Sqrt(2);
        grid = new Vector3[Mathf.CeilToInt(width / cellSize),
                           Mathf.CeilToInt(height / cellSize),
                           Mathf.CeilToInt(depth / cellSize)];
    }

    /// Return a lazy sequence of samples. You typically want to call this in a foreach loop, like so:
    ///   foreach (Vector2 sample in sampler.Samples()) { ... }
    public IEnumerable<Vector3> Samples()
    {
        // First sample is choosen randomly
        yield return AddSample(new Vector2(Random.value * rect.width, Random.value * rect.height));

        while (activeSamples.Count > 0)
        {

            // Pick a random active sample
            int i = (int)Random.value * activeSamples.Count;
            Vector3 sample = activeSamples[i];

            // Try `k` random candidates between [radius, 2 * radius] from that sample.
            bool found = false;
            for (int j = 0; j < k; ++j)
            {

                float angle = 2 * Mathf.PI * Random.value;
                float r = Mathf.Sqrt(Random.value * 3 * radius2 + radius2); // See: http://stackoverflow.com/questions/9048095/create-random-number-within-an-annulus/9048443#9048443
                Vector3 candidate = sample + r * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));

                // Accept candidates if it's inside the rect and farther than 2 * radius to any existing sample.
                if (rect.Contains(candidate) && IsFarEnough(candidate))
                {
                    found = true;
                    yield return AddSample(candidate);
                    break;
                }
            }

            // If we couldn't find a valid candidate after k attempts, remove this sample from the active samples queue
            if (!found)
            {
                activeSamples[i] = activeSamples[activeSamples.Count - 1];
                activeSamples.RemoveAt(activeSamples.Count - 1);
            }
        }
    }

    private bool IsFarEnough(Vector3 sample)
    {
        GridPos pos = new GridPos(sample, cellSize);

        int xmin = Mathf.Max(pos.x - 2, 0);
        int ymin = Mathf.Max(pos.y - 2, 0);
        int zmin = Mathf.Max(pos.z - 2, 0);
        int xmax = Mathf.Min(pos.x + 2, grid.GetLength(0) - 1);
        int ymax = Mathf.Min(pos.y + 2, grid.GetLength(1) - 1);
        int zmax = Mathf.Min(pos.z + 2, grid.GetLength(2) - 1);

        for (int y = ymin; y <= ymax; y++)
        {
            for (int x = xmin; x <= xmax; x++)
            {
                for (int z = zmin; z <= zmax; z++)
                {
                    Vector3 s = grid[x, y, z];
                    if (s != Vector3.zero)
                    {
                        Vector3 d = s - sample;
                        if (d.x * d.x + d.y * d.y + d.z * d.z < radius2) return false;
                    }
                }
            }
        }

        return true;
    }

    /// Adds the sample to the active samples queue and the grid before returning it
    private Vector3 AddSample(Vector3 sample)
    {
        activeSamples.Add(sample);
        GridPos pos = new GridPos(sample, cellSize);
        grid[pos.x, pos.y, pos.z] = sample;
        return sample;
    }

    /// Helper struct to calculate the x and y indices of a sample in the grid
    private struct GridPos
    {
        public int x;
        public int y;
        public int z;

        public GridPos(Vector3 sample, float cellSize)
        {
            x = (int)(sample.x / cellSize);
            y = (int)(sample.y / cellSize);
            z = (int)(sample.z / cellSize);
        }
    }

}
