﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Diagnostics;

public class PathFinding : MonoBehaviour {
    //public Transform seeker, target;

    PathfindingGrid grid;

    private void Awake() {
        grid = GetComponent<PathfindingGrid>();
    }

    //private void Update() {
    //    if (Input.GetButtonDown("Jump")) {
    //        FindPath(seeker.position, target.position);
    //    }
    //}

    public void FindSimplifiedPath(Vector3 startPos, Vector3 targetPos, Action<WorldPosition[], bool> callback) {
        StartCoroutine(FindPath(startPos, targetPos, (path, success) => {
            callback(SimplifyPath(path), success);
        }));
    }

    IEnumerator FindPath(Vector3 startPos, Vector3 targetPos, Action<Node[], bool> callback){

        Stopwatch sw = new Stopwatch();
        sw.Start();

        Node startNode = grid.nodeFromWorldPoint(new WorldPosition(startPos));
        Node targetNode = grid.nodeFromWorldPoint(new WorldPosition(targetPos));

        Heap<Node> openSet = new Heap<Node>(grid.maxSize);
        HashSet<Node> closedSet = new HashSet<Node>();

        Node[] finalPath = new Node[0];
        bool success = false;

        // We need to reset the gCost and hCost, as well as the parent for this node to be fresh
        startNode.ResetPathfindingReferences();
        targetNode.ResetPathfindingReferences();

        openSet.Add(startNode);

        //Dictionary<(int, int), Node> nodeDictionary = new Dictionary<(int, int), Node>();

        while(openSet.Count > 0) {

            // Get node with lowest fcost, or if fCosts equal, lowest hCost
            Node currentNode = openSet.PopFirstItem();
            closedSet.Add(currentNode);
            
            if (currentNode == targetNode) {
                sw.Stop();
                print("Path Took " + sw.ElapsedMilliseconds);

                finalPath = RetracePath(targetNode).ToArray();
                success = true;
                break;
            }
            
            foreach(Node neighbour in grid.GetNeighbours(currentNode)) {
                if (!neighbour.walkable || closedSet.Contains(neighbour)) {
                    continue;
                }

                int newMovementCostToNeighbour = currentNode.gCost + getDistance(currentNode, neighbour) + neighbour.movementPenalty;
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = getDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if(!openSet.Contains(neighbour)) {
                        openSet.Add(neighbour);

                        //(int, int) key = (neighbour.gridX, neighbour.gridY);
                        //if (nodeDictionary.ContainsKey(key)) {
                        //    print("What the fuck");
                        //} else {
                        //    nodeDictionary.Add(key, neighbour);
                        //}

                    } else {
                        openSet.UpdateItem(neighbour);
                    }
                }
            }
        }

        callback(finalPath, success);
        yield return null;
    }

    List<Node> RetracePath(Node fromNode) {
        List<Node> path = new List<Node>();
        Node currentNode = fromNode;
        
        while (currentNode != null) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        return path;
    }

    WorldPosition[] SimplifyPath(Node[] path) {
        List<WorldPosition> waypoints = new List<WorldPosition>();
        Vector2 oldDirection = Vector2.zero;

        GameObject mapMesh = Tag.Map.GetGameObject();
        Map map = mapMesh.GetComponent<MapContainer>().getMap();

        int lastAddedIndex = 0;

        for (int i = 1; i < path.Length; i++) {
            Vector2 newDirection = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
            if(newDirection != oldDirection) {

                if (lastAddedIndex != i - 1) {
                    WorldPosition oldPathPosition = path[i - 1].worldPosition;
                    oldPathPosition.recalculateHeight();

                    waypoints.Add(oldPathPosition);
                }                

                WorldPosition pathPosition = path[i].worldPosition;
                pathPosition.recalculateHeight();

                waypoints.Add(pathPosition);

                lastAddedIndex = i;
                oldDirection = newDirection;
            }
        }

        WorldPosition finalPathPosition = path[path.Length - 1].worldPosition;
        finalPathPosition.recalculateHeight();

        waypoints.Add(finalPathPosition);

        return waypoints.ToArray();
    }

    int getDistance(Node nodeA, Node nodeB) {
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        
        if (distX > distY) {
            return 14 * distY + 10 * (distX - distY);
        }

        return 14 * distX + 10 * (distY - distX);
    }

}
