using BepInEx;
using ShadowUtilityLIB;
using ShadowUtilityLIB.logging;
using ShadowUtilityLIB.UI;
using UnityEngine;
using Logger = ShadowUtilityLIB.logging.Logger;
using HarmonyLib;
using KSP.Game.Science;
using Newtonsoft.Json;
using I2.Loc;
using UnityEngine.UI;
using System.Security.Cryptography.Xml;
using UnityEngine.UIElements;
using KSP.Game;
using KSP.Sim.DeltaV;

namespace TechTreeManager;
[BepInPlugin("com.shadowdev.techtreemanager", "Tech Tree Manager", "0.0.2")]
[BepInDependency(ShadowUtilityLIBMod.ModId, ShadowUtilityLIBMod.ModVersion)]
public class TechTreeManagerMod : BaseUnityPlugin
{
    public static string ModId = TTM.ModId;
    public static string ModName = TTM.ModName;
    public static string ModVersion = TTM.ModVersion;

    private Logger logger = new Logger(ModName, ModVersion);//logger logger.log("stuff here")  logger.debug("only run with IsDev=true")  logger.error("error log")
    public static Manager manager;//ui manager

    void Start()
    {
        TTM.Start();
    }
}
public class TTMconfig
{
    public string TechTreeToUse { get; set; } = "default";
    public bool AllowUpdates { get; set; } = true;
}
public static class TTM
{
    public static string ModId = "com.shadowdev.techtreemanager";
    public static string ModName = "Tech Tree Manager";
    public static string ModVersion = "0.0.4";
    private static Logger logger = new Logger(ModName, ModVersion);
    public static TTMconfig config = new TTMconfig();
    public static Dictionary<string,string> TechTrees = new Dictionary<string,string>();

    public static Scrollbar VertScrol;
    public static RDCenterUIController rDCenterUIController;
    public static void Start()
    {
        ShadowUtilityLIBMod.EnableDebugMode();
        try
        {
            Harmony.CreateAndPatchAll(typeof(TTMpatch));
        }
        catch (Exception e)
        {
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
        }
        try
        {
            if (Directory.Exists("./Config")) { } else
            {
                Directory.CreateDirectory("./Config");
            }
            if(File.Exists("./Config/TTM.config")){
                config = JsonConvert.DeserializeObject<TTMconfig>(File.ReadAllText("./Config/TTM.config"));
            }
            else
            {
                config.TechTreeToUse = "default";
                File.WriteAllText("./Config/TTM.config", JsonConvert.SerializeObject(config));
            }
            
        }
        catch (Exception e)
        {
            config.TechTreeToUse = "default";
            logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");
            
        }
        foreach (var thisModDir in Directory.GetDirectories($"./BepInEx/plugins/"))
        {
            try
            {
                if (Directory.Exists($"{thisModDir}/assets/techtree"))
                {
                    TechTrees.Add(thisModDir.Split("/", StringSplitOptions.None).Last(), $"{thisModDir}/assets/techtree");
                }
            }
            catch (Exception e)
            {
                logger.Error($"{e}\n{e.Message}\n{e.InnerException}\n{e.Source}\n{e.Data}\n{e.HelpLink}\n{e.HResult}\n{e.StackTrace}\n{e.TargetSite}\n{e.GetBaseException()}");

            }
        }
    }
    public static void OnScrollVertical(float value)
    {
        if (value < 0f)
        {
            value = 0f;
        }
        float num = -((rDCenterUIController._treeContainerHorizontalSize * (float)(rDCenterUIController._tiers - 1) * value) + 3000f);
        float num2 = num * rDCenterUIController._parallaxScale;
        rDCenterUIController._treeContainer.anchoredPosition = new Vector2(rDCenterUIController._treeContainer.anchoredPosition.x, 0f - num);
    }
    
}

public static class TTMpatch
{
    public static string[] RemoveAtID(string[] input, int index)
    {
        string[] result = new string[input.Length - 1];
        for (int i = 0; i < input.Length; i++)
        {
            if (i < index)
            {
                result[i] = input[i];
            }
            else
            {
                result[i] = input[i + 1];
            }
        }
        return result;
    }

