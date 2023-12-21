using BepInEx;
using ShadowUtilityLIB;
using ShadowUtilityLIB.logging;
using ShadowUtilityLIB.UI;
using UnityEngine;
using System.Collections;
using Logger = ShadowUtilityLIB.logging.Logger;
using HarmonyLib;
using KSP.Game.Science;
using KSP.Logging;
using Shapes;
using Newtonsoft.Json;
using static RTG.CameraFocus;

namespace TechTreeManager;
[BepInPlugin("com.shadowdev.techtreemanager", "Tech Tree Manager", "0.0.1")]
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
    public string TechTreeToUse {  get; set; }
}
public static class TTM
{
    public static string ModId = "com.shadowdev.techtreemanager";
    public static string ModName = "Tech Tree Manager";
    public static string ModVersion = "0.0.1";
    private static Logger logger = new Logger(ModName, ModVersion);
    public static TTMconfig config = new TTMconfig();
    public static Dictionary<string,string> TechTrees = new Dictionary<string,string>();

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
    
}

public static class TTMpatch
{
    private static Logger logger = new Logger(TTM.ModName, TTM.ModVersion);
    [HarmonyPatch(typeof(TechNodeDataStore))]
    [HarmonyPatch("OnTechNodeDataLoaded")]
    [HarmonyPrefix]
    public static bool TechNodeDataStore_OnTechNodeDataLoaded(TechNodeDataStore __instance,ref IList<TextAsset> data)
    {
        if (TTM.config.TechTreeToUse == "default") { } else
        {
            data = new List<TextAsset>();
            foreach (var thisFile in Directory.GetFiles($"./BepInEx/plugins/{TTM.config.TechTreeToUse}/assets/techtree"))
            {
                data.Add(new TextAsset(File.ReadAllText(thisFile)));
            };
        }
        return true;
    }
}