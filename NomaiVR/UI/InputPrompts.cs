﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NomaiVR
{
    internal class InputPrompts : NomaiVRModule<InputPrompts.Behaviour, InputPrompts.Behaviour.Patch>
    {
        protected override bool IsPersistent => false;
        protected override OWScene[] Scenes => PlayableScenes;

        public class Behaviour : MonoBehaviour
        {
            private static readonly List<ScreenPrompt> _toolUnequipPrompts = new List<ScreenPrompt>(2);

            private static PromptManager Manager => Locator.GetPromptManager();

            internal void LateUpdate()
            {
                var isInShip = ToolHelper.Swapper.GetToolGroup() == ToolGroup.Ship;
                var isUsingFixedProbeTool = OWInput.IsInputMode(InputMode.StationaryProbeLauncher) || OWInput.IsInputMode(InputMode.SatelliteCam);
                if (!isInShip && !isUsingFixedProbeTool)
                {
                    foreach (var prompt in _toolUnequipPrompts)
                    {
                        prompt.SetVisibility(false);
                    }
                }
            }

            public class Patch : NomaiVRPatch
            {
                public override void ApplyPatches()
                {
                    Postfix<ProbePromptController>("LateInitialize", nameof(RemoveProbePrompts));
                    Postfix<ProbePromptController>("Awake", nameof(ChangeProbePrompts));

                    Postfix<ShipPromptController>("LateInitialize", nameof(RemoveShipPrompts));
                    Postfix<ShipPromptController>("Awake", nameof(ChangeShipPrompts));

                    Postfix<NomaiTranslatorProp>("LateInitialize", nameof(RemoveTranslatorPrompts));
                    Postfix<NomaiTranslatorProp>("Awake", nameof(ChangeTranslatorPrompts));

                    Postfix<SignalscopePromptController>("LateInitialize", nameof(RemoveSignalscopePrompts));
                    Postfix<SignalscopePromptController>("Awake", nameof(ChangeSignalscopePrompts));

                    Postfix<SatelliteSnapshotController>("OnPressInteract", nameof(RemoveSatellitePrompts));
                    Postfix<SatelliteSnapshotController>("Awake", nameof(ChangeSatellitePrompts));

                    Postfix<PlayerSpawner>("Awake", nameof(RemoveJoystickPrompts));
                    Postfix<RoastingStickController>("LateInitialize", nameof(RemoveRoastingStickPrompts));
                    Postfix<ToolModeUI>("LateInitialize", nameof(RemoveToolModePrompts));
                    Postfix<ScreenPrompt>("SetVisibility", nameof(PostScreenPromptVisibility));

                    Prefix<LockOnReticule>("Init", nameof(InitLockOnReticule));

                    Prefix<ScreenPrompt>("Init", nameof(PrePromptInit));
                    Prefix<ScreenPrompt>("SetText", nameof(PrePromptSetText));
                    Postfix<ScreenPromptElement>("BuildTwoCommandScreenPrompt", nameof(PostBuildTwoCommandPromptElement));

                    // Replace Icons with empty version
                    var getButtonTextureMethod = typeof(ButtonPromptLibrary).GetMethod("GetButtonTexture", new[] { typeof(JoystickButton) });
                    Postfix(getButtonTextureMethod, nameof(ReturnEmptyTexture));
                    var getAxisTextureMethods = typeof(ButtonPromptLibrary).GetMethods().Where(method => method.Name == "GetAxisTexture");
                    foreach (var method in getAxisTextureMethods)
                    {
                        Postfix(method, nameof(ReturnEmptyTexture));
                    }

                    // Prevent probe launcher from moving the prompts around.
                    Empty<PromptManager>("OnProbeSnapshot");
                    Empty<PromptManager>("OnProbeSnapshotRemoved");
                    Empty<PromptManager>("OnProbeLauncherEquipped");
                    Empty<PromptManager>("OnProbeLauncherUnequipped");
                    Empty<ScreenPromptElement>("BuildInCommandImage");
                }

                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Unusued parameter is needed for return value passthrough.")]
                private static Texture2D ReturnEmptyTexture(Texture2D _result)
                {
                    return AssetLoader.EmptyTexture;
                }

                [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Unusued parameter is needed for return value passthrough.")]
                private static List<string> PostBuildTwoCommandPromptElement(List<string> _result, string promptText)
                {
                    var newText = promptText.Replace("<CMD1>", "").Replace("<CMD2>", "");
                    return new List<string> { newText };
                }

                private static void AddTextIfNotExisting(string text, HashSet<string> actionTexts, VRActionInput actionInput)
                {
                    var actionInputText = actionInput.GetText();
                    if (!text.Contains(actionInputText))
                    {
                        actionTexts.Add(actionInputText);
                    }
                }

                private static void AddVRMappingToPrompt(ref string text, List<InputCommand> commandList)
                {
                    if (ControllerInput.buttonActions == null || ControllerInput.axisActions == null)
                    {
                        return;
                    }

                    var actionTexts = new HashSet<string>();


                    for (var i = 0; i < commandList.Count; i++)
                    {
                        var command = commandList[i];

                        if (command.GetType() == typeof(SingleAxisCommand))
                        {
                            var singleAxisCommand = (SingleAxisCommand)command;
                            var gamepadBinding = singleAxisCommand.GetGamepadBinding();
                            if (gamepadBinding != null)
                            {
                                var button = gamepadBinding.gamepadButtonPos;
                                if (ControllerInput.buttonActions.ContainsKey(button))
                                {
                                    AddTextIfNotExisting(text, actionTexts, ControllerInput.buttonActions[button]);
                                }
                                var axis = gamepadBinding.axisID;
                                if (ControllerInput.axisActions.ContainsKey(axis))
                                {
                                    AddTextIfNotExisting(text, actionTexts, ControllerInput.axisActions[axis]);
                                }
                            }
                        }
                        else if (command.GetType() == typeof(DoubleAxisCommand))
                        {
                            var doubleAxisCommand = (DoubleAxisCommand)command;
                            var axis = doubleAxisCommand.GetGamepadAxis();
                            if (ControllerInput.axisActions.ContainsKey(axis))
                            {
                                AddTextIfNotExisting(text, actionTexts, ControllerInput.axisActions[axis]);
                            }
                        }
                    }

                    actionTexts.Reverse();
                    var cleanOriginalText = text.Replace("+", "");
                    var actionText = string.Join(" + ", actionTexts.ToArray());
                    text = $"{actionText} {cleanOriginalText}";
                }

                private static void PrePromptSetText(ref string text, List<InputCommand> ____commandList)
                {
                    AddVRMappingToPrompt(ref text, ____commandList);
                }

                private static void PrePromptInit(ref string prompt, List<InputCommand> ____commandList)
                {
                    AddVRMappingToPrompt(ref prompt, ____commandList);
                }

                private static void PostScreenPromptVisibility(bool isVisible)
                {
                    if (isVisible)
                    {
                        MaterialHelper.MakeGraphicChildrenDrawOnTop(Locator.GetPromptManager().gameObject);
                    }
                }

                private static bool InitLockOnReticule(
                    ref ScreenPrompt ____lockOnPrompt,
                    ref bool ____initialized,
                    ref bool ____showFullLockOnPrompt,
                    ref string ____lockOnPromptText,
                    ref string ____lockOnPromptTextShortened,
                    ScreenPromptList ____promptListBlock,
                    ref JetpackPromptController ____jetpackPromptController,
                    ref ScreenPrompt ____matchVelocityPrompt,
                    Text ____readout
                )
                {
                    if (!____initialized)
                    {
                        ____jetpackPromptController = Locator.GetPlayerTransform().GetComponent<JetpackPromptController>();
                        ____lockOnPromptText = "<CMD>" + UITextLibrary.GetString(UITextType.PressPrompt) + "   " + UITextLibrary.GetString(UITextType.LockOnPrompt);
                        ____lockOnPromptTextShortened = "<CMD>";
                        ____showFullLockOnPrompt = !PlayerData.GetPersistentCondition("HAS_PLAYER_LOCKED_ON");
                        if (____showFullLockOnPrompt)
                        {
                            ____lockOnPrompt = new ScreenPrompt(InputLibrary.interact, ____lockOnPromptText, 0, false, false);
                        }
                        else
                        {
                            ____lockOnPrompt = new ScreenPrompt(InputLibrary.interact, ____lockOnPromptTextShortened, 0, false, false);
                        }
                        ____matchVelocityPrompt = new ScreenPrompt(InputLibrary.matchVelocity, "<CMD>" + UITextLibrary.GetString(UITextType.HoldPrompt) + "   " + UITextLibrary.GetString(UITextType.MatchVelocityPrompt), 0, false, false);
                        ____readout.gameObject.SetActive(false);
                        ____promptListBlock.Init();
                        Locator.GetPromptManager().AddScreenPrompt(____lockOnPrompt, ____promptListBlock, TextAnchor.MiddleLeft, 20, false);
                        Locator.GetPromptManager().AddScreenPrompt(____matchVelocityPrompt, ____promptListBlock, TextAnchor.MiddleLeft, 20, false);
                        ____initialized = true;
                    }

                    return false;
                }

                private static void ChangeSatellitePrompts(ref ScreenPrompt ____forwardPrompt)
                {
                    ____forwardPrompt = new ScreenPrompt(InputLibrary.interact, ____forwardPrompt.GetText(), 0, false, false);
                }

                private static void RemoveSatellitePrompts(ScreenPrompt ____rearviewPrompt)
                {
                    Manager.RemoveScreenPrompt(____rearviewPrompt);
                }

                private static void RemoveJoystickPrompts(ref bool ____lookPromptAdded)
                {
                    ____lookPromptAdded = true;
                }

                private static void RemoveRoastingStickPrompts(
                    ScreenPrompt ____tiltPrompt,
                    ScreenPrompt ____mallowPrompt
                )
                {
                    Manager.RemoveScreenPrompt(____tiltPrompt);
                    Manager.RemoveScreenPrompt(____mallowPrompt);
                }

                private static void RemoveToolModePrompts(
                    ScreenPrompt ____freeLookPrompt,
                    ScreenPrompt ____probePrompt,
                    ScreenPrompt ____signalscopePrompt,
                    ScreenPrompt ____flashlightPrompt,
                    ScreenPrompt ____centerFlashlightPrompt,
                    ScreenPrompt ____centerTranslatePrompt,
                    ScreenPrompt ____centerProbePrompt,
                    ScreenPrompt ____centerSignalscopePrompt
                )
                {
                    Manager.RemoveScreenPrompt(____freeLookPrompt);
                    Manager.RemoveScreenPrompt(____probePrompt);
                    Manager.RemoveScreenPrompt(____signalscopePrompt);
                    Manager.RemoveScreenPrompt(____flashlightPrompt);
                    Manager.RemoveScreenPrompt(____centerFlashlightPrompt);
                    Manager.RemoveScreenPrompt(____centerTranslatePrompt);
                    Manager.RemoveScreenPrompt(____centerProbePrompt);
                    Manager.RemoveScreenPrompt(____centerSignalscopePrompt);
                }

                private static void ChangeProbePrompts(
                    ref ScreenPrompt ____launchPrompt,
                    ref ScreenPrompt ____retrievePrompt,
                    ref ScreenPrompt ____takeSnapshotPrompt,
                    ref ScreenPrompt ____forwardCamPrompt
                )
                {
                    ____launchPrompt = new ScreenPrompt(InputLibrary.interact, ____launchPrompt.GetText());
                    ____forwardCamPrompt = new ScreenPrompt(InputLibrary.interact, ____takeSnapshotPrompt.GetText());
                    ____retrievePrompt = new ScreenPrompt(InputLibrary.swapShipLogMode, UITextLibrary.GetString(UITextType.ProbeRetrievePrompt) + "   <CMD>");
                    ____takeSnapshotPrompt = new ScreenPrompt(InputLibrary.interact, ____takeSnapshotPrompt.GetText());
                }

                private static void ChangeShipPrompts(
                    ref ScreenPrompt ____exitLandingCamPrompt,
                    ref ScreenPrompt ____autopilotPrompt,
                    ref ScreenPrompt ____abortAutopilotPrompt
                )
                {
                    ____exitLandingCamPrompt = new ScreenPrompt(InputLibrary.cancel, ____exitLandingCamPrompt.GetText());
                    ____autopilotPrompt = new ScreenPrompt(InputLibrary.swapShipLogMode, ____autopilotPrompt.GetText());
                    ____abortAutopilotPrompt = new ScreenPrompt(InputLibrary.interact, ____abortAutopilotPrompt.GetText());
                }

                private static void RemoveProbePrompts(
                    ScreenPrompt ____unequipPrompt,
                    ScreenPrompt ____photoModePrompt,
                    ScreenPrompt ____rotatePrompt,
                    ScreenPrompt ____rotateCenterPrompt,
                    ScreenPrompt ____launchModePrompt
                )
                {
                    _toolUnequipPrompts.Add(____unequipPrompt);
                    Manager.RemoveScreenPrompt(____photoModePrompt);
                    Manager.RemoveScreenPrompt(____rotatePrompt);
                    Manager.RemoveScreenPrompt(____rotateCenterPrompt);
                    Manager.RemoveScreenPrompt(____launchModePrompt);
                }

                private static void ChangeSignalscopePrompts(ref ScreenPrompt ____zoomModePrompt)
                {
                    ____zoomModePrompt = new ScreenPrompt(InputLibrary.interact, UITextLibrary.GetString(UITextType.SignalscopeZoomInPrompt) + "   <CMD>");
                }

                private static void RemoveSignalscopePrompts(
                    ScreenPrompt ____unequipPrompt,
                    ScreenPrompt ____changeFrequencyPrompt,
                    ScreenPrompt ____zoomLevelPrompt
                )
                {
                    _toolUnequipPrompts.Add(____unequipPrompt);
                    Manager.RemoveScreenPrompt(____changeFrequencyPrompt);
                    Manager.RemoveScreenPrompt(____zoomLevelPrompt);
                }

                private static void RemoveShipPrompts(
                    ScreenPrompt ____freeLookPrompt,
                    ScreenPrompt ____landingModePrompt,
                    ScreenPrompt ____liftoffCamera
                )
                {
                    Manager.RemoveScreenPrompt(____freeLookPrompt);
                    Manager.RemoveScreenPrompt(____landingModePrompt);
                    Manager.RemoveScreenPrompt(____liftoffCamera);
                }

                private static void RemoveTranslatorPrompts(
                    ScreenPrompt ____unequipPrompt,
                    ScreenPrompt ____scrollPrompt,
                    ScreenPrompt ____pagePrompt
                )
                {
                    Manager.RemoveScreenPrompt(____unequipPrompt);
                    Manager.RemoveScreenPrompt(____scrollPrompt);
                    Manager.RemoveScreenPrompt(____pagePrompt);
                }

                private static void ChangeTranslatorPrompts(ref ScreenPrompt ____translatePrompt)
                {
                    ____translatePrompt = new ScreenPrompt(InputLibrary.swapShipLogMode, UITextLibrary.GetString(UITextType.TranslatorUsePrompt) + "   <CMD>");
                }
            }
        }
    }
}