    private static Logger logger = new Logger(TTM.ModName, TTM.ModVersion);
    [HarmonyPatch(typeof(RDCenterUIController))]
    [HarmonyPatch("InitializeTechTreeEnviroment")]
    [HarmonyPostfix]
    public static void RDCenterUIController_InitializeTechTreeEnviroment(RDCenterUIController __instance)
    {
       
        GameObject Se = GameObject.Instantiate(GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/RDCenterUI(Clone)/Container/TechTree/ELE-Scrollbar-Horizontal"), GameObject.Find("GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/RDCenterUI(Clone)/Container/TechTree").transform);
        TTM.VertScrol = Se.GetComponent<Scrollbar>();
        TTM.VertScrol.onValueChanged.RemoveAllListeners();
        TTM.VertScrol.onValueChanged.AddListener(TTM.OnScrollVertical);
        __instance._treeContainer.sizeDelta = new Vector2(__instance._treeContainerHorizontalSize * (float)__instance._tiers, __instance._treeContainerHorizontalSize * (float)__instance._tiers);
        Se.transform.rotation = Quaternion.Euler(Se.transform.eulerAngles.x, Se.transform.eulerAngles.y, 270f);
        Se.transform.localPosition = new Vector3(0,450,0);
        Se.transform.localScale = new Vector3(0.6f, 1, 1);
        TTM.rDCenterUIController = __instance;
        __instance._treeContainer.anchoredPosition = new Vector2(__instance._treeContainer.anchoredPosition.x, 3000f);

    }
    [HarmonyPatch(typeof(TechNodeDataStore))]
    [HarmonyPatch("OnTechNodeDataLoaded")]
    [HarmonyPrefix]
    public static bool TechNodeDataStore_OnTechNodeDataLoaded(TechNodeDataStore __instance,ref IList<TextAsset> data)
    {
        if (TTM.config.TechTreeToUse == "default") { } else
        {
            data = new List<TextAsset>();
            LanguageSourceData languageSourceData = null;
            var info = new DirectoryInfo($"./BepInEx/plugins/{TTM.config.TechTreeToUse}/assets/techtree");
            foreach (var csvFile in info.GetFiles("*.csv"))
            {
                languageSourceData = new LanguageSourceData();
                var csvData = File.ReadAllText(csvFile.FullName).Replace("\r\n", "\n");
                languageSourceData.Import_CSV("", csvData, eSpreadsheetUpdateMode.AddNewTerms);
                LocalizationManager.AddSource(languageSourceData);
            }
            
            foreach (var thisFile in Directory.GetFiles($"./BepInEx/plugins/{TTM.config.TechTreeToUse}/assets/techtree","*.json"))
            {
                data.Add(new TextAsset(File.ReadAllText(thisFile)));
            };
        }
        return true;
    }
    [HarmonyPatch(typeof(TechNodeDataStore))]
    [HarmonyPatch("OnTechNodeDataLoaded")]
    [HarmonyPostfix]
    public static void TechNodeDataStore_OnTechNodeDataLoaded_PF(TechNodeDataStore __instance, ref IList<TextAsset> data)
    {
        if (TTM.config.AllowUpdates)
        {
            __instance._techNodesLoaded = false;
            foreach (var thisPluginDir in Directory.GetDirectories($"./BepInEx/plugins"))
            {
                if (Directory.Exists($"{thisPluginDir}/assets/techtree"))
                {
                    foreach (var thisFile in Directory.GetFiles($"{thisPluginDir}/assets/techtree", "*.nodeupdate"))
                    {
                        TechNodeData techNodeData = JsonUtility.FromJson<TechNodeData>(File.ReadAllText(thisFile));
                        if (__instance._availableData.ContainsKey(techNodeData.ID))
                        {
                            __instance._availableData[techNodeData.ID].IconID = techNodeData.IconID;
                            __instance._availableData[techNodeData.ID].NameLocKey = techNodeData.NameLocKey;
                            __instance._availableData[techNodeData.ID].CategoryID = techNodeData.CategoryID;
                            __instance._availableData[techNodeData.ID].HiddenByNodeID = techNodeData.HiddenByNodeID;
                            __instance._availableData[techNodeData.ID].DescriptionLocKey = techNodeData.DescriptionLocKey;
                            __instance._availableData[techNodeData.ID].RequiredSciencePoints = techNodeData.RequiredSciencePoints;

                            foreach (var PartID in techNodeData.UnlockedPartsIDs)
                            {
                                if (!__instance._availableData[techNodeData.ID].UnlockedPartsIDs.Contains(PartID))
                                {
                                    __instance._availableData[techNodeData.ID].UnlockedPartsIDs.AddItem(PartID);
                                }
                            }
                            foreach (var PartID in __instance._availableData[techNodeData.ID].UnlockedPartsIDs)
                            {
                                if (!techNodeData.UnlockedPartsIDs.Contains(PartID))
                                {
                                    __instance._availableData[techNodeData.ID].UnlockedPartsIDs = RemoveAtID(__instance._availableData[techNodeData.ID].UnlockedPartsIDs, __instance._availableData[techNodeData.ID].UnlockedPartsIDs.IndexOf(PartID));
                                }
                            }

                            foreach (var PartID in techNodeData.RequiredTechNodeIDs)
                            {
                                if (!__instance._availableData[techNodeData.ID].RequiredTechNodeIDs.Contains(PartID))
                                {
                                    __instance._availableData[techNodeData.ID].RequiredTechNodeIDs.AddItem(PartID);
                                }
                            }
                            foreach (var PartID in __instance._availableData[techNodeData.ID].RequiredTechNodeIDs)
                            {
                                if (!techNodeData.RequiredTechNodeIDs.Contains(PartID))
                                {
                                    __instance._availableData[techNodeData.ID].RequiredTechNodeIDs = RemoveAtID(__instance._availableData[techNodeData.ID].RequiredTechNodeIDs, __instance._availableData[techNodeData.ID].RequiredTechNodeIDs.IndexOf(PartID));
                                }
                            }

                            __instance._availableData[techNodeData.ID].TierToUnlock = techNodeData.TierToUnlock;
                            __instance._availableData[techNodeData.ID].TechTreePosition = techNodeData.TechTreePosition;
                        }
                    };
                    foreach (var thisFile in Directory.GetFiles($"./BepInEx/plugins/{thisPluginDir}/assets/techtree", "*.nodeadd"))
                    {
                        TechNodeData techNodeData = JsonUtility.FromJson<TechNodeData>(File.ReadAllText(thisFile));
                        if (!__instance._availableData.ContainsKey(techNodeData.ID))
                        {
                            __instance._availableData.Add(techNodeData.ID, techNodeData);
                        }
                    };
                    foreach (var thisFile in Directory.GetFiles($"./BepInEx/plugins/{thisPluginDir}/assets/techtree", "*.nodereduce"))
                    {
                        TechNodeData techNodeData = JsonUtility.FromJson<TechNodeData>(File.ReadAllText(thisFile));
                        if (__instance._availableData.ContainsKey(techNodeData.ID))
                        {
                            foreach (var PartID in techNodeData.UnlockedPartsIDs)
                            {
                                if (!__instance._availableData[techNodeData.ID].UnlockedPartsIDs.Contains(PartID))
                                {
                                    __instance._availableData[techNodeData.ID].UnlockedPartsIDs = RemoveAtID(__instance._availableData[techNodeData.ID].UnlockedPartsIDs, __instance._availableData[techNodeData.ID].UnlockedPartsIDs.IndexOf(PartID));
                                }
                            }

                            foreach (var PartID in techNodeData.RequiredTechNodeIDs)
                            {
                                if (!__instance._availableData[techNodeData.ID].RequiredTechNodeIDs.Contains(PartID))
                                {
                                    __instance._availableData[techNodeData.ID].RequiredTechNodeIDs = RemoveAtID(__instance._availableData[techNodeData.ID].RequiredTechNodeIDs, __instance._availableData[techNodeData.ID].RequiredTechNodeIDs.IndexOf(PartID));
                                }
                            }
                        }
                        else
                        {
                            __instance._availableData.Add(techNodeData.ID, techNodeData);
                        }
                    };
                }
            };
            __instance.CacheTechNodeRelationshipData();
        }
        
    }
}