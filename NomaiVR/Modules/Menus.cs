﻿using OWML.Common;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NomaiVR {
    public class Menus: MonoBehaviour {
        public bool isInGame;
        static float _farClipPlane = -1;
        static int _cullingMask;

        public static class CanvasTypes {
            public const string PauseMenu = "PauseMenu";
            public const string DialogueCanvas = "DialogueCanvas";
            public const string ScreenPromptCanvas = "ScreenPromptCanvas";
            public const string TitleMenu = "TitleMenu";
        }

        void Awake () {
            NomaiVR.Helper.Events.Subscribe<CanvasMarkerManager>(Events.AfterStart);
            NomaiVR.Helper.Events.OnEvent += OnEvent;
        }

        void Start () {
            NomaiVR.Log("Start Menus");

            // Make UI elements draw on top of everything.
            Canvas.GetDefaultCanvasMaterial().SetInt("unity_GUIZTestMode", (int) CompareFunction.Always);

            if (isInGame) {
                FixGameCanvases(new[] {
                    new CanvasInfo(CanvasTypes.PauseMenu, 0.0005f),
                    new CanvasInfo(CanvasTypes.DialogueCanvas),
                });

                if (SceneManager.GetActiveScene().name == "SolarSystem") {
                    GlobalMessenger.AddListener("WakeUp", OnWakeUp);

                    if (_farClipPlane == -1) {
                        _cullingMask = Camera.main.cullingMask;
                        _farClipPlane = Camera.main.farClipPlane;
                        Locator.GetPlayerCamera().postProcessingSettings.eyeMaskEnabled = false;
                        Camera.main.cullingMask = (1 << 5);
                        Camera.main.farClipPlane = 5;
                    }
                }
            } else {
                FixMainMenuCanvas();
            }
        }

        private void OnEvent (MonoBehaviour behaviour, Events ev) {
            if (behaviour.GetType() == typeof(CanvasMarkerManager) && ev == Events.AfterStart) {
                var canvas = GameObject.Find("CanvasMarkerManager").GetComponent<Canvas>();
                canvas.planeDistance = 5;
            }
        }

        public static void Reset () {
            _farClipPlane = -1;
        }

        void OnWakeUp () {
            Camera.main.cullingMask = _cullingMask;
            Camera.main.farClipPlane = _farClipPlane;
        }

        void MoveCanvasToWorldSpace (CanvasInfo canvasInfo) {
            GameObject canvas = GameObject.Find(canvasInfo.name);

            if (canvas == null) {
                NomaiVR.Log("Couldn't find canvas with name: " + canvasInfo.name);
                return;
            }

            Canvas[] subCanvases = canvas.GetComponentsInChildren<Canvas>();

            foreach (Canvas subCanvas in subCanvases) {
                subCanvas.renderMode = RenderMode.WorldSpace;
                subCanvas.transform.localPosition = Vector3.zero;
                subCanvas.transform.localRotation = Quaternion.identity;
                subCanvas.transform.localScale = Vector3.one;
            }

            canvas.transform.parent = Camera.main.transform;
            canvas.transform.localPosition = canvasInfo.offset;
            canvas.transform.localEulerAngles = new Vector3(0, 0, 0);
            canvas.transform.localScale = Vector3.one * canvasInfo.scale;

            // Masks are used for hiding the overflowing elements in scrollable menus.
            // Apparently masks change the material of the canvas element being masked,
            // and I'm not sure how to change unity_GUIZTestMode there.
            // So for now I'm disabling the mask completely, which breaks some menus.
            var masks = canvas.GetComponentsInChildren<Mask>(true);
            foreach (var mask in masks) {
                mask.enabled = false;
                mask.graphic.enabled = false;
            }
        }

        void FixMainMenuCanvas () {
            MoveCanvasToWorldSpace(new CanvasInfo(CanvasTypes.TitleMenu, 0.0005f));
        }

        void FixGameCanvases (CanvasInfo[] canvasInfos) {
            foreach (CanvasInfo canvasInfo in canvasInfos) {
                MoveCanvasToWorldSpace(canvasInfo);
            }
        }

        void FixAllCanvases () {
            var canvases = GameObject.FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases) {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = Camera.main;
                    canvas.planeDistance = 5;
                }
            }
        }

        protected class CanvasInfo {
            public string name;
            public Vector3 offset;
            public float scale;
            const float _defaultScale = 0.001f;

            public CanvasInfo (string _name, Vector3 _offset, float _scale = _defaultScale) {
                name = _name;
                offset = _offset;
                scale = _scale;
            }

            public CanvasInfo (string _name, float _scale = _defaultScale) {
                name = _name;
                offset = new Vector3(0, 0, 1);
                scale = _scale;
            }
        }
    }
}