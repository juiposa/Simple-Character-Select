using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Serilog;
using SimpleCharacterSelectPlugin.Windows.Styles;
using SimpleCharacterSelectPlugin.Windows.Utils;
using SimpleCharacterSelectPlugin;
using SimpleCharacterSelectPlugin.Managers;
using SimpleCharacterSelectPlugin.Models;

namespace SimpleCharacterSelectPlugin.Windows.Components
{
    public class CharacterGrid : IDisposable
    {
        private Plugin plugin;
        private UIStyles uiStyles;
        private Dictionary<int, float> hoverAnimations = new();
        private bool showSearchBar = false;
        private string searchQuery = "";
        private string selectedTag = "All";
        private bool showTagFilter = false;

        // Pagination
        private int currentPage = 0;
        private int charactersPerPage = 40;
        private List<(int characterIndex, Vector2 min, Vector2 max)> cardRects = new();
        private int? currentDropTargetIndex = null;
        private bool cardRectsDirty = true;

        // Performance optimizations
        private List<Character> cachedFilteredCharacters = new();
        private List<Character> cachedPagedCharacters = new();
        private string lastSearchQuery = "";
        private string lastSelectedTag = "All";
        private int lastCharacterCount = 0;
        private bool filterCacheDirty = true;

        // Cache UI calculations
        private float cachedCardWidth = 0f;
        private int cachedColumnCount = 0;
        private float cachedColumnWidth = 0f;
        private float cachedAvailableWidth = 0f;
        private float cachedScale = 0f;
        private bool layoutCacheDirty = true;

        // Cache expensive string operations
        private readonly Dictionary<string, bool> fileExistsCache = new();
        private readonly Dictionary<string, Vector2> textSizeCache = new();
        private volatile bool isCacheWarming = false;

        // Frame limiting for animations
        private float lastAnimationUpdate = 0f;
        private const float AnimationUpdateInterval = 1f / 60f; // 60 FPS max

        // Ghost image state
        private Character? draggedCharacter = null;
        private Vector2 ghostImageSize = new Vector2(120f, 120f);
        private float ghostImageAlpha = 0.8f;

        public Plugin.SortType CurrentSort { get; private set; }

        public CharacterGrid(Plugin plugin, UIStyles uiStyles)
        {
            this.plugin = plugin;
            this.uiStyles = uiStyles;
            CurrentSort = (Plugin.SortType)plugin.Configuration.CurrentSortIndex;
        }

        public void Dispose()
        {
            // Clear caches
            fileExistsCache.Clear();
            textSizeCache.Clear();
            //characterFavoriteEffects.Clear();
        }

        public void Draw()
        {
            // Calculate responsive scaling using Dalamud's GlobalScale
            var totalScale = GetSafeScale(ImGuiHelpers.GlobalScale * plugin.Configuration.UIScaleMultiplier);

            ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None;
            
            DrawToolbar(totalScale);
            DrawCharacterGridContent(totalScale);

            DrawPagination(totalScale);
        }

        private void DrawToolbar(float scale)
        {
            if (!plugin.WindowState.IsAddCharacterWindowOpen)
            {
                float buttonHeight = 25f * scale;

                if (ImGui.Button("Add Character", new Vector2(0, buttonHeight)))
                {
                    plugin.MainWindow.OpenAddCharacterWindow();
                    
                    InvalidateCache();
                }

                plugin.WindowState.AddCharacterButtonPos = ImGui.GetItemRectMin();
                plugin.WindowState.AddCharacterButtonSize = ImGui.GetItemRectSize();

                DrawSearchAndFilters(scale);
            }
        }

        private void DrawSearchAndFilters(float scale)
        {
            float tagDropdownWidth = 200f * scale;
            float tagIconOffset = 70f * scale;
            float tagDropdownOffset = tagDropdownWidth + tagIconOffset + (10f * scale);
            float buttonSize = 25f * scale;

            // Tag Filter Toggle (hidden when search bar is open)
            if (!showSearchBar)
            {
                ImGui.SameLine(ImGui.GetWindowWidth() - tagIconOffset - (20f * scale));
                if (uiStyles.IconButton("\uf0b0", "Filter by Tags"))
                {
                    showTagFilter = !showTagFilter;
                    InvalidateCache();
                }

                // Tag Filter Dropdown
                if (showTagFilter)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - tagDropdownOffset - (20f * scale));
                    ImGui.SetNextItemWidth(tagDropdownWidth);
                    if (ImGui.BeginCombo("##TagFilter", selectedTag))
                    {
                        var allTags = plugin.Characters
                            .SelectMany(c => c.Data.Tags ?? new List<string>())
                            .Distinct()
                            .OrderBy(f => f)
                            .Prepend("All")
                            .ToList();

                        foreach (var tag in allTags)
                        {
                            bool isSelected = tag == selectedTag;
                            if (ImGui.Selectable(tag, isSelected))
                            {
                                selectedTag = tag;
                                InvalidateFilterCache();
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }
                }
            }

            // Search Button
            ImGui.SameLine(ImGui.GetWindowWidth() - (55f * scale));
            if (uiStyles.IconButton("\uf002", "Search for a Character"))
            {
                showSearchBar = !showSearchBar;
                if (!showSearchBar)
                {
                    searchQuery = "";
                    InvalidateFilterCache();
                }
                else
                {
                    // Close tag filter when opening search
                    showTagFilter = false;
                }
            }

            // Search Input Field
            if (showSearchBar)
            {
                ImGui.SameLine(ImGui.GetWindowWidth() - (265f * scale));
                ImGui.SetNextItemWidth(210f * scale);
                if (ImGui.InputTextWithHint("##SearchCharacters", "Search characters...", ref searchQuery, 100))
                    InvalidateFilterCache();
            }
        }

