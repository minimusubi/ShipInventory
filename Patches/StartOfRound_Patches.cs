﻿using System.Linq;
using HarmonyLib;
using ShipInventory.Helpers;
using ShipInventory.Objects;
using UnityEngine;

namespace ShipInventory.Patches;

[HarmonyPatch(typeof(StartOfRound))]
internal class StartOfRound_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(StartOfRound.Start))]
    [HarmonyPriority(600)]
    private static void SetupVent(StartOfRound __instance)
    {
        GameObject? vent = null;
        
        // Spawn vent if server
        if (__instance.IsServer || __instance.IsHost)
        {
            var prefab = NetworkPrefabUtils.GetPrefab(Constants.VENT_PREFAB);
            
            if (prefab is null)
                return;
        
            vent = Object.Instantiate(prefab);
            
            var chuteServer = vent.GetComponent<ChuteInteract>();
            chuteServer.NetworkObject.Spawn();
            chuteServer.NetworkObject.TrySetParent(GameObject.Find(Constants.SHIP_PATH));
        }
        
        vent ??= Object.FindAnyObjectByType<ChuteInteract>().gameObject;
        
        if (vent == null)
            return;
        
        var chute = vent.GetComponent<ChuteInteract>();
        var interact = chute.GetComponent<InteractTrigger>();
        interact.onInteract.AddListener(chute.StoreHeldItem);
        interact.timeToHold = 0.5f;

        vent.transform.localPosition = new Vector3(1.9f, 1f, -4.45f);
        vent.transform.localRotation = Quaternion.Euler(35, 0, 0);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(nameof(StartOfRound.LoadShipGrabbableItems))]
    private static void LoadStoredItems()
    {
        // If key missing, skip
        if (!ES3.KeyExists(Constants.STORED_ITEMS, GameNetworkManager.Instance.currentSaveFileName))
            return;
        
        ChuteInteract.SetItems(
            ES3.Load<ItemData[]>(Constants.STORED_ITEMS, GameNetworkManager.Instance.currentSaveFileName).ToList()
        );
    }
}