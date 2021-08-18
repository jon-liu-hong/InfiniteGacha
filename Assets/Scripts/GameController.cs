using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.EventSystems;
using System.Linq;
using TMPro;

#if UNITY_WEBGL
using System.Runtime.InteropServices;
#elif UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Game Controller.
/// </summary>
/// <remarks>
/// Calls the settings loader. Spawns new chests. Returns new loot. Generates tiered materials.
/// Updates GUI, Tooltip and Tier Counters.
/// </remarks>
public class GameController : MonoBehaviour
{
#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void HelloString(string str);
#endif
    private GraphicRaycaster canvasRaycaster;
    private EventSystem canvasEventSystem;
    public RectTransform tooltipRect;
    private Text tooltipText;
    public Text[] counterText;
    private List<int> tierCounter;
    public static GameController instance;
    public static Settings Settings;
    public Inventory inventory;
    public GameObject chestPrefab;
    private GameObject activeChest;
    public AudioSource audioSrc0;
    public AudioSource audioSrc1;
    public AudioMixer audioMixer;
    private List<GameObject> mLootObjects;
    private List<List<Material>> mLootObjectsMaterials;
    //common-white, uncommon-green, rare-yellow, epic-purple, legendary-orange
    public List<Color> tierColors = new List<Color>();
    private List<GameObject> commonLoot = new List<GameObject>();
    private List<GameObject> uncommonLoot = new List<GameObject>();
    private List<GameObject> rareLoot = new List<GameObject>();
    private List<GameObject> epicLoot = new List<GameObject>();
    private List<GameObject> legendaryLoot = new List<GameObject>();
    public List<string> tierNames = new List<string>();
    private Material auraMat;
    public Material AuraMat
    {
        get
        {
            return auraMat;
        }
    }
    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
        tierColors.Add(new Color(1, 1, 1, 1));
        tierColors.Add(new Color(0, 1, 0, 1));
        tierColors.Add(new Color(1, 1, 0, 1));
        tierColors.Add(new Color(1, 0, 1, 1));
        tierColors.Add(new Color(1, 0.5f, 0, 1));
        tierNames.Add("Common");
        tierNames.Add("Uncommon");
        tierNames.Add("Rare");
        tierNames.Add("Epic");
        tierNames.Add("Legendary");
        tierCounter = Enumerable.Repeat<int>(0, 5).ToList();
        auraMat = new Material(Shader.Find("Mobile/Particles/Additive"));
    }
    public Dictionary<string, Sprite> imgDict; //Inventory icons accessed by prefab name.
    IEnumerator Start()
    {
        var canv = GameObject.Find("Canvas");
        canvasRaycaster = canv.GetComponent<GraphicRaycaster>();
        canvasEventSystem = canv.GetComponent<EventSystem>();
        tooltipText = tooltipRect.GetChild(0).GetComponent<Text>();
        Settings = new Settings();
        yield return Settings.init("Settings.ini");

        mLootObjects = Resources.LoadAll("LootObjectsActive").OfType<GameObject>().ToList();
        // foreach (var item in mLootObjects) Debug.Log(item);
        new System.Random().Shuffle(mLootObjects);
        int idx = Settings.Variety;
        if (idx > mLootObjects.Count) idx = mLootObjects.Count;
        mLootObjects.RemoveRange(idx, mLootObjects.Count - idx); //leave only Variety number of possible objects
        mLootObjectsMaterials = new List<List<Material>>();
        for (int i = 0; i < mLootObjects.Count; i++) mLootObjectsMaterials.Add(null);

        for (int i = 0; i < mLootObjects.Count; i++)                                                // Loop through loot object list, check rarity of loot and add loot to its corresponding rarity list.
            if (mLootObjects[i].GetComponent<LootData>().rarity == "Common"){
                commonLoot.Add(mLootObjects[i]);
            }
            else if (mLootObjects[i].GetComponent<LootData>().rarity == "Uncommon"){
                uncommonLoot.Add(mLootObjects[i]);
            }
            else if (mLootObjects[i].GetComponent<LootData>().rarity == "Rare"){
                rareLoot.Add(mLootObjects[i]);
            }
            else if (mLootObjects[i].GetComponent<LootData>().rarity == "Epic"){
                epicLoot.Add(mLootObjects[i]);
            }
            else{
                legendaryLoot.Add(mLootObjects[i]);
            }

        // Shuffle each rarity list.
        new System.Random().Shuffle(commonLoot);
        new System.Random().Shuffle(uncommonLoot);
        new System.Random().Shuffle(rareLoot);
        new System.Random().Shuffle(epicLoot);
        new System.Random().Shuffle(legendaryLoot);

        imgDict = new Dictionary<string, Sprite>(mLootObjects.Count);
        var imgs = Resources.LoadAll("InventoryIcons").OfType<Sprite>().ToList();
        foreach (var s in imgs) imgDict.Add(s.name, s); //maybe should add a check if the object is present in mLootObjects after trimming

        var audios = GetComponents<AudioSource>();
        audioSrc0 = audios[0];
        audioSrc1 = audios[1];
        audioMixer.SetFloat("ReverbRoomVol", Settings.AudioReverbVol);
        audioMixer.SetFloat("ReverbDecayTime", Settings.AudioReverbDecay);
        audioMixer.SetFloat("FlangeWet", Settings.AudioFlangeMix);
        audioMixer.SetFloat("FlangeDry", 1 - Settings.AudioFlangeMix);
        
        closeLootData();
        spawnNewChest();
        setCounterTexts();
        randomBool();
        Shader.WarmupAllShaders();
    }

    private int mLootCounter = 0;
    private int mRigCounter = 0;
    private List<int> spawnedLoot = new List<int>();                                               // Create empty list to record all loot spawned via their id.
    
    private bool rarityText;                                                                        // Set loot rarity display, either text (eg. "common") or stars (eg. 1 star). 
    private bool dialogue;                                                                         // Set loot dialogue/description text on/off.

    /// <summary>
    /// Generate random settings for rarity text and dialogue for player.
    /// </summary>
    public void randomBool(){
        rarityText = (Random.Range(0, 2) == 0);
        dialogue = (Random.Range(0, 2) == 0);
    }

    /// <summary>
    /// Returns next loot.
    /// </summary>
    /// <remarks>
    /// Returns a random item from LootObjectsActive folder with random tier.
    /// </remarks>
    public GameObject getNextLoot() 
    {
        //bool isRigged = false;                                                                   // Check if rigged. Rigged items follow a set rarity spawn order eg. 10 common, 2 rare, 1 common, 1 epic, etc.
        //if (!isRigged)                                                                           // Non-rigged, items spawned are assigned a random rarity. Calcalating loot rarity based on weighting.
        //{
        var tierRnd = Random.value;                                                                // Random float between 0.0~1.0, draw a random item rarity
        //var tierRnd = 0.69;                                                                      // Test cases: 0.69 - common, 0.89 - uncommon, 0.94 -rare, 0.97 - epic, 0.99 - legendary
        for (int i = 0; i < Settings.Probabilities.Count; i++)                                     // Get settings.ini rarity total probability weighting, total being 5.
            if (tierRnd < Settings.Probabilities[i])                                               // Iterate and compare random number to current weight.
            {
                if (i <= 0){
                    getLootData(commonLoot,0,rarityText,dialogue);
                }
                else if (i <= 1){
                    getLootData(uncommonLoot,1,rarityText,dialogue);
                }
                else if (i <= 2){
                    getLootData(rareLoot,2,rarityText,dialogue);
                }
                else if (i <= 3){
                    getLootData(epicLoot,3,rarityText,dialogue);   
                }
                else{
                    getLootData(legendaryLoot,4,rarityText,dialogue);
                }
                break;
            }
        //}
        /// <remarks>
        /// Redundant functionality. Maybe in the future we would reconfigure it to work with the other loot system.
        /// </remarks>
        //if (Settings.RiggedItems.Length > mRigCounter)                                             // Rigged item list
        //{
            //var idx = Random.Range(0, mLootObjects.Count);                                         // Get random number from 0 to max number of loot items in lootboxobjectsactive
            //var lootPrefab = mLootObjects[idx];                                                    // Use random index found to get the prefab of loot from random index 
            //var go = Instantiate(lootPrefab);                                                      // Instantiate loot prefab
            //var loot3d = go.GetComponent<Loot3D>();                                                // Assign returned component of loot3D from prefab 
            //if (!loot3d) Debug.Log("Loot3D component is not present in the prefab." + go.name);    //Debug

            //var rigItem = Settings.RiggedItems[mRigCounter];
            //if (rigItem.number == mLootCounter)
            //{
                //loot3d.lootTier = rigItem.tier;
                //mRigCounter++;
                //isRigged = true;
            //}

            //tierCounter[loot3d.lootTier]++;                                                        // Increase counter for no. of times player draws from lootbox
            //setCounterTexts(); 
            //loot3d.iconName = lootPrefab.name;
            //go.name = tierNames[loot3d.lootTier] + " " + loot3d.Name;

            //if (mLootObjectsMaterials[idx] == null)                                              // Disabled as uneeded for 2D loot.
                //mLootObjectsMaterials[idx] = generateMaterialTiers(lootPrefab.GetComponent<Renderer>().sharedMaterial);
            //go.GetComponent<Renderer>().material = mLootObjectsMaterials[idx][loot3d.lootTier];
            //mLootCounter++;
            //return go;
        //}
        var goEmpty = new GameObject();                                                            // Return empty go instance to stop the program from complaining.
        return goEmpty;
    }

    /// <summary>
    /// Returns a item from the set item rarity that has been sorted from the LootObjectsActive folder with the passed tier.
    /// </summary>
    private GameObject spawnLoot(List<GameObject> lootList, int tier){
        var idx = Random.Range(0,lootList.Count);
        var lootPrefab = lootList[idx];                                                            // Use random index found to get the prefab of loot from random index 
        var go = Instantiate(lootPrefab);                                                          // Instantiate loot prefab
        var loot3d = go.GetComponent<Loot3D>();                                                    // Assign returned component of loot3D from prefab 
        if (!loot3d) Debug.Log("Loot3D component is not present in the prefab." + go.name);        //Debug

        tierCounter[tier]++;                                                                       // Increase counter for no. of times player draws from lootbox
        setCounterTexts(); 
        loot3d.iconName = lootPrefab.name;
        go.name = tierNames[tier] + " " + go.GetComponent<LootData>().name;
        spawnedLoot.Add(go.GetComponent<LootData>().id);
        mLootCounter++;

        return go;     
    }

    /// <summary>
    /// Displays loot data: name, dialogue/description and rarity.
    /// </summary>
    private void getLootData(List<GameObject> lootObjList, int rarity, bool rarityText, bool dialogue){
        var go = spawnLoot(lootObjList,rarity);
        setLootName(go.GetComponent<LootData>().name);
        if (dialogue){
            setDialogue(go.GetComponent<LootData>().dialogue);
        }
        if (rarityText){
            //setRarityText(go.GetComponent<LootData>().rarity);
            setRarityStars(rarity);
        }
        //else{
            //setRarityStars(rarity);
        //}
    }

    /// <summary>
    /// Set loot display information off before first/after pull.
    /// </summary>
    private void closeLootData(){
        closeLootName();  
        closeDialogue();
        closeRarity();
        closeRarityStars();
    }

    /// <summary>
    /// Take input loot data and display it in the UI.
    /// </summary>
    private void setLootName(string lootName){
        GameObject canvas = GameObject.Find("Canvas");
        LootNameText lootNameText = canvas.GetComponent<LootNameText>();
        lootNameText.ShowLootName(lootName);
    }

    /// <summary>
    /// Close loot data display object.
    /// </summary>
    private void closeLootName(){
        GameObject canvas = GameObject.Find("Canvas");
        LootNameText lootNameText = canvas.GetComponent<LootNameText>();
        lootNameText.CloseLootNameBox();
    }

    /// <summary>
    /// Take input loot data and display it in the UI.
    /// </summary>
    private void setDialogue(string lootDialogue){
        GameObject canvas = GameObject.Find("Canvas");
        DialogueUI dialogue = canvas.GetComponent<DialogueUI>();
        dialogue.ShowDialogue(lootDialogue);
    }

    /// <summary>
    /// Close loot data display object.
    /// </summary>
    private void closeDialogue(){
        GameObject canvas = GameObject.Find("Canvas");
        DialogueUI dialogue = canvas.GetComponent<DialogueUI>();
        dialogue.CloseDialogueBox();
    }

    /// <summary>
    /// Take input loot data and display it in the UI.
    /// </summary>
    private void setRarityText(string lootRarity){
        GameObject canvas = GameObject.Find("Canvas");
        LootRarity rarity = canvas.GetComponent<LootRarity>();
        rarity.ShowRarity(lootRarity);
    }

    /// <summary>
    /// Close loot data display object.
    /// </summary>
    private void closeRarity(){
        GameObject canvas = GameObject.Find("Canvas");
        LootRarity rarity = canvas.GetComponent<LootRarity>();
        rarity.CloseRarityBox();
    }

    /// <summary>
    /// Take input loot data and display it in the UI.
    /// </summary>
    private void setRarityStars(int lootRarity){
        GameObject canvas = GameObject.Find("Canvas");
        LootStars rarityStars = canvas.GetComponent<LootStars>();
        rarityStars.Run();
        rarityStars.ShowRarityStars(lootRarity);
    }

    /// <summary>
    /// Close loot data display object.
    /// </summary>
    private void closeRarityStars(){
        GameObject canvas = GameObject.Find("Canvas");
        LootStars rarityStars = canvas.GetComponent<LootStars>();
        rarityStars.Run();
        rarityStars.CloseRarityStarsBox();
    }

    private int mHoverCounter = 0;
    void Update()
    {
        //update tooltip
        Loot2D hitLoot = null;
        var rcObj = RaycastScreenPos(Input.mousePosition);
        if (rcObj) hitLoot = rcObj.GetComponent<Loot2D>();
        if (hitLoot)
        {
            if (!tooltipRect.gameObject.activeSelf) mHoverCounter++;
            tooltipRect.gameObject.SetActive(true);
            tooltipText.text = hitLoot.lootName;
            tooltipText.color = tierColors[hitLoot.lootTier];
            tooltipRect.position = Input.mousePosition;
            return;
        }
        CounterLabel label = null;
        if (rcObj) label = rcObj.GetComponent<CounterLabel>();
        if (label)
        {
            var txt = label.GetComponent<Text>();
            tooltipRect.gameObject.SetActive(true);
            tooltipText.text = label.gameObject.name;
            tooltipText.color = txt.color;
            tooltipRect.position = Input.mousePosition - new Vector3(0, tooltipRect.rect.height, 0);
            return;
        }
        else
        {
            tooltipRect.gameObject.SetActive(false);
        }
    }
    /// <summary>
    /// Updates the counters on top of the inventory panel.
    /// </summary>
    void setCounterTexts()
    {
        counterText[0].text = "C:" + tierCounter[0];
        counterText[1].text = "U:" + tierCounter[1];
        counterText[2].text = "R:" + tierCounter[2];
        counterText[3].text = "E:" + tierCounter[3];
        counterText[4].text = "L:" + tierCounter[4];
        counterText[5].text = "T:" + tierCounter.Sum();
    }
    
    /// <summary>
    /// Wrapper for the coroutine chest spawn call.
    /// </summary>
    public void spawnNewChest(float delay = 0)
    {
        StartCoroutine(spawnNewChestAfter(delay));
    }
    public IEnumerator spawnNewChestAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        closeLootData();
        if (GameObject.Find ("New Game Object") != null){
            var goEmpty = GameObject.Find("New Game Object");
            Destroy(goEmpty.gameObject); // Kill empty game object for performance.
        } 
        activeChest = Instantiate(chestPrefab, new Vector3(-100, -100, -100), Quaternion.identity);
    }
    List<RaycastResult> results = new List<RaycastResult>();

    GameObject RaycastScreenPos(Vector2 sp)
    {
        results.Clear();
        var pointerEventData = new PointerEventData(canvasEventSystem);
        pointerEventData.position = sp;
        canvasRaycaster.Raycast(pointerEventData, results);
        if (results.Count() > 0)
            return results[0].gameObject;
        return null;
    }
    
    /// <summary>
    /// Saves data to clipboard if in Editor, 
    /// or calls DataAlert.jslib and shows window.prompt with the Save data if in WebGL.
    /// </summary>
    public void saveDataToClipboard()
    {
        var spawnedLootStr = string.Join(";", spawnedLoot.Select(item => item.ToString()).ToArray());
        var gameStr = mLootCounter + "&" + Time.time.ToString("0.000") + "&" + mHoverCounter + "&" + inventory.SortCounter + "&" + rarityText + "&" + dialogue + "&" + spawnedLootStr;
        #if UNITY_WEBGL
                HelloString(gameStr);
        #elif UNITY_EDITOR
                    EditorGUIUtility.systemCopyBuffer = gameStr; 
        #endif
    }
    /// <summary>
    /// Generate and return 5 tiered versions of the input material.
    /// </summary>
    /// <remarks>
    /// Redundant functionality, unneeded for 2D loot.
    /// </remarks>
    //private List<Material> generateMaterialTiers(Material mat)
    //{
        //var ret = new List<Material>();
        //var propName = "_Color";

        //for (int i = 0; i < //tierColors.Count; i++)
        //{
            //ret.Add(new Material(mat));
            //ret.Last().SetColor(propName, //tierColors[i]);
        //}

        //return ret;
    //}

}