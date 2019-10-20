﻿using System.Diagnostics;
using System.IO;
using BepInEx.Harmony;
using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
using Manager;
using UnityEngine;
using UnityEngine.UI;

namespace BrowserFolders.Hooks.KKP
{
    public class MakerFolders : IFolderBrowser
    {
        public BrowserType Type => BrowserType.Maker;

        private static Toggle _catToggle;
        private static CustomCharaFile _customCharaFile;
        private static FolderTreeView _folderTreeView;
        private static Toggle _loadCharaToggle;
        private static Toggle _saveCharaToggle;
        private static GameObject _saveFront;

        public static string CurrentRelativeFolder => _folderTreeView?.CurrentRelativeFolder;

        private static bool _refreshList;
        private static string _targetScene;

        public MakerFolders()
        {
            _folderTreeView = new FolderTreeView(Utils.NormalizePath(UserData.Path), Utils.NormalizePath(UserData.Path));
            _folderTreeView.CurrentFolderChanged = OnFolderChanged;

            HarmonyWrapper.PatchAll(typeof(MakerFolders));

            MakerCardSave.RegisterNewCardSavePathModifier(DirectoryPathModifier, null);

            Overlord.Init();
        }

        private static string DirectoryPathModifier(string currentDirectoryPath)
        {
            return _folderTreeView != null ? _folderTreeView.CurrentFolder : currentDirectoryPath;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaFile), "Start")]
        public static void InitHook(CustomCharaFile __instance)
        {
            _folderTreeView.DefaultPath = Overlord.GetDefaultPath(CustomBase.Instance.modeSex);
            _folderTreeView.CurrentFolder = _folderTreeView.DefaultPath;

            _customCharaFile = __instance;

            var gt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/06_SystemTop");
            _loadCharaToggle = gt.transform.Find("tglLoadChara").GetComponent<Toggle>();
            _saveCharaToggle = gt.transform.Find("tglSaveChara").GetComponent<Toggle>();

            var mt = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMainMenu/BaseTop/tglSystem");
            _catToggle = mt.GetComponent<Toggle>();

            _saveFront = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CvsCaptureFront");

            _targetScene = Scene.Instance.AddSceneName;
        }

        public void OnGui()
        {
            // Check the opened category
            if (_catToggle != null && _catToggle.isOn && _targetScene == Scene.Instance.AddSceneName)
            {
                // Check opened tab
                if (_loadCharaToggle != null && _loadCharaToggle.isOn || _saveCharaToggle != null && _saveCharaToggle.isOn)
                {
                    // Check if the character picture take screen is displayed
                    if (_saveFront == null || !_saveFront.activeSelf)
                    {
                        if (_refreshList)
                        {
                            OnFolderChanged();
                            _refreshList = false;
                        }

                        var screenRect = new Rect((int)(Screen.width * 0.004), (int)(Screen.height * 0.57f), (int)(Screen.width * 0.125), (int)(Screen.height * 0.35));
                        Utils.DrawSolidWindowBackground(screenRect);
                        GUILayout.Window(362, screenRect, TreeWindow, "Select character folder");
                    }
                }
            }
        }

        private static void OnFolderChanged()
        {
            if (_customCharaFile == null) return;

            if (_loadCharaToggle != null && _loadCharaToggle.isOn || _saveCharaToggle != null && _saveCharaToggle.isOn)
            {
                // private bool Initialize(bool isDefaultDataAdd, bool reCreate)
                Traverse.Create(_customCharaFile).Method("Initialize", _loadCharaToggle?.isOn == true, false).GetValue();
            }
        }

        private static void TreeWindow(int id)
        {
            GUILayout.BeginVertical();
            {
                _folderTreeView.DrawDirectoryTree();

                GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
                {
                    if (Overlord.DrawDefaultCardsToggle())
                        OnFolderChanged();

                    if (GUILayout.Button("Refresh thumbnails"))
                        OnFolderChanged();

                    GUILayout.Space(1);

                    GUILayout.Label("Open in explorer...");
                    if (GUILayout.Button("Current folder"))
                        Process.Start("explorer.exe", $"\"{_folderTreeView.CurrentFolder}\"");
                    if (GUILayout.Button("Screenshot folder"))
                        Process.Start("explorer.exe", $"\"{Path.Combine(Utils.NormalizePath(UserData.Path), "cap")}\"");
                    if (GUILayout.Button("Main game folder"))
                        Process.Start("explorer.exe", $"\"{Path.GetDirectoryName(Utils.NormalizePath(UserData.Path))}\"");
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
    }
}
