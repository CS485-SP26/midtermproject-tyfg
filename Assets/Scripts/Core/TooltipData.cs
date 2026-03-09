using Core;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[System.Serializable]
public class StatEntry
{
    public string statName;
    public float statValue;
    public string unit;  // optional, e.g. "dmg", "%", "m/s"
}
[System.Serializable]
public class TooltipData
{
    public string title;
    public string description;
    public Sprite icon;
    public Color rarityColor = Color.white;
    public List<StatEntry> stats;   // e.g. "Damage: 50"
}