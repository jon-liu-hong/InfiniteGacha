using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TMP_Text textLabel;
    private TypeWriterEffect typewriterEffect;

    public void ShowDialogue(string dialogue){
        dialogueBox.SetActive(true);
        typewriterEffect = GameObject.Find("Canvas").GetComponent<TypeWriterEffect>();
        typewriterEffect.Run(dialogue, textLabel);  
    }

    public void CloseDialogueBox(){
        dialogueBox.SetActive(false);
        textLabel.text=string.Empty;
    }
}
