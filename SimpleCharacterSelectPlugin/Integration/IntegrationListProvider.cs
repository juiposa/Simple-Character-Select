using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleCharacterSelectPlugin.Integration
{
    /// <summary>
    /// Provides cached lists of available options from integrated plugins.
    /// Used to populate autocomplete dropdowns in character/design forms.
    /// </summary>
    public class IntegrationListProvider : IDisposable
    {
        private readonly Plugin plugin;

        // Cached lists
        public List<string> CachedPenumbraCollections = new();
        public List<string> CachedGlamourerDesigns = new();
        public List<(Guid, string)> CachedCustomizePlusProfiles = new();
        public List<(Guid, string)> CachedMoodlesPresets = new();
        public List<string> CachedHonorificTitles = new();

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
            if (!forceRefresh && DateTime.Now - lastPenumbraRefresh < CacheDuration && CachedPenumbraCollections.Count > 0)
            {
                return CachedPenumbraCollections;
            }

            try
            {
                var collections = PenumbraIpc.GetCollections.InvokeFunc();
                if (collections != null)
                {
                    CachedPenumbraCollections = collections.Values
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    lastPenumbraRefresh = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"[IntegrationListProvider] Failed to get Penumbra collections: {ex.Message}");
            }

            return CachedPenumbraCollections;
        }

        /// <summary>Gets available Glamourer designs.</summary>
        public IReadOnlyList<string> GetGlamourerDesigns(bool forceRefresh = false)
        {
            if (!forceRefresh && DateTime.Now - lastGlamourerRefresh < CacheDuration && CachedGlamourerDesigns.Count > 0)
            {
                return CachedGlamourerDesigns;
            }

            try
            {
                var designs = GlamourerIpc.GetDesigns?.InvokeFunc();
                if (designs != null)
                {
                    CachedGlamourerDesigns = designs.Values
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    lastGlamourerRefresh = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"[IntegrationListProvider] Failed to get Glamourer designs: {ex.Message}");
            }

            return CachedGlamourerDesigns;
        }

        /// <summary>Gets available Customize+ profiles.</summary>
        public IReadOnlyList<(Guid, string)> GetCustomizePlusProfiles(bool forceRefresh = false)
        {
            if (!forceRefresh && DateTime.Now - lastCustomizePlusRefresh < CacheDuration && CachedCustomizePlusProfiles.Count > 0)
            {
                return CachedCustomizePlusProfiles;
            }

            try
            {
                var profiles = CustomizeIpc.GetProfileList?.InvokeFunc();
                if (profiles != null)
                {
                    // Profile tuple: (Guid id, string name, string characterName, IList<...> characters, int priority, bool enabled)
                    CachedCustomizePlusProfiles = profiles
                        .Select(p => (p.Item1, p.Item2)) // Item2 is the profile name
                        .Where(n => !string.IsNullOrWhiteSpace(n.Item2))
                        .Distinct()
                        .OrderBy(n => n.Item2, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    lastCustomizePlusRefresh = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"[IntegrationListProvider] Failed to get Customize+ profiles: {ex.Message}");
            }

            return CachedCustomizePlusProfiles;
        }

        /// <summary>Gets available Moodles presets.</summary>
        public IReadOnlyList<(Guid, string)> GetMoodlesPresets(bool forceRefresh = false)
        {
            if (!forceRefresh && DateTime.Now - lastMoodlesRefresh < CacheDuration && CachedMoodlesPresets.Count > 0)
            {
                return CachedMoodlesPresets;
            }

            try
            {
                var presets = MoodlesIpc.GetPresets?.InvokeFunc();
                Plugin.Log.Debug($"Fetched moodle presets {string.Join(",", presets)}");
                if (presets.Count > 0)
                {
                    CachedMoodlesPresets = presets;
                    lastMoodlesRefresh = DateTime.Now;
                    Plugin.Log.Debug($"Cached Moodle Presets: {string.Join(",", CachedMoodlesPresets)}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"[IntegrationListProvider] Failed to get Moodles presets: {ex.Message}");
            }

            return CachedMoodlesPresets;
        }

        /// <summary>
        /// Gets available Honorific titles for the current character.
        /// Note: Honorific titles are per-character, not global.
        /// </summary>
        public IReadOnlyList<string> GetHonorificTitles(bool forceRefresh = false)
        {
            if (!forceRefresh && DateTime.Now - lastHonorificRefresh < CacheDuration && CachedHonorificTitles.Count > 0)
            {
                return CachedHonorificTitles;
            }

            try
            {
                var localPlayer = Plugin.ObjectTable?.LocalPlayer;
                if (localPlayer == null)
                {
                    return CachedHonorificTitles;
                }

                var name = localPlayer.Name.TextValue;
                var worldId = localPlayer.HomeWorld.RowId;

                var titles = HonorificIpc.GetCharacterTitleList?.InvokeFunc(name, worldId);
                if (titles != null)
                {
                    // TitleData has a Title property - we need to extract it via reflection or dynamic
                    CachedHonorificTitles = titles
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

            return CachedHonorificTitles;
        }

        /// <summary>Gets the currently active Penumbra collection for the local player.</summary>
        public string? GetCurrentPenumbraCollection()
        {
            try
            {
                var result = PenumbraIntegration.GetCurrentCollection();
                if (string.IsNullOrEmpty(result.collectionName))
                {
                    return result.collectionName;
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
            CachedPenumbraCollections.Clear();
            CachedGlamourerDesigns.Clear();
            CachedCustomizePlusProfiles.Clear();
            CachedMoodlesPresets.Clear();
            CachedHonorificTitles.Clear();

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
