﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipThrusters : Building {
    public override string title => "Thrusters";
    protected override float constructionModifierSpeed => 0.20f;

    protected override void Awake() {
        base.Awake();
        ResearchSingleton.sharedInstance.AddBuildingCount(this);
    }

    public override void Destroy() {
        base.Destroy();
        ResearchSingleton.sharedInstance.RemoveBuildingCount(this);
    }

    protected override void UpdateCompletionPercent(float percent) {

    }

    protected override void CompleteBuilding() {
        ResearchSingleton.sharedInstance.unitSpeedMultiplier = 1.5f;

        Script.Get<NotificationPanel>().AddNotification(new NotificationItem("Ship Thrusters Complete! Units move 50% faster!", transform));
    }
}
