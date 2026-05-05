using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleCharacterSelectPlugin.Managers;

public class PoseManager
{
    private readonly IClientState clientState;
    private readonly IFramework framework;
    private readonly IChatGui chatGui;
    private readonly ICommandManager commandManager;
    private readonly IObjectTable objectTable;
    private readonly Plugin plugin;

    public PoseManager(IClientState clientState, IFramework framework, IChatGui chatGui, ICommandManager commandManager, IObjectTable objectTable, Plugin plugin)
    {
        this.clientState = clientState;
        this.framework = framework;
        this.chatGui = chatGui;
        this.commandManager = commandManager;
        this.objectTable = objectTable;
        this.plugin = plugin;

        framework.Update += OnFrameworkUpdate;
    }

    public void ApplyPose(EmoteController.PoseType type, byte index)
    {
        Plugin.Log.Debug($"[ApplyPose] Applying {type} pose {index}");

        if (index >= 7 || Plugin.ObjectTable.LocalPlayer == null)
            return;

        var characterAddress = Plugin.ObjectTable.LocalPlayer.Address;
        
        unsafe
        {
            var charPtr = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)characterAddress;

            // Apply to memory first
            PlayerState.Instance()->SelectedPoses[(int)type] = index;

            // Check pose state
            var currentState = TranslatePoseState(charPtr->ModeParam);
            if (currentState == type)
            {
                // We're in the correct state, now update the visual pose
                var currentPose = charPtr->EmoteController.CPoseState;
                
                // Command-based approach (default for sync compatibility)
                if (plugin.Configuration.UseCommandBasedPoses ?? true)
                {
                    // Use /cpose to cycle if not already at target
                    if (currentPose != index)
                    {
                        // Direct memory write for immediate effect
                        charPtr->EmoteController.CPoseState = index;
                        Plugin.Log.Debug($"[ApplyPose] Set CPoseState directly to {index} for immediate effect");
                        
                        // Command approach ensures sync plugins see the change
                        StartApplyPoseTask(type, index, characterAddress);
                    }
                    else
                    {
                        Plugin.Log.Debug($"[ApplyPose] Already at target pose {index}, no need to cycle");
                    }
                }
                else
                {
                    // Direct memory write (legacy)
                    charPtr->EmoteController.CPoseState = index;
                }
            }
            else
            {
                // Not in correct state, memory updated for later
                Plugin.Log.Debug($"[ApplyPose] Not in correct state for {type}, only updating memory");
            }
        }
    }

    private void StartApplyPoseTask(EmoteController.PoseType type, byte index, IntPtr characterAddress)
    {
        _ = Task.Run(async () => 
        {
            await ApplyPoseViaCommand(type, index, characterAddress);
        });
    }
    
    private async Task ApplyPoseViaCommand(EmoteController.PoseType type, byte targetIndex, IntPtr characterAddress)
    {
        // Brief delay for memory write to settle
        await Task.Delay(50);
        
        var maxAttempts = 8;
        var attempts = 0;
        
        // Cycle /cpose for network sync
        while (attempts < maxAttempts)
        {
            // Check state on framework thread
            var (currentPose, shouldContinue) = await framework.RunOnFrameworkThread(() =>
            {
                unsafe
                {
                    var charPtr = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)characterAddress;
                    var current = charPtr->EmoteController.CPoseState;
                    
                    if (current == targetIndex)
                    {
                        Plugin.Log.Debug($"[ApplyPoseViaCommand] Confirmed at target pose {targetIndex}");
                        return (current, false);
                    }
                    
                    Plugin.Log.Debug($"[ApplyPoseViaCommand] Executing /cpose to sync from {current} to {targetIndex}");
                    commandManager.ProcessCommand("/cpose");
                    
                    return (current, true);
                }
            });
            
            if (!shouldContinue)
                break;
            
            // Brief delay between cycles
            await Task.Delay(50);
            attempts++;
        }
        
        if (attempts >= maxAttempts)
        {
            Plugin.Log.Warning($"[ApplyPoseViaCommand] Could not sync pose to {targetIndex} after {maxAttempts} attempts");
        }
        else
        {
            Plugin.Log.Info($"[ApplyPoseViaCommand] Successfully synced pose to {targetIndex}");
        }
    }

    private void ApplyIdle(int idleIndex)
    {
        if (idleIndex == -1)
        {
            // /select idle - check current pose
            if (objectTable.LocalPlayer != null)
            {
                unsafe
                {
                    var charPtr = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)objectTable.LocalPlayer.Address;
                    var currentIdle = charPtr->EmoteController.CPoseState;
        
                    chatGui.Print($"[SCS] Current idle pose: {currentIdle} (range: 0-6)");
                }
            }
            else
            {
                chatGui.PrintError("[SCS] You must be logged in to check idle pose.");
            }
        }
        else if (idleIndex < 7)
        {
            // /select idle <0-6> - set pose
            ApplyPose(EmoteController.PoseType.Idle, Convert.ToByte(idleIndex));
            GameCommandManager.ExecuteMacro("/penumbra redraw self");
        }
        else
        {
            chatGui.PrintError("[SCS] Usage: /select idle [0-6]");
        }
    }

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        if (!plugin.Configuration.EnablePoseAutoSave || !clientState.IsLoggedIn)
            return;
        if (Plugin.ObjectTable.LocalPlayer == null)
            return;

        var charPtr = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)Plugin.ObjectTable.LocalPlayer.Address;
    }

    private EmoteController.PoseType TranslatePoseState(byte state)
    {
        return state switch
        {
            1 => EmoteController.PoseType.GroundSit,
            2 => EmoteController.PoseType.Sit,
            3 => EmoteController.PoseType.Doze,
            _ => EmoteController.PoseType.Idle
        };
    }

    public void Dispose()
    {
        framework.Update -= OnFrameworkUpdate;
    }
}
