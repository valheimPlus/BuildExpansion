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
        public const string version = "1.0.5.0";

        public static ConfigEntry<int> maxGridHeight;
        public static ConfigEntry<int> newGridWidth;
        public static ConfigEntry<bool> disableScrollCategories;
        public static ConfigEntry<bool> isEnabled;
        //public static ConfigEntry<bool> autoExpand;        

        public Harmony harmony;
        
        public static BepInEx.Logging.ManualLogSource buildFilterLogger;

        public void Awake()
        {
            maxGridHeight = Config.Bind("General.Constants", "MaxGridHeight", 15, "Sets a maximum value for grid height, if over 30 can impact performance.");
            newGridWidth = Config.Bind("General", "GridWidth", 10, "Width in number of columns of the build grid, maximum value of 10.");
            disableScrollCategories = Config.Bind("General.Toggles", "DisableScrollCategories", true, "Should the mousewheel stop scrolling categories, RECOMMEND TRUE.");
            isEnabled = Config.Bind("General.Toggles", "EnableExpansion", true, "Whether or not to expand the build grid.");
            //autoExpand = Config.Bind("General.Toggles", "AutoExpand", true, "Alpha feature to auto expand based on number of pieces.");
            if (newGridWidth.Value > 10)
                newGridWidth.Value = 10;
            harmony = new Harmony(ID);
            harmony.PatchAll();
            buildFilterLogger = Logger;

            buildFilterLogger.LogDebug("Build Expansion loaded.");
        }
    }

    #region Transpilers

    #region PieceCategory

    #endregion

    #region Hud
    public static class HudTranspiles
    {
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
                    codes[2].operand = BuildExpansion.maxGridHeight.Value;
                    return codes;
                }
                return instructions;
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdatePieceList))]
        public static class Hud_UpdatePieceList_Transpiler
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                if (BuildExpansion.isEnabled.Value)
                {
                    var codes = new List<CodeInstruction>(instructions);
                    codes[3].operand = BuildExpansion.newGridWidth.Value;
                    codes[5].opcode = OpCodes.Ldc_I4_S;
                    codes[5].operand = BuildExpansion.maxGridHeight.Value;
                    return codes;
                }
                return instructions;
            }
        }
    }

    #endregion

    #endregion

    #region Patches 

    #region Hud
    public static class HudPatches
    {
        public static Scrollbar myScroll;
        public static int calculatedRows = 1;

        [HarmonyPatch(typeof(Hud), "Awake")]
        public static class Hud_Awake_Patch
        {
            public static void Prefix(ref Hud __instance)
            {
                if (BuildExpansion.isEnabled.Value)
                {
                    DefaultControls.Resources uiRes = new DefaultControls.Resources();
                    uiRes.standard = __instance.m_pieceCategoryRoot.transform.parent.GetChild(0).gameObject.GetComponent<Image>().sprite;
                    myScroll = DefaultControls.CreateScrollbar(uiRes).GetComponent<Scrollbar>();
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
                    testScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                    __instance.m_pieceListRoot.sizeDelta = new Vector2((int)(__instance.m_pieceIconSpacing * BuildExpansion.newGridWidth.Value), (int)(__instance.m_pieceIconSpacing * BuildExpansion.maxGridHeight.Value) + 16);
                }
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdatePieceList))]
        public static class Hud_UpdatePieceList_Patch
        {
            public static bool Prefix(ref Hud __instance, ref Player player, ref Vector2Int selectedNr, 
                                      ref Piece.PieceCategory category, ref bool updateAllBuildStatuses)
            {
                if (BuildExpansion.isEnabled.Value)
                {
                    List<Piece> buildPieces = player.GetBuildPieces();                    
                    int columns = BuildExpansion.newGridWidth.Value;
                    calculatedRows = (buildPieces.Count / columns) + 1;
                    __instance.m_pieceListRoot.sizeDelta = new Vector2((int)(__instance.m_pieceIconSpacing * BuildExpansion.newGridWidth.Value), (int)(__instance.m_pieceIconSpacing * calculatedRows) + 16);
                    if (buildPieces.Count <= 1)
                    {
                        calculatedRows = 1;
                        columns = 1;
                    }
                    if (__instance.m_pieceIcons.Count != calculatedRows * columns)
                    {
                        foreach (Hud.PieceIconData pieceIconData in __instance.m_pieceIcons)
                        {
                            UnityEngine.Object.Destroy(pieceIconData.m_go);
                        }
                        __instance.m_pieceIcons.Clear();
                        for (int yaxis = 0; yaxis < calculatedRows; yaxis++)
                        {
                            for (int xaxis = 0; xaxis < columns; xaxis++)
                            {
                                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(__instance.m_pieceIconPrefab, __instance.m_pieceListRoot);
                                (gameObject.transform as RectTransform).anchorMin = new Vector2(0, 1);
                                (gameObject.transform as RectTransform).anchorMax = new Vector2(0, 1);
                                (gameObject.transform as RectTransform).pivot = new Vector2(0, 1);
                                (gameObject.transform as RectTransform).anchoredPosition = Vector2.zero;
                                (gameObject.transform as RectTransform).localPosition = new Vector2(xaxis * __instance.m_pieceIconSpacing, -16 + (-yaxis) * __instance.m_pieceIconSpacing);
                                Hud.PieceIconData templatePieceData = new Hud.PieceIconData();
                                templatePieceData.m_go = gameObject;
                                templatePieceData.m_tooltip = gameObject.GetComponent<UITooltip>();
                                templatePieceData.m_icon = gameObject.transform.Find("icon").GetComponent<Image>();
                                templatePieceData.m_marker = gameObject.transform.Find("selected").gameObject;
                                templatePieceData.m_upgrade = gameObject.transform.Find("upgrade").gameObject;
                                templatePieceData.m_icon.color = new Color(1f, 0f, 1f, 0f);
                                UIInputHandler templateHandler = gameObject.GetComponent<UIInputHandler>();
                                templateHandler.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(
                                    templateHandler.m_onLeftDown, new Action<UIInputHandler>(__instance.OnLeftClickPiece));
                                templateHandler.m_onRightDown = (Action<UIInputHandler>)Delegate.Combine(
                                    templateHandler.m_onRightDown, new Action<UIInputHandler>(__instance.OnRightClickPiece));
                                templateHandler.m_onPointerEnter = (Action<UIInputHandler>)Delegate.Combine(
                                    templateHandler.m_onPointerEnter, new Action<UIInputHandler>(__instance.OnHoverPiece));
                                templateHandler.m_onPointerExit = (Action<UIInputHandler>)Delegate.Combine(
                                    templateHandler.m_onPointerExit, new Action<UIInputHandler>(__instance.OnHoverPieceExit));
                                __instance.m_pieceIcons.Add(templatePieceData);
                            }
                        }
                    }
                    for (int yaxis = 0; yaxis < columns; yaxis++)
                    {
                        for (int xaxis = 0; xaxis < calculatedRows; xaxis++)
                        {
                            int index = yaxis * calculatedRows + xaxis;
                            Hud.PieceIconData templatePieceData = __instance.m_pieceIcons[index];
                            templatePieceData.m_marker.SetActive(new Vector2Int(xaxis, yaxis) == selectedNr);
                            if (index < buildPieces.Count)
                            {
                                Piece piece = buildPieces[index];
                                templatePieceData.m_icon.sprite = piece.m_icon;
                                templatePieceData.m_icon.enabled = true;
                                templatePieceData.m_tooltip.m_text = piece.m_name;
                                templatePieceData.m_upgrade.SetActive(piece.m_isUpgrade);
                                templatePieceData.m_go.SetActive(true);
                            }
                            else
                            {
                                templatePieceData.m_icon.enabled = false;
                                templatePieceData.m_tooltip.m_text = "";
                                templatePieceData.m_upgrade.SetActive(false);
                                templatePieceData.m_go.SetActive(false);
                            }
                        }
                    }
                    __instance.UpdatePieceBuildStatus(buildPieces, player);
                    if (updateAllBuildStatuses)
                    {
                        __instance.UpdatePieceBuildStatusAll(buildPieces, player);
                    }
                    if (__instance.m_lastPieceCategory != category)
                    {
                        __instance.m_lastPieceCategory = category;
                        __instance.m_pieceBarPosX = __instance.m_pieceBarTargetPosX;
                        __instance.UpdatePieceBuildStatusAll(buildPieces, player);
                    }
                    return false;
                }
                return true;
            }         
        }
    }

    #endregion

    #region PieceTable
    public static class PieceTablePatches
    {
        [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.PrevCategory))]
        public static class PieceTable_PrevCategory
        {
            public static bool Prefix(ref PieceTable __instance)
            {
                if (BuildExpansion.isEnabled.Value)
                {
                    if (BuildExpansion.disableScrollCategories.Value)
                    {
                        if (Input.GetAxis("Mouse ScrollWheel") != 0f)
                        {
                            return false;
                        }
                    }
                    HudPatches.myScroll.value = 0;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.NextCategory))]
        public static class PieceTable_NextCategory
        {
            public static bool Prefix(ref PieceTable __instance)
            {
                if (BuildExpansion.isEnabled.Value)
                {
                    if (BuildExpansion.disableScrollCategories.Value)
                    {
                        if (Input.GetAxis("Mouse ScrollWheel") != 0f)
                        {
                            return false;
                        }
                    }
                    HudPatches.myScroll.value = 0;
                }
                return true;
            }
        }
    }
    #endregion

    #endregion
}
