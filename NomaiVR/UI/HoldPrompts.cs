﻿using UnityEngine;

namespace NomaiVR
{
    internal class HoldPrompts : NomaiVRModule<HoldPrompts.Behaviour, NomaiVRModule.EmptyPatch>
    {
        protected override bool IsPersistent => false;
        protected override OWScene[] Scenes => PlayableScenes;

        public class Behaviour : MonoBehaviour
        {
            private Transform _holdTransform;

            internal void Start()
            {
                var canvas = GameObject.Find("ScreenPromptCanvas").GetComponent<Canvas>();
                canvas.gameObject.layer = LayerMask.NameToLayer("VisibleToPlayer");
                canvas.transform.localScale = Vector3.one * 0.0015f;
                canvas.transform.localPosition = Vector3.zero;
                canvas.transform.localRotation = Quaternion.identity;

                canvas.renderMode = RenderMode.WorldSpace;
                canvas.transform.localPosition = Vector3.zero;
                canvas.transform.localRotation = Quaternion.identity;

                var holdCanvas = canvas.gameObject.AddComponent<Holdable>();
                holdCanvas.transform.localPosition = new Vector3(-0.09f, -0.11f, 0.13f);
                _holdTransform = holdCanvas.transform;

                foreach (Transform child in canvas.transform)
                {
                    child.localPosition = Vector3.zero;
                }
            }

            internal void Update()
            {
                if (Camera.main)
                {
                    _holdTransform.LookAt(2 * _holdTransform.position - Camera.main.transform.position, PlayerHelper.PlayerHead.up);
                }
            }
        }
    }
}