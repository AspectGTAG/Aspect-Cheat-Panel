﻿using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using Aspect.Utilities;
using System.Collections.Generic;
using GorillaExtensions;
using GorillaGameModes;
using BepInEx;
using ExitGames.Client.Photon;
using Photon.Realtime;
using System;
using GorillaNetworking;
using static UnityEngine.Rendering.DebugUI;

namespace Aspect.MenuLib
{
    /// <summary>
    /// This class is used to store all GorillaMods and HarmonyPatches.
    /// You should modify this class, when you add new mods.
    /// </summary>
    public static class GorillaMods
    {
        #region SAFETY
        // Report Disconnect
        public static void ReportDisconnect()
        {
        }

        // Anticheat Disconnect
        public static bool acDisconnect = false;
        public static void AntiCheatDisconnect(bool reset = false)
        {
            if (reset)
            {
                acDisconnect = false;
            }
            acDisconnect = true;
        }
        #endregion

        #region VISUAL
        // Unlock Framerate 
        static int defaultTargetFramerate = 0;
        public static void UnlockFramrate(bool reset = false)
        {
            if (reset)
            {
                if (defaultTargetFramerate != 0) Application.targetFrameRate = defaultTargetFramerate;
            }
            else
            {
                if (defaultTargetFramerate == 0) defaultTargetFramerate = Application.targetFrameRate;
                Application.targetFrameRate = 10000;
            }
        }

        // Set Performance Mode
        static int defaultParticleRaycastBudget = 0;
        public static void SetPerformanceMode(bool reset = false)
        {
            if (reset) // Reset back to normal graphics
            {
                QualitySettings.globalTextureMipmapLimit = 0; // Reset graphics

                if (defaultParticleRaycastBudget != 0) QualitySettings.particleRaycastBudget = defaultParticleRaycastBudget; // Turn on particles

                UnlockFramrate(true); // Lock framerate
            }
            else // Set performance mode
            {
                QualitySettings.globalTextureMipmapLimit = 10; // Turn down graphics

                if (defaultParticleRaycastBudget == 0) defaultParticleRaycastBudget = QualitySettings.particleRaycastBudget; // Turn off particles
                QualitySettings.particleRaycastBudget = 0;

                UnlockFramrate(); // Unlock fps
            }
        }

        // Simple Camera Mod
        public static void SimpleCameraMod(GameObject cameraGO)
        {
            cameraGO.transform.position = GorillaTagger.Instance.mainCamera.transform.position;
            cameraGO.transform.rotation = GorillaTagger.Instance.mainCamera.transform.rotation;
            cameraGO.GetComponent<Camera>().fieldOfView = 100f;
        }

        // ESP stuff
        static Dictionary<string, Color> OriginalRigColors = new Dictionary<string, Color>();
        static Dictionary<string, GameObject> TracerObjects = new Dictionary<string, GameObject>();
        static Dictionary<string, GameObject[]> Boxes = new Dictionary<string, GameObject[]>();
        static Vector3[] boxLinesOffset = new Vector3[] { new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, -0.5f, 0f) };
        static Color taggedColor = new Color(255f / 255f, 0f / 255f, 0f / 255f, 0.3f);
        static Color casualColor = new Color(0f / 255f, 255f / 255f, 0f / 255f, 0.3f);
        static Color huntedColor = new Color(0f / 255f, 128f / 255f, 255f / 255f, 0.3f);
        static Color sodaInfected = new Color(0f / 255f, 255f / 255, 128f / 255, 0.3f);
        static Color blueAlive = new Color(0f / 255f, 128f / 255f, 255f / 255f, 0.3f);
        static Color blueHit = new Color(0f / 255f, 64f / 255f, 255f / 255f, 0.3f);
        static Color paintsplatterblue = new Color(0f / 255f, 0f / 255f, 255f / 255f, 0.3f);
        static Color orangeAlive = new Color(255f / 255f, 128f / 255f, 0f / 255f, 0.3f);
        static Color orangeHit = new Color(255f / 255f, 64f / 255f, 0f / 255f, 0.3f);
        static Color paintsplatterorange = new Color(255f / 255f, 0f / 255f, 0f / 255f, 0.3f);
        public static void Chams(bool reset = false)
        {
            // Resets rig materials if Reset = true
            if (reset)
            {
                // Set original rig colors after turning off
                foreach (VRRig rig in GorillaParent.instance.vrrigs)
                {
                    rig.mainSkin.material.shader = Shader.Find("GorillaTag/UberShader");
                    if (OriginalRigColors.ContainsKey(RigManager.VRRigToPhotonView(rig).Owner.UserId))
                    {
                        if (RigManager.IsTagged(rig))
                        {
                            rig.mainSkin.material.color = Color.white;
                        }
                        else
                        {
                            rig.mainSkin.material.color = OriginalRigColors[RigManager.VRRigToPhotonView(rig).Owner.UserId];
                        }
                    }
                }
                OriginalRigColors.Clear();
                return;
            }

            // Goes through all rigs to draw chams
            for (int i = 0; i < GorillaParent.instance.vrrigs.Count; i++)
            {
                // Skip if its your rig
                VRRig rig = GorillaParent.instance.vrrigs[i];
                if (rig == GorillaTagger.Instance.offlineVRRig) continue;

                // Save original rig colors so chams can turn off
                if (!OriginalRigColors.ContainsKey(RigManager.VRRigToPhotonView(rig).Owner.UserId))
                {
                    if (RigManager.IsTagged(rig))
                    {
                        OriginalRigColors.Add(RigManager.VRRigToPhotonView(rig).Owner.UserId, UnityEngine.Random.ColorHSV());
                    }
                    else
                    {
                        OriginalRigColors.Add(RigManager.VRRigToPhotonView(rig).Owner.UserId, rig.mainSkin.material.color);
                    }
                }

                // Get Color
                Color color;
                if (RigManager.IsTagged(rig) || GorillaTagger.Instance.offlineVRRig.huntComputer.GetComponent<GorillaHuntComputer>().myTarget == RigManager.VRRigToPhotonView(rig).Owner) color = taggedColor;
                else if (rig.mainSkin.material.name.Contains(RigManager.hunted)) color = huntedColor;
                else if (rig.mainSkin.material.name.Contains(RigManager.sodainfected)) color = sodaInfected;
                else if (rig.mainSkin.material.name.Contains(RigManager.bluealive)) color = blueAlive;
                else if (rig.mainSkin.material.name.Contains(RigManager.bluehit)) color = blueHit;
                else if (rig.mainSkin.material.name.Contains(RigManager.paintsplatterblue)) color = paintsplatterblue;
                else if (rig.mainSkin.material.name.Contains(RigManager.orangealive)) color = orangeAlive;
                else if (rig.mainSkin.material.name.Contains(RigManager.orangehit)) color = orangeHit;
                else if (rig.mainSkin.material.name.Contains(RigManager.paintsplatterorange)) color = paintsplatterorange;
                else color = casualColor;

                // Set chams
                if (rig.mainSkin.material.color != color || rig.mainSkin.material.shader != Shader.Find("GUI/Text Shader"))
                {
                    rig.mainSkin.material.color = color;
                    rig.mainSkin.material.shader = Shader.Find("GUI/Text Shader");
                }
            }
        }
        public static void Tracers(bool stateDepender, bool lefthand, bool reset = false)
        {
            // Destroy tracers if Reset = true
            if (reset)
            {
                foreach (VRRig rig in GorillaParent.instance.vrrigs)
                {
                    if (TracerObjects.ContainsKey(RigManager.VRRigToPhotonView(rig).Owner.UserId) && TracerObjects[RigManager.VRRigToPhotonView(rig).Owner.UserId])
                    {
                        GameObject.Destroy(TracerObjects[RigManager.VRRigToPhotonView(rig).Owner.UserId]);
                        TracerObjects.Remove(RigManager.VRRigToPhotonView(rig).Owner.UserId);
                    }
                }
                return;
            }

            // Goes through all the rigs to draw the tracers
            for (int i = 0; i < GorillaParent.instance.vrrigs.Count; i++)
            {
                // skip if it is your rig
                VRRig rig = GorillaParent.instance.vrrigs[i];
                if (rig == GorillaTagger.Instance.offlineVRRig) continue;

                // Get Color
                Color color;
                if (RigManager.IsTagged(rig) || GorillaTagger.Instance.offlineVRRig.huntComputer.GetComponent<GorillaHuntComputer>().myTarget == RigManager.VRRigToPhotonView(rig).Owner) color = taggedColor;
                else if (rig.mainSkin.material.name.Contains(RigManager.hunted)) color = huntedColor;
                else if (rig.mainSkin.material.name.Contains(RigManager.sodainfected)) color = sodaInfected;
                else if (rig.mainSkin.material.name.Contains(RigManager.bluealive)) color = blueAlive;
                else if (rig.mainSkin.material.name.Contains(RigManager.bluehit)) color = blueHit;
                else if (rig.mainSkin.material.name.Contains(RigManager.paintsplatterblue)) color = paintsplatterblue;
                else if (rig.mainSkin.material.name.Contains(RigManager.orangealive)) color = orangeAlive;
                else if (rig.mainSkin.material.name.Contains(RigManager.orangehit)) color = orangeHit;
                else if (rig.mainSkin.material.name.Contains(RigManager.paintsplatterorange)) color = paintsplatterorange;
                else color = casualColor;

                // Draw tracers if stateDepender = true
                if (stateDepender)
                {
                    // Create a tracers if there is none for the current player
                    if (!TracerObjects.ContainsKey(RigManager.VRRigToPhotonView(rig).Owner.UserId))
                    {
                        GameObject line = new GameObject(RigManager.VRRigToPhotonView(rig).Owner.UserId);
                        LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
                        lineRenderer.material.shader = Shader.Find("GUI/Text Shader");
                        lineRenderer.startWidth = 0.025f;
                        lineRenderer.endWidth = 0.025f;
                        lineRenderer.startColor = color;
                        lineRenderer.endColor = color;
                        lineRenderer.SetPosition(0, GorillaLocomotion.Player.Instance.leftControllerTransform.position);
                        lineRenderer.SetPosition(1, rig.transform.position);
                        TracerObjects.Add(RigManager.VRRigToPhotonView(rig).Owner.UserId, line);
                    }
                    // Updates tracer
                    else
                    {
                        LineRenderer tracer = TracerObjects[RigManager.VRRigToPhotonView(rig).Owner.UserId].GetComponent<LineRenderer>();
                        tracer.startColor = color;
                        tracer.endColor = color;
                        tracer.SetPosition(0, GorillaLocomotion.Player.Instance.leftControllerTransform.position);
                        tracer.SetPosition(1, rig.transform.position);
                    }
                }
                else
                {
                    // Destroy tracers if stateDepender = false
                    if (TracerObjects.Values.Count != 0)
                    {
                        GameObject.Destroy(TracerObjects[RigManager.VRRigToPhotonView(rig).Owner.UserId]);
                        TracerObjects.Remove(RigManager.VRRigToPhotonView(rig).Owner.UserId);
                    }
                }
            }

            // Destroys a tracer if a player leaves
            if (TracerObjects.Values.Count != 0)
            {
                List<string> ids = new List<string>();
                foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                {
                    ids.Add(player.UserId);
                }
                foreach (string id in TracerObjects.Keys)
                {
                    if (!ids.Contains(id))
                    {
                        GameObject.Destroy(TracerObjects[id]);
                        TracerObjects.Remove(id);
                    }
                }
            }
        }
        public static void BoxESP(bool reset = false)
        {
            if (reset)
            {
                foreach (KeyValuePair<string, GameObject[]> boxLines in Boxes)
                {
                    foreach (GameObject boxLine in boxLines.Value)
                    {
                        GameObject.Destroy(boxLine);
                    }
                }
                Boxes.Clear();
                return;
            }

            // Draw boxes
            foreach (VRRig rig in GorillaParent.instance.vrrigs)
            {
                // skip if it is your rig
                if (rig == GorillaTagger.Instance.offlineVRRig) continue;

                // Get Color
                Color color;
                if (RigManager.IsTagged(rig) || GorillaTagger.Instance.offlineVRRig.huntComputer.GetComponent<GorillaHuntComputer>().myTarget == RigManager.VRRigToPhotonView(rig).Owner) color = taggedColor;
                else if (rig.mainSkin.material.name.Contains(RigManager.hunted)) color = huntedColor;
                else if (rig.mainSkin.material.name.Contains(RigManager.sodainfected)) color = sodaInfected;
                else if (rig.mainSkin.material.name.Contains(RigManager.bluealive)) color = blueAlive;
                else if (rig.mainSkin.material.name.Contains(RigManager.bluehit)) color = blueHit;
                else if (rig.mainSkin.material.name.Contains(RigManager.paintsplatterblue)) color = paintsplatterblue;
                else if (rig.mainSkin.material.name.Contains(RigManager.orangealive)) color = orangeAlive;
                else if (rig.mainSkin.material.name.Contains(RigManager.orangehit)) color = orangeHit;
                else if (rig.mainSkin.material.name.Contains(RigManager.paintsplatterorange)) color = paintsplatterorange;
                else color = casualColor;

                // Create a new box
                GameObject boxParent = new GameObject("BoxParent");
                boxParent.transform.position = rig.transform.position;
                boxParent.transform.LookAt(Camera.main.transform);
                if (!Boxes.ContainsKey(RigManager.VRRigToPhotonView(rig).Owner.UserId))
                {
                    // List for sides
                    List<GameObject> sides = new List<GameObject>();

                    // Create top
                    GameObject top = new GameObject("top");
                    LineRenderer topLine = top.AddComponent<LineRenderer>();
                    topLine.material.shader = Shader.Find("GUI/Text Shader");
                    topLine.startWidth = 0.025f;
                    topLine.endWidth = 0.025f;
                    topLine.startColor = color;
                    topLine.endColor = color;
                    topLine.SetPosition(0, boxParent.transform.position + boxLinesOffset[0]);
                    topLine.SetPosition(1, boxParent.transform.position + boxLinesOffset[1]);
                    topLine.transform.LookAt(GorillaTagger.Instance.mainCamera.transform);
                    sides.Add(top);

                    // Create bottom
                    GameObject bottom = new GameObject("bottom");
                    LineRenderer bottomLine = bottom.AddComponent<LineRenderer>();
                    bottomLine.material.shader = Shader.Find("GUI/Text Shader");
                    bottomLine.startWidth = 0.025f;
                    bottomLine.endWidth = 0.025f;
                    bottomLine.startColor = color;
                    bottomLine.endColor = color;
                    bottomLine.SetPosition(0, boxParent.transform.position + boxLinesOffset[1]);
                    bottomLine.SetPosition(1, boxParent.transform.position + boxLinesOffset[2]);
                    bottomLine.transform.LookAt(GorillaTagger.Instance.mainCamera.transform);
                    sides.Add(bottom);

                    // Create left
                    GameObject left = new GameObject("left");
                    LineRenderer leftLine = left.AddComponent<LineRenderer>();
                    leftLine.material.shader = Shader.Find("GUI/Text Shader");
                    leftLine.startWidth = 0.025f;
                    leftLine.endWidth = 0.025f;
                    leftLine.startColor = color;
                    leftLine.endColor = color;
                    leftLine.SetPosition(0, boxParent.transform.position + boxLinesOffset[2]);
                    leftLine.SetPosition(1, boxParent.transform.position + boxLinesOffset[3]);
                    leftLine.transform.LookAt(GorillaTagger.Instance.mainCamera.transform);
                    sides.Add(left);

                    // Create right
                    GameObject right = new GameObject("right");
                    LineRenderer rightLine = right.AddComponent<LineRenderer>();
                    rightLine.material.shader = Shader.Find("GUI/Text Shader");
                    rightLine.startWidth = 0.025f;
                    rightLine.endWidth = 0.025f;
                    rightLine.startColor = color;
                    rightLine.endColor = color;
                    rightLine.SetPosition(0, boxParent.transform.position + boxLinesOffset[3]);
                    rightLine.SetPosition(1, boxParent.transform.position + boxLinesOffset[0]);
                    rightLine.transform.LookAt(GorillaTagger.Instance.mainCamera.transform);
                    sides.Add(right);
                    Boxes.Add(RigManager.VRRigToPhotonView(rig).Owner.UserId, sides.ToArray());
                }

                for (int i = 0; i < Boxes[RigManager.VRRigToPhotonView(rig).Owner.UserId].Length; i++)
                {
                    // get gameobjects
                    GameObject[] gameObjects = Boxes[RigManager.VRRigToPhotonView(rig).Owner.UserId];

                    // set material of the line
                    gameObjects[i].GetComponent<LineRenderer>().startColor = color;
                    gameObjects[i].GetComponent<LineRenderer>().endColor = color;
                    gameObjects[i].GetComponent<LineRenderer>().material.shader = Shader.Find("GUI/Text Shader");

                    // set position of the line
                    gameObjects[i].GetComponent<LineRenderer>().SetPosition(0, boxParent.transform.position + boxLinesOffset[i]);
                    gameObjects[i].GetComponent<LineRenderer>().SetPosition(1, boxParent.transform.position + boxLinesOffset[(i + 1) % 4]);

                }
                GameObject.Destroy(boxParent, Time.deltaTime);
            }

            // Destroys the box if a player leaves
            if (Boxes.Values.Count != 0)
            {
                List<string> ids = new List<string>();
                foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                {
                    ids.Add(player.UserId);
                }
                foreach (string id in Boxes.Keys)
                {
                    if (!ids.Contains(id))
                    {
                        foreach (GameObject go in Boxes[id])
                        {
                            GameObject.Destroy(go);
                            Boxes.Remove(id);
                        }
                    }
                }
            }
        }

