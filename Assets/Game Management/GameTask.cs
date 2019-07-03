﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTask {

    public enum ActionType { Build, Mine, PickUp, DropOff, FlattenPath };

    public string description;

    public WorldPosition target;
    private string targetDescription;
    public PathRequestTargetType pathRequestTargetType;

    // Target for Gather Action
    public GameResourceManager.GatherType gatherType = GameResourceManager.GatherType.Ore;

    public ActionType action;
    public ActionableItem actionItem;

    public MasterGameTask parentTask;

    public System.Func<bool> SatisfiesStartRequirements;

    public GameTask(string description, WorldPosition target, ActionType action, ActionableItem actionItem, PathRequestTargetType targetType = PathRequestTargetType.World) {
        this.target = target;
        Init(description, action, actionItem, targetType);
    }

    public GameTask(string description, GameResourceManager.GatherType gatherGoal, ActionType action, ActionableItem actionItem) {
        this.gatherType = gatherGoal;
        Init(description, action, actionItem, PathRequestTargetType.Unknown);
    }

    private void Init(string description, ActionType action, ActionableItem actionItem, PathRequestTargetType targetType = PathRequestTargetType.World) {
        this.pathRequestTargetType = targetType;
        this.action = action;
        this.description = description;
        this.actionItem = actionItem;

        MapCoordinate mapCoordinate = MapCoordinate.FromWorldPosition(target);
        LayoutCoordinate layoutCoordinate = new LayoutCoordinate(mapCoordinate);
        targetDescription = layoutCoordinate.description;
    }

    public GameTask Clone() {
        GameTask newTask = new GameTask(description, target, action, actionItem, pathRequestTargetType);

        return newTask;
    }
}

public class MasterGameTask {

    private static int gameTaskCounter = 0;

    public Unit assignedUnit;

    // Don't know if we need a type on the master...
    public enum ActionType { Mine, Build, Move};
    public ActionType actionType;

    public int taskNumber;

    public MasterGameTask blockerTask;
    public MasterGameTask taskBlockedByThis;

    public List<GameTask> childGameTasks;

    public string description;

    // If we have extra repeat counts, do not pop the item off the stack but instead make a clone of the task and give it to the unit
    public int repeatCount = 0;
    public List<MasterGameTask> childMasterTasks;
    MasterGameTask parentMasterTask;

    public MasterGameTask(ActionType actionType, string description, GameTask[] childTasks, MasterGameTask blocker = null) {

        taskNumber = gameTaskCounter;
        gameTaskCounter++;

        this.actionType = actionType;
        this.description = description;
        this.childGameTasks = new List<GameTask>();
        this.childMasterTasks = new List<MasterGameTask>();

        foreach(GameTask task in childTasks) {
            this.childGameTasks.Add(task);
            task.parentTask = this;
        }

        if (blocker != null) {
            blockerTask = blocker;
            blockerTask.taskBlockedByThis = this;
        }
    }

    private void UpdateAllGameTasksActionItemsWith(MasterGameTask masterGameTask) {
        foreach(GameTask gameTask in childGameTasks) {
            if (gameTask.actionItem != null) {
                gameTask.actionItem.UpdateMasterTaskByGameTask(gameTask, masterGameTask);
            }            
        }
    }

    public void CancelTask() {
        if(assignedUnit != null) {
            assignedUnit.CancelTask();
        } else {            
            Script.Get<TaskQueueManager>().DeQueueTask(this);
        }

        // Remove references to this gameTask
        UpdateAllGameTasksActionItemsWith(null);


        // TODO?
        childGameTasks.Clear();
        
        //if(taskBlockedByThis != null) {
        //    taskBlockedByThis.UnblockTask(this);
        //    taskBlockedByThis = null;
        //}
    }

    public void MarkTaskFinished() {
        if (taskBlockedByThis != null) {
            taskBlockedByThis.UnblockTask(this);
            taskBlockedByThis = null;
        }

        if (parentMasterTask != null) {
            parentMasterTask.MarkChildFinished(this);
        }

        // Complete all children GameTask's ActionItems
        UpdateAllGameTasksActionItemsWith(null);
    }

    public void MarkChildFinished(MasterGameTask childMasterTask) {
        childMasterTasks.Remove(childMasterTask);

        if (childMasterTasks.Count == 0 && repeatCount == 0) {
            MarkTaskFinished();
        }
    }

    public MasterGameTask CloneTask() {
        if (repeatCount <= 0) {
            return null;
        }

        List<GameTask> newGameTasks = new List<GameTask>();
        foreach (GameTask childGameTask in childGameTasks) {
            newGameTasks.Add(childGameTask.Clone());
        }

        MasterGameTask newMasterTask = new MasterGameTask(actionType, description, newGameTasks.ToArray());
        newMasterTask.parentMasterTask = this;

        childMasterTasks.Add(newMasterTask);
        repeatCount -= 1;

        return newMasterTask;
    }

    public void UnblockTask(MasterGameTask blockerTask) {
        if (this.blockerTask == blockerTask) {
            this.blockerTask = null;
        }        
    }

    public bool SatisfiesStartRequirements() {
        if (blockerTask != null) {
            return false;
        }

        foreach (GameTask task in childGameTasks) {
            if (task.SatisfiesStartRequirements == null) {
                continue;
            }

            if (!task.SatisfiesStartRequirements()) {
                return false;
            }
        }

        return true;
    }
}
