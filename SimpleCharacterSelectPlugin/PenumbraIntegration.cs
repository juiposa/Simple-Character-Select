using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin;

namespace SimpleCharacterSelectPlugin
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
    public class PenumbraIntegration : IDisposable
    {
        private readonly IPluginLog log;
        private readonly IDalamudPluginInterface pluginInterface;
        private readonly IClientState clientState;

        // Availability check
        private ICallGateSubscriber<int>? penumbraApiVersion;

        // Event subscribers for mod cache updates - using EventSubscriber pattern
        private IDisposable? modAddedSubscriber;
        private IDisposable? modDeletedSubscriber;
        private IDisposable? modMovedSubscriber;

        // Static debounce mechanism for mod deletion warnings (shared across instances)
        private static readonly Dictionary<string, DateTime> recentModDeletionWarnings = new();
        private static readonly object debounceLock = new object();
        private readonly TimeSpan debounceTime = TimeSpan.FromSeconds(5); // Increased from 2 to 5 seconds

        public bool IsPenumbraAvailable { get; private set; }

        public PenumbraIntegration(IDalamudPluginInterface pluginInterface, IPluginLog log, IClientState clientState)
        {
            this.pluginInterface = pluginInterface;
            this.log = log;
            this.clientState = clientState;

            InitializePenumbraAPI();
        }
        
        private void InitializePenumbraAPI()
        {
            try
            {
                // Check if Penumbra is available
                penumbraApiVersion = pluginInterface.GetIpcSubscriber<int>("Penumbra.ApiVersion");
                var version = penumbraApiVersion.InvokeFunc();
                
                if (version < 5)
                {
                    log.Warning($"Penumbra API version {version} is too old, requires version 5+");
                    return;
                }
                
                IsPenumbraAvailable = true;
                log.Information($"Penumbra API v{version} integration initialized successfully");
            }
            catch (Exception ex)
            {
                log.Warning($"Failed to initialize Penumbra API: {ex.Message}");
                IsPenumbraAvailable = false;
            }
        }
        
        
        // Copy of Penumbra.Api EventSubscriber for single parameter events
        private sealed class EventSubscriber<T1> : IDisposable
        {
            private readonly string _label;
            private readonly IPluginLog _log;
            private readonly Dictionary<Action<T1>, Action<T1>> _delegates = new();
            private ICallGateSubscriber<T1, object?>? _subscriber;
            private bool _disabled;

            public EventSubscriber(IDalamudPluginInterface pi, string label, IPluginLog log, params Action<T1>[] actions)
            {
                _label = label;
                _log = log;
                try
                {
                    _subscriber = pi.GetIpcSubscriber<T1, object?>(label);
                    foreach (var action in actions)
                        Event += action;

                    _disabled = false;
                }
                catch (Exception e)
                {
                    _log.Error($"Error registering IPC Subscriber for {label}\n{e}");
                    _subscriber = null;
                }
            }

            public event Action<T1> Event
            {
                add
                {
                    if (_subscriber != null && !_delegates.ContainsKey(value))
                    {
                        void Action(T1 a)
                        {
                            try
                            {
                                value(a);
                            }
                            catch (Exception e)
                            {
                                _log.Error($"Exception invoking IPC event {_label}:\n{e}");
                            }
                        }

                        if (_delegates.TryAdd(value, Action) && !_disabled)
                            _subscriber.Subscribe(Action);
                    }
                }
                remove
                {
                    if (_subscriber != null && _delegates.Remove(value, out var action))
                        _subscriber.Unsubscribe(action);
                }
            }

            public void Dispose()
            {
                if (!_disabled)
                {
                    if (_subscriber != null)
                        foreach (var action in _delegates.Values)
                            _subscriber.Unsubscribe(action);

                    _disabled = true;
                }
                _subscriber = null;
                _delegates.Clear();
            }
        }

        // Copy of Penumbra.Api EventSubscriber for two parameter events
        private sealed class EventSubscriber<T1, T2> : IDisposable
        {
            private readonly string _label;
            private readonly IPluginLog _log;
            private readonly Dictionary<Action<T1, T2>, Action<T1, T2>> _delegates = new();
            private ICallGateSubscriber<T1, T2, object?>? _subscriber;
            private bool _disabled;

            public EventSubscriber(IDalamudPluginInterface pi, string label, IPluginLog log, params Action<T1, T2>[] actions)
            {
                _label = label;
                _log = log;
                try
                {
                    _subscriber = pi.GetIpcSubscriber<T1, T2, object?>(label);
                    foreach (var action in actions)
                        Event += action;

                    _disabled = false;
                }
                catch (Exception e)
                {
                    _log.Error($"Error registering IPC Subscriber for {label}\n{e}");
                    _subscriber = null;
                }
            }

            public event Action<T1, T2> Event
            {
                add
                {
                    if (_subscriber != null && !_delegates.ContainsKey(value))
                    {
                        void Action(T1 a, T2 b)
                        {
                            try
                            {
                                value(a, b);
                            }
                            catch (Exception e)
                            {
                                _log.Error($"Exception invoking IPC event {_label}:\n{e}");
                            }
                        }

                        if (_delegates.TryAdd(value, Action) && !_disabled)
                            _subscriber.Subscribe(Action);
                    }
                }
                remove
                {
                    if (_subscriber != null && _delegates.Remove(value, out var action))
                        _subscriber.Unsubscribe(action);
                }
            }

            public void Dispose()
            {
                if (!_disabled)
                {
                    if (_subscriber != null)
                        foreach (var action in _delegates.Values)
                            _subscriber.Unsubscribe(action);

                    _disabled = true;
                }
                _subscriber = null;
                _delegates.Clear();
            }
        }
        
        private void OnModAdded(string modDirectoryName)
        {
            try
            {
                log.Information($"New mod detected: {modDirectoryName}");
            }
            catch (Exception ex)
            {
                log.Error($"Error processing added mod {modDirectoryName}: {ex}");
            }
        }
        
        /// <summary>
        /// Switch the Penumbra UI to display the specified collection and set it as current
        /// This fixes both the collection assignment and the UI display
        /// </summary>
        public bool SwitchCollection(string collectionName)
        {
            if (!IsPenumbraAvailable)
            {
                log.Warning("Penumbra API not available for collection switching");
                return false;
            }
            
            try
            {
                // First, get all available collections to find the GUID
                var collections = GetAvailableCollections();
                var targetCollection = collections.FirstOrDefault(kvp => kvp.Value == collectionName);
                
                if (targetCollection.Key == Guid.Empty)
                {
                    log.Warning($"Collection '{collectionName}' not found in available collections");
                    return false;
                }
                
                // Use the correct SetCollection API signature
                // Only set "Current" to update the Penumbra UI display (collection assignment already works)
                var setCollectionIpc = pluginInterface.GetIpcSubscriber<byte, Guid?, bool, bool, (int, (Guid Id, string Name)?)>("Penumbra.SetCollection");
                
                log.Debug($"Setting Penumbra UI current collection - Name: {collectionName}, GUID: {targetCollection.Key}");
                
                // Set the current/UI collection for display only
                var (resultInt, oldCollection) = setCollectionIpc.InvokeFunc(
                    (byte)ApiCollectionType.Current,  // Set as current collection (controls UI display only)
                    targetCollection.Key,       // Collection GUID
                    false,                      // Don't allow creation
                    false                       // Don't allow deletion
                );
                
                var result = (PenumbraApiEc)resultInt;
                
                log.Debug($"SetCollection(Current) result: {result}");
                
                if (result == PenumbraApiEc.Success || result == PenumbraApiEc.NothingChanged)
                {
                    log.Information($"Successfully switched Penumbra UI to collection: {collectionName} (GUID: {targetCollection.Key})");
                    return true;
                }
                else
                {
                    log.Warning($"Failed to switch Penumbra collection '{collectionName}': {result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error switching to collection '{collectionName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reset the player's collection to the one assigned to "Your Character" in Penumbra's Collection Assignments.
        /// </summary>
        public bool ResetCollectionToDefault()
        {
            if (!IsPenumbraAvailable)
            {
                log.Warning("Penumbra API not available for collection reset");
                return false;
            }

            try
            {
                // Get local player's object index
                var localPlayer = Plugin.ObjectTable.LocalPlayer;
                if (localPlayer == null)
                {
                    log.Warning("No local player for collection reset");
                    return false;
                }

                int objectIndex = (int)localPlayer.ObjectIndex;

                // Step 1: Get the collection assigned to "Your Character" type
                var getCollectionIpc = pluginInterface.GetIpcSubscriber<byte, (Guid Id, string Name)?>("Penumbra.GetCollection");
                var yourCharacterCollection = getCollectionIpc.InvokeFunc((byte)ApiCollectionType.Yourself);

                if (yourCharacterCollection == null)
                {
                    log.Warning("No collection assigned to 'Your Character' in Penumbra");
                    return false;
                }

                log.Debug($"Found 'Your Character' collection: {yourCharacterCollection.Value.Name} ({yourCharacterCollection.Value.Id})");

                // Step 2: Apply that collection to the player object
                var setCollectionForObjectIpc = pluginInterface.GetIpcSubscriber<int, Guid?, bool, bool, (int, (Guid Id, string Name)?)>("Penumbra.SetCollectionForObject.V5");

                var (resultInt, oldCollection) = setCollectionForObjectIpc.InvokeFunc(
                    objectIndex,                        // The player's object index
                    yourCharacterCollection.Value.Id,   // The GUID of "Your Character" collection
                    false,                              // Don't allow creation
                    false                               // Don't allow deletion
                );

                var result = (PenumbraApiEc)resultInt;

                log.Debug($"ResetCollectionToDefault result: {result}, old collection: {oldCollection?.Name ?? "none"}");

                if (result == PenumbraApiEc.Success || result == PenumbraApiEc.NothingChanged)
                {
                    log.Information($"Successfully switched to 'Your Character' collection: {yourCharacterCollection.Value.Name}");

                    // Also update the Penumbra UI to display this collection
                    var setCollectionIpc = pluginInterface.GetIpcSubscriber<byte, Guid?, bool, bool, (int, (Guid Id, string Name)?)>("Penumbra.SetCollection");
                    var (uiResultInt, _) = setCollectionIpc.InvokeFunc(
                        (byte)ApiCollectionType.Current,      // Set as current collection (UI display)
                        yourCharacterCollection.Value.Id,     // Collection GUID
                        false,                                // Don't allow creation
                        false                                 // Don't allow deletion
                    );

                    var uiResult = (PenumbraApiEc)uiResultInt;
                    log.Debug($"SetCollection(Current) UI update result: {uiResult}");

                    return true;
                }
                else
                {
                    log.Warning($"Failed to reset Penumbra collection: {result}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error resetting Penumbra collection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get list of all available Penumbra collections
        /// </summary>
        public Dictionary<Guid, string> GetAvailableCollections()
        {
            if (!IsPenumbraAvailable)
                return new Dictionary<Guid, string>();
            
            try
            {
                var ipc = pluginInterface.GetIpcSubscriber<Dictionary<Guid, string>>("Penumbra.GetCollections.V5");
                return ipc.InvokeFunc();
            }
            catch (Exception ex)
            {
                log.Error($"Error getting Penumbra collections: {ex}");
                return new Dictionary<Guid, string>();
            }
        }
        
        /// <summary>
        /// Get the collection actually affecting the player character (most accurate method)
        /// </summary>
        public (bool success, Guid collectionId, string collectionName) GetPlayerCollection()
        {
            if (!IsPenumbraAvailable)
                return (false, Guid.Empty, string.Empty);
            
            try
            {
                // Use GetCollectionForObject with object ID 0 (player) - most accurate method
                var ipc = pluginInterface.GetIpcSubscriber<int, (bool, bool, (Guid, string))>("Penumbra.GetCollectionForObject.V5");
                var (objectValid, individualSet, (id, name)) = ipc.InvokeFunc(0); // 0 = player object
                
                if (objectValid)
                {
                    return (true, id, name);
                }
                
                log.Warning("Player object not valid for collection detection");
                return (false, Guid.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                log.Debug($"GetCollectionForObject.V5 failed: {ex.Message}, trying fallback");
                
                // Fallback to the older method
                return GetCurrentCollectionFallback();
            }
        }
        
        /// <summary>
        /// Fallback collection detection method
        /// </summary>
        private (bool success, Guid collectionId, string collectionName) GetCurrentCollectionFallback()
        {
            try
            {
                // Try the GetCollection method
                var ipc = pluginInterface.GetIpcSubscriber<byte, (Guid, string)?>("Penumbra.GetCollection");
                var result = ipc.InvokeFunc(0); // 0 = current character/yourself (ApiCollectionType.Current)
                
                if (result?.Item1 != null && result?.Item2 != null)
                {
                    log.Information($"Fallback: got current collection: {result.Value.Item2}");
                    return (true, result.Value.Item1, result.Value.Item2);
                }
                
                // Final fallback: use first available collection
                var collections = GetAvailableCollections();
                if (collections.Any())
                {
                    var firstCollection = collections.First();
                    log.Information($"Final fallback: using first available collection: {firstCollection.Value}");
                    return (true, firstCollection.Key, firstCollection.Value);
                }
                
                return (false, Guid.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                log.Error($"All collection detection methods failed: {ex}");
                return (false, Guid.Empty, string.Empty);
            }
        }
        
        /// <summary>
        /// Get the current collection ID safely (legacy method for backward compatibility)
        /// </summary>
        public (bool success, Guid collectionId, string collectionName) GetCurrentCollection()
        {
            return GetPlayerCollection();
        }
        
        public void Dispose()
        {
            // Dispose event subscriptions
            try
            {
                modAddedSubscriber?.Dispose();
                modDeletedSubscriber?.Dispose();
                modMovedSubscriber?.Dispose();
            }
            catch (Exception ex)
            {
                log.Warning($"Error during event subscription disposal: {ex.Message}");
            }
            
            // Dispose of IPC subscribers if needed
            penumbraApiVersion = null;
            modAddedSubscriber = null;
            modDeletedSubscriber = null;
            modMovedSubscriber = null;
        }

    }
    
}