using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Ipc;
using SimpleCharacterSelectPlugin.IPC;

namespace SimpleCharacterSelectPlugin.Managers
{
    /// <summary>
    /// Provides cached lists of available options from integrated plugins.
    /// Used to populate autocomplete dropdowns in character/design forms.
    /// </summary>
    public class IntegrationListProvider : IDisposable
    {
        private readonly Plugin plugin;

        // Cached lists
        private List<string> cachedPenumbraCollections = new();
        private List<string> cachedGlamourerDesigns = new();
        private List<string> cachedCustomizePlusProfiles = new();
        private List<string> cachedMoodlesPresets = new();
        private List<string> cachedHonorificTitles = new();

        // Cache timestamps
        private DateTime lastPenumbraRefresh = DateTime.MinValue;
        private DateTime lastGlamourerRefresh = DateTime.MinValue;
        private DateTime lastCustomizePlusRefresh = DateTime.MinValue;
        private DateTime lastMoodlesRefresh = DateTime.MinValue;
        private DateTime lastHonorificRefresh = DateTime.MinValue;

        // Cache duration
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

        public IntegrationListProvider(Plugin plugin)
        {
            this.plugin = plugin;
        }

        /// <summary>Gets available Penumbra collections.</summary>
        public IReadOnlyList<string> GetPenumbraCollections(bool forceRefresh = false)
        {
            if (!forceRefresh && DateTime.Now - lastPenumbraRefresh < CacheDuration && cachedPenumbraCollections.Count > 0)
            {
                return cachedPenumbraCollections;
            }

            try
            {
                var collections = PenumbraIpc.GetCollections.InvokeFunc();
                if (collections != null)
                {
                    cachedPenumbraCollections = collections.Values
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    lastPenumbraRefresh = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"[IntegrationListProvider] Failed to get Penumbra collections: {ex.Message}");
            }

            return cachedPenumbraCollections;
        }

        /// <summary>Gets available Glamourer designs.</summary>
        public IReadOnlyList<string> GetGlamourerDesigns(bool forceRefresh = false)
        {
            if (!forceRefresh && DateTime.Now - lastGlamourerRefresh < CacheDuration && cachedGlamourerDesigns.Count > 0)
            {
                return cachedGlamourerDesigns;
            }

            try
            {
                var designs = GlamourerIpc.GetDesigns?.InvokeFunc();
                if (designs != null)
                {
                    cachedGlamourerDesigns = designs.Values
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    lastGlamourerRefresh = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"[IntegrationListProvider] Failed to get Glamourer designs: {ex.Message}");
            }

            return cachedGlamourerDesigns;
        }

        /// <summary>Gets available Customize+ profiles.</summary>
        public IReadOnlyList<string> GetCustomizePlusProfiles(bool forceRefresh = false)
        {
            if (!forceRefresh && DateTime.Now - lastCustomizePlusRefresh < CacheDuration && cachedCustomizePlusProfiles.Count > 0)
            {
                return cachedCustomizePlusProfiles;
            }

            try
            {
                var profiles = CustomizeIpc.GetProfileList?.InvokeFunc();
                if (profiles != null)
                {
                    // Profile tuple: (Guid id, string name, string characterName, IList<...> characters, int priority, bool enabled)
                    cachedCustomizePlusProfiles = profiles
                        .Select(p => p.Item2) // Item2 is the profile name
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct()
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    lastCustomizePlusRefresh = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"[IntegrationListProvider] Failed to get Customize+ profiles: {ex.Message}");
            }

            return cachedCustomizePlusProfiles;
        }

        /// <summary>Gets available Moodles presets.</summary>
        public IReadOnlyList<string> GetMoodlesPresets(bool forceRefresh = false)
        {
            if (!forceRefresh && DateTime.Now - lastMoodlesRefresh < CacheDuration && cachedMoodlesPresets.Count > 0)
            {
                return cachedMoodlesPresets;
            }

            try
            {
                var presets = MoodlesIpc.GetPresets?.InvokeFunc();
                if (presets != null)
                {
                    // Preset tuple: (Guid ID, string FullPath)
                    // Use the full path as Moodles commands require it (e.g., "Chars/Male/Rayven/Rayven")
                    cachedMoodlesPresets = presets
                        .Select(p => p.Item2) // Use full path, not just the name
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct()
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    lastMoodlesRefresh = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"[IntegrationListProvider] Failed to get Moodles presets: {ex.Message}");
            }

            return cachedMoodlesPresets;
        }

