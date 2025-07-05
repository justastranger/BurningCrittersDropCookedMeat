using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BurningCrittersDropCookedMeat
{
    [BepInPlugin("jas.Dinkum.BurningCrittersDropCookedMeat", "Burning Critters Drop Cooked Meat", "1.0.0")]
    public class BurningCrittersDropCookedMeat : BaseUnityPlugin
    {
        public static BurningCrittersDropCookedMeat instance;
        public static new ManualLogSource Logger;
        internal static readonly Dictionary<int, int> rawToCookedMeats = new Dictionary<int, int>
        {   // meat,     drumstick, giant drumstick, croco meat,    flake,      prime meat,     grub meat
            { 21, 19 }, { 308, 310 }, { 584, 646 }, { 648, 647 }, { 772, 773 }, { 853, 852 }, { 1168, 1169 }
        };
        internal static readonly MethodInfo DropGuaranteedDrops = AccessTools.Method(typeof(Damageable), "DropGuaranteedDrops");
        internal static readonly FieldInfo myAnimalAi = AccessTools.Field(typeof(Damageable), "myAnimalAi");

        internal static readonly Harmony harmony = new Harmony("jas.Dinkum.BurningCrittersDropCookedMeat");

        public void Awake()
        {
            instance = this;
            Logger = base.Logger;
            Logger.LogInfo("Mod jas.Dinkum.BurningCrittersDropCookedMeat is loaded!");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(Damageable))]
    public class DamageablePatch
    {

        [HarmonyPatch(typeof(Damageable), "DropGuaranteedDrops")]
        [HarmonyPrefix]
        public static bool DropGuaranteedDrops_Prefix(Damageable __instance)
        {
            BurningCrittersDropCookedMeat.Logger.LogMessage("DropGuaranteedDrops_Prefix");
            // if the critter is on fire at the moment of death, take over the drops
            if (__instance.onFire)
            {
                // return early if there's nothing for us to do
                if (!__instance.isAnAnimal() || !__instance.guaranteedDrops)
                {

                    return false;
                }
                InventoryItem randomDropFromTable = __instance.guaranteedDrops.getRandomDropFromTable();
                if (randomDropFromTable)
                {
                    int XpType = 5;
                    if (!randomDropFromTable.hasFuel)
                    {
                        // could be food, so we have to check
                        int itemId = Inventory.Instance.getInvItemId(randomDropFromTable);
                        if (BurningCrittersDropCookedMeat.rawToCookedMeats.ContainsKey(itemId))
                        {
                            NetworkMapSharer.Instance.spawnAServerDrop(BurningCrittersDropCookedMeat.rawToCookedMeats[itemId], 1, __instance.transform.position, null, true, XpType);
                        }
                        else
                        {
                            NetworkMapSharer.Instance.spawnAServerDrop(itemId, 1, __instance.transform.position, null, true, XpType);
                        }
                    }
                    else
                    {
                        // has a fuel/energy bar, so obviously not food that can be cooked
                        NetworkMapSharer.Instance.spawnAServerDrop(Inventory.Instance.getInvItemId(randomDropFromTable), randomDropFromTable.fuelMax, __instance.transform.position, null, tryNotToStack: true, XpType);
                    }
                }
                // cancel original function if we successfully replaced the meat
                return false;
            }
            else
            {
                // Allow the original function to run if not on fire
                return true;
            }
        }

        [HarmonyPatch(typeof(Damageable), "disapearAfterDeathAnimation")]
        [HarmonyPrefix]
        public static bool disapearAfterDeathAnimation_Prefix(Damageable __instance)
        {
            BurningCrittersDropCookedMeat.Logger.LogMessage("disapearAfterDeathAnimation_Prefix");
            // if it's not an animal, skip to the normal function
            if (!__instance.isAnAnimal())
            {
                return true;
            }
            // only do work if animal is on fire
            if (__instance.onFire)
            {
                SoundManager.Instance.playASoundAtPoint(SoundManager.Instance.animalDiesSound, __instance.transform.position);
                if (!__instance.isServer)
                {
                    // clients only play death noise anyways, so cancel the original function after doing so

                    return false;
                }
                int dropCount = 1;
                // double drops from dangerous critters
                if (NetworkMapSharer.Instance.wishManager.IsWishActive(WishManager.WishType.DangerousWish))
                {
                    dropCount = 2;
                }
                for (int i = 0; i < dropCount; i++)
                {
                    // this loop allows a single critter to drop multiple items normally
                    Transform[] dropPositions = __instance.dropPositions;
                    foreach (Transform transform in dropPositions)
                    {
                        // grab our potential item
                        InventoryItem randomDropFromTable = __instance.lootDrops.getRandomDropFromTable();
                        // check if it's good to use
                        if (randomDropFromTable)
                        {
                            int xPType = 5;
                            int dropId = Inventory.Instance.getInvItemId(randomDropFromTable);
                            if (!randomDropFromTable.hasFuel)
                            {
                                // if we're dropping raw meat from the burning animal
                                if (BurningCrittersDropCookedMeat.rawToCookedMeats.ContainsKey(dropId))
                                {
                                    // swap it out with cooked meat
                                    NetworkMapSharer.Instance.spawnAServerDrop(BurningCrittersDropCookedMeat.rawToCookedMeats[dropId], 1, transform.position, null, tryNotToStack: true, xPType);
                                }
                                else
                                {
                                    // otherwise drop it normally
                                    NetworkMapSharer.Instance.spawnAServerDrop(dropId, 1, transform.position, null, tryNotToStack: true, xPType);
                                }
                            }
                            else
                            {
                                // drop item that has fuel, ignoring the potential fire
                                NetworkMapSharer.Instance.spawnAServerDrop(dropId, randomDropFromTable.fuelMax, transform.position, null, tryNotToStack: true, xPType);
                            }
                        }
                    }
                    // trigger guaranteed drops if present
                    if (__instance.guaranteedDrops)
                    {
                        BurningCrittersDropCookedMeat.DropGuaranteedDrops.Invoke(__instance, null);
                    }
                }
                // despawn the animal once everything's dropped
                NetworkNavMesh.nav.UnSpawnAnAnimal((AnimalAI)BurningCrittersDropCookedMeat.myAnimalAi.GetValue(__instance), false);
                // cancel original function
                return false;
            }
            else
            {
                // non-burning animals pass through to original function
                return true;
            }
        }
    }
}
