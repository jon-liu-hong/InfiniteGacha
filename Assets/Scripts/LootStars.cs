using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootStars : MonoBehaviour
{
    [SerializeField] private GameObject star1;
    [SerializeField] private GameObject star2;
    [SerializeField] private GameObject star3;
    [SerializeField] private GameObject star4;
    [SerializeField] private GameObject star5;
    private List<GameObject> starList = new List<GameObject>();
    [SerializeField] private int frameDelay = 5;

    public void Run(){
    	starList.Add(star1);
    	starList.Add(star2);
    	starList.Add(star3);
    	starList.Add(star4);
    	starList.Add(star5);
    }

    public void ShowRarityStars(int lootRarity){
    	StartCoroutine(displayStars(lootRarity));
    }

    IEnumerator displayStars(int lootRarity){
    	for (int i = 0; i < lootRarity+1; i++){
    		starList[i].SetActive(true);
    		//yield return 0;
    		yield return new WaitForFrames(frameDelay);
    	}
    }

    public void CloseRarityStarsBox(){
        for (int i = 0; i < starList.Count; i++) starList[i].SetActive(false);
    }
}


