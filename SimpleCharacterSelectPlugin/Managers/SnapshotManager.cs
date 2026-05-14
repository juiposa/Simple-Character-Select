namespace SimpleCharacterSelectPlugin.Managers;

public class SnapshotManager
{
    // TODO I don't know if I'm keeping this
    // private void DrawSnapshotDialog(float scale)
    //     {
    //         if (!isSnapshotDialogOpen)
    //             return;
    //
    //         // Force window size to fit content without scrolling
    //         ImGui.SetNextWindowSize(new Vector2(500 * scale, 400 * scale), ImGuiCond.Always);
    //         ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f));
    //
    //         bool isOpen = true;
    //         if (ImGui.Begin("Create Design from Current Look", ref isOpen, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
    //         {
    //             if (snapshotTargetCharacter == null)
    //             {
    //                 ImGui.Text("Error: No character selected");
    //                 ImGui.End();
    //                 isSnapshotDialogOpen = false;
    //                 return;
    //             }
    //
    //             // Apply simple dialog styling
    //
    //             // Header with icon and styling
    //             ImGui.PushFont(UiBuilder.IconFont);
    //             ImGui.TextColored(new Vector4(0.6f, 0.8f, 1.0f, 1.0f), "\uf030");
    //             ImGui.PopFont();
    //             ImGui.SameLine();
    //             ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.9f, 1.0f), "Snapshot Current Character State");
    //             
    //             // Subtle styled separator
    //             ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.4f, 0.6f, 0.8f, 0.5f));
    //             ImGui.Separator();
    //             ImGui.PopStyleColor();
    //             ImGui.Spacing();
    //
    //             // Design name input with improved styling
    //             ImGui.TextColored(new Vector4(0.8f, 0.9f, 1.0f, 1.0f), "Design Name:");
    //             ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.1f, 0.15f, 0.2f, 0.8f));
    //             ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.15f, 0.2f, 0.25f, 0.9f));
    //             ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.2f, 0.25f, 0.3f, 1.0f));
    //             ImGui.SetNextItemWidth(-1);
    //             ImGui.InputText("##SnapshotName", ref snapshotDesignName, 256);
    //             ImGui.PopStyleColor(3);
    //             ImGui.Spacing();
    //
    //             // Styled section header
    //             ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.4f, 0.6f, 0.8f, 0.5f));
    //             ImGui.Separator();
    //             ImGui.PopStyleColor();
    //             ImGui.Spacing();
    //
    //             // Auto-detection status with improved layout
    //             ImGui.TextColored(new Vector4(0.8f, 0.9f, 1.0f, 1.0f), "Auto-Detection Status:");
    //             ImGui.Spacing();
    //
    //             // Create a child region for detection status to control layout better
    //             ImGui.BeginChild("DetectionStatus", new Vector2(0, 90 * scale), false);
    //
    //             // Glamourer detection with icon
    //             ImGui.PushFont(UiBuilder.IconFont);
    //             ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "\uf013");
    //             ImGui.PopFont();
    //             ImGui.SameLine();
    //             ImGui.Text("Glamourer State:");
    //             ImGui.SameLine();
    //             
    //             float statusPosX = ImGui.GetContentRegionAvail().X - 80 * scale;
    //             ImGui.SetCursorPosX(ImGui.GetCursorPosX() + statusPosX);
    //             
    //             if (snapshotDetectedMods.Count > 0)
    //             {
    //                 ImGui.TextColored(new Vector4(0.3f, 0.8f, 0.3f, 1.0f), "Detected");
    //             }
    //             else if (snapshotIsProcessing)
    //             {
    //                 ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.3f, 1.0f), "Detecting...");
    //             }
    //             else
    //             {
    //                 ImGui.TextColored(new Vector4(0.8f, 0.3f, 0.3f, 1.0f), "None");
    //             }
    //
    //             // Customize+ detection with icon
    //             ImGui.PushFont(UiBuilder.IconFont);
    //             ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "\uf007");
    //             ImGui.PopFont();
    //             ImGui.SameLine();
    //             ImGui.Text("Customize+ Profile:");
    //             ImGui.SameLine();
    //             
    //             ImGui.SetCursorPosX(ImGui.GetCursorPosX() + statusPosX);
    //             
    //             if (!string.IsNullOrEmpty(snapshotDetectedCustomizePlusProfile))
    //             {
    //                 ImGui.TextColored(new Vector4(0.3f, 0.8f, 0.3f, 1.0f), "Found");
    //             }
    //             else if (snapshotIsProcessing)
    //             {
    //                 ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.3f, 1.0f), "Detecting...");
    //             }
    //             else
    //             {
    //                 ImGui.TextColored(new Vector4(0.8f, 0.3f, 0.3f, 1.0f), "None");
    //             }
    //
    //             // Clipboard image detection with icon
    //             ImGui.PushFont(UiBuilder.IconFont);
    //             ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "\uf03e");
    //             ImGui.PopFont();
    //             ImGui.SameLine();
    //             ImGui.Text("Clipboard Image:");
    //             ImGui.SameLine();
    //             
    //             ImGui.SetCursorPosX(ImGui.GetCursorPosX() + statusPosX);
    //             
    //             if (snapshotHasClipboardImage)
    //             {
    //                 ImGui.TextColored(new Vector4(0.3f, 0.8f, 0.3f, 1.0f), "Available");
    //             }
    //             else
    //             {
    //                 ImGui.TextColored(new Vector4(0.8f, 0.3f, 0.3f, 1.0f), "None");
    //             }
    //
    //             ImGui.EndChild();
    //
    //             // Status message
    //             if (!string.IsNullOrEmpty(snapshotStatusMessage))
    //             {
    //                 ImGui.Spacing();
    //                 ImGui.TextColored(new Vector4(0.8f, 0.6f, 0.3f, 1.0f), snapshotStatusMessage);
    //             }
    //
    //             // Bottom section with buttons
    //             ImGui.Spacing();
    //             ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.4f, 0.6f, 0.8f, 0.5f));
    //             ImGui.Separator();
    //             ImGui.PopStyleColor();
    //             ImGui.Spacing();
    //
    //             // Buttons with improved styling
    //             float buttonWidth = 120 * scale;
    //             float spacing = 10 * scale;
    //             float totalButtonWidth = (buttonWidth * 2) + spacing;
    //             float offsetX = (ImGui.GetContentRegionAvail().X - totalButtonWidth) * 0.5f;
    //             
    //             ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);
    //
    //             // Create button with plugin-style colors
    //             bool canCreate = !string.IsNullOrWhiteSpace(snapshotDesignName) && !snapshotIsProcessing;
    //             if (!canCreate)
    //                 ImGui.BeginDisabled();
    //
    //             ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.9f, 0.7f));
    //             ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.7f, 1.0f, 0.8f));
    //             ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.8f, 1.0f, 1.0f));
    //
    //             if (ImGui.Button("Create Design", new Vector2(buttonWidth, 0)))
    //             {
    //                 CreateSnapshotDesign();
    //             }
    //
    //             ImGui.PopStyleColor(3);
    //
    //             if (!canCreate)
    //                 ImGui.EndDisabled();
    //
    //             // Cancel button
    //             ImGui.SameLine(0, spacing);
    //             if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0)))
    //             {
    //                 isSnapshotDialogOpen = false;
    //             }
    //             ImGui.End();
    //         }
    //
    //         if (!isOpen)
    //             isSnapshotDialogOpen = false;
    //     }
    //
    //     private void OpenSnapshotDialog(Character character)
    //     {
    //         snapshotTargetCharacter = character;
    //         snapshotDesignName = $"Design {DateTime.Now:yyyy-MM-dd HH:mm}";
    //         snapshotDetectedMods.Clear();
    //         snapshotDetectedCustomizePlusProfile = null;
    //         snapshotHasClipboardImage = false;
    //         snapshotIsProcessing = false;
    //         snapshotStatusMessage = "";
    //         
    //         // Start background detection tasks
    //         Task.Run(async () =>
    //         {
    //             try
    //             {
    //                 snapshotIsProcessing = true;
    //                 snapshotStatusMessage = "Detecting Glamourer state...";
    //                 
    //                 // Detect Glamourer state
    //                 await DetectGlamourerState();
    //                 
    //                 snapshotStatusMessage = "Detecting Customize+ profile...";
    //                 
    //                 // Detect Customize+ profile
    //                 await DetectCustomizePlusProfile();
    //                 
    //                 snapshotStatusMessage = "Checking clipboard for images...";
    //                 
    //                 // Check clipboard for images
    //                 CheckClipboardForImage();
    //                 
    //                 snapshotStatusMessage = "Detection complete";
    //                 snapshotIsProcessing = false;
    //             }
    //             catch (Exception ex)
    //             {
    //                 Plugin.Log.Error($"Error during snapshot detection: {ex}");
    //                 snapshotStatusMessage = "Error during auto-detection";
    //                 snapshotIsProcessing = false;
    //             }
    //         });
    //         
    //         isSnapshotDialogOpen = true;
    //     }
    //
    //     private void CreateSnapshotDesign()
    //     {
    //         if (snapshotTargetCharacter == null)
    //             return;
    //
    //         snapshotIsProcessing = true;
    //         snapshotStatusMessage = "Creating design...";
    //
    //         // Task.Run(async () =>
    //         // {
    //         //     try
    //         //     {
    //         //         // Generate the appropriate macro based on CR mode
            //         var snapshotMacro = GenerateSnapshotMacro(snapshotUseConflictResolution);
            //         
            //         // For CR mode, generate different macros
            //         var regularMacro = GenerateSnapshotMacro(false); // Regular macro without CR
            //         var advancedMacro = snapshotUseConflictResolution ? GenerateSnapshotMacro(true) : ""; // CR macro if enabled
            //         
            //         var newDesign = new CharacterDesign(
            //             snapshotDesignName,
            //             regularMacro, // Always use regular macro for base
            //             snapshotUseConflictResolution, // Enable Advanced Mode if CR is checked
            //             advancedMacro, // Advanced/CR macro
            //             "", // GlamourerDesign - will be set later
            //             "", // Automation
            //             "", // CustomizePlusProfile - will be set later
            //             null // PreviewImagePath - will be set later
            //         );
            //
            //         // Create Glamourer design from current state if detected
            //         if (snapshotDetectedMods.Count > 0)
            //         {
            //             var glamourerDesignName = $"{snapshotDesignName}";
            //             var glamourerDesignId = await CreateGlamourerDesignFromCurrentState(glamourerDesignName);
            //             if (glamourerDesignId != Guid.Empty)
            //             {
            //                 // Store the design name, not the GUID, for SCS compatibility
            //                 newDesign.GlamourerDesign = glamourerDesignName;
            //                 Plugin.Log.Information($"Created Glamourer design: {glamourerDesignName} (ID: {glamourerDesignId})");
            //             }
            //         }
            //
            //         // Set Customize+ profile if detected (only if it's not the Character default)
            //         if (!string.IsNullOrEmpty(snapshotDetectedCustomizePlusProfile) && 
            //             snapshotDetectedCustomizePlusProfile != "Character")
            //         {
            //             newDesign.CustomizePlusProfile = snapshotDetectedCustomizePlusProfile;
            //         }
            //
            //         // Set up Secret Mode state for CR mode
            //         if (snapshotUseConflictResolution)
            //         {
            //             // Get only gear/hair mods from Currently Affecting You tab (prevents body/sculpt/eye mods from being managed)
            //             var allAffectingMods = plugin.PenumbraIntegration?.GetOnScreenTabMods();
            //             var currentlyAffectingMods = new HashSet<string>();
            //             
            //             if (allAffectingMods != null)
            //             {
            //                 foreach (var modDir in allAffectingMods)
            //                 {
            //                     try
            //                     {
            //                         // Get mod type from cache or determine it
            //                         ModType modType;
            //                         if (plugin.modCategorizationCache.ContainsKey(modDir))
            //                         {
            //                             modType = plugin.modCategorizationCache[modDir];
            //                         }
            //                         else
            //                         {
            //                             // Use the static method to determine mod type
            //                             modType = SecretModeModWindow.DetermineModType(modDir, "", plugin);
            //                             plugin.modCategorizationCache[modDir] = modType;
            //                         }
            //
            //                         // Only include gear and hair mods (safe to toggle, won't break body/sculpt/eyes)
            //                         if (modType == ModType.Gear || modType == ModType.Hair)
            //                         {
            //                             currentlyAffectingMods.Add(modDir);
            //                         }
            //                     }
            //                     catch (Exception ex)
            //                     {
            //                         Plugin.Log.Warning($"Failed to determine mod type for {modDir}: {ex.Message}");
            //                     }
            //                 }
            //             }
            //             if (currentlyAffectingMods != null && currentlyAffectingMods.Count > 0)
            //             {
            //                 // Create mod state dictionary with all currently affecting mods enabled
            //                 newDesign.SecretModState = new Dictionary<string, bool>();
            //                 foreach (var modName in currentlyAffectingMods)
            //                 {
            //                     newDesign.SecretModState[modName] = true;
            //                 }
            //                 Plugin.Log.Information($"Detected {newDesign.SecretModState.Count} currently affecting mods for CR design");
            //             }
            //             else
            //             {
            //                 Plugin.Log.Information("No currently affecting mods detected for CR design");
            //             }
            //         }
            //
            //         // Save clipboard image if available
            //         if (snapshotHasClipboardImage)
            //         {
            //             var imagePath = await SaveClipboardImageForDesign(newDesign.Id);
            //             if (!string.IsNullOrEmpty(imagePath))
            //             {
            //                 newDesign.PreviewImagePath = imagePath;
            //             }
            //         }
            //
            //         // The macro was already set during construction, no need to regenerate
            //
            //         // Add the design to the character
            //         snapshotTargetCharacter.Designs.Add(newDesign);
            //         
            //         // Save configuration
            //         plugin.Configuration.Save();
            //
            //         snapshotStatusMessage = "Design created successfully!";
            //         
            //         // Close dialog after a brief delay
            //         await Task.Delay(1000);
            //         isSnapshotDialogOpen = false;
            //     }
            //     catch (Exception ex)
            //     {
            //         Plugin.Log.Error($"Error creating snapshot design: {ex}");
            //         snapshotStatusMessage = $"Error: {ex.Message}";
            //     }
            //     finally
            //     {
            //         snapshotIsProcessing = false;
            //     }
            // });
        // }
        //
        // private string GenerateSnapshotMacro(bool useConflictResolution)
        // {
        //     var macroLines = new List<string>();
        //
        //     if (useConflictResolution)
        //     {
        //         // CR Mode: Generate macro that works with Secret Mode CR system
        //         // No bulktag commands - CR system handles mod management automatically
        //         
        //         // Add Glamourer apply if we have a design
        //         if (snapshotDetectedMods.Count > 0)
        //         {
        //             macroLines.Add($"/glamour apply {snapshotDesignName} | self");
        //         }
        //
        //         // Add Customize+ profile commands if we have a non-Character profile
        //         if (!string.IsNullOrEmpty(snapshotDetectedCustomizePlusProfile) && snapshotDetectedCustomizePlusProfile != "Character")
        //         {
        //             macroLines.Add("/customize profile disable <me>");
        //             macroLines.Add($"/customize profile enable <me>, {snapshotDetectedCustomizePlusProfile}");
        //         }
        //
        //         // Add penumbra redraw at the end
        //         macroLines.Add("/penumbra redraw self");
        //     }
        //     else
        //     {
        //         // Regular Mode: Generate bulktag macros for non-CR designs
        //         // Add Glamourer apply if we have a design
        //         if (snapshotDetectedMods.Count > 0)
        //         {
        //             macroLines.Add($"/glamour apply {snapshotDesignName} | self");
        //         }
        //
        //         // Add Customize+ profile commands if we have a non-Character profile
        //         if (!string.IsNullOrEmpty(snapshotDetectedCustomizePlusProfile) && snapshotDetectedCustomizePlusProfile != "Character")
        //         {
        //             macroLines.Add("/customize profile disable <me>");
        //             macroLines.Add($"/customize profile enable <me>, {snapshotDetectedCustomizePlusProfile}");
        //         }
        //
        //         // Always add penumbra redraw at the end
        //         macroLines.Add("/penumbra redraw self");
        //     }
        //
        //     return string.Join("\n", macroLines);
        // }
        //
        // private async Task<Guid> CreateGlamourerDesignFromCurrentState(string designName)
        // {
        //     try
        //     {
        //         // Get current player's object index (usually 0 for local player)
        //         var playerIndex = 0;
        //         
        //         // First, get the current state data from Glamourer
        //         var glamourerStateIpc = Plugin.PluginInterface.GetIpcSubscriber<int, uint, (int, string?)>("Glamourer.GetStateBase64");
        //         var (stateError, stateData) = await Task.Run(() => glamourerStateIpc.InvokeFunc(playerIndex, 0));
        //         
        //         if (stateError != 0 || string.IsNullOrEmpty(stateData))
        //         {
        //             Plugin.Log.Warning($"Failed to get Glamourer state for design creation (error: {stateError})");
        //             return Guid.Empty;
        //         }
        //         
        //         // Create design from the state data
        //         var glamourerAddDesignIpc = Plugin.PluginInterface.GetIpcSubscriber<string, string, (int, Guid)>("Glamourer.AddDesign");
        //         var (addError, designId) = await Task.Run(() => glamourerAddDesignIpc.InvokeFunc(stateData, designName));
        //         
        //         if (addError == 0 && designId != Guid.Empty) // Success
        //         {
        //             Plugin.Log.Information($"Created Glamourer design '{designName}' with ID {designId}");
        //             return designId;
        //         }
        //         else
        //         {
        //             Plugin.Log.Warning($"Failed to create Glamourer design (error: {addError})");
        //             return Guid.Empty;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Plugin.Log.Error($"Failed to create Glamourer design: {ex.Message}");
        //         return Guid.Empty;
        //     }
        // }
        //
        // private async Task DetectGlamourerState()
        // {
        //     try
        //     {
        //         snapshotDetectedMods.Clear();
        //         
        //         // Get current player's object index (usually 0 for local player)
        //         var playerIndex = 0;
        //         
        //         // Use real Glamourer IPC to get current state
        //         var glamourerStateIpc = Plugin.PluginInterface.GetIpcSubscriber<int, uint, (int, string?)>("Glamourer.GetStateBase64");
        //         var (errorCode, stateData) = await Task.Run(() => glamourerStateIpc.InvokeFunc(playerIndex, 0));
        //         
        //         if (errorCode == 0 && !string.IsNullOrEmpty(stateData)) // Success
        //         {
        //             // We have a valid state, which means there are modifications
        //             snapshotDetectedMods.Add("Current Glamourer State");
        //             Plugin.Log.Information($"Glamourer detection completed: Active state detected");
        //         }
        //         else
        //         {
        //             Plugin.Log.Information($"Glamourer detection completed: No modifications detected (error: {errorCode})");
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Plugin.Log.Warning($"Failed to detect Glamourer state: {ex.Message}");
        //         snapshotDetectedMods.Clear();
        //     }
        // }
        //
        // private async Task DetectCustomizePlusProfile()
        // {
        //     try
        //     {
        //         // Get current player's object index (usually 0 for local player)
        //         var playerIndex = (ushort)0;
        //         
        //         // Use real Customize+ IPC to get active profile
        //         var customizePlusIpc = Plugin.PluginInterface.GetIpcSubscriber<ushort, (int, Guid?)>("CustomizePlus.Profile.GetActiveProfileIdOnCharacter");
        //         var (errorCode, profileId) = await Task.Run(() => customizePlusIpc.InvokeFunc(playerIndex));
        //         
        //         if (errorCode == 0 && profileId.HasValue && profileId.Value != Guid.Empty) // Success with profile
        //         {
        //             // Get profile list to find the profile name
        //             var profileListIpc = Plugin.PluginInterface.GetIpcSubscriber<(Guid, string, string, List<(string, ushort, byte, ushort)>, int, bool)[]>("CustomizePlus.Profile.GetList");
        //             var profileList = await Task.Run(() => profileListIpc.InvokeFunc());
        //             
        //             // Find the active profile in the list
        //             var activeProfile = profileList.FirstOrDefault(p => p.Item1 == profileId.Value);
        //             
        //             if (activeProfile.Item1 != Guid.Empty) // Found the profile
        //             {
        //                 var profileName = activeProfile.Item2; // The Name field from IPCProfileDataTuple
        //                 
        //                 // If it's an empty name or default, treat as Character
        //                 if (string.IsNullOrWhiteSpace(profileName) || profileName == "Default")
        //                 {
        //                     profileName = "Character";
        //                 }
        //                 
        //                 snapshotDetectedCustomizePlusProfile = profileName;
        //                 Plugin.Log.Information($"Customize+ detection completed: Profile '{profileName}' active");
        //             }
        //             else
        //             {
        //                 snapshotDetectedCustomizePlusProfile = "Character";
        //                 Plugin.Log.Information("Customize+ detection completed: Active profile not found in profile list");
        //             }
        //         }
        //         else
        //         {
        //             // No profile or error - assume Character default
        //             snapshotDetectedCustomizePlusProfile = "Character";
        //             Plugin.Log.Information($"Customize+ detection completed: Character profile active (error: {errorCode})");
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Plugin.Log.Warning($"Failed to detect Customize+ profile: {ex.Message}");
        //         snapshotDetectedCustomizePlusProfile = "Character";
        //     }
        // }
        //    /// <summary>
        // /// Sets up the snapshot state and creates a design from a chat command, using the same logic as the UI button
        // /// </summary>
        // public void SetupSnapshotFromCommand(Character character, string designName, bool useConflictResolution)
        // {
        //     // Set up the snapshot state variables (same as OpenSnapshotDialog)
        //     snapshotTargetCharacter = character;
        //     snapshotDesignName = designName;
        //     snapshotUseConflictResolution = useConflictResolution;
        //     snapshotDetectedMods = new HashSet<string>();
        //     snapshotDetectedCustomizePlusProfile = "";
        //     snapshotHasClipboardImage = Clipboard.ContainsImage();
        //     snapshotIsProcessing = false;
        //     snapshotStatusMessage = "";
        //
        //     // Start the detection and creation process (same as the UI button logic)
        //     Task.Run(async () =>
        //     {
        //         try
        //         {
        //             // Run detection in parallel (same as UI)
        //             var detectionTasks = new Task[]
        //             {
        //                 DetectGlamourerState(),
        //                 DetectCustomizePlusProfile()
        //             };
        //
        //             await Task.WhenAll(detectionTasks);
        //             
        //             // Create the design (same as clicking "Create Design" button)
        //             CreateSnapshotDesign();
        //         }
        //         catch (Exception ex)
        //         {
        //             Plugin.Log.Error($"Error in snapshot creation from command: {ex}");
        //             Plugin.ChatGui.PrintError($"[Simple Character Select] Failed to create snapshot design: {ex.Message}");
        //         }
        //     });
        // }
        //
        // public void CreateSmartSnapshotFromCommand(Character character, bool useConflictResolution)
        // {
        //     CreateSmartSnapshot(character, useConflictResolution);
        // }
        //
        // private void CreateSmartSnapshot(Character character, bool useConflictResolution)
        // {
        //     Task.Run(async () =>
        //     {
        //         try
        //         {
        //             Plugin.Log.Information($"Starting smart snapshot for character '{character.Data.Name}' with CR: {useConflictResolution}");
        //
        //             // Get the most recently created Glamourer design
        //             var recentDesign = await GetMostRecentGlamourerDesign();
        //             if (recentDesign == null)
        //             {
        //                 Plugin.ChatGui.PrintError("[Simple Character Select] No recent Glamourer design found. Please create a design in Glamourer first or use the regular snapshot dialog.");
        //                 return;
        //             }
        //
        //             Plugin.Log.Information($"Found recent Glamourer design: '{recentDesign.Value.Name}' created on {recentDesign.Value.CreationDate}");
        //
        //             // Set snapshot data using the recent design
        //             snapshotTargetCharacter = character;
        //             snapshotDesignName = recentDesign.Value.Name;
        //             snapshotUseConflictResolution = useConflictResolution;
        //             snapshotIsProcessing = true;
        //
        //             // Auto-detect current state
        //             var detectionTasks = new Task[]
        //             {
        //                 DetectGlamourerState(),
        //                 DetectCustomizePlusProfile(),
        //                 Task.Run(() => CheckClipboardForImage())
        //             };
        //
        //             await Task.WhenAll(detectionTasks);
        //
        //             // Create the SCS design with the Glamourer design field populated
        //             CreateSmartSnapshotDesign(recentDesign.Value);
        //
        //             Plugin.ChatGui.Print($"[Simple Character Select] Smart snapshot created: '{recentDesign.Value.Name}' {(useConflictResolution ? "with" : "without")} CR");
        //         }
        //         catch (Exception ex)
        //         {
        //             Plugin.Log.Error($"Error in smart snapshot creation: {ex}");
        //             Plugin.ChatGui.PrintError($"[Simple Character Select] Failed to create smart snapshot: {ex.Message}");
        //         }
        //     });
        // }
        //
        // private async Task<(string Name, DateTimeOffset CreationDate, Guid Id)?> GetMostRecentGlamourerDesign()
        // {
        //     try
        //     {
        //         // Get Glamourer API with correct IPC method names
        //         var glamourerApi = Plugin.PluginInterface.GetIpcSubscriber<Dictionary<Guid, string>>("Glamourer.GetDesignList.V2");
        //         var designsDict = await Task.Run(() => glamourerApi.InvokeFunc());
        //
        //         if (designsDict == null || designsDict.Count == 0)
        //             return null;
        //
        //         var glamourerJObjectApi = Plugin.PluginInterface.GetIpcSubscriber<Guid, Newtonsoft.Json.Linq.JObject?>("Glamourer.GetDesignJObject");
        //
        //         // Get design data with timestamps
        //         var designsWithTimestamps = new List<(string Name, DateTimeOffset CreationDate, Guid Id)>();
        //
        //         foreach (var kvp in designsDict)
        //         {
        //             try
        //             {
        //                 var designJson = await Task.Run(() => glamourerJObjectApi.InvokeFunc(kvp.Key));
        //                 if (designJson != null)
        //                 {
        //                     var name = designJson["Name"]?.Value<string>() ?? kvp.Value;
        //                     var creationDate = designJson["CreationDate"]?.Value<DateTimeOffset>() ?? DateTimeOffset.MinValue;
        //                     
        //                     designsWithTimestamps.Add((name, creationDate, kvp.Key));
        //                 }
        //             }
        //             catch (Exception ex)
        //             {
        //                 Plugin.Log.Warning($"Failed to get timestamp for design {kvp.Key}: {ex.Message}");
        //             }
        //         }
        //
        //         // Return the most recently created design
        //         return designsWithTimestamps
        //             .Where(d => d.CreationDate > DateTimeOffset.MinValue)
        //             .OrderByDescending(d => d.CreationDate)
        //             .FirstOrDefault();
        //     }
        //     catch (Exception ex)
        //     {
        //         Plugin.Log.Error($"Failed to get recent Glamourer designs: {ex}");
        //         return null;
        //     }
        // }
        //
        // private string GenerateSnapshotMacro(Character character, string glamourerDesign, string customizePlusProfile)
        // {
        //     if (string.IsNullOrWhiteSpace(glamourerDesign))
        //         return "";
        //
        //     string macro = $"/glamour apply {glamourerDesign} | self";
        //
        //     // Conditionally include automation line
        //     if (plugin.Configuration.EnableAutomations)
        //     {
        //         string automationToUse = !string.IsNullOrWhiteSpace(character.Data.CharacterAutomation)
        //             ? character.Data.CharacterAutomation
        //             : "None";
        //
        //         macro += $"\n/glamour automation enable {automationToUse}";
        //     }
        //
        //     // Always disable Customize+ first
        //     macro += "\n/customize profile disable <me>";
        //
        //     // Determine Customize+ profile
        //     string customizeProfileToUse = !string.IsNullOrWhiteSpace(customizePlusProfile)
        //         ? customizePlusProfile
        //         : !string.IsNullOrWhiteSpace(character.Data.CustomizeProfile)
        //             ? character.Data.CustomizeProfile
        //             : string.Empty;
        //
        //     // Enable only if needed
        //     if (!string.IsNullOrWhiteSpace(customizeProfileToUse))
        //         macro += $"\n/customize profile enable <me>, {customizeProfileToUse}";
        //
        //     // Redraw line
        //     macro += "\n/penumbra redraw self";
        //
        //     return macro;
        // }
        //
        // private void CreateSmartSnapshotDesign((string Name, DateTimeOffset CreationDate, Guid Id) recentDesign)
        // {
        //     try
        //     {
        //         if (snapshotTargetCharacter == null)
        //         {
        //             Plugin.Log.Error("No target character set for smart snapshot");
        //             return;
        //         }
        //
        //         Plugin.Log.Information($"Creating smart snapshot design for character '{snapshotTargetCharacter.Data.Name}' using Glamourer design '{recentDesign.Name}'");
        //
        //         // Generate the proper macro for the snapshot design
        //         string snapshotMacro = GenerateSnapshotMacro(snapshotTargetCharacter, recentDesign.Name, 
        //             !string.IsNullOrEmpty(snapshotDetectedCustomizePlusProfile) && snapshotDetectedCustomizePlusProfile != "Character" 
        //                 ? snapshotDetectedCustomizePlusProfile 
        //                 : "");
        //
        //         // Create new design based on detected character state
        //         var newDesign = new CharacterDesign(
        //             name: recentDesign.Name,
        //             macro: snapshotMacro,
        //             isAdvancedMode: false,
        //             advancedMacro: "",
        //             glamourerDesign: recentDesign.Name, // Use the Glamourer design name
        //             automation: "",
        //             customizePlusProfile: !string.IsNullOrEmpty(snapshotDetectedCustomizePlusProfile) && snapshotDetectedCustomizePlusProfile != "Character" 
        //                 ? snapshotDetectedCustomizePlusProfile 
        //                 : ""
        //         );
        //
        //         // Handle clipboard image if available
        //         if (snapshotHasClipboardImage)
        //         {
        //             Task.Run(async () =>
        //             {
        //                 try
        //                 {
        //                     var imagePath = await SaveClipboardImageForDesign(Guid.NewGuid());
        //                     if (!string.IsNullOrEmpty(imagePath))
        //                     {
        //                         newDesign.PreviewImagePath = imagePath;
        //                         Plugin.Log.Information($"Saved clipboard image for smart snapshot: {imagePath}");
        //                     }
        //                 }
        //                 catch (Exception ex)
        //                 {
        //                     Plugin.Log.Warning($"Failed to save clipboard image for smart snapshot: {ex}");
        //                 }
        //             });
        //         }
        //
        //         // Add to character's designs
        //         snapshotTargetCharacter.Data.Designs.Add(newDesign);
        //
        //         // Save configuration
        //         plugin.Configuration.Save();
        //
        //         Plugin.Log.Information($"Smart snapshot design '{newDesign.Name}' created successfully for character '{snapshotTargetCharacter.Data.Name}'");
        //     }
        //     catch (Exception ex)
        //     {
        //         Plugin.Log.Error($"Error creating smart snapshot design: {ex}");
        //         Plugin.ChatGui.PrintError($"[Simple Character Select] Failed to create smart snapshot design: {ex.Message}");
        //     }
        //     finally
        //     {
        //         snapshotIsProcessing = false;
        //     }
        // }
        //
        //
        //
        // private void CloseSnapshotDialog()
        // {
        //     isSnapshotDialogOpen = false;
        //     snapshotDesignName = "";
        //     snapshotUseConflictResolution = true;
        //     snapshotTargetCharacter = null;
        //     snapshotDetectedMods.Clear();
        //     snapshotDetectedCustomizePlusProfile = null;
        //     snapshotHasClipboardImage = false;
        //     snapshotIsProcessing = false;
        //     snapshotStatusMessage = "";
        // }
}