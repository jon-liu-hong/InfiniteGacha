using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="LootProp",menuName="LootProp")]
public class LootProp : ScriptableObject
{
    public string rarity;
    public string dialogue;
    public float weighting;
    public GameObject spriteModel;
}