        // Change Time Of Day
        static int[] Times = { 1, 4, 6, 0 };
        static int currentTime = 0;
        public static void ChangeTimeOfDay(bool defaultSwitchToNext = true, int nextTime = 1)
        {
            if (defaultSwitchToNext)
            {
                BetterDayNightManager.instance.SetTimeOfDay(Times[currentTime % 4]);
                currentTime++;
            }
            else
            {
                BetterDayNightManager.instance.SetTimeOfDay(nextTime);
            }
        }
        #endregion

        #region ADVANTAGE
        // Tag Gun
        static float TagCooldownRPC = 0f;
        static float notificationCooldown = 0f;
        static bool sendGameModeNotificationTG = false;
        static Dictionary<string, string> gameModeNotificationsTG = new Dictionary<string, string>()
        {
            { "INFECTION", "You have joined an infection lobby, you can use the tag-gun freely in here." },
            { "HUNT", "You have joined a hunt lobby, you can use the tag-gun freely in here." },
            { "BATTLE", "You have joined a battle lobby, you can only use the tag-gun when you are the masterclient." },
            { "CASUAL", "You have joined a casual lobby, you can't use the tag-gun in here." },
        };
        public static void TagGun(float cooldown = 0.3f)
        {
            // send a notification when you join a lobby
            if (!PhotonNetwork.InRoom && sendGameModeNotificationTG) sendGameModeNotificationTG = false;
            if (PhotonNetwork.InRoom && !sendGameModeNotificationTG)
            {
                NotifiLib.SendNotification($"[<color=red>SERVER</color>] {gameModeNotificationsTG[RigManager.CurrentGameMode()]}");
                sendGameModeNotificationTG = true;
            }

            // make tag gun using GorillaExtensions.GunTemplate()
            RaycastHit hit = GorillaExtensions.GunTemplate();
            if (Input.instance.CheckButton(Input.ButtonType.trigger, false) && hit.collider.GetComponentInParent<VRRig>() != null)
            {
                if (RigManager.CurrentGameMode() == "HUNT" && !RigManager.IsHunted(hit.collider.GetComponentInParent<VRRig>()))
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        GorillaGameManager.instance.GetComponent<GorillaHuntManager>().HitPlayer(RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner);
                    }
                    else if (TagCooldownRPC < Time.time)
                    {
                        GameMode.ReportTag(RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner);
                        TagCooldownRPC = Time.time + cooldown;
                    }
                    return;
                }
                else if (RigManager.CurrentGameMode() == "BATTLE")
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        GorillaGameManager.instance.GetComponent<GorillaBattleManager>().HitPlayer(RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner);
                    }
                    return;
                }
                else if (RigManager.CurrentGameMode() == "INFECTION" && !RigManager.IsTagged(hit.collider.GetComponentInParent<VRRig>()))
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        if (PhotonNetwork.PlayerList.Length < 4)
                        {
                            GorillaGameManager.instance.GetComponent<GorillaTagManager>().ChangeCurrentIt(RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner);
                            return;
                        }
                        GorillaGameManager.instance.GetComponent<GorillaTagManager>().AddInfectedPlayer(RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner);
                        return;
                    }
                    else
                    {
                        if (PhotonNetwork.PlayerList.Length > 3)
                        {
                            GorillaTagger.Instance.offlineVRRig.enabled = false;
                            GorillaTagger.Instance.offlineVRRig.transform.position = hit.point + new Vector3(0, 2, 0);
                            GorillaLocomotion.Player.Instance.leftControllerTransform.gameObject.transform.position = hit.point;
                            return;
                        }
                        else if (TagCooldownRPC < Time.time)
                        {
                            GameMode.ReportTag(RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner);
                            TagCooldownRPC = Time.time + cooldown;
                        }
                        return;
                    }
                }
                else if (hit.collider.GetComponentInParent<VRRig>() && notificationCooldown < Time.time)
                {
                    NotifiLib.SendNotification("[<color=red>SERVER</color>] You can't use Tag Gun in Casual");
                    notificationCooldown = Time.time + 5;
                }
            }
            if (!GorillaTagger.Instance.offlineVRRig.enabled) GorillaTagger.Instance.offlineVRRig.enabled = true;
        }

        // Tag All
        static float RPCCooldown = 0f;
        public static void TagAll(Menu.ButtonTemplate button)
        {
            if (button != null)
            {
                if (PhotonNetwork.IsMasterClient && RigManager.CurrentGameMode() == "INFECTION")
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                        {
                            if (player.UserId == PhotonNetwork.LocalPlayer.UserId) continue;

                            if (PhotonNetwork.PlayerList.Length < 4)
                            {
                                GorillaGameManager.instance.GetComponent<GorillaTagManager>().ChangeCurrentIt(player);
                                break;
                            }
                            GorillaGameManager.instance.GetComponent<GorillaTagManager>().AddInfectedPlayer(player);
                        }
                        button.ButtonState = false;
                        Menu.RefreshMenu(Update.menu);
                        NotifiLib.SendNotification("All players are tagged.");
                    }
                    else
                    {
                        button.ButtonState = false;
                        Menu.RefreshMenu(Update.menu);
                        NotifiLib.SendNotification("Tag All connot be used in infection.");
                    }
                }
                if (RigManager.CurrentGameMode() == "HUNT")
                {
                    if (PhotonNetwork.CountOfPlayers < 3)
                    {
                        button.ButtonState = false;
                        Menu.RefreshMenu(Update.menu);
                        NotifiLib.SendNotification("Not enough players to use tag all.");
                        return;
                    }

                    if (GorillaGameManager.instance.GetComponent<GorillaHuntManager>().currentHunted.Count == PhotonNetwork.CountOfPlayers - 2)
                    {
                        button.ButtonState = false;
                        Menu.RefreshMenu(Update.menu);
                        NotifiLib.SendNotification("All players are tagged.");
                        return;
                    }

                    if (RPCCooldown < Time.time)
                    {
                        GameMode.ReportTag(GorillaTagger.Instance.offlineVRRig.huntComputer.GetComponent<GorillaHuntComputer>().myTarget);
                        RPCCooldown = Time.time + 0.1f;
                    }
                }
            }
            else
            {
                Debug.Log("Button was not found!");
            }
        }

        // Tag Aura
        static float TagCooldown = 0f;
        public static void TagAura()
        {
            if (PhotonNetwork.InRoom && RigManager.CurrentGameMode() == "INFECTION")
            {
                foreach (VRRig rig in GorillaParent.instance.vrrigs)
                {
                    if (TagCooldown < Time.time && rig != GorillaTagger.Instance.offlineVRRig && !RigManager.IsTagged(rig) && RigManager.IsTagged(GorillaTagger.Instance.offlineVRRig))
                    {
                        if (GorillaTagger.Instance.offlineVRRig.CheckDistance(rig.transform.position, 6f) && GorillaTagger.Instance.offlineVRRig.CheckTagDistanceRollback(rig, 6f, 0.2f))
                        {
                            GameMode.ReportTag(RigManager.VRRigToPhotonView(rig).Owner);
                            TagCooldown = Time.time + 0.2f;
                        }
                    }
                }
            }
        }

        // Flick Tag
        public static void FlickTag()
        {
            if (PhotonNetwork.InRoom)
            {
                if (RigManager.CurrentGameMode() == "INFECTION" && Input.instance.CheckButton(Input.ButtonType.grip, false))
                {
                    foreach (VRRig rig in GorillaParent.instance.vrrigs)
                    {
                        if (rig != GorillaTagger.Instance.offlineVRRig && !RigManager.IsTagged(rig) && RigManager.IsTagged(GorillaTagger.Instance.offlineVRRig))
                        {
                            if (GorillaTagger.Instance.offlineVRRig.CheckDistance(rig.transform.position, 6f) && GorillaTagger.Instance.offlineVRRig.CheckTagDistanceRollback(rig, 6f, 0.2f))
                            {
                                GorillaLocomotion.Player.Instance.rightControllerTransform.gameObject.transform.position = rig.transform.position;
                            }
                        }
                    }
                }
                if (RigManager.CurrentGameMode() == "HUNT" && Input.instance.CheckButton(Input.ButtonType.grip, false))
                {
                    foreach (VRRig rig in GorillaParent.instance.vrrigs)
                    {
                        if (rig != GorillaTagger.Instance.offlineVRRig && GorillaTagger.Instance.offlineVRRig.huntComputer.GetComponent<GorillaHuntComputer>().myTarget == RigManager.VRRigToPhotonView(rig).Owner)
                        {
                            if (GorillaTagger.Instance.offlineVRRig.CheckDistance(rig.transform.position, 6f) && GorillaTagger.Instance.offlineVRRig.CheckTagDistanceRollback(rig, 6f, 0.2f))
                            {
                                GorillaLocomotion.Player.Instance.rightControllerTransform.gameObject.transform.position = rig.transform.position;
                            }
                        }
                    }
                }
            }
        }

        // Tag Self
        static float timeSinceStart = 0f;
        public static void TagSelf(Menu.ButtonTemplate button)
        {
            if (timeSinceStart == 0f) timeSinceStart = Time.time + 2f;
            if (timeSinceStart < Time.time)
            {
                if (!GorillaTagger.Instance.offlineVRRig.enabled) GorillaTagger.Instance.offlineVRRig.enabled = true;
                button.ButtonState = false;
                Menu.RefreshMenu(Update.menu);
                timeSinceStart = 0f;
                return;
            }

            if (RigManager.CurrentGameMode() == "INFECTION")
            {
                if (PhotonNetwork.PlayerList.Length > 3)
                {
                    GorillaTagManager tagManager = GorillaGameManager.instance.GetComponent<GorillaTagManager>();
                    if (!tagManager.currentInfected.Contains(RigManager.VRRigToPhotonView(GorillaTagger.Instance.offlineVRRig).Owner))
                    {
                        VRRig infectedPlayer = RigManager.GetClosestTagged();
                        if (PhotonNetwork.IsMasterClient)
                        {
                            tagManager.AddInfectedPlayer(RigManager.VRRigToPhotonView(infectedPlayer).Owner);
                            return;
                        }
                        GorillaTagger.Instance.offlineVRRig.enabled = false;
                        Vector3 random;
                        Vector3 pointToTP;
                        GorillaMath.LineSegClosestPoints(GorillaTagger.Instance.offlineVRRig.syncPos, -GorillaTagger.Instance.offlineVRRig.LatestVelocity() * 0.3f, infectedPlayer.syncPos, -infectedPlayer.LatestVelocity() * 0.3f, out random, out pointToTP);
                        GorillaTagger.Instance.offlineVRRig.transform.position = pointToTP;
                    }
                    else
                    {
                        GorillaTagger.Instance.offlineVRRig.enabled = true;
                        button.ButtonState = false;
                        Menu.RefreshMenu(Update.menu);
                        timeSinceStart = 0f;
                    }
                }
                else
                {
                    GorillaTagManager tagManager = GorillaGameManager.instance.GetComponent<GorillaTagManager>();
                    if (tagManager.currentIt == RigManager.VRRigToPhotonView(GorillaTagger.Instance.offlineVRRig).Owner)
                    {
                        VRRig infectedPlayer = GorillaTagger.Instance.offlineVRRig;
                        if (PhotonNetwork.IsMasterClient)
                        {
                            tagManager.ChangeCurrentIt(RigManager.VRRigToPhotonView(infectedPlayer).Owner);
                            return;
                        }

                        infectedPlayer = RigManager.GetClosestTagged();
                        GorillaTagger.Instance.offlineVRRig.enabled = false;
                        Vector3 random;
                        Vector3 pointToTP;
                        GorillaMath.LineSegClosestPoints(GorillaTagger.Instance.offlineVRRig.syncPos, -GorillaTagger.Instance.offlineVRRig.LatestVelocity() * 0.2f, infectedPlayer.syncPos, -infectedPlayer.LatestVelocity() * 0.2f, out random, out pointToTP);
                        GorillaTagger.Instance.offlineVRRig.transform.position = pointToTP;
                    }
                    else
                    {
                        GorillaTagger.Instance.offlineVRRig.enabled = true;
                        button.ButtonState = false;
                        Menu.RefreshMenu(Update.menu);
                        timeSinceStart = 0f;
                    }
                }

            }
            else
            {
                button.ButtonState = false;
                Menu.RefreshMenu(Update.menu);
                timeSinceStart = 0f;
            }
        }

        // No Tag Freeze
        public static void TagFreeze(bool disable)
        {
            GorillaLocomotion.Player.Instance.disableMovement = !disable;
        }

        // No Tag On Join
        public static void NoTagOnJoin(bool Disable = false)
        {
            if (!Disable)
            {
                if (PlayerPrefs.GetString("tutorial") == "done")
                {
                    PlayerPrefs.SetString("didTutorial", "false");
                    PlayerPrefs.Save();
                }
            }
            else
            {
                if (PlayerPrefs.GetString("tutorial") != "done")
                {
                    PlayerPrefs.SetString("didTutorial", "done");
                    PlayerPrefs.Save();
                }
            }
        }
        #endregion

        #region MASTER
        // Freeze Gun
        public static void FreezeGun()
        {
            RaycastHit hit = GorillaExtensions.GunTemplate();
            if (Input.instance.CheckButton(Input.ButtonType.trigger, false) && hit.collider.GetComponentInParent<VRRig>() != null)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    GorillaTagManager tagManager = GorillaGameManager.instance.GetComponent<GorillaTagManager>();
                    tagManager.AddInfectedPlayer(RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner);
                    tagManager.currentInfected.Remove(RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner);
                    tagManager.UpdateInfectionState();
                }
            }
        }

        // Vibrate Gun
        public static void VibrateGun()
        {
            RaycastHit hit = GorillaExtensions.GunTemplate();
            if (Input.instance.CheckButton(Input.ButtonType.trigger, false) && hit.collider.GetComponentInParent<VRRig>() != null)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    GorillaTagManager tagManager = GorillaGameManager.instance.GetComponent<GorillaTagManager>();
                    tagManager.AddInfectedPlayer(RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner, false);
                    tagManager.currentInfected.Remove(RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner);
                    tagManager.UpdateInfectionState();
                }
            }
        }

        // Material Gun
        static float materialCooldown = 0f;
        public static void MaterialGun(float cooldown = 0.2f)
        {
            RaycastHit hit = GorillaExtensions.GunTemplate();
            if (Input.instance.CheckButton(Input.ButtonType.trigger, false) && hit.collider.GetComponentInParent<VRRig>() != null)
            {
                if (PhotonNetwork.IsMasterClient && RigManager.CurrentGameMode() == "INFECTION")
                {
                    if (materialCooldown < Time.time)
                    {
                        VRRig targetRig = hit.collider.GetComponentInParent<VRRig>();
                        Photon.Realtime.Player target = RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner;
                        GorillaTagManager tagManager = GorillaGameManager.instance.GetComponent<GorillaTagManager>();

                        if (tagManager.isCurrentlyTag)
                        {
                            if (!targetRig.mainSkin.material.name.Contains(RigManager.it))
                            {
                                tagManager.ChangeCurrentIt(target);
                            }
                            else
                            {
                                tagManager.ChangeCurrentIt(PhotonNetwork.LocalPlayer, false);
                            }
                        }
                        else
                        {
                            if (!targetRig.mainSkin.material.name.Contains(RigManager.infected))
                            {
                                tagManager.AddInfectedPlayer(RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner);
                            }
                            else
                            {
                                tagManager.currentInfected.Remove(RigManager.VRRigToPhotonView(hit.collider.GetComponentInParent<VRRig>()).Owner);
                                tagManager.UpdateInfectionState();
                            }
                        }

                        materialCooldown = Time.time + cooldown;
                    }
                }
            }
        }

        // Material All
        public static void MaterialAll(float cooldown = 0.2f)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                foreach (VRRig targetRig in GorillaParent.instance.vrrigs)
                {
                    if (PhotonNetwork.IsMasterClient && RigManager.CurrentGameMode() == "INFECTION")
                    {
                        if (materialCooldown < Time.time)
                        {
                            Photon.Realtime.Player target = RigManager.VRRigToPhotonView(targetRig).Owner;
                            GorillaTagManager tagManager = GorillaGameManager.instance.GetComponent<GorillaTagManager>();

                            if (tagManager.isCurrentlyTag)
                            {
                                if (!targetRig.mainSkin.material.name.Contains(RigManager.it))
                                {
                                    tagManager.ChangeCurrentIt(target);
                                }
                                else
                                {
                                    tagManager.ChangeCurrentIt(PhotonNetwork.LocalPlayer, false);
                                }
                            }
                            else
                            {
                                if (!targetRig.mainSkin.material.name.Contains(RigManager.infected))
                                {
                                    tagManager.AddInfectedPlayer(target);
                                }
                                else
                                {
                                    tagManager.currentInfected.Remove(target);
                                    tagManager.UpdateInfectionState();
                                }
                            }

                            materialCooldown = Time.time + cooldown;
                        }
                    }
                }
            }
        }

        // Material Self
        public static void MaterialSelf(float cooldown = 0.2f)
        {
            if (PhotonNetwork.IsMasterClient && RigManager.CurrentGameMode() == "INFECTION")
            {
                if (materialCooldown < Time.time)
                {
                    VRRig targetRig = GorillaTagger.Instance.offlineVRRig;
                    Photon.Realtime.Player target = PhotonNetwork.LocalPlayer;
                    GorillaTagManager tagManager = GorillaGameManager.instance.GetComponent<GorillaTagManager>();

                    if (tagManager.isCurrentlyTag)
                    {
                        if (!targetRig.mainSkin.material.name.Contains(RigManager.it))
                        {
                            tagManager.ChangeCurrentIt(target);
                        }
                        else
                        {
                            tagManager.ChangeCurrentIt(PhotonNetwork.LocalPlayer, false);
                        }
                    }
                    else
                    {
                        if (!targetRig.mainSkin.material.name.Contains(RigManager.infected))
                        {
                            tagManager.AddInfectedPlayer(target);
                        }
                        else
                        {
                            tagManager.currentInfected.Remove(target);
                            tagManager.UpdateInfectionState();
                        }
                    }

                    materialCooldown = Time.time + cooldown;
                }
            }
        }
        #endregion

        #region RIG
        // RGB (only works in stump for now)
        static float RGBcooldown = 0f;
        public static void RGB(float Cooldown = 1f)
        {
            // Create RGB action
            if (Cooldown < 0.0404f) Cooldown = 0.0404f;
            Color color = Color.HSVToRGB((Time.frameCount / 180f) % 1f, 1f, 1f);
            if (RGBcooldown < Time.time)
            {
                Util.ChangeColor(color.r, color.g, color.b);
                RGBcooldown = Time.time + Cooldown;
            }
        }

        // Set Handtap Volume
        public static void SetHandtapVolume(float volume = 0.1f)
        {
            GorillaTagger.Instance.handTapVolume = volume;
        }

        // Solid Monkeys
        static Dictionary<string, GameObject[]> SolidMonkeyParts = new Dictionary<string, GameObject[]>();
        public static void SolidMonkeys(bool reset = false)
        {
            // Reset/Destroy all gameobjects
            if (reset)
            {
                foreach (KeyValuePair<string, GameObject[]> solidMonkeyParts in SolidMonkeyParts)
                {
                    foreach (GameObject part in solidMonkeyParts.Value)
                    {
                        GameObject.Destroy(part);
                    }
                }
                Boxes.Clear();
                return;
            }

            // Create gameobjects
            foreach (VRRig rig in GorillaParent.instance.vrrigs)
            {
                if (rig == GorillaTagger.Instance.offlineVRRig) return;
                if (!SolidMonkeyParts.ContainsKey(RigManager.VRRigToPhotonView(rig).Owner.UserId))
                {
                    // Make the list that contains all gameobjects
                    List<GameObject> gameObjects = new List<GameObject>();

                    // Create primitives
                    GameObject lHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    //GameObject.Destroy(lHand.GetComponent<Renderer>());
                    GameObject.Destroy(lHand.GetComponent<Rigidbody>());
                    lHand.transform.SetParent(rig.leftHandTransform);
                    lHand.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    gameObjects.Add(lHand);

                    GameObject rHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    //GameObject.Destroy(rHand.GetComponent<Renderer>());
                    GameObject.Destroy(rHand.GetComponent<Rigidbody>());
                    rHand.transform.SetParent(rig.rightHandTransform);
                    rHand.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    gameObjects.Add(rHand);

                    GameObject Head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    //GameObject.Destroy(Head.GetComponent<Renderer>());
                    GameObject.Destroy(Head.GetComponent<Rigidbody>());
                    Head.transform.SetParent(rig.headConstraint);
                    Head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    gameObjects.Add(Head);

                    GameObject Body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //GameObject.Destroy(Body.GetComponent<Renderer>());
                    GameObject.Destroy(Body.GetComponent<Rigidbody>());
                    Body.transform.SetParent(rig.headConstraint);
                    Body.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
                    Body.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                    gameObjects.Add(Body);
                    SolidMonkeyParts.Add(RigManager.VRRigToPhotonView(rig).Owner.UserId, gameObjects.ToArray());
                }
            }

            // Destroy objects for a player if they leave
            if (SolidMonkeyParts.Values.Count != 0)
            {
                List<string> ids = new List<string>();
                foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                {
                    ids.Add(player.UserId);
                }
                foreach (string id in SolidMonkeyParts.Keys)
                {
                    if (!ids.Contains(id))
                    {
                        foreach (GameObject go in SolidMonkeyParts[id])
                        {
                            go.transform.parent = null;
                            GameObject.Destroy(go);
                            SolidMonkeyParts.Remove(id);
                        }
                    }
                }
            }
            return;
        }

        // Ghost Monkey
        static bool IsGhost = false;
        static bool IsGhostCooldown = false;
        public static void GhostMonkey(bool enable = true)
        {
            if (enable)
            {
                if (!IsGhostCooldown && Input.instance.CheckButton(Input.ButtonType.primary, false))
                {
                    IsGhost = !IsGhost;
                    IsGhostCooldown = true;
                    GorillaTagger.Instance.offlineVRRig.enabled = false;
                }
                else if (!Input.instance.CheckButton(Input.ButtonType.primary, false))
                {
                    IsGhostCooldown = false;
                }
                if (!IsGhost)
                {
                    GorillaTagger.Instance.offlineVRRig.enabled = true;
                }
            }
            else
            {
                GorillaTagger.Instance.offlineVRRig.enabled = true;
                IsGhostCooldown = false;
                IsGhost = false;
            }
        }

        // Invisibility
        static bool IsInvis = false;
        static bool IsInvisCooldown = false;
        public static void Invisibility(bool enable = true)
        {
            if (enable)
            {
                if (!IsInvisCooldown && Input.instance.CheckButton(Input.ButtonType.primary, false))
                {
                    IsInvis = !IsInvis;
                    IsInvisCooldown = true;
                    GorillaTagger.Instance.offlineVRRig.enabled = false;
                    GorillaTagger.Instance.offlineVRRig.transform.position = GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.transform.position + new Vector3(0, -1000, 0);
                }
                else if (!Input.instance.CheckButton(Input.ButtonType.primary, false))
                {
                    IsInvisCooldown = false;
                }
                if (!IsInvis)
                {
                    GorillaTagger.Instance.offlineVRRig.enabled = true;
                }
            }
            else
            {
                GorillaTagger.Instance.offlineVRRig.enabled = true;
                IsInvisCooldown = false;
                IsInvis = false;
            }
        }
        #endregion

        #region MOVEMENT
        // Platforms
        static GameObject left;
        static GameObject right;
        public static void Platforms(bool Reset = false, bool sticky = false, int oIndex = 0, bool silent = false)
        {
            if (Reset)
            {
                if (silent == true) GorillaTagger.Instance.handTapVolume = 0.1f;
                if (left != null) UnityEngine.Object.Destroy(left);
                if (right != null) UnityEngine.Object.Destroy(right);
                return;
            }

            if (silent) sticky = true;
            PrimitiveType primitiveType = sticky ? PrimitiveType.Sphere : PrimitiveType.Cube;
            Vector3 size = oIndex != 61 ? new Vector3(0.001f, 0.2f, 0.2f) : new Vector3(0.001f, 0.4f, 0.4f);

            if (Input.instance.CheckButton(Input.ButtonType.grip, false) && right == null)
            {
                if (silent == true) GorillaTagger.Instance.handTapVolume = 0f;
                right = GameObject.CreatePrimitive(primitiveType);
                Menu.ColorChanger colorChanger = right.AddComponent<Menu.ColorChanger>();
                right.AddComponent<GorillaSurfaceOverride>().overrideIndex = oIndex;
                right.transform.localScale = size;
                right.transform.position = GorillaLocomotion.Player.Instance.rightControllerTransform.transform.position + -GorillaLocomotion.Player.Instance.rightControllerTransform.transform.right / 20;
                right.transform.rotation = GorillaLocomotion.Player.Instance.rightControllerTransform.transform.rotation;
                //GorillaExtensions.SendNetworkedPlatform(right, false, sticky);
            }
            else if (!Input.instance.CheckButton(Input.ButtonType.grip, false) && right != null)
            {
                if (silent == true && !Input.instance.CheckButton(Input.ButtonType.grip, true)) GorillaTagger.Instance.handTapVolume = 0.1f;
                //GorillaExtensions.DeleteNetworkedPlatform(right, false);
                UnityEngine.Object.Destroy(right);
                right = null;
            }
            if (Input.instance.CheckButton(Input.ButtonType.grip, true) && left == null)
            {
                if (silent == true) GorillaTagger.Instance.handTapVolume = 0f;
                left = GameObject.CreatePrimitive(primitiveType);
                Menu.ColorChanger colorChanger = left.AddComponent<Menu.ColorChanger>();
                left.AddComponent<GorillaSurfaceOverride>().overrideIndex = oIndex;
                left.transform.localScale = size;
                left.transform.position = GorillaLocomotion.Player.Instance.leftControllerTransform.transform.position + GorillaLocomotion.Player.Instance.leftControllerTransform.transform.right / 20;
                left.transform.rotation = GorillaLocomotion.Player.Instance.leftControllerTransform.transform.rotation;
                //GorillaExtensions.SendNetworkedPlatform(left, true, sticky);
            }
            else if (!Input.instance.CheckButton(Input.ButtonType.grip, true) && left != null)
            {
                if (silent == true && !Input.instance.CheckButton(Input.ButtonType.grip, false)) GorillaTagger.Instance.handTapVolume = 0.1f;
                //GorillaExtensions.DeleteNetworkedPlatform(left, true);
                UnityEngine.Object.Destroy(left);
                left = null;
            }
        }

        // Frozone
        static float RightSpawnCooldown = 0f;
        static float LeftSpawnCooldown = 0f;
        public static void Frozone(float Cooldown = 0.05f)
        {
            if (Input.instance.CheckButton(Input.ButtonType.grip, false))
            {
                if (RightSpawnCooldown < Time.time)
                {
                    GameObject rightPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Menu.ColorChanger colorChanger = rightPlatform.AddComponent<Menu.ColorChanger>();
                    colorChanger.Color1 = new Color32(85, 15, 150, 1);
                    colorChanger.Color2 = new Color32(125, 15, 200, 1);
                    rightPlatform.AddComponent<GorillaSurfaceOverride>().overrideIndex = 61;
                    rightPlatform.transform.localScale = new Vector3(0.001f, 0.4f, 0.4f);
                    rightPlatform.transform.position = GorillaLocomotion.Player.Instance.rightControllerTransform.transform.position + -GorillaLocomotion.Player.Instance.rightControllerTransform.transform.right / 20;
                    rightPlatform.transform.rotation = GorillaLocomotion.Player.Instance.rightControllerTransform.transform.rotation;
                    GameObject.Destroy(rightPlatform, 1);
                    RightSpawnCooldown = Time.time + Cooldown;
                }
            }
            if (Input.instance.CheckButton(Input.ButtonType.grip, true))
            {
                if (LeftSpawnCooldown < Time.time)
                {
                    GameObject leftPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Menu.ColorChanger colorChanger = leftPlatform.AddComponent<Menu.ColorChanger>();
                    colorChanger.Color1 = new Color32(85, 15, 150, 1);
                    colorChanger.Color2 = new Color32(125, 15, 200, 1);
                    leftPlatform.AddComponent<GorillaSurfaceOverride>().overrideIndex = 61;
                    leftPlatform.transform.localScale = new Vector3(0.001f, 0.4f, 0.4f);
                    leftPlatform.transform.position = GorillaLocomotion.Player.Instance.leftControllerTransform.transform.position + GorillaLocomotion.Player.Instance.leftControllerTransform.transform.right / 20;
                    leftPlatform.transform.rotation = GorillaLocomotion.Player.Instance.leftControllerTransform.transform.rotation;
                    GameObject.Destroy(leftPlatform, 1);
                    LeftSpawnCooldown = Time.time + Cooldown;
                }
            }
        }

        // No-Clip
        public static bool isNoclipping = false;
        public static void NoClip(bool enable = true, bool extraStateDepender = false)
        {
            if (enable)
            {
                if (Input.instance.CheckButton(Input.ButtonType.trigger, false) || extraStateDepender)
                {
                    foreach (MeshCollider collider in MeshCollider.FindObjectsOfType<MeshCollider>())
                    {
                        collider.enabled = false;
                    }
                    isNoclipping = true;
                    return;
                }
                foreach (MeshCollider collider in MeshCollider.FindObjectsOfType<MeshCollider>())
                {
                    collider.enabled = true;
                }
                isNoclipping = false;
            }
            else
            {
                foreach (MeshCollider collider in MeshCollider.FindObjectsOfType<MeshCollider>())
                {
                    collider.enabled = true;
                }
                isNoclipping = false;
            }
        }

        // Climb Anywhere
        public static void ClimbAnywhere(bool reset = false)
        {
            if (!reset)
            {
                if (Input.instance.CheckButton(Input.ButtonType.grip, false) && right == null && GorillaLocomotion.Player.Instance.IsHandTouching(false))
                {
                    right = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    GameObject.Destroy(right.GetComponent<Renderer>());
                    right.transform.localScale = new Vector3(0.001f, 0.2f, 0.2f);
                    right.transform.position = GorillaLocomotion.Player.Instance.rightControllerTransform.transform.position;
                    right.transform.rotation = GorillaLocomotion.Player.Instance.rightControllerTransform.transform.rotation;
                }
                else if (!Input.instance.CheckButton(Input.ButtonType.grip, false) && right != null || !GorillaLocomotion.Player.Instance.IsHandTouching(false) && right != null)
                {
                    UnityEngine.Object.Destroy(right);
                    right = null;
                }
                if (Input.instance.CheckButton(Input.ButtonType.grip, true) && left == null && GorillaLocomotion.Player.Instance.IsHandTouching(true))
                {
                    left = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    GameObject.Destroy(left.GetComponent<Renderer>());
                    left.transform.localScale = new Vector3(0.001f, 0.2f, 0.2f);
                    left.transform.position = GorillaLocomotion.Player.Instance.leftControllerTransform.transform.position;
                    left.transform.rotation = GorillaLocomotion.Player.Instance.leftControllerTransform.transform.rotation;
                }
                else if (!Input.instance.CheckButton(Input.ButtonType.grip, true) && left != null || !GorillaLocomotion.Player.Instance.IsHandTouching(true) && left != null)
                {
                    UnityEngine.Object.Destroy(left);
                    left = null;
                }
            }
            else
            {
                if (left != null) UnityEngine.Object.Destroy(left);
                if (right != null) UnityEngine.Object.Destroy(right);
            }
        }

        // Iron Monkey
        public static void IronMonkey(float Acceleration = 20f)
        {
            if (Input.instance.CheckButton(Input.ButtonType.primary, false))
            {
                GorillaLocomotion.Player.Instance.GetComponent<Rigidbody>().AddForce(Acceleration * GorillaLocomotion.Player.Instance.rightControllerTransform.right * Util.GetFixedDeltaTime(), ForceMode.Acceleration);
            }
            if (Input.instance.CheckButton(Input.ButtonType.primary, true))
            {
                GorillaLocomotion.Player.Instance.GetComponent<Rigidbody>().AddForce(Acceleration * GorillaLocomotion.Player.Instance.leftControllerTransform.right * -1f * Util.GetFixedDeltaTime(), ForceMode.Acceleration);
            }
        }

        // Change Gravity
        static Vector3 defaultGravity;
        public static void ChangeGravity(bool ResetGravity = false, float gravity = 3f)
        {
            if (!ResetGravity)
            {
                if (defaultGravity == Vector3.zero) defaultGravity = Physics.gravity;
                if (Physics.gravity != new Vector3(0f, -gravity, 0f)) Physics.gravity = new Vector3(0f, -gravity, 0f);
            }
            else if (defaultGravity != Vector3.zero && Physics.gravity != defaultGravity)
            {
                Physics.gravity = defaultGravity;
                defaultGravity = Vector3.zero;
            }
        }

        // Super Monkey
        static bool secondaryPower = false;
        static bool checkPowerOnce = false;
        static bool resetVelocity = false;
        public static void SuperMonkey(bool enable = true, float speed = 16f)
        {
            // flight
            if (Input.instance.CheckButton(Input.ButtonType.secondary, false))
            {
                GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.velocity = Vector3.zero;
                GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.velocity += GorillaLocomotion.Player.Instance.rightControllerTransform.forward * speed * Util.GetFixedDeltaTime();
                resetVelocity = true;
            }
            else if (resetVelocity)
            {
                GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.velocity = Vector3.zero;
                resetVelocity = false;
            }

            // secondary power
            if (enable)
            {
                if (!checkPowerOnce && Input.instance.CheckButton(Input.ButtonType.primary, false))
                {
                    secondaryPower = !secondaryPower;
                    checkPowerOnce = true;
                }
                else if (!Input.instance.CheckButton(Input.ButtonType.primary, false))
                {
                    checkPowerOnce = false;
                }
                if (secondaryPower)
                {
                    ChangeGravity(false, 0.01f);
                }
                if (!secondaryPower && defaultGravity != Vector3.zero && Physics.gravity != defaultGravity)
                {
                    ChangeGravity(true);
                }
            }
            else if (defaultGravity != Vector3.zero && Physics.gravity != defaultGravity)
            {
                ChangeGravity(true);
                secondaryPower = false;
                checkPowerOnce = false;
            }
        }

        // Speedboost
        static float OriginalMaxJumpSpeed;
        static float OriginalJumpmultiplier;
        public static void Speedboost(float jumpMax, float jumpMulti, bool ResetSpeed = false)
        {
            if (!ResetSpeed)
            {
                if (OriginalJumpmultiplier == 0f && OriginalMaxJumpSpeed == 0f)
                {
                    OriginalJumpmultiplier = GorillaLocomotion.Player.Instance.jumpMultiplier;
                    OriginalMaxJumpSpeed = GorillaLocomotion.Player.Instance.maxJumpSpeed;
                }
                GorillaLocomotion.Player.Instance.maxJumpSpeed = jumpMax;
                GorillaLocomotion.Player.Instance.jumpMultiplier = jumpMulti;
            }
            else
            {
                if (OriginalJumpmultiplier != 0f && OriginalMaxJumpSpeed != 0f)
                {
                    GorillaLocomotion.Player.Instance.jumpMultiplier = OriginalJumpmultiplier;
                    GorillaLocomotion.Player.Instance.maxJumpSpeed = OriginalMaxJumpSpeed;
                }
            }
        }

        // Up And Down
        public static void UpAndDown(float Acceleration = 20f)
        {
            if (Input.instance.CheckButton(Input.ButtonType.trigger, false))
            {
                GorillaLocomotion.Player.Instance.GetComponent<Rigidbody>().AddForce(new Vector3(0, -Acceleration, 0) - Physics.gravity, ForceMode.Acceleration);
            }
            if (Input.instance.CheckButton(Input.ButtonType.trigger, true))
            {
                GorillaLocomotion.Player.Instance.GetComponent<Rigidbody>().AddForce(new Vector3(0, Acceleration, 0) + GorillaTagger.Instance.offlineVRRig.transform.forward, ForceMode.Acceleration);
            }
        }

        // Wall Walk
        static RaycastHit latestHandInfo = new RaycastHit();
        public static void WallWalk(float stregnth = 10, bool stateDepender = true)
        {
            if (GorillaLocomotion.Player.Instance.IsHandTouching(true) || GorillaLocomotion.Player.Instance.IsHandTouching(false))
            {
                latestHandInfo = (RaycastHit)Traverse.Create(GorillaLocomotion.Player.Instance).Field("lastHitInfoHand").GetValue();
            }

            if (stateDepender && latestHandInfo.point != Vector3.zero && GorillaTagger.Instance.offlineVRRig.CheckDistance(latestHandInfo.point, 2))
            {
                GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.AddForce(latestHandInfo.normal * -stregnth * Util.GetFixedDeltaTime(), ForceMode.Acceleration);
            }
        }

        // Disable Quitbox - being turned on after being turned off doesnt work
        public static void DisableQuitbox(bool disable)
        {
            GameObject.Find("QuitBox").SetActive(!disable);
        }

        // Teleport Gun
        static bool IsTeleportCooldown = false;
        public static void TeleportGun()
        {
            RaycastHit hit = GorillaExtensions.GunTemplate();
            if (!IsTeleportCooldown && Input.instance.CheckButton(Input.ButtonType.trigger, false))
            {
                GorillaPatches.TeleportPatch.Teleport(hit.point + GorillaLocomotion.Player.Instance.rightControllerTransform.up);
                IsTeleportCooldown = true;
            }
            else if (!Input.instance.CheckButton(Input.ButtonType.trigger, false))
            {
                IsTeleportCooldown = false;
            }
        }

        // Teleport To Random Player
        public static void TeleportToRandomPlayer(string prefix = "ANY") // the current prefixes are, ANY, UNTAGGED, TAGGED
        {
            List<VRRig> rigs = new List<VRRig>();
            if (prefix == "ANY") rigs = Util.AddVRRigsExcept(new List<Photon.Realtime.Player>() { PhotonNetwork.LocalPlayer });
            if (RigManager.CurrentGameMode() == "INFECTION")
            {
                if (prefix == "TAGGED") rigs = Util.GetVRRigsFromPlayerlist(GorillaGameManager.instance.GetComponent<GorillaTagManager>().currentInfected);
                if (prefix == "UNTAGGED") rigs = Util.AddVRRigsExcept(GorillaGameManager.instance.GetComponent<GorillaTagManager>().currentInfected);
            }
            else if (prefix == "TAGGED" || prefix == "UNTAGGED")
            {
                NotifiLib.SendNotification("You're not in an infection lobby.");
            }
            GorillaPatches.TeleportPatch.Teleport(rigs[UnityEngine.Random.Range(0, rigs.Count-1)].transform.position + new Vector3(0f, 0.2f, 0f));
        }

        // Ender pearl
        public static void ProjectileTeleport(bool ride)
        {
            string[] projectilesToCheck = { "SnowballProjectile", "SlingshotProjectile" };
            foreach (string projectile in projectilesToCheck)
            {
                foreach (GameObject obj in GameObject.FindGameObjectsWithTag(projectile))
                {
                    if (obj.GetComponent<SlingshotProjectile>().projectileOwner == PhotonNetwork.LocalPlayer && !obj.GetComponent<GorillaExtensions.TeleportCollider>())
                    {
                        obj.AddComponent<GorillaExtensions.TeleportCollider>().ride = ride;
                    }
                }
            }
        }

        // Longarms
        public static void LongArms(Vector3 lOffset, Vector3 rOffset, bool turnOff = false)
        {
            if (turnOff)
            {
                // Reset arms
                if (Update.lHandOffset != Vector3.zero)
                {
                    GorillaLocomotion.Player.Instance.leftHandOffset = Update.lHandOffset;
                }
                if (Update.rHandOffset != Vector3.zero)
                {
                    GorillaLocomotion.Player.Instance.rightHandOffset = Update.rHandOffset;
                }
                return;
            }

            // Get current armlength if you don't have it already
            if (Update.lHandOffset == Vector3.zero)
            {
                Update.lHandOffset = GorillaLocomotion.Player.Instance.leftHandOffset;
            }
            if (Update.rHandOffset == Vector3.zero)
            {
                Update.rHandOffset = GorillaLocomotion.Player.Instance.rightHandOffset;
            }

            // Set Longarms
            GorillaLocomotion.Player.Instance.leftHandOffset = lOffset;
            GorillaLocomotion.Player.Instance.rightHandOffset = rOffset;
        }

        // C4
        static GameObject go_C4;
        public static void C4(float explosionForce = 10f, bool turnOff = false)
        {
            // Plant C4
            if (!go_C4)
            {
                if (Input.instance.CheckButton(Input.ButtonType.grip, true))
                {
                    go_C4 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    GameObject.Destroy(go_C4.GetComponent<Collider>());
                    GameObject.Destroy(go_C4.GetComponent<Rigidbody>());
                    go_C4.AddComponent<Menu.ColorChanger>();
                    go_C4.transform.localScale = new Vector3(0.4f, 0.1f, 0.2f);
                    go_C4.transform.position = GorillaLocomotion.Player.Instance.rightControllerTransform.position;
                }
            }

            // Detonate C4
            if (go_C4)
            {
                if (Input.instance.CheckButton(Input.ButtonType.grip, false) || turnOff)
                {
                    if (!turnOff) GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.AddExplosionForce(explosionForce*6000, go_C4.transform.position, 20f);
                    GameObject.Destroy(go_C4);
                }
            }   
        }

        // Keyboard Movement
        static Vector3 previousMousePosition;
        static Vector3 currentCamPos = Vector3.zero;
        static bool turnOffNoclipOnce = false;
        static bool hasTurnedOff = false;
        public static void AdvancedWASD(float speed)
        {
            if (currentCamPos == Vector3.zero || hasTurnedOff) currentCamPos = Camera.main.transform.position;
            float NSpeed = speed * Time.deltaTime;
            if (UnityInput.Current.GetKey(KeyCode.LeftShift) || UnityInput.Current.GetKey(KeyCode.RightShift))
            {
                NSpeed *= 3f;
            }
            if (UnityInput.Current.GetKey(KeyCode.LeftArrow) || UnityInput.Current.GetKey(KeyCode.A))
            {
                currentCamPos += Camera.main.transform.right * -1f * NSpeed;
            }
            if (UnityInput.Current.GetKey(KeyCode.RightArrow) || UnityInput.Current.GetKey(KeyCode.D))
            {
                currentCamPos += Camera.main.transform.right * NSpeed;
            }
            if (UnityInput.Current.GetKey(KeyCode.UpArrow) || UnityInput.Current.GetKey(KeyCode.W))
            {
                currentCamPos += Camera.main.transform.forward * NSpeed;
            }
            if (UnityInput.Current.GetKey(KeyCode.DownArrow) || UnityInput.Current.GetKey(KeyCode.S))
            {
                currentCamPos += Camera.main.transform.forward * -1f * NSpeed;
            }
            if (UnityInput.Current.GetKey(KeyCode.Space) || UnityInput.Current.GetKey(KeyCode.PageUp))
            {
                currentCamPos += Camera.main.transform.up * NSpeed;
                GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.velocity = Vector3.zero;
            }
            if (UnityInput.Current.GetKey(KeyCode.LeftControl) || UnityInput.Current.GetKey(KeyCode.PageDown))
            {
                currentCamPos += Camera.main.transform.up * -1f * NSpeed;
                GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.velocity = Vector3.zero;
            }
            if (UnityInput.Current.GetMouseButton(1))
            {
                // Lock cursor and make it invisible
                Cursor.visible = false;

                // Turn camera when right clicking and dragging mouse
                Vector3 val = UnityInput.Current.mousePosition - previousMousePosition;
                float num2 = Camera.main.transform.localEulerAngles.y + val.x * 0.3f;
                float num3 = Camera.main.transform.localEulerAngles.x - val.y * 0.3f;
                Camera.main.transform.localEulerAngles = new Vector3(num3, num2, 0f);
            }
            else
            {
                Cursor.visible = true; // Make the cursor visible again
            }
            previousMousePosition = UnityInput.Current.mousePosition;
            GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.velocity = Vector3.zero;
            Camera.main.transform.position = currentCamPos;
        }
        public static void DoKeyboardMovement(bool shouldBeDoing, float speed)
        {
            if (shouldBeDoing)
            {
                NoClip(true, true);
                AdvancedWASD(speed);
                turnOffNoclipOnce = true;
                hasTurnedOff = false;
            }
            else if (turnOffNoclipOnce)
            {
                NoClip(false);
                turnOffNoclipOnce = false;
                hasTurnedOff = true;
            }
        }
        #endregion

        #region FUN & RANDOM
        // Snipe Bug
        public static void SnipeBug()
        {
            if (Input.instance.CheckButton(Input.ButtonType.grip, false) && GameObject.Find("Floating Bug Holdable").GetComponent<ThrowableBug>().IsGrabbable())
            {
                GorillaLocomotion.Player.Instance.rightControllerTransform.gameObject.transform.position = GameObject.Find("Floating Bug Holdable").transform.position;
            }
        }

        // Snipe Bat
        public static void SnipeBat()
        {
            if (Input.instance.CheckButton(Input.ButtonType.grip, false) && GameObject.Find("Cave Bat Holdable").GetComponent<ThrowableBug>().IsGrabbable())
            {
                GorillaLocomotion.Player.Instance.rightControllerTransform.gameObject.transform.position = GameObject.Find("Cave Bat Holdable").transform.position;
            }
        }

        // Platform Gun
        static float platformSpawnDelay = 0f;
        public static void PlatformGun()
        {
            RaycastHit hit = GorillaExtensions.GunTemplate();
            if (Input.instance.CheckButton(Input.ButtonType.trigger, false) && platformSpawnDelay < Time.time)
            {
                // create platform
                GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.AddComponent<Menu.ColorChanger>();
                platform.AddComponent<GorillaSurfaceOverride>().overrideIndex = 0;
                platform.transform.localScale = new Vector3(0.001f, 0.2f, 0.2f);
                platform.transform.position = hit.point;
                GameObject.Destroy(platform, 1); // destroy platform after 1 second timer
                platformSpawnDelay = Time.time + 0.1f; // add time to the timer
            }
        }

        // Click Buttons Gun
        public static void ClickButtonsGun()
        {
            RaycastHit hit = GorillaExtensions.GunTemplate(); 
            if (Input.instance.CheckButton(Input.ButtonType.trigger))
            {
                GorillaTagger.Instance.leftHandTriggerCollider.transform.position = hit.point;
            }
        }

        // Control Bug
        public static void ControlBug()
        {
            RaycastHit hit = GorillaExtensions.GunTemplate();
            if (Input.instance.CheckButton(Input.ButtonType.trigger, false))
            {
                if (GameObject.Find("Floating Bug Holdable").GetComponent<ThrowableBug>().ownerRig == GorillaTagger.Instance.offlineVRRig)
                {
                    GameObject.Find("Floating Bug Holdable").transform.position = hit.point;
                }
            }
        }

        // Sound Spam
        static float playSoundCooldwon = 0f;
        public static void SoundSpam()
        {
            if (playSoundCooldwon < Time.time)
            {
                playSoundCooldwon = Time.time + 0.2f;
                GorillaTagger.Instance.myVRRig.RPC("PlayHandTap", RpcTarget.All, new object[]
                {
                    UnityEngine.Random.Range(0, 254),
                    false,
                    1000f
                });
            }
        }

        // Accept Legal Agreeement
        public static void AcceptTOS()
        {
            GameObject.Find("MiscellaneousScripts/LegalAgreementCheck/LegalAgreements").GetComponent<LegalAgreements>().testFaceButtonPress = true;
        }
        #endregion
    }

    /// <summary>  
    /// This class contains all mod templates, such as: Gun-Template, and Teleport Collider.
    /// </summary>
    public class GorillaExtensions
    {
        // Gun template, can be used for all types of gun-mods)
        static GameObject gunPointer;
        static GameObject gunLine;
        static LineRenderer lineRenderer;

        public static RaycastHit GunTemplate(bool destroyPointer = false, bool controlledFromPC = false, bool vibrateOnTarget = true)
        {
            if (destroyPointer)
            {
                UnityEngine.Object.Destroy(gunPointer);
                UnityEngine.Object.Destroy(gunLine);
                UnityEngine.Object.Destroy(lineRenderer);
                return new RaycastHit();
            }

            RaycastHit hit;
            if (Input.instance.CheckButton(Input.ButtonType.grip, false))
            {
                if (controlledFromPC || Update.AllGunsOnPC)
                {
                    LayerMask combinedLayerMask = GorillaLocomotion.Player.Instance.locomotionEnabledLayers | 16384;
                    Physics.Raycast(Update.thirdPersonCameraGO.GetComponent<Camera>().ScreenPointToRay(UnityInput.Current.mousePosition), out hit, float.PositiveInfinity, combinedLayerMask);
                }
                else
                {
                    LayerMask combinedLayerMask = GorillaLocomotion.Player.Instance.locomotionEnabledLayers | 16384;
                    Physics.Raycast(GorillaLocomotion.Player.Instance.rightControllerTransform.position, -GorillaLocomotion.Player.Instance.rightControllerTransform.up, out hit, float.PositiveInfinity, combinedLayerMask);
                }
                if (gunPointer == null)
                {
                    gunPointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    UnityEngine.Object.Destroy(gunPointer.GetComponent<Collider>());
                    UnityEngine.Object.Destroy(gunPointer.GetComponent<Rigidbody>());
                    gunPointer.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    Menu.ColorChanger colorChanger = gunPointer.AddComponent<Menu.ColorChanger>();
                    colorChanger.Color1 = new Color32(85, 15, 150, 1);
                    colorChanger.Color2 = new Color32(125, 15, 200, 1);
                    colorChanger.shader = Shader.Find("GUI/Text Shader");

                    gunLine = new GameObject("Line");
                    lineRenderer = gunLine.AddComponent<LineRenderer>();
                    lineRenderer.material.shader = Shader.Find("GUI/Text Shader");
                    lineRenderer.startWidth = 0.025f;
                    lineRenderer.endWidth = 0.025f;
                    lineRenderer.startColor = colorChanger.Color1;
                    lineRenderer.endColor = colorChanger.Color1;
                    lineRenderer.SetPosition(0, GorillaTagger.Instance.rightHandTransform.position);
                    lineRenderer.SetPosition(1, hit.point);

                    gunPointer.transform.position = hit.point;
                    return hit;
                }
                lineRenderer.startColor = Color.black;
                lineRenderer.endColor = gunPointer.GetComponent<Renderer>().material.color;
                lineRenderer.SetPosition(0, GorillaTagger.Instance.rightHandTransform.position);
                lineRenderer.SetPosition(1, gunPointer.transform.position);

                if (vibrateOnTarget && hit.collider.GetComponentInParent<VRRig>() != null && !Update.AllGunsOnPC) GorillaTagger.Instance.DoVibration(UnityEngine.XR.XRNode.RightHand, 0.1f, 0.01f);

                gunPointer.transform.position = hit.point;
                return hit;
            }

            UnityEngine.Object.Destroy(gunPointer);
            gunPointer = null;
            UnityEngine.Object.Destroy(gunLine);
            gunLine = null;
            UnityEngine.Object.Destroy(lineRenderer);
            lineRenderer = null;
            Physics.Raycast(new Vector3(0, 0, 0), new Vector3(0, -1, 0), out hit);

            return hit;
        }

        // Can be added to gameobjects to make them teleport you when colliding
        public class TeleportCollider : MonoBehaviour
        {
            public bool ride;
            public Vector3 velocity;

            public void LateUpdate()
            {
                // cancel script if the projectile is not yours
                if (base.GetComponent<SlingshotProjectile>().projectileOwner != PhotonNetwork.LocalPlayer) return;

                // ride projectile
                if (ride)
                {
                    GorillaPatches.TeleportPatch.Teleport(base.transform.position);
                    velocity = new Vector3(base.GetComponent<Rigidbody>().velocity.x, 0, base.GetComponent<Rigidbody>().velocity.z);
                }
            }

            public void OnCollisionEnter(Collision collision)
            {
                // cancel script if the projectile is not yours
                if (base.GetComponent<SlingshotProjectile>().projectileOwner != PhotonNetwork.LocalPlayer) return;

                // get player velocity if sudden teleport
                if (!ride) velocity = GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.velocity;

                // final teleport
                if (!ride) GorillaPatches.TeleportPatch.Teleport(base.transform.position);

                // set new velocity and destroy projectile
                GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody.velocity = velocity;
                Destroy(base.GetComponent<TeleportCollider>());
            }
        }

        // Platform Network
        /*public static void SendNetworkedPlatform(GameObject platform, bool left, bool sticky)
        {
            if (platform != null)
            {
                if (left)
                {
                    object[] eventContent = new object[]
                    {
                        platform.transform.position,
                        platform.transform.rotation
                    };
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions
                    {
                        Receivers = ReceiverGroup.Others
                    };
                    PhotonNetwork.RaiseEvent(69, eventContent, raiseEventOptions, SendOptions.SendReliable);
                }
                else
                {
                    object[] eventContent = new object[]
                    {
                        platform.transform.position,
                        platform.transform.rotation
                    };
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions
                    {
                        Receivers = ReceiverGroup.Others
                    };
                    PhotonNetwork.RaiseEvent(70, eventContent, raiseEventOptions, SendOptions.SendReliable);
                }
            }
        }

        public static void DeleteNetworkedPlatform(GameObject platform, bool left)
        {
            if (platform != null)
            {
                if (left)
                {
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions
                    {
                        Receivers = ReceiverGroup.Others
                    };
                    PhotonNetwork.RaiseEvent(71, null, raiseEventOptions, SendOptions.SendReliable);
                }
                else
                {
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions
                    {
                        Receivers = ReceiverGroup.Others
                    };
                    PhotonNetwork.RaiseEvent(72, null, raiseEventOptions, SendOptions.SendReliable);
                }
            }
        }

        private static Dictionary<int, GameObject> leftPlatforms = new Dictionary<int, GameObject>();
        private static Dictionary<int, GameObject> rightPlatforms = new Dictionary<int, GameObject>();
        public static void PlatformNetwork(EventData eventData)
        {
            switch (eventData.Code)
            {
                case 69:
                    object[] data1 = (object[])eventData.CustomData; // Get platformdata
                    leftPlatforms[eventData.Sender] = GameObject.CreatePrimitive(PrimitiveType.Cube); // Create platform from platformdata
                    GameObject.Destroy(leftPlatforms[eventData.Sender].GetComponent<Rigidbody>());
                    leftPlatforms[eventData.Sender].AddComponent<Menu.ColorChanger>();
                    leftPlatforms[eventData.Sender].AddComponent<GorillaSurfaceOverride>().overrideIndex = 0;
                    leftPlatforms[eventData.Sender].transform.localScale = new Vector3(0.001f, 0.2f, 0.2f);
                    leftPlatforms[eventData.Sender].transform.position = (Vector3)data1[0];
                    leftPlatforms[eventData.Sender].transform.rotation = (Quaternion)data1[1];
                    break;
                case 70:
                    object[] data2 = (object[])eventData.CustomData;
                    rightPlatforms[eventData.Sender] = GameObject.CreatePrimitive(PrimitiveType.Cube); // Create platform from platformdata
                    GameObject.Destroy(rightPlatforms[eventData.Sender].GetComponent<Rigidbody>());
                    rightPlatforms[eventData.Sender].AddComponent<Menu.ColorChanger>();
                    rightPlatforms[eventData.Sender].AddComponent<GorillaSurfaceOverride>().overrideIndex = 0;
                    rightPlatforms[eventData.Sender].transform.localScale = new Vector3(0.001f, 0.2f, 0.2f);
                    rightPlatforms[eventData.Sender].transform.position = (Vector3)data2[0];
                    rightPlatforms[eventData.Sender].transform.rotation = (Quaternion)data2[1];
                    break;
                case 71:
                    GameObject.Destroy(leftPlatforms[eventData.Sender]);
                    leftPlatforms[eventData.Sender] = null;
                    break;
                case 72:
                    GameObject.Destroy(rightPlatforms[eventData.Sender]);
                    rightPlatforms[eventData.Sender] = null;
                    break;
            }
        }*/

        // Report Network
        public static void ReportNetwork(EventData eventData)
        {
            if (eventData.Code == 50)
            {
                object[] array = (object[])eventData.CustomData;
                if ((string)array[0] == PhotonNetwork.LocalPlayer.UserId)
                {
                    if (Update.DisconnectOnReport) PhotonNetwork.Disconnect();
                    Update.reportCount++;
                    NotifiLib.SendNotification($"Your report count is at {Update.reportCount} now", true);
                }
            }
        }

        // Add this to a gameobject to make a specific Action invoke when it's destroyed
        public class InvokeOnDestroy : MonoBehaviour
        {
            public Action action;

            public void OnDestroy()
            {
                action.Invoke();
            }
        }
    }

    /// <summary>
    /// This class contains all mod-related harmonypatches, such as:
    /// AntiCheat Patches
    /// Set-Color fixer
    /// </summary>
    class GorillaPatches
    {
        [HarmonyPatch(typeof(GorillaLocomotion.Player), "AntiTeleportTechnology")]
        internal class AntiTeleportPatch
        {
            private static bool Prefix(GorillaLocomotion.Player __instance)
            {
                return false;
            }
        }

        // Makes teleporting possible, and easy
        [HarmonyPatch(typeof(GorillaLocomotion.Player), "LateUpdate")]
        internal class TeleportPatch
        {
            private static bool teleporting;
            private static Vector3 destination;
            private static bool teleportOnce;

            internal static bool Prefix(GorillaLocomotion.Player __instance, ref Vector3 ___lastPosition, ref Vector3[] ___velocityHistory, ref Vector3 ___lastHeadPosition, ref Vector3 ___lastLeftHandPosition, ref Vector3 ___lastRightHandPosition, ref Vector3 ___currentVelocity, ref Vector3 ___denormalizedVelocityAverage)
            {
                if (teleporting)
                {
                    Vector3 place = destination - __instance.bodyCollider.transform.position + __instance.transform.position;

                    try
                    {
                        __instance.bodyCollider.attachedRigidbody.velocity = Vector3.zero;
                        __instance.bodyCollider.attachedRigidbody.isKinematic = true;

                        ___velocityHistory = new Vector3[__instance.velocityHistorySize];
                        ___currentVelocity = Vector3.zero;
                        ___denormalizedVelocityAverage = Vector3.zero;

                        ___lastRightHandPosition = place;
                        ___lastLeftHandPosition = place;
                        ___lastHeadPosition = place;
                        __instance.transform.position = place;
                        ___lastPosition = place;

                        __instance.bodyCollider.attachedRigidbody.isKinematic = false;
                        __instance.bodyCollider.attachedRigidbody.velocity = Vector3.zero;
                    }
                    catch { }

                    teleporting = false;
                }

                return true;
            }

            internal static void Teleport(Vector3 TeleportDestination)
            {
                teleporting = true;
                destination = TeleportDestination;
            }

            internal static void TeleportOnce(Vector3 TeleportDestination, bool stateDepender)
            {
                if (stateDepender)
                {
                    if (!teleportOnce)
                    {
                        teleporting = true;
                        destination = TeleportDestination;
                    }
                    teleportOnce = true;
                }
                else
                {
                    teleportOnce = false;
                }
            }
        }

        // Patches VRRig.OnDisable() because it causes ban
        static GameObject rigWhenDisabled;
        public static bool createRig = true;
        [HarmonyPatch(typeof(VRRig), "OnDisable")]
        internal class OnDisablePatch
        {
            public static bool DisableCreateRig()
            {
                createRig = !createRig;
                return createRig;
            }

            public static bool Prefix(VRRig __instance)
            {
                if (__instance == GorillaTagger.Instance.offlineVRRig)
                {
                    if (!rigWhenDisabled && createRig)
                    {
                        rigWhenDisabled = GameObject.Instantiate(GorillaTagger.Instance.offlineVRRig.gameObject);
                        rigWhenDisabled.GetComponent<VRRig>().enabled = true;
                        rigWhenDisabled.GetComponent<VRRig>().mainSkin.material.SetColor("_Color", new Color(1f, 1f, 1f, 0.3f));
                        rigWhenDisabled.GetComponent<VRRig>().mainSkin.material.shader = Shader.Find("GUI/Text Shader");
                        GorillaMods.SetHandtapVolume(0f);
                        Menu.RefreshMenu(Update.menu);
                    }
                    return false;
                }
                return true;
            }
        }

        // Patches VRRig.OnEnable() because i need it for mods
        [HarmonyPatch(typeof(VRRig), "OnEnable")]
        internal class OnEnablePatch
        {
            public static void Prefix(VRRig __instance)
            {
                if (__instance == GorillaTagger.Instance.offlineVRRig)
                {
                    if (rigWhenDisabled)
                    {
                        GameObject.Destroy(rigWhenDisabled);
                        GorillaMods.SetHandtapVolume();
                        Menu.RefreshMenu(Update.menu);
                    }
                }
            }
        }

        // Patches SetColor because it fucks up custom gameobject-colors (i think ... don't judge, i made this script 7 months ago)
        [HarmonyPatch(typeof(Material), "SetColor", new[] { typeof(string), typeof(Color) })]
        internal class GameObjectRenderFixer
        {
            private static void Prefix(Material __instance, string name, Color value)
            {
                if (name == "_Color")
                {
                    __instance.shader = Shader.Find("GorillaTag/UberShader");
                    __instance.color = value;
                    return;
                }
            }
        }

        // Sends notifications when players join
        static Photon.Realtime.Player lastJoined;
        [HarmonyPatch(typeof(MonoBehaviourPunCallbacks), "OnPlayerEnteredRoom")]
        internal class OnPlayerJoin
        {
            public static void Prefix(ref Photon.Realtime.Player newPlayer)
            {
                if (lastJoined != newPlayer && newPlayer != PhotonNetwork.LocalPlayer && newPlayer.NickName.Length < 21 || newPlayer.UserId == "D00E9BA5BE32DC84")
                {
                    lastJoined = newPlayer;
                    NotifiLib.SendNotification($"[<color=yellow>SERVER</color>] Player {newPlayer.NickName} joined");
                }
            }
        }
        static Photon.Realtime.Player lastLeft;
        [HarmonyPatch(typeof(MonoBehaviourPunCallbacks), "OnPlayerLeftRoom")]
        internal class OnPlayerLeave
        {
            public static void Prefix(Photon.Realtime.Player otherPlayer)
            {
                if (lastLeft != otherPlayer && otherPlayer != PhotonNetwork.LocalPlayer && otherPlayer.NickName.Length < 21 || otherPlayer.UserId == "D00E9BA5BE32DC84")
                {
                    lastLeft = otherPlayer;
                    NotifiLib.SendNotification($"[<color=yellow>SERVER</color>] Player {otherPlayer.NickName} left");
                }
            }
        }

        // Anticheat Patches
        static float timer = 0;
        static string lastReport;
        [HarmonyPatch(typeof(GorillaNot), "SendReport")]
        internal class OnGorillaNotReport
        {
            public static bool Prefix(ref string susReason, ref string susId, ref string susNick)
            {
                if (susId == PhotonNetwork.LocalPlayer.UserId)
                {
                    if (lastReport != susReason || timer < Time.time)
                    {
                        NotifiLib.SendNotification($"[<color=red>ANTICHEAT</color>] The menu blocked an AC-Report: {susReason}");
                        lastReport = susReason;
                        timer = Time.time + 0.3f;
                    }
                    if (GorillaMods.acDisconnect)
                    {
                        PhotonNetwork.Disconnect();
                    }
                    return false;
                }
                return !RigManager.GetPlayerFromID(susId).CustomProperties["mods"].ToString().Contains("aspect.cheat.panel");
            }
        }
        [HarmonyPatch(typeof(GorillaNetworkPublicTestJoin2), "LateUpdate")]
        internal class GraceperiodPatch1
        {
            public static bool Prefix()
            {
                return false;
            }
        }
        [HarmonyPatch(typeof(GorillaNetworkPublicTestsJoin), "LateUpdate")]
        internal class GraceperiodPatch2
        {
            public static bool Prefix()
            {
                return false;
            }
        }
    }
}