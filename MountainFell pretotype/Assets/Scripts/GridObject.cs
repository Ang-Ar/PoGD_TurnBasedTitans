using System.Collections;
using System.Collections.Generic;
using TheoryTeam.PolymorphicGrid;
using UnityEngine;

public class GridObject : MonoBehaviour
{
    [SerializeField]
    protected GameObject body;

    // really reconsider how these two are gonna be used, maybe dropping one altogether
    [HideInInspector]
    public Node homeNode;
    [HideInInspector]
    public Node travelingNode;

    protected bool suppressHome = false;
    protected bool suppresstravelling = false;

    [SerializeField]
    protected GridMaster grid;

    [SerializeField]
    protected Material selectedMaterial;
    protected Material defaultMaterial;

    protected bool selected = false;
    public bool Selected 
    { 
        get => selected; 
        set {
            selected = value;
            body.GetComponent<MeshRenderer>().material = selected ? selectedMaterial : defaultMaterial;
        }
    }

    public float moveSpeed;
    //[HideInInspector]
    public bool moving = false;

    private void Awake()
    {
        defaultMaterial = body.GetComponent<MeshRenderer>().material;
    }

    private void Start()
    {
        homeNode = grid.GetNode(transform.position);
        travelingNode = grid.GetNode(transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        Node physicalNode = grid.GetNode(transform.position);
        homeNode = suppressHome ? homeNode : physicalNode;
        travelingNode = suppresstravelling ? travelingNode : physicalNode;
        //transform.position = CurrentNode.WorldPosition;
    }

    public void QueuePath(IEnumerable<Node> path)
    {
        //Debug.Log("queue attmepted, moving: " + moving);
        // Title is a lie, really only accepts one path at a time
        if (! moving)
            StartCoroutine(MoveAlongPath(path));
    }

    public int movementCounter = 0;
    public IEnumerator MoveAlongPath(IEnumerable<Node> path)
    {
        //movementCounter++;
        //Debug.Log(">>> move " + movementCounter );
        moving = true;
        suppresstravelling = true;
        suppressHome = true;

        foreach (Node nextNode in path)
        {
            bool nextReached = false;
            while (! nextReached)
            {
                Vector3 targetDelta = nextNode.WorldPosition - transform.position;
                if (targetDelta.magnitude <= moveSpeed * Time.deltaTime)
                {
                    //Debug.Log(name + "Arrived at node" + nextNode.Index);
                    travelingNode = nextNode;
                    nextReached = true;
                }
                else
                {
                    targetDelta = targetDelta.normalized * (moveSpeed * Time.deltaTime);
                }
                transform.position += targetDelta;
                yield return null;
            }
        }

        suppresstravelling = false;
        suppressHome = false;
        moving = false;
        //Debug.Log("<<< move " + movementCounter);
        //movementCounter--;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(travelingNode.WorldPosition, 0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(homeNode.WorldPosition, 0.2f);
    }
}
