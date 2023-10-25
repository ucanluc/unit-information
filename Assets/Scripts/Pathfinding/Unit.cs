using System.Collections;
using System.Collections.Generic;
using MapMono;
using UnityEngine;

namespace Pathfinding
{
    public class Unit : MonoBehaviour
    {
        const float minPathUpdateTime = .2f;
        const float pathUpdateMoveThreshold = 0.5f;

        public Rigidbody rb;
        public Transform target;
        public float speed = 5f;
        public float turnSpeed = 3f;
        public float turnDst = 5f;
        public float stoppingDst = 10f;
        public int orderFollowIndex = 0;
        public List<Vector2Int> currentOrder = new List<Vector2Int>();
        public MapHandler MapHandler;
        public MovingVirtual enemy;
        public MovingVirtual SelfVirtual;


        Path path;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            StartCoroutine(UpdatePath());
            target = new GameObject("OrderTarget").transform;
            target.position = transform.position;
            MapHandler = GameObject.Find("Map").GetComponent<MapHandler>();
            SelfVirtual = GetComponent<MovingVirtual>();
            if (!SelfVirtual.friendly)
            {
                target.position = MapHandler.getRandPos();
            }
        
        }

        private void OnPathFound(Vector3[] waypoints, bool pathSuccessful)
        {
            if (pathSuccessful)
            {
                path = new Path(waypoints, transform.position, turnDst, stoppingDst);
                StopCoroutine(nameof(FollowPath));
                StartCoroutine(nameof(FollowPath));
            }
        }

        public void OnDrawGizmos()
        {
            if (path != null)
            {
                path.DrawWithGizmos();
            }
        }


        IEnumerator UpdatePath()
        {
            if (Time.timeSinceLevelLoad < .3f)
            {
                yield return new WaitForSeconds(.3f);
            }

            PathRequestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));

            float sqrMoveThreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;
            Vector3 targetPosOld = target.position;
            while (true)
            {
                yield return new WaitForSeconds(minPathUpdateTime);
                if (target.position != targetPosOld)
                {
                    PathRequestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));
                    targetPosOld = target.position;
                }
            }
        }

        private IEnumerator FollowPath()
        {
            bool followingPath = true;
            int pathIndex = 0;
            transform.LookAt(path.lookPoints[0]);

            float speedPercent = 1;

            while (followingPath)
            {
                Vector2 pos2D = new Vector2(transform.position.x, transform.position.z);
                while (path.turnBoundaries[pathIndex].HasCrossedLine(pos2D))
                {
                    if (pathIndex == path.finishLineIndex)
                    {
                        followingPath = false;

                        break;
                    }
                    else
                    {
                        pathIndex++;
                    }
                }

                if (followingPath)
                {
                    if (pathIndex >= path.slowDownIndex && stoppingDst > 0)
                    {
                        speedPercent =
                            Mathf.Clamp01(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(pos2D) /
                                          stoppingDst);
                        if (speedPercent < 0.01f)
                        {
                            followingPath = false;
                        }
                    }

                    var dir = path.lookPoints[pathIndex] - transform.position;
                    Quaternion targetRotation =
                        Quaternion.LookRotation(path.lookPoints[pathIndex] - transform.position);
                    // clamp target rotation to the y axis
                    var euler = targetRotation.eulerAngles;
                    euler.x = 0;
                    euler.z = 0;
                    targetRotation = Quaternion.Euler(euler);
                    // transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
                    transform.rotation = targetRotation;
                    rb.velocity = (dir.normalized) * speed * speedPercent;
                    // transform.Translate(Vector3.forward , Space.Self);
                }

                yield return null;
            }

            if (orderFollowIndex < currentOrder.Count - 1)
            {
                orderFollowIndex++;
                target.position =
                    MapHandler.GetWorldPosition(currentOrder[orderFollowIndex].x, currentOrder[orderFollowIndex].y);
            }
        }

        private void CheckNextOrder()
        {
        }

        public void UpdateOrders(List<Vector2Int> unitOrder)
        {
            if (unitOrder.Count == 0)
            {
                target.position = transform.position;
                enemy = null;
                return;
            }

            for (var index = 0; index < unitOrder.Count; index++)
            {
                var vec2 = unitOrder[index];
                if (index >= currentOrder.Count)
                {
                    orderFollowIndex = index;
                    break;
                }

                var currentVec2 = currentOrder[index];
                if (vec2 != currentVec2)
                {
                    orderFollowIndex = index;
                    break;
                }
            }

            var nextOrder = unitOrder[orderFollowIndex];
            target.position = MapHandler.GetWorldPosition(nextOrder.x, nextOrder.y);
            enemy = null;
        }

        public void SetOrders(List<Vector2Int> order, MovingVirtual enemy, bool selfFriendly)
        {
            currentOrder = order;
            orderFollowIndex = 0;
            var nextOrder = order[orderFollowIndex];
            target.position = MapHandler.GetWorldPosition(nextOrder.x, nextOrder.y);
            this.enemy = enemy;
        }
    }
}