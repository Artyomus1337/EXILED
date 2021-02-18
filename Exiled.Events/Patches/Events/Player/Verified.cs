// -----------------------------------------------------------------------
// <copyright file="Verified.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Events.Player
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using Exiled.API.Features;
    using Exiled.Events.EventArgs;
    using Exiled.Events.Utils;
    using HarmonyLib;
    using MEC;
    using PlayerAPI = Exiled.API.Features.Player;
    using PlayerEvents = Exiled.Events.Handlers.Player;

#pragma warning disable SA1600 // Elements should be documented

    [HarmonyPatch(typeof(ServerRoles), nameof(ServerRoles.CallCmdServerSignatureComplete))]
    internal static class Verified
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var targetMethod = AccessTools.Method(typeof(ServerRoles), nameof(ServerRoles.RefreshPermissions));
            var did = false;

            using (var nextEnumerator = new NextEnumerator<CodeInstruction>(instructions.GetEnumerator()))
            {
                while (nextEnumerator.MoveNext())
                {
                    if (!did
                        && nextEnumerator.Current.opcode == OpCodes.Ldc_I4_0
                        && nextEnumerator.NextCurrent != null && nextEnumerator.NextCurrent.opcode == OpCodes.Call && (MethodInfo)nextEnumerator.NextCurrent.operand == targetMethod)
                    {
                        did = true;

                        // Think I wanna have a deal with IL?
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Verified), nameof(CallEvent)));
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                    }

                    yield return nextEnumerator.Current;
                    if (nextEnumerator.NextCurrent != null)
                        yield return nextEnumerator.NextCurrent;
                }
            }
        }

        private static void CallEvent(ServerRoles instance)
        {
            try
            {
                Player player = new PlayerAPI(instance._hub);
                PlayerAPI.Dictionary.Add(instance._hub.gameObject, player);

                Player p = player;
                Timing.CallDelayed(0.25f, () =>
                {
                    if (p.IsMuted)
                        p.ReferenceHub.characterClassManager.SetDirtyBit(2UL);
                });

                player.IsVerified = true;

                Log.SendRaw($"Player {player.Nickname} ({player.UserId}) ({player.Id}) connected with the IP: {player.IPAddress}", ConsoleColor.Green);

                PlayerEvents.OnVerified(new VerifiedEventArgs(player));
            }
            catch (Exception ex)
            {
                Log.Error($"{typeof(Verified).FullName}.{nameof(CallEvent)}:\n{ex}");
            }
        }
    }
}