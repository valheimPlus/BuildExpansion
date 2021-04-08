using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace VPlusBuildExpansion
{
    [BepInPlugin(ID, "Valheim Plus Build Expansion", version)]
    public class BuildExpansion : BaseUnityPlugin
    {
        public const string ID = "mixone.valheimplus.buildexpansion";
        public const string version = "0.0.1.0";

        public static ConfigEntry<int> newGridHeight;
        public static ConfigEntry<int> newGridWidth;
        public static ConfigEntry<bool> disableScrollCategories;
        public static ConfigEntry<bool> isEnabled;

        public Harmony harmony;

        public static BepInEx.Logging.ManualLogSource buildFilterLogger;

        public void Awake()
        {
            // Config setup
            newGridHeight = Config.Bind("General", "GridHeight",  10, "Height in number of rows of the build grid."); 
            newGridWidth = Config.Bind("General","GridWidth",10,"Width in number of columns of the build grid, maximum value of 10.");
            disableScrollCategories = Config.Bind("General.Toggles", "DisableScrollCategories", true,"Should the mousewheel stop scrolling categories, RECOMMEND TRUE.");
            isEnabled = Config.Bind("General.Toggles","EnableExpansion",true,"Whether or not to expand the build grid.");

            harmony = new Harmony(ID);
            harmony.PatchAll();
            buildFilterLogger = Logger;

            buildFilterLogger.LogDebug("Build Expansion loaded.");
        }
    }

    #region Transpilers

    #region Hud

    [HarmonyPatch(typeof(Hud), nameof(Hud.GetSelectedGrid))]
    public static class Hud_GetSelectedGrid_Transpiler
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (BuildExpansion.isEnabled.Value)
            {
                var codes = new List<CodeInstruction>(instructions);
                codes[0].operand = BuildExpansion.newGridWidth.Value;
                codes[2].opcode = OpCodes.Ldc_I4_S;
                codes[2].operand = BuildExpansion.newGridHeight.Value;
                return codes;
            }
            return instructions;
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdatePieceList))]
    public static class Hud_UpdatePieceList_Transpiler
    {
        public static bool haveReanchored = false;
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (BuildExpansion.isEnabled.Value)
            {
                var codes = new List<CodeInstruction>(instructions);
                codes[3].operand = BuildExpansion.newGridWidth.Value;
                codes[5].opcode = OpCodes.Ldc_I4_S;
                codes[5].operand = BuildExpansion.newGridHeight.Value;
                return codes;
            }
            return instructions;
        }

        public static void Postfix(ref Hud __instance)
        {
            if (BuildExpansion.isEnabled.Value)
            {
                if (!haveReanchored)
                {
                    foreach (Transform pieceTrans in __instance.m_pieceListRoot.transform)
                    {
                        (pieceTrans as RectTransform).anchoredPosition = (pieceTrans as RectTransform).anchoredPosition +
                            new Vector2(
                                (-1 * (int)((__instance.m_pieceIconSpacing * BuildExpansion.newGridWidth.Value) / 2)),
                                ((int)((__instance.m_pieceIconSpacing * BuildExpansion.newGridHeight.Value) / 2))-16);
                    }
                    haveReanchored = true;
                }
            }
        }
    }

    #endregion

    #endregion

    #region Patches 

    #region Hud

    [HarmonyPatch(typeof(Hud), "Awake")]
    public static class Hud_Awake_Patch
    {
        public static void Prefix(ref Hud __instance)
        {
            if (BuildExpansion.isEnabled.Value)
            {
                DefaultControls.Resources uiRes = new DefaultControls.Resources();
                uiRes.standard = __instance.m_pieceCategoryRoot.transform.parent.GetChild(0).gameObject.GetComponent<Image>().sprite;
                Scrollbar myScroll = DefaultControls.CreateScrollbar(uiRes).GetComponent<Scrollbar>();
                myScroll.GetComponent<RectTransform>().anchorMin = new Vector2(1f, 0f);
                myScroll.GetComponent<RectTransform>().anchorMax = new Vector2(1f, 1f);
                myScroll.GetComponent<RectTransform>().pivot = new Vector2(1f, 1f);
                myScroll.GetComponent<RectTransform>().localPosition = new Vector2(0, 0);
                myScroll.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 0);
                myScroll.direction = Scrollbar.Direction.BottomToTop;
                myScroll.gameObject.GetComponent<Image>().color = new Color32(0, 0, 0, 150);
                myScroll.gameObject.transform.SetParent(__instance.m_pieceListRoot.transform.parent, false);
                ScrollRect testScroll = __instance.m_pieceListRoot.transform.parent.gameObject.AddComponent<ScrollRect>();
                testScroll.content = __instance.m_pieceListRoot;
                testScroll.viewport = __instance.m_pieceListRoot.transform.parent.gameObject.GetComponent<RectTransform>();
                testScroll.verticalScrollbar = myScroll;
                testScroll.movementType = ScrollRect.MovementType.Clamped;
                testScroll.inertia = false;
                testScroll.scrollSensitivity = __instance.m_pieceIconSpacing;
                testScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                __instance.m_pieceListRoot.sizeDelta = new Vector2((int)(__instance.m_pieceIconSpacing * BuildExpansion.newGridWidth.Value), (int)(__instance.m_pieceIconSpacing * BuildExpansion.newGridHeight.Value)+16);
            }
        }
    }    

    #endregion

    #region PieceTable

    [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.PrevCategory))]
    public static class PieceTable_PrevCategory
    {
        public static bool Prefix(ref PieceTable __instance)
        {
            if (BuildExpansion.isEnabled.Value && BuildExpansion.disableScrollCategories.Value)
            {
                if (Input.GetAxis("Mouse ScrollWheel") != 0f)
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.NextCategory))]
    public static class PieceTable_NextCategory
    {
        public static bool Prefix(ref PieceTable __instance)
        {
            if (BuildExpansion.isEnabled.Value && BuildExpansion.disableScrollCategories.Value)
            {
                if (Input.GetAxis("Mouse ScrollWheel") != 0f)
                {
                    return false;
                }
            }
            return true;
        }
    }

    #endregion
    #endregion
}
