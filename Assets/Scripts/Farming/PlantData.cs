using UnityEngine;

[System.Serializable]
public class PlantData
{
    public string plantName;
    public int sellValue;
    public int quantity;

    public PlantData(string name, int value, int qty = 1)
    {
        plantName = name;
        sellValue = value;
        quantity = qty;
    }
}