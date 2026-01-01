using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public bool stackable = true;
}
