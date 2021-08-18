using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LootNameText : MonoBehaviour
{
    [SerializeField] private GameObject LootNameBox;
    [SerializeField] private TMP_Text textLabel;
    private TypeWriterEffect typewriterEffect;

    public void ShowLootName(string name){
    	LootNameBox.SetActive(true);
        typewriterEffect = GameObject.Find("Canvas").GetComponent<TypeWriterEffect>();
        typewriterEffect.Run(name, textLabel);  
    }
	public void CloseLootNameBox(){
        LootNameBox.SetActive(false);
        textLabel.text=string.Empty;
    }
}
