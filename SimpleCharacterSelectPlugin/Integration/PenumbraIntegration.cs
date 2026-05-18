using System;
using System.Collections.Generic;
using System.Linq;
using Penumbra.Api.Api;
using SimpleCharacterSelectPlugin.Managers;

namespace SimpleCharacterSelectPlugin.Integration
{
    /// <summary>
    /// Penumbra API Collection Types
    /// </summary>
    public enum ApiCollectionType : byte
    {
        Yourself = 0,
        Current = 0xE2,
        Default = 0xE0,
        Interface = 0xE1
    }

    /// <summary>
    /// Penumbra API Error Codes
    /// </summary>
    public enum PenumbraApiEc
    {
        Success = 0,
        NothingChanged = 1,
        CollectionMissing = 2,
        InvalidArgument = 11,
        UnknownError = 255
    }

    /// <summary>
    /// Handles integration with Penumbra API for collection management and mod tagging
    /// </summary>
    public static class PenumbraIntegration
    {
        public static bool IsPenumbraAvailable()
        {
            try
            {
                // Check if Penumbra is available
                var version = PenumbraIpc.ApiVersion.InvokeFunc();

                if (version < 5)
                {
                    Plugin.Log.Debug($"Penumbra API version {version} is too old, requires version 5+");
                    return false;
                }

                Plugin.Log.Debug($"Penumbra API v{version} is live");
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"Failed to initialize Penumbra API: {ex.Message}");
                return false;
            }

            return true;
        }

