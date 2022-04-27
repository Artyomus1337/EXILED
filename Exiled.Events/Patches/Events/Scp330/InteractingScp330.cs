// -----------------------------------------------------------------------
// <copyright file="InteractingScp330.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Events.Scp330
{
    using System;
#pragma warning disable SA1118
#pragma warning disable SA1313

    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    using CustomPlayerEffects;

    using Exiled.API.Features;
    using Exiled.Events.EventArgs;

    using Footprinting;

    using HarmonyLib;

    using Interactables.Interobjects;

    using InventorySystem;
    using InventorySystem.Items.Usables.Scp330;
    using InventorySystem.Searching;

    using NorthwoodLib.Pools;

    using UnityEngine;

    using static HarmonyLib.AccessTools;

    /// <summary>
    /// Patches the <see cref="Scp330Interobject.ServerInteract"/> method to add the <see cref="Handlers.Scp330.InteractingScp330"/> event.
    /// </summary>
    [HarmonyPatch(typeof(Scp330Interobject), nameof(Scp330Interobject.ServerInteract))]

    public static class InteractingScp330
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);

            Label returnFalse = generator.DefineLabel();
            Label continueProcessing = generator.DefineLabel();

            Label shouldSever = generator.DefineLabel();
            Label shouldNotSever = generator.DefineLabel();

            LocalBuilder eventHandler = generator.DeclareLocal(typeof(InteractingScp330EventArgs));

            LocalBuilder playerEffect = generator.DeclareLocal(typeof(PlayerEffect));

            int offset = -3;
            int index = newInstructions.FindLastIndex(instruction => instruction.Calls(Method(typeof(Scp330Bag), nameof(Scp330Bag.ServerProcessPickup)))) + offset;

            // I can confirm this works during testing
            newInstructions.InsertRange(index, new[]
            {
                // Load arg 0 (No param, instance of object) EStack[ReferenceHub Instance]
                new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(newInstructions[index]),

                // Using Owner call Player.Get static method with it (Reference hub) and get a Player back  EStack[Player ]
                new(OpCodes.Call, Method(typeof(Player), nameof(Player.Get), new[] { typeof(ReferenceHub) })),

                // Get random candy EStack[Player, Candy]
                new(OpCodes.Call, Method(typeof(Scp330Candies), nameof(Scp330Candies.GetRandom))),

                // num2 EStack[Player, Candy, num2]
                new(OpCodes.Ldloc_2),

                // EStack[Player, Candy, num2, ReferenceHub Instance]
                new(OpCodes.Ldarg_1),

                // EStack[Player, Candy, num2, characterClassManager]
                new(OpCodes.Ldfld, Field(typeof(ReferenceHub), nameof(ReferenceHub.characterClassManager))),

                // EStack[Player, Candy, num2, IsHuman]
                new(OpCodes.Callvirt, Method(typeof(CharacterClassManager), nameof(CharacterClassManager.IsHuman))),

                // Pass all 4 variables to InteractingScp330EventArgs  New Object, get a new object in return EStack[InteractingScp330EventArgs  Instance]
                new(OpCodes.Newobj, GetDeclaredConstructors(typeof(InteractingScp330EventArgs))[0]),

                 // Copy it for later use again EStack[InteractingScp330EventArgs Instance, InteractingScp330EventArgs Instance]
                new(OpCodes.Dup),

                // EStack[InteractingScp330EventArgs Instance]
                new(OpCodes.Stloc, eventHandler.LocalIndex),

                // EStack[InteractingScp330EventArgs Instance, InteractingScp330EventArgs Instance]
                new(OpCodes.Ldloc, eventHandler.LocalIndex),

                // Call Method on Instance EStack[InteractingScp330EventArgs Instance] (pops off so that's why we needed to dup)
                new(OpCodes.Call, Method(typeof(Handlers.Scp330), nameof(Handlers.Scp330.OnInteractingScp330))),

                // Call its instance field (get; set; so property getter instead of field) EStack[IsAllowed]
                new(OpCodes.Callvirt, PropertyGetter(typeof(InteractingScp330EventArgs), nameof(InteractingScp330EventArgs.IsAllowed))),

                // If isAllowed = 1, jump to continue route, otherwise, return occurs below EStack[]
                new(OpCodes.Brtrue, continueProcessing),

                // False Route
                new CodeInstruction(OpCodes.Ret).WithLabels(returnFalse),

                // Good route of is allowed being true 
                new CodeInstruction(OpCodes.Nop).WithLabels(continueProcessing),
            });





            int removeServerProcessOffset = -2;
            int removeServerProcessIndex = newInstructions.FindLastIndex(instruction => instruction.Calls(Method(typeof(Scp330Bag), nameof(Scp330Bag.ServerProcessPickup)))) + removeServerProcessOffset;

            newInstructions.RemoveRange(removeServerProcessIndex, 3);

            Label ignoreOverlay = generator.DefineLabel();

            LocalBuilder scp330Bag = generator.DeclareLocal(typeof(Scp330Bag));

            newInstructions.InsertRange(removeServerProcessIndex, new[]
            {
                // EStack [Referencehub, InteractingScp330EventArgs]
                new CodeInstruction(OpCodes.Ldloc, eventHandler),

                // EStack [Referencehub, Candy]
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(InteractingScp330EventArgs), nameof(InteractingScp330EventArgs.Candy))),

                // EStack [Referencehub, Candy, Scp330Pickup Address]


                new CodeInstruction(OpCodes.Ldloca_S, scp330Bag.LocalIndex),

                //new CodeInstruction(OpCodes.Pop),
                //new CodeInstruction(OpCodes.Pop),
                //new CodeInstruction(OpCodes.Pop),
                //new CodeInstruction(OpCodes.Ldc_I4_1),

                // EStack []

                new CodeInstruction(OpCodes.Call, Method(typeof(InteractingScp330), nameof(InteractingScp330.ServerProcessPickup), new[] {typeof(ReferenceHub), typeof(CandyKindID), typeof(Scp330Bag)})),

                new CodeInstruction(OpCodes.Brtrue, ignoreOverlay),



                new CodeInstruction(OpCodes.Nop).WithLabels(ignoreOverlay),
            });

            //Log.Info($"removeServerProcessIndex {removeServerProcessIndex}");

            //newInstructions.RemoveAt(newInstructions.Count - 2);
            //newInstructions.RemoveAt(newInstructions.Count - 2);
            //newInstructions.RemoveAt(newInstructions.Count - 2);
            //newInstructions.RemoveAt(newInstructions.Count - 2);
            //newInstructions.add(newInstructions.Count - 2);


            int addShouldSeverOffset = 1;
            int addShouldSeverIndex = newInstructions.FindLastIndex(instruction => instruction.Calls(Method(typeof(Scp330Interobject), nameof(Scp330Interobject.RpcMakeSound)))) + addShouldSeverOffset;

            int includeSameLine = 1;
            int nextReturn = newInstructions.FindIndex(addShouldSeverIndex, instruction => instruction.opcode == OpCodes.Ret) + includeSameLine;

            newInstructions.RemoveRange(addShouldSeverIndex, nextReturn - addShouldSeverIndex); //nextReturn - overwriteIndex, get rid of blt.s, 3 , 14

            addShouldSeverIndex = newInstructions.FindLastIndex(instruction => instruction.Calls(Method(typeof(Scp330Interobject), nameof(Scp330Interobject.RpcMakeSound)))) + addShouldSeverOffset;

            newInstructions.InsertRange(addShouldSeverIndex, new[]
            {
                // Load local ev object we stored before EStack[InteractingScp330EventArgs Instance]
                new CodeInstruction(OpCodes.Ldloc, eventHandler.LocalIndex),

                // Get field shouldsever EStack[ShouldSever]
                new (OpCodes.Callvirt, PropertyGetter(typeof(InteractingScp330EventArgs), nameof(InteractingScp330EventArgs.ShouldSever))),

                // IF we should sever, continue, otherwise branch EStack[]
                new (OpCodes.Brfalse, shouldNotSever),

                // Load reference hub EStack[Referencehub]
                new CodeInstruction(OpCodes.Ldarg_1),

                // Load playereffects EStack[playerEffectsController]
                new CodeInstruction(OpCodes.Ldfld, Field(typeof(ReferenceHub), nameof(ReferenceHub.playerEffectsController))),

                // Load SeveredHands string EStack[playerEffectsController, "SeveredHands"]
                new CodeInstruction(OpCodes.Ldstr, nameof(SeveredHands)),

                // Load duration value EStack[playerEffectsController, "SeveredHands", 0f]
                new CodeInstruction(OpCodes.Ldc_R4, 0f),

                // Load increase duration if exists value EStack[playerEffectsController, "SeveredHands", 0f, 0]
                new CodeInstruction(OpCodes.Ldc_I4_0),

                // Call our method to force SeveredHands effect EStack[bool]
                new CodeInstruction(OpCodes.Callvirt, Method(typeof(PlayerEffectsController), nameof(PlayerEffectsController.EnableByString), new[] { typeof(string), typeof(float), typeof(bool) })),

                // Remove success result EStack[]
                new CodeInstruction(OpCodes.Pop),

                // Return
                new CodeInstruction(OpCodes.Ret),
            });

            // This introduces bug, need to wipe player after they die, do mec call after 5 seconds, tbh.

            int addTakenCandiesOffset = -1;

            int addTakenCandiesIndex = newInstructions.FindLastIndex(instruction => instruction.LoadsField(Field(typeof(Scp330Interobject), nameof(Scp330Interobject._takenCandies)))) + addTakenCandiesOffset;

            // This is a jump to ensure we can escape original NW logic without deleting the original code. Might be better to just delete it. Will defer to Joker/Nao.
            newInstructions.InsertRange(addTakenCandiesIndex, new[]
            {
                new CodeInstruction(OpCodes.Nop).WithLabels(shouldNotSever).MoveLabelsFrom(newInstructions[addTakenCandiesIndex]),
            });

            for (int z = 0; z < newInstructions.Count; z++)
            {
                yield return newInstructions[z];
            }

            Log.Info($" Index {index} ");

            int count = 0;
            int il_pos = 0;
            foreach (CodeInstruction instr in newInstructions)
            {
                Log.Info($"Current op code: {instr.opcode} and index {count} and {instr.operand} and {il_pos} and {instr.opcode.OperandType}");
                il_pos += instr.opcode.Size;
                count++;
            }

            ListPool<CodeInstruction>.Shared.Return(newInstructions);
        }

        private static bool ServerProcessPickup(ReferenceHub ply, CandyKindID candy, out Scp330Bag bag)
        {
            if (!Scp330Bag.TryGetBag(ply, out bag))
            {
                return ply.inventory.ServerAddItem(ItemType.SCP330, ushort.MinValue) != null;
            }

            bool result = bag.TryAddSpecific(candy);

            if (bag.AcquisitionAlreadyReceived)
            {
                bag.ServerRefreshBag();
            }

            return result;
        }
    }
}
