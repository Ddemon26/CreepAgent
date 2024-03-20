using ScriptableArchitect.Variables;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Controls the movement of a creep agent towards the player or targets, avoiding obstacles using raycasts.
/// Adjusted to consider an additional unblocker layer that negates a blocker if hit first.
/// </summary>
public class CreepAgentController : MonoBehaviour
{
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Transform player;
    [SerializeField] Transform[] targets;
    [SerializeField] Transform raycastTarget;
    [SerializeField] float offsetToPlayer = 5.0f;
    [SerializeField] LayerMask blockTargetLayerMask;
    [SerializeField] LayerMask unblockLayerMask;
    [SerializeField] BoolReference isMoving;

    [System.Serializable]
    public class DirectEvent
    {
        public UnityEvent onAgentStopped;
        public UnityEvent onAgentStarted;
    }
    [SerializeField] DirectEvent directEvent = new DirectEvent();

    private void Update()
    {
        MoveAgentBasedOnRaycastClearance();
    }
    /// <summary>
    /// Moves the agent towards the player if all raycasts to targets are clear, considering both blocker and unblocker layers.
    /// </summary>
    private void MoveAgentBasedOnRaycastClearance()
    {
        if (AreAllRaycastsClear())
        {
            MoveTowardsPlayerWithOffset();
            isMoving.SetRefValue(true);
            directEvent.onAgentStarted.Invoke();
        }
        else
        {
            StopAgentMovement();
            isMoving.SetRefValue(false);
            directEvent.onAgentStopped.Invoke();
        }
    }
    /// <summary>
    /// Checks if raycasts from all targets to the raycastTarget are clear of obstacles, considering both blocker and unblocker layers.
    /// </summary>
    /// <returns>True if all raycasts are clear, false otherwise.</returns>
    private bool AreAllRaycastsClear()
    {
        foreach (Transform target in targets)
        {
            if (IsRaycastBlocked(target))
            {
                return false;
            }
        }
        return true;
    }
    /// <summary>
    /// Determines if a raycast from a given target to the raycastTarget is blocked by an obstacle, considering both blocker and unblocker layers.
    /// </summary>
    /// <param name="target">The starting point of the raycast.</param>
    /// <returns>True if the raycast is blocked by the blocker layer without being unblocked, false if clear or unblocked.</returns>
    private bool IsRaycastBlocked(Transform target)
    {
        Vector3 direction = raycastTarget.position - target.position;
        if (Physics.Raycast(target.position, direction.normalized, out RaycastHit hit, direction.magnitude, blockTargetLayerMask | unblockLayerMask))
        {
            // Check if the hit object is part of the unblockLayerMask
            if ((unblockLayerMask.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                // Hit object is part of the unblockLayerMask, so consider the path clear
                return false;
            }
            // Hit object is part of the blockTargetLayerMask, consider the path blocked
            return true;
        }
        // No hit, consider the path clear
        return false;
    }
    /// <summary>
    /// Moves the agent towards the player's position with a specified offset.
    /// </summary>
    private void MoveTowardsPlayerWithOffset()
    {
        Vector3 offsetPosition = player.position - (player.position - transform.position).normalized * offsetToPlayer;
        agent.isStopped = false;
        agent.SetDestination(offsetPosition);
    }
    /// <summary>
    /// Stops the agent's movement.
    /// </summary>
    private void StopAgentMovement()
    {
        agent.ResetPath();
        agent.isStopped = true;
    }


    private void OnDrawGizmos()
    {
        if (targets == null || raycastTarget == null) return;

        foreach (Transform target in targets)
        {
            DrawRaycastGizmo(target);
        }
    }
    /// <summary>
    /// Draws a Gizmo line for the raycast from target to raycastTarget, considering both blocker and unblocker layers.
    /// </summary>
    /// <param name="target">The starting point of the raycast.</param>
    private void DrawRaycastGizmo(Transform target)
    {
        Vector3 direction = raycastTarget.position - target.position;
        bool hitSomething = Physics.Raycast(target.position, direction.normalized, 
            out RaycastHit hit, direction.magnitude, blockTargetLayerMask | unblockLayerMask);

        bool isUnblocked = hitSomething && ((unblockLayerMask.value & (1 << hit.collider.gameObject.layer)) != 0);
        Gizmos.color = isUnblocked ? Color.blue : (hitSomething ? Color.red : Color.green);
        Gizmos.DrawLine(target.position, target.position + direction.normalized * direction.magnitude);
    }
}