        /// <summary>
        /// Gets available Honorific titles for the current character.
        /// Note: Honorific titles are per-character, not global.
        /// </summary>
        public IReadOnlyList<string> GetHonorificTitles(bool forceRefresh = false)
        {
            if (!forceRefresh && DateTime.Now - lastHonorificRefresh < CacheDuration && cachedHonorificTitles.Count > 0)
            {
                return cachedHonorificTitles;
            }

            try
            {
                var localPlayer = Plugin.ObjectTable?.LocalPlayer;
                if (localPlayer == null)
                {
                    return cachedHonorificTitles;
                }

                var name = localPlayer.Name.TextValue;
                var worldId = localPlayer.HomeWorld.RowId;

                var titles = HonorificIpc.GetCharacterTitleList?.InvokeFunc(name, worldId);
                if (titles != null)
                {
                    // TitleData has a Title property - we need to extract it via reflection or dynamic
                    cachedHonorificTitles = titles
                        .Select(t => ExtractTitleFromTitleData(t))
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct()
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    lastHonorificRefresh = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"[IntegrationListProvider] Failed to get Honorific titles: {ex.Message}");
            }

            return cachedHonorificTitles;
        }

        /// <summary>Gets the currently active Penumbra collection for the local player.</summary>
        public string? GetCurrentPenumbraCollection()
        {
            try
            {
                var result = plugin.PenumbraIntegration?.GetCurrentCollection();
                if (result.HasValue && result.Value.success && !string.IsNullOrEmpty(result.Value.collectionName))
                {
                    return result.Value.collectionName;
                }
            }
            catch
            {
                // Silently fail - this is called frequently during UI rendering
            }
            return null;
        }

        /// <summary>Gets the currently active Customize+ profile name for the local player.</summary>
        public string? GetCurrentCustomizePlusProfile()
        {
            try
            {
                var profileName = GetCurrentCustomizePlusProfileName();
                if (!string.IsNullOrEmpty(profileName))
                {
                    return profileName;
                }
            }
            catch
            {
                // Silently fail - this is called frequently during UI rendering
            }
            return null;
        }
        
        /// <summary>Gets the name of the currently active Customize+ profile for the local player.</summary>
        public string? GetCurrentCustomizePlusProfileName()
        {
            try
            {
                var localPlayer = Plugin.ObjectTable?.LocalPlayer;
                if (localPlayer == null) return null;

                // Get active profile GUID
                var activeResult = CustomizeIpc.GetActiveProfile.InvokeFunc((ushort)localPlayer.ObjectIndex);

                if (activeResult.Item1 != 0 || !activeResult.Item2.HasValue || activeResult.Item2.Value == Guid.Empty)
                    return null;

                var activeProfileId = activeResult.Item2.Value;

                // Get profile list and find the matching profile
                var profileList = CustomizeIpc.GetProfileList.InvokeFunc();

                if (profileList.Count == 0) return null;

                // Find the profile by GUID and return its name (Item2)
                var activeProfile = profileList.FirstOrDefault(p => p.Item1 == activeProfileId);
                if (activeProfile.Item1 != Guid.Empty && !string.IsNullOrWhiteSpace(activeProfile.Item2))
                {
                    return activeProfile.Item2;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Extracts title string from Honorific TitleData object.</summary>
        private static string ExtractTitleFromTitleData(object titleData)
        {
            if (titleData == null)
                return "";

            try
            {
                // Try to get Title property via reflection
                var titleProperty = titleData.GetType().GetProperty("Title");
                if (titleProperty != null)
                {
                    return titleProperty.GetValue(titleData)?.ToString() ?? "";
                }

                // Try as a field
                var titleField = titleData.GetType().GetField("Title");
                if (titleField != null)
                {
                    return titleField.GetValue(titleData)?.ToString() ?? "";
                }
            }
            catch
            {
                // Silently fail
            }

            return "";
        }

        /// <summary>Forces refresh of all caches.</summary>
        public void RefreshAll()
        {
            GetPenumbraCollections(true);
            GetGlamourerDesigns(true);
            GetCustomizePlusProfiles(true);
            GetMoodlesPresets(true);
            GetHonorificTitles(true);
        }

        /// <summary>Clears all caches.</summary>
        public void ClearCaches()
        {
            cachedPenumbraCollections.Clear();
            cachedGlamourerDesigns.Clear();
            cachedCustomizePlusProfiles.Clear();
            cachedMoodlesPresets.Clear();
            cachedHonorificTitles.Clear();

            lastPenumbraRefresh = DateTime.MinValue;
            lastGlamourerRefresh = DateTime.MinValue;
            lastCustomizePlusRefresh = DateTime.MinValue;
            lastMoodlesRefresh = DateTime.MinValue;
            lastHonorificRefresh = DateTime.MinValue;
        }

        public void Dispose()
        {
            ClearCaches();
        }
    }
}
