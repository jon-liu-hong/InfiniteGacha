using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LootRarity : MonoBehaviour
{
    [SerializeField] private GameObject lootRarityBox;
    [SerializeField] private TMP_Text textLabel;
    private TypeWriterEffect typewriterEffect;

    public void ShowRarity(string rarity){
        lootRarityBox.SetActive(true);
        typewriterEffect = GameObject.Find("Canvas").GetComponent<TypeWriterEffect>();
        typewriterEffect.Run(rarity, textLabel);  
    }

    public void CloseRarityBox(){
        lootRarityBox.SetActive(false);
        textLabel.text=string.Empty;
    }
}
