﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    //public Transform target;

    public float speed;
    public float turnSpeed;
    public float turnDistance;
    public float stoppingDistance;

    Path path;

    Task task;
    //TaskQueue taskQueue;

    private void Start() {
        //taskQueue = Script.Get<TaskQueue>();
    }


    public void BeginQueueing() {
        StartCoroutine(SearchForTask(Script.Get<TaskQueue>()));


    }

    public void DoTask() {
        StopCoroutine(SearchForTask(Script.Get<TaskQueue>()));

        PathRequestManager.RequestPath(transform.position, task.target.vector3, (waypoints, success) => {
            if(success) {
                path = new Path(waypoints, transform.position, turnDistance, stoppingDistance);
                StopCoroutine(FollowPath());
                StartCoroutine(FollowPath());
            }
        });
    }

    //public void beginPathFinding() {
    //    PathRequestManager.RequestPath(transform.position, target.position, (waypoints, success) => {
    //        if(success) {
    //            path = new Path(waypoints, transform.position, turnDistance, stoppingDistance);
    //            StopCoroutine(FollowPath());
    //            StartCoroutine(FollowPath());
    //        }
    //    });
    //}

    IEnumerator SearchForTask(TaskQueue taskQueue) {
        while (true) {
            if(taskQueue.Count() > 0) {
                task = taskQueue.Pop();
                DoTask();
            }

            // If nothing to queue
            yield return new WaitForSeconds(0.5f);
        }        
    }

    IEnumerator FollowPath() {

        bool followingPath = true;
        int pathIndex = 0;

        transform.LookAt(path.lookPoints[0].vector3);

        float speedPercent = 1;

        while(followingPath) {
            Vector2 position2D = transform.position.ToVector2();
            while(path.turnBoundaries[pathIndex].HasCrossedLine(position2D)) {
                if (pathIndex == path.finishLineIndex) {
                    followingPath = false;
                    BeginQueueing();
                    break;
                } else {
                    pathIndex++;
                }
            }

            if (followingPath) {
                if (pathIndex >= path.slowDownIndex && stoppingDistance > 0) {
                    speedPercent = Mathf.Clamp(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(position2D) / stoppingDistance, 0.15f, 1);
                }

                Vector3 lookPoint = path.lookPoints[pathIndex].vector3;
                lookPoint.y = transform.position.y;
                Quaternion targetRotation = Quaternion.LookRotation(lookPoint - transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
                transform.Translate(Vector3.forward * Time.deltaTime * speed * speedPercent, Space.Self);

                print("Following Path");
            }

            yield return null;
        }


            // Old


            //Vector3 currentWayPoint = path[0];

            //while(true) {
            //    if (transform.position == currentWayPoint) {
            //        targetIndex++;
            //        if (targetIndex >= path.Length) {
            //            yield break;
            //        }
            //        currentWayPoint = path[targetIndex];
            //    }

            //    transform.position = Vector3.MoveTowards(transform.position, currentWayPoint, speed * Time.deltaTime);
            //    yield return null;
            //}
        }

    public void SetSelected(bool selected) {

        Color tintColor = selected ? Color.cyan : Color.white;
        gameObject.GetComponent<MeshRenderer>().material.color = tintColor;
    }

    public void OnDrawGizmos() {
        if (path != null) {
            path.DrawWithGizmos();
        }
    }
}