        private void DrawCharacterGridContent(float scale)
        {
            var filteredCharacters = GetFilteredCharacters();
            var pagedCharacters = GetPagedCharacters(filteredCharacters);

            float availableWidth = ImGui.GetContentRegionAvail().X;
            if (Math.Abs(availableWidth - cachedAvailableWidth) > 1f || 
                Math.Abs(scale - cachedScale) > 0.01f || 
                layoutCacheDirty)
            {
                RecalculateLayout(availableWidth, scale);
            }

            float cardWidth = cachedCardWidth;
            int columnCount = cachedColumnCount;

            // Centre the grid horizontally
            float columnWidth = cardWidth + (plugin.WindowState.ProfileSpacing * scale) + (24f * scale);
            float totalGridWidth = columnCount > 1
                ? columnCount * columnWidth
                : cardWidth;
            float horizontalIndent = Math.Max(17f * scale, (availableWidth - totalGridWidth) / 2f);
            float verticalMargin = 17f * scale;

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalMargin);
            ImGui.Indent(horizontalIndent);

            if (columnCount > 1)
            {
                ImGui.Columns(columnCount, "CharacterGrid", false);
                for (int i = 0; i < columnCount; i++)
                {
                    ImGui.SetColumnWidth(i, columnWidth);
                }
            }

            bool shouldRebuildRects = cardRectsDirty || pagedCharacters.Count != cardRects.Count;

            if (shouldRebuildRects)
            {
                RebuildCardRects(pagedCharacters, cardWidth, scale);
            }

            // Draw character cards
            for (int i = 0; i < pagedCharacters.Count; i++)
            {
                var character = pagedCharacters[i];
                int realCharacterIndex = plugin.Characters.IndexOf(character);
                if (realCharacterIndex == -1) continue;

                DrawCharacterCard(character, realCharacterIndex, cardWidth, scale);

                if (columnCount > 1)
                    ImGui.NextColumn();
            }

            // Reset columns
            if (columnCount > 1)
            {
                ImGui.Columns(1);
            }

            ImGui.Unindent(horizontalIndent);
        }

        private void RecalculateLayout(float availableWidth, float scale)
        {
            float profileSpacing = plugin.WindowState.ProfileSpacing * scale;
            int columnCount = plugin.WindowState.ProfileColumns;

            if (plugin.WindowState.IsDesignPanelOpen)
            {
                columnCount = Math.Max(1, columnCount - 1);
            }

            float cardWidth = 250 * plugin.WindowState.ProfileImageScale * scale;
            float borderMargin = 12f * scale;
            float totalCardWidth = cardWidth + (borderMargin * 2);
            float columnWidth = totalCardWidth + profileSpacing;

            // Ensure column count fits within available space
            columnCount = Math.Max(1, Math.Min(columnCount, (int)(availableWidth / columnWidth)));

            // Cache the results
            cachedCardWidth = cardWidth;
            cachedColumnCount = columnCount;
            cachedColumnWidth = columnWidth;
            cachedAvailableWidth = availableWidth;
            cachedScale = scale;
            layoutCacheDirty = false;
        }

        private void RebuildCardRects(List<Character> pagedCharacters, float cardWidth, float scale)
        {
            cardRects.Clear();
            for (int i = 0; i < pagedCharacters.Count; i++)
            {
                var character = pagedCharacters[i];
                int realCharacterIndex = plugin.Characters.IndexOf(character);
                if (realCharacterIndex == -1) continue;

                var cardStartPos = ImGui.GetCursorScreenPos();
                float nameplateHeight = 70 * scale;
                float imageHeight = cardWidth;
                float totalCardHeight = imageHeight + nameplateHeight;
                var cardMin = cardStartPos;
                var cardMax = cardStartPos + new Vector2(cardWidth, totalCardHeight);

                cardRects.Add((realCharacterIndex, cardMin, cardMax));
            }
            cardRectsDirty = false;
        }

        private void DrawCharacterCard(Character character, int index, float cardWidth, float scale)
        {
            cardWidth = Math.Clamp(cardWidth, 64 * scale, 512 * scale);
            float nameplateHeight = 70 * scale;
            float imageHeight = cardWidth;
            float totalCardHeight = imageHeight + nameplateHeight;
            float spacing = 12f * scale;

            string pluginDirectory = plugin.PluginDirectory;
            string defaultImagePath = Path.Combine(pluginDirectory, "Assets", "Default.png");

            string finalImagePath = GetCachedImagePath(character.Data.ImagePath, defaultImagePath);

            // Check if this character is the main character
            bool isMainCharacter = !string.IsNullOrEmpty(plugin.Configuration.MainCharacterName) &&
                                   character.Data.Name == plugin.Configuration.MainCharacterName;

            ImGui.BeginGroup();

            var cardStartPos = ImGui.GetCursorScreenPos();
            var cardMin = cardStartPos;
            var cardMax = cardStartPos + new Vector2(cardWidth, totalCardHeight);

            ImGui.Dummy(new Vector2(cardWidth, totalCardHeight));
            var cardArea = ImGui.GetItemRectMin();

            ImGui.SetCursorScreenPos(cardArea);
            ImGui.InvisibleButton($"##CharCard{index}", new Vector2(cardWidth, imageHeight));
            bool isHovered = ImGui.IsItemHovered();

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                HandleCharacterClick(character, index);
            }

