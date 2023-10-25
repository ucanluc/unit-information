using System.Collections;
using System.Collections.Generic;
using Knowledge;
using MapMono;
using UnityEngine;

public class MovingVirtual : MonoBehaviour
{
    public bool player;
    public bool friendly;
    public MapHandler mapHandler;
    public int uid;
    public Vector2Int gridPosition;

    public List<GameObject> visibleElements = new List<GameObject>();

    private bool isVisible = true;

    public UnitKnowledge selfKnowledge;
    
    public Animator animator;
    public Rigidbody rb;
    
    private void Awake()
    {
        uid = gameObject.GetInstanceID();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        uid = gameObject.GetInstanceID();
        gridPosition = mapHandler.GetGlobalPosAsGridPos(transform.position);
        mapHandler.mapDataReal.UpdateUnitPosition(uid, gridPosition, this);
        if (!player)
        {
            DisableVisibility();
        }
    }

    // Update is called once per frame
    void Update()
    {
        var newGridPosition = mapHandler.GetGlobalPosAsGridPos(transform.position);
        if (newGridPosition != gridPosition)
        {
            mapHandler.mapDataReal.UpdateUnitPosition(uid, newGridPosition, this);
            gridPosition = newGridPosition;
        }

        if (isVisible && !player)
        {
            var velocity = rb.velocity;

            // project velocity onto the forward vector
            var forward = transform.forward;
            var forwardVelocity = Vector3.Project(rb.velocity, forward).magnitude;
            var sidewaysVelocity = Vector3.Project(rb.velocity, transform.right).magnitude;
            animator.SetFloat("InputX", sidewaysVelocity);
            // get sideways velocity
            
            animator.SetFloat("InputY", forwardVelocity);
        }
    }

    public void DisableVisibility()
    {
        if (player)
        {
            return;
        }

        foreach (var element in visibleElements)
        {
            element.SetActive(false);
        }
        isVisible = false;
    }

    public void EnableVisibility()
    {
        if (player)
        {
            return;
        }

        foreach (var element in visibleElements)
        {
            element.SetActive(true);
        }
        isVisible = true;
    }
}