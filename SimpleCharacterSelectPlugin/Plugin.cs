using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SimpleCharacterSelectPlugin.Windows;
using System.Collections.Generic;
using System.Numerics;
using System;
using SimpleCharacterSelectPlugin.Managers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Types;
using System.Threading;
using SimpleCharacterSelectPlugin.Integration;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public static Plugin? Instance { get; private set; }
        
        [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] internal static IClientState ClientState { get; private set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService] internal static IPluginLog Log { get; private set; } = null!;
        [PluginService] internal static IChatGui ChatGui { get; set; } = null!;
        [PluginService] internal static IFramework Framework { get; private set; } = null!;
        [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
        [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
        [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
        [PluginService] internal static ICondition Condition { get; private set; } = null!;
        [PluginService] internal static INamePlateGui NamePlateGui { get; private set; } = null!;
        [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

        private static readonly string Version = typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "(Unknown Version)";
        public static readonly string CurrentPluginVersion = Version; // Match repo.json and .csproj version
        
        public readonly WindowSystem WindowSystem = new("SimpleCharacterSelectPlugin");
        public MainWindow MainWindow { get; init; }
        public QuickSwitchWindow QuickSwitchWindow { get; set; } // Quick Switch Window
        public ImGuiFileBrowserWindow? FileBrowserWindow { get; private set; } = null;
        
        public enum SortType { Manual, Favorites, Alphabetical, Recent, Oldest }

        public ActivePlayerCharacter ActivePlayer;
        public Vector3 NewCharacterColor { get; set; } = new Vector3(1.0f, 1.0f, 1.0f); // Default to white


        public string PluginPath => PluginInterface.GetPluginConfigDirectory();
        public string PluginDirectory => PluginInterface.AssemblyLocation.DirectoryName ?? "";
        
        public static Configuration Configuration { get; set; } 
        public List<Character> Characters => Configuration.Characters;
        public Dictionary<int, GearsetAssignment> GearsetAssignments => Configuration.GearsetAssignments;
        public byte NewCharacterIdlePoseIndex { get; set; } = 0;
        private DateTime loginTime;
        
        // Integration List Provider for autocomplete dropdowns
        public IntegrationListProvider? IntegrationListProvider { get; private set; }

        // IPC Providers
        //private IPCProvider? ipcProvider;
        
        public bool StartupComplete = false;
        
        private DateTime pluginInitTime = DateTime.Now;
        
        private Commands commands;

        public WindowState WindowState { get; set; } = new WindowState();
        //private NPCDialogueProcessor? dialogueProcessor;

        public Plugin(IGameInteropProvider gameInteropProvider)
        {
            loginTime = DateTime.Now;
            Instance = this;
            GameInteropProvider = gameInteropProvider;

            Configuration = Configuration.LoadConfigurationSafely(PluginInterface);

            //ActivePlayer = new ActivePlayerCharacter();
            
            // Run backup on background thread to prevent UI freeze
            var existingConfig = PluginInterface.GetPluginConfig() as Configuration;
            if (existingConfig != null)
            {
                Task.Run(() =>
                {
                    try
                    {
                        BackupManager.CreateBackupIfNeeded(existingConfig, CurrentPluginVersion);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[Backup] Could not create pre-load backup: {ex.Message}");
                    }
                });
            }
            
            GameCommandManager.Init(Log, CommandManager, PluginInterface);
                
            commands = new Commands(this, CommandManager, ChatGui, Log, Configuration);
            commands.AddCommands();
            
            MigrationManager.RunMigrations(this);

            // Load Forms assembly in background to avoid ~750ms main thread stall
            Task.Run(() =>
            {
                try
                {
                    System.Reflection.Assembly.Load("System.Windows.Forms");
                    Plugin.Log.Info("System.Windows.Forms loaded (background).");
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"Failed to load System.Windows.Forms: {ex.Message}");
                }
            });

            // Initialize integration list provider for autocomplete dropdowns
            IntegrationListProvider = new IntegrationListProvider(this);
            IntegrationListProvider.GetPenumbraCollections(true);
            IntegrationListProvider.GetGlamourerDesigns(true);
            IntegrationListProvider.GetCustomizePlusProfiles(true);
            IntegrationListProvider.GetMoodlesPresets(true);
            IntegrationListProvider.GetHonorificTitles(true);

            // Initialize the MainWindow and ConfigWindow
            MainWindow = new Windows.MainWindow(this);
            MainWindow.SortCharacters();
            QuickSwitchWindow = new QuickSwitchWindow(this); // Quick Switch Window
            QuickSwitchWindow.IsOpen = Configuration.IsQuickSwitchWindowOpen; // Restore last open state

            // Initialize ImGui File Browser
            FileBrowserWindow = new ImGuiFileBrowserWindow();
            FileBrowserWindow.SetConfiguration(Configuration);
            WindowSystem.AddWindow(FileBrowserWindow);

            // Initialize IPC provider for other plugins
            //ipcProvider = new IPCProvider(this, PluginInterface);
            
            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(QuickSwitchWindow); // Quick Switch Window

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += QuickSwitchWindow.Toggle;
            PluginInterface.UiBuilder.OpenMainUi += MainWindow.Toggle;
            
            Framework.Update += FrameworkUpdate;

            ClientState.Login += () =>
            {
               Log.Debug($"[Simple Character Select] Local character name: {ObjectTable.LocalPlayer?.Name.TextValue}");
            };
            
        }
        
        private void FrameworkUpdate(IFramework framework)
        {
            if (Configuration.EnableSafeMode)
                return;
            if (!ClientState.IsLoggedIn || ObjectTable.LocalPlayer == null)
                return;
            
            var player = ObjectTable.LocalPlayer!;
            
            // start up
            if (!StartupComplete && player.HomeWorld.IsValid && ClientState.IsLoggedIn && ClientState.TerritoryType != 0)
            {
                string world = player.HomeWorld.Value.Name.ToString();
                string fullKey = $"{player.Name.TextValue}@{world}";

                if (!Configuration.PlayerCharacters.ContainsKey(fullKey)) //new PC, create an entry for it
                {
                    var newPc = PcManager.MustNewPlayerCharacter(fullKey);
                    Configuration.PlayerCharacters[fullKey] = newPc;
                    ActivePlayer = new ActivePlayerCharacter(player, newPc);
                    Configuration.Save();
                    Log.Info($"New player character created: {newPc.FullName}");
                }
                else // else load the existing one
                {
                    Log.Debug($"Loading existing character: {player.Name.TextValue}");
                    ActivePlayer = new ActivePlayerCharacter(player, Configuration.PlayerCharacters[fullKey]);
                    Log.Info($"Player character loaded: {ActivePlayer.Pc.FullName}");
                }
                
                StartupComplete = true;
                
                if (!Configuration.EnableLastUsedCharacterAutoload)
                    return;
                
                Log.Debug($"Loading last used character: {ActivePlayer.Pc.ActiveCharacter?.Data.Name}");
                PcManager.ApplyLastUsedOrAssignedCharacter(ActivePlayer.Pc);
                QuickSwitchWindow.RefreshSelection();
                Configuration.Save();
                return;
            }
            //end start up

            if (Configuration.EnableGearsetDesignSwitching && ActivePlayer.GearsetHasChanged() && GearsetAssignments.ContainsKey((int)ActivePlayer.LastKnownGearset!.Index))
            {
                
                var assignment = GearsetAssignments[(int)GearsetManager.GetCurrentGearset().Index];
                var character = Characters.Find(c => c.Data.Name == assignment.CharacterName);
                Plugin.Log.Debug($"SWITCH DESIGN GEARSET {character?.Data.Name} {assignment.DisplayName()} {assignment.DesignId}");
                if (character == null || character.GetDesignById(assignment.DesignId!.Value) == null)
                {
                    Log.Info($"Outdated gearset assignment {assignment.DisplayName()} found, deleting");
                    GearsetAssignments.Remove(assignment.GearsetIndex);
                }
                else
                {
                    Log.Info($"Apply gearset assignment {assignment.DisplayName()}");
                    ActivePlayer.QueueUpdate(character, assignment.DesignId);
                }
            }

            if (ActivePlayer.RequiresUpdate()) // trigger an update
            {
                Log.Debug("Updating player character");
                DesignManager.ApplyUpdate(ActivePlayer);
                QuickSwitchWindow.RefreshSelection();
                Configuration.Save();
            }
        }
        
        public void OpenFilePicker(string title, string filter, Action<string> onFileSelected, string? startDirectory = null)
        {
            // Use ImGui file browser
            if (FileBrowserWindow != null)
            {
                FileBrowserWindow.OnFileSelected = onFileSelected;
                FileBrowserWindow.Open(startDirectory);
            }
        }
        
        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            MainWindow.Dispose();
            commands.RemoveHandlers();
            Framework.Update -= FrameworkUpdate; // Fixed: should be -= not +=
            //dialogueProcessor?.Dispose();
            IntegrationListProvider?.Dispose();

            // Dispose IPC provider
            //ipcProvider?.Dispose();

            try
            {
                string sessionFilePath = Path.Combine(PluginInterface.GetPluginConfigDirectory(), "boot_session.txt");
                if (File.Exists(sessionFilePath))
                {
                    File.Delete(sessionFilePath);
                    Plugin.Log.Debug("[Dispose] Deleted boot_session.txt for next launch.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning($"[Dispose] Failed to delete boot_session.txt: {ex.Message}");
            }
            
            Instance = null;
        }

        private void DrawUI()
        {
            WindowSystem.Draw();

            // Track and persist Quick Switch window state
            bool currentState = QuickSwitchWindow.IsOpen;
            if (Configuration.IsQuickSwitchWindowOpen != currentState)
            {
                Configuration.IsQuickSwitchWindowOpen = currentState;
                Configuration.Save();
            }
        }

        public void SaveConfiguration()
        {
            try
            {
                // Update properties first
                var profileImageScaleProperty = Configuration.GetType().GetProperty("ProfileImageScale");
                if (profileImageScaleProperty != null && profileImageScaleProperty.CanWrite)
                {
                    profileImageScaleProperty.SetValue(Configuration, WindowState.ProfileImageScale);
                }

                var profileColumnsProperty = Configuration.GetType().GetProperty("ProfileColumns");
                if (profileColumnsProperty != null && profileColumnsProperty.CanWrite)
                {
                    profileColumnsProperty.SetValue(Configuration, WindowState.ProfileColumns);
                }

                var profileSpacingProperty = Configuration.GetType().GetProperty("ProfileSpacing");
                if (profileSpacingProperty != null && profileSpacingProperty.CanWrite)
                {
                    profileSpacingProperty.SetValue(Configuration, WindowState.ProfileSpacing);
                }

                // Save configuration
                Configuration.Save();

                // Create backup occasionally (roughly every 10th save)
                if (DateTime.Now.Millisecond % 100 == 0)
                {
                    BackupManager.CreateBackupIfNeeded(Configuration, CurrentPluginVersion);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"[Config] Failed to save configuration: {ex.Message}");

                // Create emergency backup
                try
                {
                    BackupManager.CreateEmergencyBackup(Configuration);
                }
                catch (Exception backupEx)
                {
                    Plugin.Log.Error($"[Config] Emergency backup also failed: {backupEx.Message}");
                }
            }
        }

        public void CreateManualBackup()
        {
            try
            {
                BackupManager.CreateEmergencyBackup(Configuration);
                Plugin.Log.Info("[Backup] Manual backup created successfully");
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"[Backup] Manual backup failed: {ex.Message}");
            }
        }
        
        // TODO this is definitely way too long for what it's doing
        public void SelectRandomCharacterAndDesign()
        {
            var random = new Random();
            // pick a character TODO
            // pick a design
            // apply it
        }
   
        /// <summary>
        /// Apply character/design to target using direct IPC calls instead of macros
        /// This works for GPose actors spawned by Brio/Ktisis unlike the old macro approach
        /// </summary>
        public async Task<bool> ApplyToTarget(Character character, int designIndex = -1)
        {
            // try TODO readd target application
            // {
            //     // Get target info from the main thread to ensure fresh data
            //     IGameObject? targetObject = null;
            //     await Framework.RunOnFrameworkThread(() =>
            //     {
            //         targetObject = GetCurrentTarget();
            //     });
            //     
            //     if (targetObject == null)
            //     {
            //         ChatGui.PrintError("[Simple Character Select] No valid target selected.");
            //         return false;
            //     }
            //     
            //     
            //     return await ApplyToTarget(character, designIndex, (int)targetObject.ObjectIndex, targetObject.ObjectKind, targetObject.Name?.ToString() ?? "Unknown");
            // }
            // catch (Exception ex)
            // {
            //     Log.Error($"Error applying character to target: {ex}");
            //     ChatGui.PrintError($"[Simple Character Select] Failed to apply to target: {ex.Message}");
            //     return false;
            // }
            return true;
        }

        /// <summary>
        /// Get the current target with proper handling for GPose and regular gameplay
        /// </summary>
        public IGameObject? GetCurrentTarget()
        {
            try
            {
                // Check if we're in GPose first
                var isInGPose = ClientState.IsGPosing;
                
                // Get all available targets
                var target = TargetManager.Target;
                var softTarget = TargetManager.SoftTarget;
                var focusTarget = TargetManager.FocusTarget;
                var mouseOverTarget = TargetManager.MouseOverTarget;
                
                // In GPose, targeting works differently - show more ObjectTable info
                if (isInGPose)
                {
                    for (int i = 0; i < Math.Min(ObjectTable.Length, 20); i++)
                    {
                        var obj = ObjectTable[i];
                        if (obj != null)
                        {
                            // GPose object scan without debug logging
                        }
                    }
                    
                    // In GPose, try GPoseTarget from native TargetSystem (what Ktisis/Brio/Glamourer use)
                    if (target == null)
                    {
                        
                        try
                        {
                            unsafe
                            {
                                var targetSystem = TargetSystem.Instance();
                                if (targetSystem != null && targetSystem->GPoseTarget != null)
                                {
                                    var gposeTarget = ObjectTable.CreateObjectReference((IntPtr)targetSystem->GPoseTarget);
                                    if (gposeTarget != null)
                                    {
                                        return gposeTarget;
                                    }
                                    else
                                    {
                                    }
                                }
                                else
                                {
                                    Log.Debug($"[GetCurrentTarget] No GPoseTarget set - user needs to target a GPose actor first");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"[GetCurrentTarget] Error accessing GPoseTarget: {ex}");
                        }
                        
                        // Fallback to other targeting methods
                        if (softTarget != null)
                        {
                            Log.Debug($"[GetCurrentTarget] Fallback to SoftTarget: {softTarget.Name} (Index: {softTarget.ObjectIndex})");
                            return softTarget;
                        }
                        
                        if (focusTarget != null)
                        {
                                return focusTarget;
                        }
                        
                        Log.Warning($"[GetCurrentTarget] No target found - user needs to select a GPose actor using Ktisis/Brio 'Target Actor'");
                        return null;
                    }
                }
                else
                {
                    // Regular gameplay - show fewer objects
                    for (int i = 0; i < Math.Min(ObjectTable.Length, 10); i++)
                    {
                        var obj = ObjectTable[i];
                        if (obj != null)
                        {
                            // Object scan logic without debug logging
                        }
                    }
                }
                
                return target;
            }
            catch (Exception ex)
            {
                Log.Error($"[GetCurrentTarget] Error: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Check if a target is a valid type for appearance modifications
        /// </summary>
        private bool IsValidTargetForModification(IGameObject target)
        {
            // Only allow modification of players, GPose actors, and companions
            return target.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Pc ||
                   target.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc ||
                   target.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Companion;
        }
        
        /// <summary>
        /// Comprehensive validation for target object safety before modifications
        /// </summary>
        private async Task<bool> ValidateTargetObjectSafety(int objectIndex, string targetName)
        {
            try
            {
                // Get the current target object to validate it's still valid
                IGameObject? targetObject = null;
                await Framework.RunOnFrameworkThread(() =>
                {
                    if (objectIndex >= 0 && objectIndex < ObjectTable.Length)
                    {
                        targetObject = ObjectTable[objectIndex];
                    }
                });
                
                if (targetObject == null)
                {
                    Log.Warning($"[ValidateTarget] Target object at index {objectIndex} is null");
                    return false;
                }
                
                // Validate object is still valid and hasn't been destroyed/replaced
                if (targetObject.Address == nint.Zero)
                {
                    Log.Warning($"[ValidateTarget] Target object at index {objectIndex} has invalid address");
                    return false;
                }
                
                // Validate object name hasn't changed (indicates object was replaced)
                var currentName = targetObject.Name?.ToString() ?? "";
                if (!string.IsNullOrEmpty(targetName) && !currentName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Warning($"[ValidateTarget] Target name changed from '{targetName}' to '{currentName}' - object may have been replaced");
                    return false;
                }
                
                // Check if we're in a cutscene or other unsafe state
                if (Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInCutSceneEvent] ||
                    Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.WatchingCutscene78] ||
                    Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInEvent])
                {
                    Log.Warning($"[ValidateTarget] Cannot modify target during cutscene or event");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[ValidateTarget] Error validating target safety: {ex}");
                return false;
            }
        }

        private async Task<string> SaveClipboardImageForDesign(Guid designId)
        {
            try
            {
                string imagePath = "";
                
                // Clipboard operations need to be on STA thread
                var thread = new Thread(() =>
                {
                    try
                    {
                        if (!System.Windows.Forms.Clipboard.ContainsImage())
                            return;

                        var image = System.Windows.Forms.Clipboard.GetImage();
                        if (image == null)
                            return;

                        // Create designs directory if it doesn't exist
                        var designsDir = Path.Combine(PluginInterface.ConfigDirectory.FullName, "Designs");
                        Directory.CreateDirectory(designsDir);

                        // Generate filename with timestamp
                        var fileName = $"design_{designId:N}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                        imagePath = Path.Combine(designsDir, fileName);

                        // Save the image as PNG
                        image.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
                        Log.Information($"Saved clipboard image to: {imagePath}");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Failed to save clipboard image: {ex.Message}");
                    }
                });
                
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                return imagePath;
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to process clipboard image: {ex.Message}");
                return "";
            }
        }

    }
}
