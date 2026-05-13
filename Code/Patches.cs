using Extensions;
using HarmonyLib;
using Mirror;
using System.Collections.Generic;

namespace GWYF.WhoNeedsEyes.Code
{
    [HarmonyPatch(typeof(BodyShreddingMachine), "TryShredEye")]
    public class BodyShreddingMachine_TryShredEye_Patch
    {
        static bool Prefix(BodyShreddingMachine __instance, PlayerOrgans po, PlayerOrganData data, ref bool __result)
        {
            if (!NetworkServer.active)
            {
                __result = false;
                return false;
            }

            // Original requires BOTH eyes — change to require at least ONE
            if (!data.leftEye && !data.rightEye)
            {
                __result = false;
                return false;
            }

            // If both exist, pick randomly. If only one exists, remove that one.
            if (data.leftEye && data.rightEye)
            {
                NetworkSingleton<OrganManager>.Instance.ServerToggleOrgan(
                    po,
                    UnityEngine.Random.value > 0.5f ? OrganType.RightEye : OrganType.LeftEye,
                    isEnabled: false);
            }
            else if (data.rightEye)
            {
                NetworkSingleton<OrganManager>.Instance.ServerToggleOrgan(po, OrganType.RightEye, isEnabled: false);
            }
            else
            {
                NetworkSingleton<OrganManager>.Instance.ServerToggleOrgan(po, OrganType.LeftEye, isEnabled: false);
            }

            NetworkSingleton<MoneyManager>.Instance.TryChangeTicketBalance(
                Traverse.Create(__instance).Field<int>("_eyePrice").Value);

            Traverse.Create(__instance).Method("RpcOnEyeShredded").GetValue();

            __result = true;
            return false; // Skip original
        }
    }


    [HarmonyPatch(typeof(QuotaGun), "TryRemoveRandomOrgan")]
    public class QuotaGun_TryRemoveRandomOrgan_Patch
    {
        // Returning false skips the original method entirely
        static bool Prefix(QuotaGun __instance, PlayerController pc)
        {
            // Access private fields via Harmony's traversal helpers
            int removedOrganCount = Traverse.Create(__instance).Field<int>("_removedOrganCount").Value;
            int removableOrganCount = Traverse.Create(__instance).Field<int>("removableOrganCount").Value;

            if (removedOrganCount >= removableOrganCount
                || NetworkSingleton<GameManager>.Instance.state != GameState.Game
                || NetworkSingleton<MoneyManager>.Instance.balance >= NetworkSingleton<GameManager>.Instance.currentQuota)
            {
                return false;
            }

            PlayerOrgans component = pc.GetComponent<PlayerOrgans>();
            PlayerOrganData organData = NetworkSingleton<OrganManager>.Instance.GetOrganData(component);

            if (organData == null) return false;

            List<OrganType> list = new List<OrganType>();

            if (organData.body) list.Add(OrganType.Body);
            if (organData.mouth) list.Add(OrganType.Mouth);

            // YOUR CHANGE HERE — e.g. allow removing a single eye too:
            if (organData.leftEye || organData.rightEye)
            {
                if (organData.leftEye) list.Add(OrganType.LeftEye);
                if (organData.rightEye) list.Add(OrganType.RightEye);
            }

            if (list.Count > 0)
            {
                NetworkSingleton<OrganManager>.Instance.ServerToggleOrgan(
                    component, list.GetRandomElement(), isEnabled: false);

                removedOrganCount++;
                Traverse.Create(__instance).Field<int>("_removedOrganCount").Value = removedOrganCount;

                Traverse.Create(__instance).Method("RpcSetRemovedOrganIndicator", removedOrganCount).GetValue();
                Traverse.Create(__instance).Method("AwardQuotaFraction").GetValue();
            }

            return false;
        }
    }
}
