﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedBuilder : Unit {
    public override int duration => 570;
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Build;

    protected override void UnitCustomInit() {

    }

    public override float SpeedForTask(MasterGameTask.ActionType actionType) {
        switch(actionType) {
            case MasterGameTask.ActionType.Mine:
                return 0.3f;
            case MasterGameTask.ActionType.Build:
                return 2f;
            case MasterGameTask.ActionType.Move:
                return 1f;
        }

        return 0.1f;
    }

    protected override void AnimateState(AnimationState state, float rate = 1.0f) {
        //throw new System.NotImplementedException();
    }
}