            if (ImGui.BeginPopupContextItem($"##ContextMenu_{character.Data.Name}"))
            {
                DrawContextMenu(character, scale);
                ImGui.EndPopup();
            }

            float hoverAmount = UpdateHoverAnimation(index, isHovered);

            Vector3 borderColor = character.Data.NameplateColor;
            
            float borderIntensity = 0.6f + hoverAmount * 0.4f;
            
            // Apply wiggle offset to card positions

            var borderMargin = (4f + (hoverAmount * 2f)) * scale;
            uiStyles.DrawGlowingBorder(
                cardMin - new Vector2(borderMargin, borderMargin),
                cardMax + new Vector2(borderMargin, borderMargin),
                borderColor,
                borderIntensity,
                isHovered
            );

            var drawList = ImGui.GetWindowDrawList();
            
            uint cardBgColor = ImGui.GetColorU32(new Vector4(0.12f, 0.12f, 0.12f, 0.95f));
            drawList.AddRectFilled(cardMin, cardMax, cardBgColor, 12f * scale);

            var imageArea = cardMin;
            var imageAreaSize = new Vector2(cardWidth, imageHeight);

            if (!string.IsNullOrEmpty(finalImagePath))
            {
                var texture = Plugin.TextureProvider.GetFromFile(finalImagePath).GetWrapOrDefault();

                if (texture != null)
                {
                    float originalWidth = texture.Width;
                    float originalHeight = texture.Height;
                    float aspectRatio = originalWidth / originalHeight;

                    float imageAreaWidth = imageAreaSize.X - (8 * scale);
                    float imageAreaHeight = imageAreaSize.Y - (8 * scale);

                    float displayWidth, displayHeight;
                    if (aspectRatio > 1)
                    {
                        displayWidth = imageAreaWidth;
                        displayHeight = imageAreaWidth / aspectRatio;
                        if (displayHeight > imageAreaHeight)
                        {
                            displayHeight = imageAreaHeight;
                            displayWidth = imageAreaHeight * aspectRatio;
                        }
                    }
                    else
                    {
                        displayHeight = imageAreaHeight;
                        displayWidth = imageAreaHeight * aspectRatio;
                        if (displayWidth > imageAreaWidth)
                        {
                            displayWidth = imageAreaWidth;
                            displayHeight = imageAreaWidth / aspectRatio;
                        }
                    }

                    float hoverScale = plugin.Configuration.EnableCharacterHoverEffects
                        ? 1f + (0.05f * hoverAmount)
                        : 1f;

                    float finalWidth = displayWidth * hoverScale;
                    float finalHeight = displayHeight * hoverScale;

                    float paddingX = (imageAreaSize.X - finalWidth) / 2;
                    float paddingY = (imageAreaSize.Y - finalHeight) / 2;
                    float liftOffset = -2f * hoverAmount * scale; 

                    var imagePos = imageArea + new Vector2(paddingX, paddingY + liftOffset);
                    var imagePosMax = imagePos + new Vector2(finalWidth, finalHeight);

                    // For high-resolution images, use slightly inset UVs to improve sampling quality
                    Vector2 uvMin = new Vector2(0, 0);
                    Vector2 uvMax = new Vector2(1, 1);
                    
                    // Detect very large textures that might look crunchy when downscaled
                    bool isHighRes = originalWidth > 1920 || originalHeight > 1080;
                    if (isHighRes)
                    {
                        // Use slightly inset UV coordinates to avoid edge artifacts and improve sampling
                        float uvInset = 0.001f; // Very small inset to avoid sampling edge pixels
                        uvMin = new Vector2(uvInset, uvInset);
                        uvMax = new Vector2(1.0f - uvInset, 1.0f - uvInset);
                    }

                    drawList.AddImageRounded(
                        (ImTextureID)texture.Handle,
                        imagePos,
                        imagePosMax,
                        uvMin,
                        uvMax,
                        ImGui.GetColorU32(new Vector4(1, 1, 1, 1)),
                        8f * scale,
                        ImDrawFlags.RoundCornersTop
                    );

                    if (isMainCharacter && plugin.Configuration.ShowMainCharacterCrown)
                    {
                        DrawMainCharacterCrown(drawList, imagePosMax, imagePos, hoverAmount, scale);
                    }
                }
            }
            else
            {
                var textPos = imageArea + imageAreaSize / 2 - new Vector2(30 * scale, 10 * scale); // Scale text position
                drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(0.7f, 0.7f, 0.7f, 1f)), "No Image");
            }

            DrawIntegratedNameplate(character, cardMin, cardWidth, imageHeight, nameplateHeight, index, hoverAmount, scale);

            ImGui.EndGroup();
            ImGui.Dummy(new Vector2(0, spacing));
        }

        private string GetCachedImagePath(string? characterImagePath, string defaultImagePath)
        {
            if (!string.IsNullOrEmpty(characterImagePath))
            {
                bool exists;
                lock (fileExistsCache)
                {
                    if (!fileExistsCache.TryGetValue(characterImagePath, out exists))
                    {
                        // Not in cache yet - check synchronously (should be rare if pre-warm ran)
                        exists = File.Exists(characterImagePath);
                        fileExistsCache[characterImagePath] = exists;
                    }
                }

                if (exists)
                    return characterImagePath;
            }

            bool defaultExists;
            lock (fileExistsCache)
            {
                if (!fileExistsCache.TryGetValue(defaultImagePath, out defaultExists))
                {
                    defaultExists = File.Exists(defaultImagePath);
                    fileExistsCache[defaultImagePath] = defaultExists;
                }
            }

            return defaultExists ? defaultImagePath : "";
        }

        private Vector2 GetCachedTextSize(string text)
        {
            if (!textSizeCache.TryGetValue(text, out Vector2 size))
            {
                size = ImGui.CalcTextSize(text);
                textSizeCache[text] = size;
            }
            return size;
        }

        private void DrawMainCharacterCrown(ImDrawListPtr drawList, Vector2 imagePosMax, Vector2 imagePos, float hoverAmount, float scale)
        {
            float crownBadgeSize = 32f * scale;
            var badgePos = new Vector2(
                imagePosMax.X - crownBadgeSize - (4 * scale),
                imagePos.Y + (4 * scale)
            );
            var badgeCenter = badgePos + new Vector2(crownBadgeSize / 2, crownBadgeSize / 2);

            uint badgeBg = ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.7f));
            drawList.PathClear();
            drawList.PathArcTo(badgeCenter, crownBadgeSize / 2 + (2 * scale), 0, MathF.PI * 2);
            drawList.PathFillConvex(badgeBg);

            uint badgeRing = ImGui.GetColorU32(new Vector4(1f, 0.8f, 0.2f, 0.9f + hoverAmount * 0.1f));
            drawList.PathClear();
            drawList.PathArcTo(badgeCenter, crownBadgeSize / 2, 0, MathF.PI * 2);
            drawList.PathStroke(badgeRing, ImDrawFlags.Closed, 3f * scale);

            ImGui.PushFont(UiBuilder.IconFont);
            string crownSymbol = "\uf521";
            var crownSize = GetCachedTextSize(crownSymbol);

            var crownPos = new Vector2(
                badgeCenter.X - crownSize.X / 2 + (1f * scale),
                badgeCenter.Y - crownSize.Y / 2 - (1f * scale)
            );

            uint crownGlow = ImGui.GetColorU32(new Vector4(1f, 0.8f, 0.2f, 0.6f + hoverAmount * 0.4f));
            drawList.AddText(crownPos + new Vector2(1 * scale, 1 * scale), crownGlow, crownSymbol);

            uint crownColor = ImGui.GetColorU32(new Vector4(1f, 0.9f, 0.3f, 1f));
            drawList.AddText(crownPos, crownColor, crownSymbol);

            ImGui.PopFont();
        }

        private void DrawIntegratedNameplate(Character character, Vector2 cardMin, float cardWidth, float imageHeight, float nameplateHeight, int characterIndex, float hoverAmount, float scale)
        {
            var drawList = ImGui.GetWindowDrawList();

            var nameplateMin = new Vector2(cardMin.X, cardMin.Y + imageHeight);
            var nameplateMax = new Vector2(cardMin.X + cardWidth, cardMin.Y + imageHeight + nameplateHeight);

            uint nameplateColor = ImGui.GetColorU32(new Vector4(0.08f, 0.08f, 0.08f, 0.95f));
            drawList.AddRectFilled(nameplateMin, nameplateMax, nameplateColor, 12f * scale, ImDrawFlags.RoundCornersBottom);

            var accentMin = new Vector2(nameplateMin.X + (6 * scale), nameplateMin.Y + (2 * scale));
            var accentMax = new Vector2(nameplateMax.X - (6 * scale), nameplateMin.Y + (6 * scale));
            uint accentColor = ImGui.GetColorU32(new Vector4(character.Data.NameplateColor.X, character.Data.NameplateColor.Y, character.Data.NameplateColor.Z, 0.9f + hoverAmount * 0.3f));
            drawList.AddRectFilled(accentMin, accentMax, accentColor, 3f * scale);

            float topRowY = nameplateMin.Y + (12 * scale);

            // Favourite Star/Ghost/Snowflake
            string starSymbol;
            
            // TODO duplicated
            starSymbol = character.Data.IsFavorite ? "★" : "☆"; // Default stars
            
            var starPos = new Vector2(nameplateMin.X + (8 * scale), topRowY);
            var starSize = GetCachedTextSize(starSymbol);

            // Get star colors based on seasonal theme
            Vector4 starMainColor, starGlowColor;
            
            // Default colours
            if (character.Data.IsFavorite)
            {
                starMainColor = new Vector4(1f, 0.9f, 0.2f, 1f); // Gold
                starGlowColor = new Vector4(1f, 0.8f, 0f, 0.5f + hoverAmount * 0.3f);
            }
            else
            {
                starMainColor = new Vector4(0.5f, 0.5f, 0.5f, 0.7f + hoverAmount * 0.3f); // Grey
                starGlowColor = starMainColor;
            }

            if (character.Data.IsFavorite)
            {
                uint starGlow = ImGui.GetColorU32(starGlowColor);
                drawList.AddText(starPos + new Vector2(1 * scale, 1 * scale), starGlow, starSymbol);
            }

            uint starColor = ImGui.GetColorU32(starMainColor);
            drawList.AddText(starPos, starColor, starSymbol);

            var starHitMin = starPos - new Vector2(2 * scale, 2 * scale);
            var starHitMax = starPos + starSize + new Vector2(2 * scale, 2 * scale);
            if (ImGui.IsMouseHoveringRect(starHitMin, starHitMax))
            {
                ImGui.SetTooltip($"{(character.Data.IsFavorite ? "Remove" : "Add")} {character.Data.Name} as a Favourite");

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    var actualCharacter = plugin.Characters[characterIndex];
                    actualCharacter.Data.IsFavorite = !actualCharacter.Data.IsFavorite;

                    Vector2 effectPos = starPos + starSize / 2;
                    //characterFavoriteEffects[characterIndex].Trigger(effectPos, actualCharacter.IsFavorite, plugin.Configuration);

                    plugin.SaveConfiguration();
                    SortCharacters();
                }
            }

            // Character Name - with truncation for narrow cards
            float availableNameWidth = cardWidth - (70 * scale); // Space between star and RP icon
            string displayName = LayoutHelper.ClampText(character.Data.Name, availableNameWidth, "...");

            var textSize = GetCachedTextSize(displayName);
            var textPos = new Vector2(
                nameplateMin.X + (cardWidth - textSize.X) / 2,
                topRowY
            );

            drawList.AddText(textPos + new Vector2(1 * scale, 1 * scale), ImGui.GetColorU32(new Vector4(0, 0, 0, 0.8f)), displayName);
            drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(0.95f, 0.95f, 0.95f, 1f)), displayName);

            // Buttons!!
            float bottomRowY = nameplateMin.Y + (35 * scale);
            float btnWidth = (cardWidth - (32 * scale)) / 3;
            float btnHeight = 22 * scale;
            float btnSpacing = 8 * scale;

            // Responsive button labels - switch to icons when buttons are too narrow
            float buttonPadding = 12 * scale;
            float designsTextWidth = ImGui.CalcTextSize("Designs").X + buttonPadding;
            bool useIcons = btnWidth < designsTextWidth;

            // FontAwesome icons for compact mode
            string designsIcon = "\uf07c";  // folder-open
            string editIcon = "\uf044";     // edit/pencil
            string deleteIcon = "\uf2ed";   // trash-alt

            ImGui.SetCursorScreenPos(new Vector2(nameplateMin.X + (8 * scale), bottomRowY));
            
            // Default button styling
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.15f, 0.15f, 0.9f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.25f, 0.25f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.35f, 0.35f, 0.35f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.9f, 0.9f, 1.0f));
            int buttonColorCount = 4;
                
            // Custom theme: don't push any button colours - use the main window style colours
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f * scale);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4 * scale, 2 * scale)); // Symmetric padding for centered text
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));

            var buttonPos = ImGui.GetCursorScreenPos();
            var buttonSize = new Vector2(btnWidth, btnHeight);

            // Scale down icons to be smaller
            float iconScale = 0.85f;

            // Designs button
            if (useIcons)
            {
                ImGui.SetWindowFontScale(iconScale);
                ImGui.PushFont(UiBuilder.IconFont);
            }
            if (ImGui.Button(useIcons ? $"{designsIcon}##{character.Data.Name}" : $"Designs##{character.Data.Name}", new Vector2(btnWidth, btnHeight)))
            {
                int realIndex = plugin.Characters.IndexOf(character);
                if (realIndex >= 0)
                    plugin.MainWindow.OpenDesignPanel(realIndex);
            }
            if (useIcons)
            {
                ImGui.PopFont();
                ImGui.SetWindowFontScale(1.0f);
            }
            if (useIcons && ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Designs");
                ImGui.EndTooltip();
            }

            // Store for tutorial
            if (plugin.Characters.IndexOf(character) == 0)
            {
                plugin.WindowState.FirstCharacterDesignsButtonPos = buttonPos;
                plugin.WindowState.FirstCharacterDesignsButtonSize = buttonSize;
            }

            ImGui.SameLine(0, btnSpacing);

            // Declare once for both Edit and Delete buttons
            bool isCtrlShiftPressed = ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;

            // Edit button
            if (useIcons)
            {
                ImGui.SetWindowFontScale(iconScale);
                ImGui.PushFont(UiBuilder.IconFont);
            }
            if (ImGui.Button(useIcons ? $"{editIcon}##{character.Data.Name}" : $"Edit##{character.Data.Name}", new Vector2(btnWidth, btnHeight)))
            {
                int realIndex = plugin.Characters.IndexOf(character);
                if (realIndex >= 0)
                {
                    // Always open edit window (either with converted or original macro)
                    plugin.MainWindow.OpenEditCharacterWindow(realIndex);
                }
            }
            if (useIcons)
            {
                ImGui.PopFont();
                ImGui.SetWindowFontScale(1.0f);
            }
            if (useIcons && ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Edit");
                ImGui.EndTooltip();
            }

            ImGui.SameLine(0, btnSpacing);

            // Delete button
            if (useIcons)
            {
                ImGui.SetWindowFontScale(iconScale);
                ImGui.PushFont(UiBuilder.IconFont);
            }
            if (ImGui.Button(useIcons ? $"{deleteIcon}##{character.Data.Name}" : $"Delete##{character.Data.Name}", new Vector2(btnWidth, btnHeight)))
            {
                if (isCtrlShiftPressed)
                {
                    plugin.Characters.Remove(character);
                    plugin.Configuration.Save();
                    InvalidateCache();
                }
            }
            if (useIcons)
            {
                ImGui.PopFont();
                ImGui.SetWindowFontScale(1.0f);
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                if (useIcons)
                    ImGui.Text("Delete - Hold Ctrl + Shift and click");
                else
                    ImGui.Text("Hold Ctrl + Shift and click to delete.");
                ImGui.EndTooltip();
            }

            ImGui.PopStyleVar(3);
            if (buttonColorCount > 0)
            {
                ImGui.PopStyleColor(buttonColorCount);
            }
        }
        
        private void DrawContextMenu(Character character, float scale)
        {
            if (ImGui.Selectable("Apply to Target"))
            {
                // Get target on main thread, then apply in background
                var target = plugin.GetCurrentTarget();
                if (target == null)
                {
                    Plugin.ChatGui.PrintError("[Simple Character Select] No target selected.");
                }
                else
                {
                    var targetInfo = new { ObjectIndex = target.ObjectIndex, ObjectKind = target.ObjectKind, Name = target.Name?.ToString() ?? "Unknown" };
                    
                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            await plugin.ApplyToTarget(character, -1);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.Error($"Error applying character to target: {ex}");
                        }
                    });
                }
            }

            bool isMainCharacter = !string.IsNullOrEmpty(plugin.Configuration.MainCharacterName) &&
                                   character.Data.Name == plugin.Configuration.MainCharacterName;

            ImGui.Separator();
            if (isMainCharacter)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.8f, 0.2f, 1f));

                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text("\uf521");
                ImGui.PopFont();

                ImGui.SameLine(0, 4 * scale);
                if (ImGui.Selectable("Remove as Main Character"))
                {
                    plugin.Configuration.MainCharacterName = null;
                    plugin.Configuration.Save();
                    InvalidateCache();
                }

                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.8f, 0.2f, 1f));

                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text("\uf521");
                ImGui.PopFont();

                ImGui.SameLine(0, 4 * scale);
                if (ImGui.Selectable("Set as Main Character"))
                {
                    plugin.Configuration.MainCharacterName = character.Data.Name;
                    plugin.Configuration.Save();
                    InvalidateCache();
                }

                ImGui.PopStyleColor();
            }

            ImGui.Spacing();
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(character.Data.NameplateColor, 1.0f));
            ImGui.BeginChild($"##Separator_{character.Data.Name}", new Vector2(ImGui.GetContentRegionAvail().X, 3 * scale), false);
            ImGui.EndChild();
            ImGui.PopStyleColor();
            ImGui.Spacing();

            if (character.Data.Designs.Count > 0)
            {
                float itemHeight = ImGui.GetTextLineHeightWithSpacing();
                float maxVisible = 10;
                float scrollHeight = Math.Min(character.Data.Designs.Count, maxVisible) * itemHeight + (8 * scale);

                if (ImGui.BeginChild($"##DesignScroll_{character.Data.Name}", new Vector2(300 * scale, scrollHeight)))
                {
                    foreach (var design in character.Data.Designs)
                    {
                        if (ImGui.Selectable($"Apply Design: {design.Name}"))
                        {
                            // Get target on main thread, then apply design in background
                            var target = plugin.GetCurrentTarget();
                            if (target == null)
                            {
                                Plugin.ChatGui.PrintError("[Simple Character Select] No target selected.");
                            }
                            else
                            {
                                var designIndex = character.Data.Designs.IndexOf(design);
                                var targetInfo = new { ObjectIndex = target.ObjectIndex, ObjectKind = target.ObjectKind, Name = target.Name?.ToString() ?? "Unknown" };
                                
                                _ = System.Threading.Tasks.Task.Run(async () =>
                                {
                                    try
                                    {
                                        await plugin.ApplyToTarget(character, designIndex);
                                    }
                                    catch (Exception ex)
                                    {
                                        Plugin.Log.Error($"Error applying design to target: {ex}");
                                    }
                                });
                            }
                        }
                    }
                    ImGui.EndChild();
                }
            }
        }
        private void DrawPagination(float scale)
        {
            var filteredCharacters = GetFilteredCharacters();

            if (filteredCharacters.Count <= charactersPerPage)
            {
                currentPage = 0;
                return;
            }

            int totalPages = (int)Math.Ceiling((double)filteredCharacters.Count / charactersPerPage);
            if (totalPages <= 1) return;

            var pagedCharacters = GetPagedCharacters(filteredCharacters);

            // For sparse pages, add extra spacing to push pagination down
            if (pagedCharacters.Count <= 4)
            {
                float availableHeight = ImGui.GetContentRegionAvail().Y;
                float minSpacingForPagination = availableHeight * 0.4f; // Push to bottom 40% of remaining space

                ImGui.Dummy(new Vector2(0, Math.Max(50f * scale, minSpacingForPagination)));
            }
            else
            {
                // Normal spacing for full pages
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();
            }

            // Rest of pagination code stays the same...
            float windowWidth = ImGui.GetContentRegionAvail().X;
            float buttonWidth = 30f * scale;
            float buttonHeight = 25f * scale;
            float buttonSpacing = 8f * scale;
            float arrowButtonWidth = 25f * scale;

            int maxPageButtons = 10;
            int startPage = Math.Max(0, currentPage - maxPageButtons / 2);
            int endPage = Math.Min(totalPages - 1, startPage + maxPageButtons - 1);
            if (endPage - startPage + 1 < maxPageButtons)
            {
                startPage = Math.Max(0, endPage - maxPageButtons + 1);
            }

            int visiblePageCount = endPage - startPage + 1;
            float totalWidth = arrowButtonWidth + buttonSpacing + (visiblePageCount * (buttonWidth + buttonSpacing)) + arrowButtonWidth;
            float startX = Math.Max(10f * scale, (windowWidth - totalWidth) / 2);

            ImGui.SetCursorPosX(startX);

            // Check if Custom theme is active - if so, use main window colours instead of pushing overrides
            int paginationArrowColorCount = 0;
            
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.2f, 0.2f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            paginationArrowColorCount = 3;

            bool canGoPrev = currentPage > 0;
            if (!canGoPrev)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            }

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button("\uf053", new Vector2(arrowButtonWidth, buttonHeight)) && canGoPrev)
            {
                currentPage--;
                InvalidateCache();
            }
            ImGui.PopFont();

            if (!canGoPrev)
            {
                ImGui.PopStyleVar();
            }

            if (ImGui.IsItemHovered() && canGoPrev)
            {
                ImGui.SetTooltip("Previous page");
            }

            ImGui.SameLine(0, buttonSpacing);

            // Page number buttons
            for (int i = startPage; i <= endPage; i++)
            {
                bool isCurrentPage = i == currentPage;
                int pageButtonColorCount = 0;


                if (isCurrentPage)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.4f, 0.6f, 1.0f, 0.8f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.7f, 1.0f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.3f, 0.5f, 0.9f, 1.0f));
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.15f, 0.15f, 0.8f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.25f, 0.25f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.35f, 0.35f, 0.35f, 1.0f));
                }
                pageButtonColorCount = 3;
                

                string pageLabel = (i + 1).ToString();
                if (ImGui.Button(pageLabel, new Vector2(buttonWidth, buttonHeight)))
                {
                    currentPage = i;
                    InvalidateCache();
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"Go to page {i + 1}");
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                if (pageButtonColorCount > 0)
                {
                    ImGui.PopStyleColor(pageButtonColorCount);
                }

                if (i < endPage)
                {
                    ImGui.SameLine(0, buttonSpacing);
                }
            }

            ImGui.SameLine(0, buttonSpacing);

            // Next button
            bool canGoNext = currentPage < totalPages - 1;
            if (!canGoNext)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            }

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button("\uf054", new Vector2(arrowButtonWidth, buttonHeight)) && canGoNext)
            {
                currentPage++;
                InvalidateCache();
            }
            ImGui.PopFont();

            if (!canGoNext)
            {
                ImGui.PopStyleVar();
            }

            if (ImGui.IsItemHovered() && canGoNext)
            {
                ImGui.SetTooltip("Next page");
            }

            if (paginationArrowColorCount > 0)
            {
                ImGui.PopStyleColor(paginationArrowColorCount);
            }

            // Page info text
            ImGui.Spacing();
            string pageInfo = $"Page {currentPage + 1} of {totalPages} ({filteredCharacters.Count} characters)";
            var textSize = ImGui.CalcTextSize(pageInfo);
            ImGui.SetCursorPosX(Math.Max(10f * scale, (windowWidth - textSize.X) / 2));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.7f, 1.0f));
            ImGui.Text(pageInfo);
            ImGui.PopStyleColor();

            ImGui.Spacing();
            ImGui.Spacing();
        }

        private void ReorderCharacters(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex || fromIndex < 0 || toIndex < 0 ||
                fromIndex >= plugin.Characters.Count || toIndex >= plugin.Characters.Count)
                return;

            var character = plugin.Characters[fromIndex];

            plugin.Characters.RemoveAt(fromIndex);

            int insertIndex;
            if (fromIndex < toIndex)
            {
                insertIndex = toIndex - 1;
            }
            else
            {
                insertIndex = toIndex;
            }

            insertIndex = Math.Clamp(insertIndex, 0, plugin.Characters.Count);
            plugin.Characters.Insert(insertIndex, character);

            for (int i = 0; i < plugin.Characters.Count; i++)
            {
                plugin.Characters[i].Data.SortOrder = i;
            }

            plugin.Configuration.CurrentSortIndex = (int)Plugin.SortType.Manual;
            plugin.SaveConfiguration();

            Plugin.Log.Debug($"[DragDrop] Moved character '{character.Data.Name}' from position {fromIndex} to {insertIndex} (target was {toIndex})");
        }

        private void HandleCharacterClick(Character character, int index)
        {
            if (plugin.WindowState.IsDesignPanelOpen)
            {
                plugin.WindowState.IsDesignPanelOpen = false;
            }
            
            // Switch gearset if assigned at character level
            if (plugin.Configuration.EnableGearsetCharacterSwitching && character.Data.AssignedGearset.HasValue)
            {
                //plugin.SwitchToGearset(character.Data.AssignedGearset.Value); TODO
            }
            
            plugin.ActivePlayer.QueueUpdate(character);
            
            plugin.QuickSwitchWindow.RefreshSelection();
        }
        
        private List<Character> GetFilteredCharacters()
        {
            if (filterCacheDirty ||
                searchQuery != lastSearchQuery ||
                selectedTag != lastSelectedTag ||
                plugin.Characters.Count != lastCharacterCount)
            {
                RecalculateFilteredCharacters();
            }

            return cachedFilteredCharacters;
        }
        private float GetSafeScale(float baseScale)
        {
            return Math.Clamp(baseScale, 0.3f, 5.0f);
        }

        private void RecalculateFilteredCharacters()
        {
            var characters = plugin.Characters.AsEnumerable();

            // Apply tag filter
            if (selectedTag != "All")
            {
                characters = characters.Where(c => c.Data.Tags?.Contains(selectedTag) ?? false);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                characters = characters.Where(c =>
                    c.Data.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            cachedFilteredCharacters = characters.ToList();

            lastSearchQuery = searchQuery;
            lastSelectedTag = selectedTag;
            lastCharacterCount = plugin.Characters.Count;
            filterCacheDirty = false;
        }

        private List<Character> GetPagedCharacters(List<Character> filteredCharacters)
        {
            int startIndex = currentPage * charactersPerPage;
            var pagedResult = filteredCharacters.Skip(startIndex).Take(charactersPerPage).ToList();

            if (cachedPagedCharacters == null || !cachedPagedCharacters.SequenceEqual(pagedResult))
            {
                cachedPagedCharacters = pagedResult;
            }

            return cachedPagedCharacters;
        }

        private float UpdateHoverAnimation(int characterIndex, bool isHovered)
        {
            if (!hoverAnimations.ContainsKey(characterIndex))
                hoverAnimations[characterIndex] = 0f;

            float target = isHovered ? 1f : 0f;
            float current = hoverAnimations[characterIndex];

            // Only update if there's a significant change
            if (Math.Abs(target - current) > 0.01f)
            {
                float speed = 8f;
                current = current + (target - current) * ImGui.GetIO().DeltaTime * speed;
                current = Math.Clamp(current, 0f, 1f);
                hoverAnimations[characterIndex] = current;
            }

            return current;
        }
        
        public void SortCharacters()
        {
            if (CurrentSort == Plugin.SortType.Favorites)
            {
                plugin.Characters.Sort((a, b) =>
                {
                    int favCompare = b.Data.IsFavorite.CompareTo(a.Data.IsFavorite);
                    if (favCompare != 0) return favCompare;
                    return string.Compare(a.Data.Name, b.Data.Name, StringComparison.OrdinalIgnoreCase);
                });
            }
            else if (CurrentSort == Plugin.SortType.Manual)
            {
                plugin.Characters.Sort((a, b) => a.Data.SortOrder.CompareTo(b.Data.SortOrder));
            }
            else if (CurrentSort == Plugin.SortType.Alphabetical)
            {
                plugin.Characters.Sort((a, b) => string.Compare(a.Data.Name, b.Data.Name, StringComparison.OrdinalIgnoreCase));
            }
            else if (CurrentSort == Plugin.SortType.Recent)
            {
                plugin.Characters.Sort((a, b) => b.Data.DateAdded.CompareTo(a.Data.DateAdded));
            }
            else if (CurrentSort == Plugin.SortType.Oldest)
            {
                plugin.Characters.Sort((a, b) => a.Data.DateAdded.CompareTo(b.Data.DateAdded));
            }

            InvalidateCache();
        }


        public void SetSortType(Plugin.SortType sortType)
        {
            CurrentSort = sortType;
            SortCharacters();
        }

        public void InvalidateCache()
        {
            cardRectsDirty = true;
            layoutCacheDirty = true;
            InvalidateFilterCache();
        }

        private void InvalidateFilterCache()
        {
            filterCacheDirty = true;
        }

        // Method to clear file cache when needed
        public void ClearFileCache()
        {
            fileExistsCache.Clear();
        }

        /// <summary>
        /// Pre-warms the file exists cache on a background thread.
        /// This prevents UI freezing when opening the window for the first time,
        /// especially for images on network paths.
        /// </summary>
        public void PreWarmCacheAsync()
        {
            if (isCacheWarming) return;
            isCacheWarming = true;

            Task.Run(() =>
            {
                try
                {
                    var characters = plugin.Configuration.Characters;
                    string pluginDirectory = plugin.PluginDirectory;
                    string defaultImagePath = Path.Combine(pluginDirectory, "Assets", "Default.png");

                    // Pre-check default image
                    var defaultExists = File.Exists(defaultImagePath);
                    lock (fileExistsCache)
                    {
                        fileExistsCache[defaultImagePath] = defaultExists;
                    }

                    // Pre-check all character images
                    foreach (var character in characters.ToList())
                    {
                        if (!string.IsNullOrEmpty(character.Data.ImagePath))
                        {
                            var exists = File.Exists(character.Data.ImagePath);
                            lock (fileExistsCache)
                            {
                                fileExistsCache[character.Data.ImagePath] = exists;
                            }
                        }

                        // Also check design preview images
                        foreach (var design in character.Data.Designs ?? Enumerable.Empty<CharacterDesign>())
                        {
                            if (!string.IsNullOrEmpty(design.PreviewImagePath))
                            {
                                var exists = File.Exists(design.PreviewImagePath);
                                lock (fileExistsCache)
                                {
                                    fileExistsCache[design.PreviewImagePath] = exists;
                                }
                            }
                        }
                    }

                    Plugin.Log.Info($"[CharacterGrid] Pre-warmed file cache for {fileExistsCache.Count} paths");
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"[CharacterGrid] Error pre-warming cache: {ex.Message}");
                }
                finally
                {
                    isCacheWarming = false;
                }
            });
        }

        // Method to clear text cache when font changes
        public void ClearTextCache()
        {
            textSizeCache.Clear();
        }

        /// <summary>Returns currently visible characters (respects search and tag filters).</summary>
        public List<Character> GetVisibleCharacters()
        {
            return GetFilteredCharacters();
        }

    }
}