        // Switch penumbra collection and set the Penumbra UI to it
        public static void SwitchCollection(string collectionName)
        {
            if (!IsPenumbraAvailable())
            {
                Plugin.Log.Warning("Penumbra API not available for collection switching");
                return;
            }

            var local = Plugin.ObjectTable.LocalPlayer;
            if (local == null) return;

            try
            {
                // First, get all available collections to find the GUID
                var collections = GetAvailableCollections();
                var targetCollection = collections.FirstOrDefault(kvp => kvp.Value == collectionName);

                if (targetCollection.Key == Guid.Empty)
                {
                    Plugin.Log.Warning($"Collection '{collectionName}' not found in available collections");
                    return;
                }

                // Use the correct SetCollection API signature
                // Only set "Current" to update the Penumbra UI display (collection assignment already works)
                Plugin.Log.Debug(
                    $"Setting Penumbra UI current collection - Name: {collectionName}, GUID: {targetCollection.Key}");

                var (resultInt, _) = PenumbraIpc.SetCollectionForObject.InvokeFunc(
                    local.ObjectIndex,
                    targetCollection.Key,
                    false,
                    false
                );

                if ((PenumbraApiEc)resultInt == PenumbraApiEc.Success)
                {
                    Plugin.Log.Debug($"Penumbra setCollection redraw: {resultInt}");
                    GameCommandManager.ExecuteCommand("/penumbra redraw self");
                }

                if (targetCollection.Key != Guid.Empty)
                {
                    (resultInt, _) = PenumbraIpc.SetCollection.InvokeFunc(
                        (byte)ApiCollectionType.Current,
                        targetCollection.Key,
                        false,
                        false
                    );
                }

                var result = (PenumbraApiEc)resultInt;

                Plugin.Log.Debug($"Penumbra setCollection result: {result}");

                if (result == PenumbraApiEc.Success || result == PenumbraApiEc.NothingChanged)
                {
                    Plugin.Log.Information(
                        $"Successfully switched Penumbra to collection: {collectionName} (GUID: {targetCollection.Key})");
                    return;
                }
                else
                {
                    Plugin.Log.Warning($"Failed to switch Penumbra collection '{collectionName}': {result}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error switching to collection '{collectionName}': {ex.Message}");
                return;
            }
        }

        public static void ResetCollectionToDefault(ushort objectIndex)
        {
            if (!IsPenumbraAvailable())
            {
                Plugin.Log.Warning("Penumbra API not available for collection reset");
                return;
            }

            try
            {
                // Step 1: Get the collection assigned to "Your Character" type
                var fallbackCollection = PenumbraIpc.GetCollection.InvokeFunc((byte)ApiCollectionType.Yourself);

                if (fallbackCollection == null)
                {
                    Plugin.Log.Warning("No collection assigned to 'Your Character' in Penumbra, presuming Default");
                    fallbackCollection = PenumbraIpc.GetCollection.InvokeFunc((byte)ApiCollectionType.Default);
                }

                Plugin.Log.Debug(
                    $"Found fallback collection: {fallbackCollection.Value.Name} ({fallbackCollection.Value.Id})");

                // Step 2: Apply that collection to the player object
                var (resultInt, oldCollection) = PenumbraIpc.SetCollectionForObject.InvokeFunc(
                    objectIndex, // The player's object index
                    fallbackCollection.Value.Id, // The GUID of "Your Character" collection
                    false, // Don't allow creation
                    false // Don't allow deletion
                );

                var result = (PenumbraApiEc)resultInt;

                Plugin.Log.Debug(
                    $"ResetCollectionToDefault result: {result}, old collection: {oldCollection?.Name ?? "none"}");

                if (result == PenumbraApiEc.Success || result == PenumbraApiEc.NothingChanged)
                {
                    Plugin.Log.Debug(
                        $"Successfully switched to 'Your Character' collection: {fallbackCollection.Value.Name}");

                    // Also update the Penumbra UI to display this collection
                    var (uiResultInt, _) = PenumbraIpc.SetCollection.InvokeFunc(
                        (byte)ApiCollectionType.Current, // Set as current collection (UI display)
                        fallbackCollection.Value.Id, // Collection GUID
                        false, // Don't allow creation
                        false // Don't allow deletion
                    );

                    var uiResult = (PenumbraApiEc)uiResultInt;
                    Plugin.Log.Debug($"SetCollection(Current) UI update result: {uiResult}");

                    GameCommandManager.ExecuteCommand("/penumbra redraw self");
                }
                else
                {
                    Plugin.Log.Warning($"Failed to reset Penumbra collection: {result}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error resetting Penumbra collection: {ex.Message}");
            }
        }

        public static Dictionary<Guid, string> GetAvailableCollections()
        {
            if (!IsPenumbraAvailable())
                return new Dictionary<Guid, string>();

            try
            {
                return PenumbraIpc.GetCollections.InvokeFunc();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"Error getting Penumbra collections: {ex}");
                return new Dictionary<Guid, string>();
            }
        }

        /// <summary>
        /// Get the collection actually affecting the player character (most accurate method)
        /// </summary>
        public static (bool success, Guid collectionId, string collectionName) GetPlayerCollection()
        {
            if (!IsPenumbraAvailable())
                return (false, Guid.Empty, string.Empty);

            try
            {
                // Use GetCollectionForObject with object ID 0 (player) - most accurate method
                var (objectValid, individualSet, (id, name)) =
                    PenumbraIpc.GetCollectionsForObject.InvokeFunc(0); // 0 = player object

                if (objectValid)
                {
                    return (true, id, name);
                }

                Plugin.Log.Warning("Player object not valid for collection detection");
                return (false, Guid.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                Plugin.Log.Debug($"GetCollectionForObject.V5 failed: {ex.Message}, trying fallback");

                // Fallback to the older method
                return GetCurrentCollectionFallback();
            }
        }

        /// <summary>
        /// Fallback collection detection method
        /// </summary>
        public static (bool success, Guid collectionId, string collectionName) GetCurrentCollectionFallback()
        {
            try
            {
                // Try the GetCollection method
                var result =
                    PenumbraIpc.GetCollection
                        .InvokeFunc(0); // 0 = current character/yourself (ApiCollectionType.Current)

                if (result?.Item1 != null && result?.Item2 != null)
                {
                    Plugin.Log.Information($"Fallback: got current collection: {result.Value.Item2}");
                    return (true, result.Value.Item1, result.Value.Item2);
                }

                // Final fallback: use first available collection
                var collections = GetAvailableCollections();
                if (collections.Any())
                {
                    var firstCollection = collections.First();
                    Plugin.Log.Information(
                        $"Final fallback: using first available collection: {firstCollection.Value}");
                    return (true, firstCollection.Key, firstCollection.Value);
                }

                return (false, Guid.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error($"All collection detection methods failed: {ex}");
                return (false, Guid.Empty, string.Empty);
            }
        }

        /// <summary>
        /// Get the current collection ID safely (legacy method for backward compatibility)
        /// </summary>
        public static (bool success, Guid collectionId, string collectionName) GetCurrentCollection()
        {
            return GetPlayerCollection();
        }
    }
}