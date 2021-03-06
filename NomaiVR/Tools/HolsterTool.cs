﻿using OWML.ModHelper.Events;
using System;
using UnityEngine;

namespace NomaiVR
{
    internal class HolsterTool : MonoBehaviour
    {
        public Transform hand;
        public ToolMode mode;
        public Vector3 position;
        public Vector3 angle;
        public float scale;
        private MeshRenderer[] _renderers;
        private bool _visible = true;
        public Action onUnequip;

        public ProximityDetector Detector { get; private set; }

        internal void Start()
        {
            Detector = gameObject.AddComponent<ProximityDetector>();
            Detector.other = HandsController.Behaviour.RightHand;
            Detector.minDistance = 0.2f;
            _renderers = gameObject.GetComponentsInChildren<MeshRenderer>();
            transform.localScale = Vector3.one * scale;

            GlobalMessenger.AddListener("SuitUp", Unequip);
            GlobalMessenger.AddListener("RemoveSuit", Unequip);
        }

        private void Equip()
        {
            ToolHelper.Swapper.EquipToolMode(mode);

            if (mode == ToolMode.Translator)
            {
                GameObject.FindObjectOfType<NomaiTranslatorProp>().SetValue("_currentTextID", 1);
            }
        }

        private void Unequip()
        {
            onUnequip?.Invoke();
            ToolHelper.Swapper.UnequipTool();
        }

        private void SetVisible(bool visible)
        {
            foreach (var renderer in _renderers)
            {
                renderer.enabled = visible;
            }
            _visible = visible;
        }

        private bool IsEquipped()
        {
            return ToolHelper.Swapper.IsInToolMode(mode, ToolGroup.Suit);
        }

        private void UpdateGrab()
        {
            if (!OWInput.IsInputMode(InputMode.Character))
            {
                if (IsEquipped())
                {
                    Unequip();
                }
                return;
            }
            if (ControllerInput.Behaviour.IsGripping && !IsEquipped() && Detector.isInside && _visible)
            {
                Equip();
            }
            if (!ControllerInput.Behaviour.IsGripping && IsEquipped())
            {
                Unequip();
            }
        }

        private void UpdateVisibility()
        {
            var isCharacterMode = OWInput.IsInputMode(InputMode.Character);
            var shouldBeVisible = !ToolHelper.IsUsingAnyTool() && isCharacterMode;

            if (!_visible && shouldBeVisible)
            {
                SetVisible(true);
            }
            if (_visible && !shouldBeVisible)
            {
                SetVisible(false);
            }
        }

        internal void LateUpdate()
        {
            UpdateGrab();
            UpdateVisibility();
            if (_visible)
            {
                var player = Locator.GetPlayerTransform();
                transform.position = Locator.GetPlayerCamera().transform.position + player.TransformVector(position);
                transform.rotation = player.rotation;
                transform.Rotate(angle);
            }
        }
    }
}
