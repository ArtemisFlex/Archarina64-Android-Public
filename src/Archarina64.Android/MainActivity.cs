#nullable enable
#pragma warning disable CS8602
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Archarina64.Core;
using System.Text;
using System.Text.Json;
using Android.Text;
using System.Globalization;
using Color = Android.Graphics.Color;

namespace Archarina64.Android;

[Activity(Label = "Archarina64 Android", MainLauncher = true, Exported = true)]
public sealed class MainActivity : Activity
{
    private const int ImportTile = 11, ImportCatalog = 12, OpenLayout = 13, ExportLayout = 14, ImportRom = 15, ExportRomPatchRequest = 16, ExportRomRequest = 17, ImportProject = 18, ExportProjectRequest = 19, ImportGeometry = 20, ImportTexture = 21;
    private readonly LayoutHistory history = new();
    private RoomViewport viewport = null!;
    private TextView status = null!, inspector = null!;
    private ListView rooms = null!;
    private ListView sceneList = null!, nativeRoomList = null!;
    private LinearLayout leftPanel = null!, rightPanel = null!, editorChrome = null!;
    private LinearLayout rightToolContent = null!;
    private readonly List<Button> leftTabButtons = [], rightTabButtons = [];
    private TextView sceneTitle = null!, roomTitle = null!, documentTitle = null!;
    private Button restoreChrome = null!;
    private int selectedSceneId = -1, selectedRoomId = -1;
    private int inlineActorIndex = -1;
    private string inlineGeometryKind = "vertex";
    private int inlineGeometryIndex = 0, inlineCommandIndex = 0;
    private string inlineCollisionKind = "vertex";
    private int inlineCollisionIndex = 0, inlineCameraPathIndex = 0;
    private string inlineSceneKind = "settings";
    private int inlineSceneIndex = 0;
    private int inlineAlternateHeaderIndex = 0, inlineAlternateCommandIndex = 0;
    private string inlineRoomKind = "settings";
    private int inlineRoomIndex = 0;
    private bool leftPanelVisible = true, rightPanelVisible;
    private string leftTab = "ROM", rightTab = "General";
    private LinearLayout pageRoot = null!;
    private readonly List<View> chromeViews = [];
    private Button fullscreenToggle = null!;
    private FrameLayout viewportFrame = null!;
    private FrameLayout actorOverlay = null!;
    private bool viewportFullscreen;
    private int selected = -1;
    private readonly HashSet<(int SceneId, int RoomId, int ActorIndex)> selectedActorKeys = [];
    private bool actorSelectionMode;
    private bool actorOverlayEnabled;
    private bool collisionOverlayEnabled;
    private bool collisionTriangleOverlayEnabled;
    private bool geometryOverlayEnabled;
    private bool nativeGeometryVisible = true;
    private bool nativeCollisionVisible = true;
    private bool busy;
    private RomDocument? rom;
    private ActorMetadataDatabase? actorDatabase;
    private RomWorkspace? romWorkspace;
    private int pendingGeometryScene = -1, pendingGeometryRoom = -1;
    private int pendingTextureScene = -1, pendingTextureRoom = -1;
    private RomTextureAsset? pendingTextureAsset;
    private byte[]? loadedRomBytes;
    private string SavePath => Path.Combine(FilesDir!.AbsolutePath, "layout.json");
    private string ExportPath => Path.Combine(FilesDir!.AbsolutePath, "export.json");

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        pageRoot = root;
        root.SetBackgroundColor(Color.Rgb(14, 19, 28));
        root.SetOnApplyWindowInsetsListener(new InsetsListener(0));
        editorChrome = new LinearLayout(this) { Orientation = Orientation.Vertical };
        editorChrome.SetBackgroundColor(Color.Rgb(28, 35, 47));
        AddToolbar(editorChrome, ("File", () => ShowTopMenu("File")), ("Edit", () => ShowTopMenu("Edit")), ("Extra", () => ShowTopMenu("Extra")), ("Window", () => ShowTopMenu("Window")), ("View", () => ShowTopMenu("View")), ("Scene", () => ShowTopMenu("Scene")), ("Room", () => ShowTopMenu("Room")), ("Actors", () => ShowTopMenu("Actors")), ("Collision", () => ShowTopMenu("Collision")), ("Geometry", () => ShowTopMenu("Geometry")));
        var quick = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        quick.SetGravity(GravityFlags.CenterVertical);
        quick.AddView(CompactButton("☰", () => SetLeftPanel(!leftPanelVisible)), new LinearLayout.LayoutParams(Dp(46), Dp(46)));
        quick.AddView(CompactButton("Open ROM", () => Pick(ImportRom)), new LinearLayout.LayoutParams(Dp(100), Dp(46)));
        documentTitle = new TextView(this) { Text = "No ROM loaded", TextSize = 13, Gravity = GravityFlags.CenterVertical };
        documentTitle.SetSingleLine(true); documentTitle.Ellipsize = global::Android.Text.TextUtils.TruncateAt.End;
        documentTitle.SetTextColor(Color.White);
        quick.AddView(documentTitle, new LinearLayout.LayoutParams(0, Dp(46), 1));
        quick.AddView(CompactButton("Tools", () => SetRightPanel(!rightPanelVisible)), new LinearLayout.LayoutParams(Dp(64), Dp(46)));
        fullscreenToggle = CompactButton("⛶", ToggleViewportFullscreen);
        quick.AddView(fullscreenToggle, new LinearLayout.LayoutParams(Dp(46), Dp(46)));
        editorChrome.AddView(quick); root.AddView(editorChrome);
        viewport = new RoomViewport(this);
        viewportFrame = new FrameLayout(this);
        viewportFrame.AddView(viewport, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
        actorOverlay = new FrameLayout(this);
        viewportFrame.AddView(actorOverlay, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
        int panelWidth = Math.Min(Dp(300), (int)(Resources!.DisplayMetrics!.WidthPixels * 0.76f));
        leftPanel = new LinearLayout(this) { Orientation = Orientation.Vertical };
        leftPanel.SetBackgroundColor(Color.Rgb(25, 33, 44));
        leftPanel.SetPadding(Dp(8), Dp(6), Dp(8), Dp(6));
        var leftHeader = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        sceneTitle = new TextView(this) { Text = "Scenes", TextSize = 17, Gravity = GravityFlags.CenterVertical };
        sceneTitle.SetTextColor(Color.Rgb(88, 222, 189)); leftHeader.AddView(sceneTitle, new LinearLayout.LayoutParams(0, Dp(44), 1));
        leftHeader.AddView(CompactButton("×", () => SetLeftPanel(false)), new LinearLayout.LayoutParams(Dp(44), Dp(44)));
        leftPanel.AddView(leftHeader);
        var leftTabs = new HorizontalScrollView(this) { HorizontalScrollBarEnabled = false };
        var leftTabRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        leftTabButtons.Add(AddTabButton(leftTabRow, "ROM scenes", () => SetLeftTab("ROM")));
        leftTabButtons.Add(AddTabButton(leftTabRow, "Layout", () => SetLeftTab("Layout")));
        leftTabs.AddView(leftTabRow); leftPanel.AddView(leftTabs, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, Dp(44)));
        sceneList = new ListView(this) { ChoiceMode = ChoiceMode.Single };
        sceneList.ItemClick += (_, e) => { var current = romWorkspace?.Document ?? rom; if (current is not null && e.Position < current.Scenes.Count) OpenSceneInViewport(current.Scenes[e.Position]); };
        leftPanel.AddView(sceneList, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1));
        roomTitle = new TextView(this) { Text = "Rooms", TextSize = 15 };
        roomTitle.SetTextColor(Color.Rgb(88, 222, 189)); roomTitle.SetPadding(Dp(4), Dp(8), 0, Dp(4)); leftPanel.AddView(roomTitle);
        nativeRoomList = new ListView(this) { ChoiceMode = ChoiceMode.Single };
        nativeRoomList.ItemClick += (_, e) => { var scene = CurrentScene(); if (scene is not null && e.Position < scene.Rooms.Count) OpenRoomInViewport(scene, scene.Rooms[e.Position]); };
        leftPanel.AddView(nativeRoomList, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1));
        rooms = new ListView(this) { ChoiceMode = ChoiceMode.Single };
        rooms.ItemClick += (_, e) => { selected = e.Position; Refresh(); };
        leftPanel.AddView(rooms, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1));
        viewportFrame.AddView(leftPanel, new FrameLayout.LayoutParams(panelWidth, ViewGroup.LayoutParams.MatchParent, GravityFlags.Left));
        rightPanel = new LinearLayout(this) { Orientation = Orientation.Vertical };
        rightPanel.SetBackgroundColor(Color.Rgb(25, 33, 44));
        rightPanel.SetPadding(Dp(8), Dp(6), Dp(8), Dp(6));
        inspector = new TextView(this) { Text = "Open a ROM to inspect scenes, rooms, actors, collision, and geometry.", TextSize = 13 };
        inspector.SetTextColor(Color.White); inspector.SetPadding(Dp(6), Dp(8), Dp(6), Dp(12));
        var rightHeader = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        var toolTitle = new TextView(this) { Text = "Inspector & tools", TextSize = 17, Gravity = GravityFlags.CenterVertical };
        toolTitle.SetTextColor(Color.Rgb(88, 222, 189)); rightHeader.AddView(toolTitle, new LinearLayout.LayoutParams(0, Dp(44), 1));
        rightHeader.AddView(CompactButton("×", () => SetRightPanel(false)), new LinearLayout.LayoutParams(Dp(44), Dp(44)));
        rightPanel.AddView(rightHeader);
        var rightTabs = new HorizontalScrollView(this) { HorizontalScrollBarEnabled = false };
        var rightTabRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        foreach (string tab in new[] { "General", "Scene", "Room", "Actors", "Collision", "Geometry", "Commands", "Tools" })
            rightTabButtons.Add(AddTabButton(rightTabRow, tab, () => SetRightTab(tab)));
        rightTabs.AddView(rightTabRow); rightPanel.AddView(rightTabs, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, Dp(44)));
        var toolScroll = new ScrollView(this); rightToolContent = new LinearLayout(this) { Orientation = Orientation.Vertical }; toolScroll.AddView(rightToolContent); rightPanel.AddView(toolScroll, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1));
        viewportFrame.AddView(rightPanel, new FrameLayout.LayoutParams(panelWidth, ViewGroup.LayoutParams.MatchParent, GravityFlags.Right));
        restoreChrome = CompactButton("Show editor", ToggleViewportFullscreen);
        var restoreLayout = new FrameLayout.LayoutParams(Dp(112), Dp(44), GravityFlags.Top | GravityFlags.Right);
        restoreLayout.SetMargins(0, Dp(8), Dp(8), 0); viewportFrame.AddView(restoreChrome, restoreLayout);
        restoreChrome.Visibility = ViewStates.Gone;
        root.AddView(viewportFrame, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1));
        status = new TextView(this) { TextSize = 11, Text = "Open a ROM to browse scenes. Drag to orbit; pinch to zoom." };
        status.SetSingleLine(true); status.Ellipsize = global::Android.Text.TextUtils.TruncateAt.End;
        status.SetTextColor(Color.Rgb(191, 203, 218)); status.SetPadding(Dp(8), Dp(2), Dp(8), Dp(2)); root.AddView(status);
        SetContentView(root);
        try { actorDatabase = ActorMetadataDatabase.LoadOoT(); } catch { actorDatabase = null; }
        try
        {
            if (File.Exists(SavePath)) history.Reset(LayoutStorage.Read(File.ReadAllText(SavePath)));
            else history.Reset(new MobileLayout { Tiles = [new LayoutTile { SourceXml = DemoRoom.Create() }] });
            selected = savedInstanceState?.GetInt("selected", 0) ?? 0;
        }
        catch (Exception error) { ShowError(error); }
        SetLeftPanel(true);
        SetRightPanel(false);
        SetLeftTab("ROM");
        SetRightTab("General");
        Refresh();
    }

    private int Dp(int value) => (int)(value * Resources!.DisplayMetrics!.Density + 0.5f);

    private void AddToolbar(LinearLayout root, params (string Text, Action Click)[] actions)
    {
        var scroll = new HorizontalScrollView(this) { HorizontalScrollBarEnabled = false };
        var row = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        foreach (var (text, action) in actions)
        {
            var button = new Button(this) { Text = text, TextSize = 12 };
            button.SetAllCaps(false);
            button.Click += (_, _) => { if (busy) return; try { action(); } catch (Exception error) { ShowError(error); } };
            row.AddView(button, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, Dp(48)));
        }
        scroll.AddView(row); root.AddView(scroll); if (ReferenceEquals(root, pageRoot)) chromeViews.Add(scroll);
    }

    private void AddSectionHeader(LinearLayout root, string text)
    {
        var header = new TextView(this) { Text = text, TextSize = 11 };
        header.SetTextColor(Color.Rgb(88, 222, 189)); header.SetTypeface(global::Android.Graphics.Typeface.Default, global::Android.Graphics.TypefaceStyle.Bold);
        header.SetPadding(Dp(4), Dp(8), Dp(4), Dp(2)); root.AddView(header); if (ReferenceEquals(root, pageRoot)) chromeViews.Add(header);
    }

    private Button CompactButton(string label, Action action)
    {
        var button = new Button(this) { Text = label, TextSize = 12 };
        button.SetAllCaps(false); button.SetPadding(Dp(2), 0, Dp(2), 0);
        button.Click += (_, _) => { if (busy) return; try { action(); } catch (Exception error) { ShowError(error); } };
        return button;
    }

    private void AddPanelButton(LinearLayout panel, string label, Action action)
        => panel.AddView(CompactButton(label, action), new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, Dp(48)));

    private Button AddTabButton(LinearLayout row, string label, Action action)
    {
        var button = CompactButton(label, action);
        button.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 11);
        button.Tag = label;
        row.AddView(button, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, Dp(44)));
        return button;
    }

    private void RefreshTabStyles()
    {
        foreach (var button in leftTabButtons)
        {
            bool active = string.Equals(button.Tag?.ToString(), leftTab == "ROM" ? "ROM scenes" : "Layout", StringComparison.Ordinal);
            button.SetBackgroundColor(active ? Color.Rgb(88, 222, 189) : Color.Rgb(76, 84, 96));
            button.SetTextColor(active ? Color.Rgb(14, 19, 28) : Color.White);
        }
        foreach (var button in rightTabButtons)
        {
            bool active = string.Equals(button.Tag?.ToString(), rightTab, StringComparison.Ordinal);
            button.SetBackgroundColor(active ? Color.Rgb(88, 222, 189) : Color.Rgb(76, 84, 96));
            button.SetTextColor(active ? Color.Rgb(14, 19, 28) : Color.White);
        }
    }

    private void SetLeftTab(string tab)
    {
        leftTab = tab;
        bool romVisible = tab == "ROM" && (romWorkspace?.Document ?? rom) is not null;
        sceneList.Visibility = romVisible ? ViewStates.Visible : ViewStates.Gone;
        roomTitle.Visibility = romVisible ? ViewStates.Visible : ViewStates.Gone;
        nativeRoomList.Visibility = romVisible ? ViewStates.Visible : ViewStates.Gone;
        rooms.Visibility = tab == "Layout" ? ViewStates.Visible : ViewStates.Gone;
        sceneTitle.Text = romVisible ? "Scenes & rooms" : "Mobile layout";
        RefreshTabStyles();
        RefreshSceneBrowser();
    }

    private void SetRightTab(string tab)
    {
        rightTab = tab;
        RefreshTabStyles();
        rightToolContent.RemoveAllViews();
        var tabTitle = new TextView(this) { Text = tab.ToUpperInvariant(), TextSize = 12, Gravity = GravityFlags.CenterVertical, LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, Dp(34)) };
        tabTitle.SetTextColor(Color.Rgb(88, 222, 189)); tabTitle.SetTypeface(global::Android.Graphics.Typeface.Default, global::Android.Graphics.TypefaceStyle.Bold); rightToolContent.AddView(tabTitle);
        if (tab == "General")
        {
            rightToolContent.AddView(inspector, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            AddPanelButton(rightToolContent, "Open ROM", () => Pick(ImportRom));
            AddPanelButton(rightToolContent, "Open project", () => Pick(ImportProject));
            AddPanelButton(rightToolContent, "Reset camera", () => viewport.ResetCamera());
            AddPanelButton(rightToolContent, "Show/hide actor gizmos", ToggleActorGizmos);
            AddPanelButton(rightToolContent, "Show/hide room geometry", () => WithRoom(ToggleNativeGeometry));
            AddPanelButton(rightToolContent, "Show/hide collision", () => WithRoom(ToggleNativeCollision));
            AddPanelButton(rightToolContent, "Gameplay editor", Gameplay);
        }
        else if (tab == "Scene")
        {
            AddPanelButton(rightToolContent, "Scene properties", () => WithScene(ShowRomScene));
            AddPanelButton(rightToolContent, "Scene settings", () => WithScene(EditNativeSceneSettings));
            AddPanelButton(rightToolContent, "Spawns", () => WithScene(ShowRomSpawns));
            AddPanelButton(rightToolContent, "Transitions", () => WithScene(ShowRomTransitions));
            AddPanelButton(rightToolContent, "Paths & waypoints", () => WithScene(ShowRomPaths));
            AddPanelButton(rightToolContent, "Environments", () => WithScene(ShowRomEnvironments));
            AddPanelButton(rightToolContent, "Alternate headers", () => WithScene(ShowRomAlternateHeaders));
            AddPanelButton(rightToolContent, "Clone scene", () => WithScene(CloneNativeScene));
            AddPanelButton(rightToolContent, "Delete scene", () => WithScene(DeleteNativeScene));
            AddPanelButton(rightToolContent, "Scene order", ShowSceneOrder);
            AddInlineSceneInspector(CurrentScene());
        }
        else if (tab == "Room")
        {
            AddPanelButton(rightToolContent, "Room properties", () => WithRoom(ShowRomRoom));
            AddPanelButton(rightToolContent, "Room settings", () => WithRoom(EditNativeRoomSettings));
            AddPanelButton(rightToolContent, "Objects", () => WithRoom(ShowRomObjects));
            AddPanelButton(rightToolContent, "Room exits", () => WithRoom(ShowRomRoomExits));
            AddPanelButton(rightToolContent, "Clone room", () => WithRoom(CloneNativeRoom));
            AddPanelButton(rightToolContent, "Delete room", () => WithRoom(DeleteNativeRoom));
            AddPanelButton(rightToolContent, "Room order", () => WithScene(ShowRoomOrder));
            AddPanelButton(rightToolContent, "Split room into tiles", () => WithRoom(AddRomRoomTile));
            AddPanelButton(rightToolContent, "Import custom geometry", () => WithRoom(ImportNativeGeometry));
            AddInlineRoomInspector(CurrentScene(), CurrentRoom());
        }
        else if (tab == "Actors")
        {
            AddPanelButton(rightToolContent, "Actors in room", () => WithRoom(ShowRomRoom));
            AddPanelButton(rightToolContent, "Add actor", () => WithRoom(AddNativeActor));
            AddPanelButton(rightToolContent, "Actor database", ShowActorBrowser);
            AddPanelButton(rightToolContent, "Select actors on viewport", () => WithRoom(ToggleActorSelection));
            AddPanelButton(rightToolContent, "Group transform", () => WithRoom(ShowActorGroupTransform));
            AddPanelButton(rightToolContent, "Spawns", () => WithScene(ShowRomSpawns));
            AddPanelButton(rightToolContent, "Transitions", () => WithScene(ShowRomTransitions));
            AddInlineActorInspector(CurrentScene(), CurrentRoom());
        }
        else if (tab == "Collision")
        {
            AddPanelButton(rightToolContent, "Collision editor", () => WithScene(ShowRomCollisionVertices));
            AddPanelButton(rightToolContent, "Collision material database", () => WithScene(ShowNativeCollisionMaterialDatabase));
            AddPanelButton(rightToolContent, "Waterboxes", () => WithScene(ShowRomWaterboxes));
            AddPanelButton(rightToolContent, "Toggle collision vertices", () => WithRoom(ToggleCollisionOverlay));
            AddPanelButton(rightToolContent, "Toggle collision triangles", () => WithRoom(ToggleCollisionTriangleOverlay));
            AddPanelButton(rightToolContent, "Recalculate collision", () => WithScene(RecalculateNativeCollision));
            AddInlineCollisionInspector(CurrentScene());
        }
        else if (tab == "Geometry")
        {
            AddPanelButton(rightToolContent, "Geometry vertices & triangles", () => WithRoom(ShowRomGeometryVertices));
            AddPanelButton(rightToolContent, "Materials", () => WithRoom(ShowNativeGeometryMaterials));
            AddPanelButton(rightToolContent, "Display-list sources", () => WithRoom(ShowNativeDisplayListSources));
            AddPanelButton(rightToolContent, "Texture commands", () => WithRoom(ShowNativeTextureCommands));
            AddPanelButton(rightToolContent, "Import custom geometry", () => WithRoom(ImportNativeGeometry));
            AddPanelButton(rightToolContent, "Export reusable tile", () => WithRoom(AddRomRoomTile));
            AddPanelButton(rightToolContent, "Toggle geometry picking", () => WithRoom(ToggleGeometryOverlay));
            AddInlineGeometryInspector(CurrentScene(), CurrentRoom());
        }
        else if (tab == "Commands")
        {
            AddPanelButton(rightToolContent, "Scene commands", () => WithScene(ShowRomSceneCommands));
            AddPanelButton(rightToolContent, "Alternate-header commands", () => WithScene(ShowRomAlternateHeaders));
            AddPanelButton(rightToolContent, "Display-list commands", () => WithRoom(ShowNativeDisplayListSources));
            AddInlineCommandInspector(CurrentScene(), CurrentRoom());
        }
        else
        {
            AddPanelButton(rightToolContent, "Verify ROM", VerifyEditedRom);
            AddPanelButton(rightToolContent, "Export ROM", ExportEditedRom);
            AddPanelButton(rightToolContent, "Export patch", ExportRomPatch);
            AddPanelButton(rightToolContent, "Export project", ExportProject);
            AddPanelButton(rightToolContent, "Import tile", () => Pick(ImportTile));
            AddPanelButton(rightToolContent, "Room catalog", () => Pick(ImportCatalog));
            AddPanelButton(rightToolContent, "Save mobile layout", () => { LayoutStorage.SaveAtomic(SavePath, history.Current); status.Text = "Layout saved on device."; });
            AddPanelButton(rightToolContent, "Help", Help);
        }
    }

    private void AddInlineSceneInspector(RomScene? scene)
    {
        AddSectionHeader(rightToolContent, "INLINE SCENE WORKSPACE");
        if (scene is null)
        {
            rightToolContent.AddView(new TextView(this) { Text = "Select a ROM scene to edit settings, spawns, transitions, environments, and waterboxes here." });
            return;
        }
        AddToolbar(rightToolContent,
            ("Settings", () => { inlineSceneKind = "settings"; inlineSceneIndex = 0; SetRightTab("Scene"); }),
            ("Spawns", () => { inlineSceneKind = "spawn"; inlineSceneIndex = 0; SetRightTab("Scene"); }),
            ("Transitions", () => { inlineSceneKind = "transition"; inlineSceneIndex = 0; SetRightTab("Scene"); }),
            ("Environment", () => { inlineSceneKind = "environment"; inlineSceneIndex = 0; SetRightTab("Scene"); }),
            ("Waterboxes", () => { inlineSceneKind = "waterbox"; inlineSceneIndex = 0; SetRightTab("Scene"); }),
            ("Commands", () => { inlineSceneKind = "command"; inlineSceneIndex = 0; SetRightTab("Scene"); }),
            ("Alt commands", () => { inlineSceneKind = "alternateCommand"; inlineAlternateCommandIndex = 0; SetRightTab("Scene"); }));
        if (inlineSceneKind == "settings")
        {
            var settings = scene.Settings ?? new RomSceneSettings(0, 0); var panel = new LinearLayout(this) { Orientation = Orientation.Vertical };
            var camera = NumberEditor(panel, "Camera movement", settings.CameraMovement.ToString(CultureInfo.InvariantCulture)); var world = NumberEditor(panel, "World map", settings.WorldMap.ToString(CultureInfo.InvariantCulture));
            rightToolContent.AddView(panel, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            AddPanelButton(rightToolContent, "Apply scene settings", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging scene edits."); romWorkspace.EditSceneSettings(scene.Id, new RomSceneSettings((byte)ParseInt(camera), (byte)ParseInt(world))); status.Text = "Native scene settings edit staged."; RefreshInlineScene(scene.Id); } catch (Exception error) { ShowError(error); } });
            return;
        }
        if (inlineSceneKind == "command")
        {
            var commands = scene.Commands ?? []; inlineSceneIndex = Math.Clamp(inlineSceneIndex, 0, Math.Max(commands.Count - 1, 0));
            var commandPanel = new LinearLayout(this) { Orientation = Orientation.Vertical }; commandPanel.SetPadding(Dp(4), Dp(4), Dp(4), Dp(4));
            int commandIndex = commands.Count == 0 ? 0 : inlineSceneIndex; var command = commands.Count == 0 ? new RomHeaderCommand(0, 0, 0) : commands[commandIndex];
            var commandEditor = NumberEditor(commandPanel, "Command (hex)", $"0x{command.Command:X2}"); var parameterEditor = NumberEditor(commandPanel, "Parameter", command.Parameter.ToString(CultureInfo.InvariantCulture)); var pointerEditor = NumberEditor(commandPanel, "Value / pointer (hex)", $"0x{command.Pointer:X8}");
            if (commands.Count > 0)
            {
                var commandIndexEditor = NumberEditor(rightToolContent, "Command index", inlineSceneIndex.ToString(CultureInfo.InvariantCulture));
                AddToolbar(rightToolContent, ("Previous", () => { inlineSceneIndex = Math.Max(0, inlineSceneIndex - 1); SetRightTab("Scene"); }), ("Next", () => { inlineSceneIndex = Math.Min(commands.Count - 1, inlineSceneIndex + 1); SetRightTab("Scene"); }), ("Load index", () => { try { inlineSceneIndex = Math.Clamp(ParseInt(commandIndexEditor), 0, commands.Count - 1); SetRightTab("Scene"); } catch (Exception error) { ShowError(error); } }));
            }
            rightToolContent.AddView(commandPanel, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            AddPanelButton(rightToolContent, commands.Count == 0 ? "Append scene command" : "Apply scene command", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging scene command edits."); var updated = new RomHeaderCommand((byte)ParseInt(commandEditor), (byte)ParseInt(parameterEditor), (uint)ParseUInt64(pointerEditor)); if (commands.Count == 0) romWorkspace.InsertSceneCommand(scene.Id, updated); else romWorkspace.EditSceneCommand(scene.Id, inlineSceneIndex, updated); status.Text = commands.Count == 0 ? "Scene command insertion staged." : $"Scene command {inlineSceneIndex:D2} edit staged."; RefreshInlineScene(scene.Id); } catch (Exception error) { ShowError(error); } });
            if (commands.Count > 0) AddPanelButton(rightToolContent, "Delete final scene command", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging scene command edits."); romWorkspace.DeleteSceneCommand(scene.Id, commands.Count - 1); inlineSceneIndex = Math.Max(0, inlineSceneIndex - 1); status.Text = "Final scene command deletion staged."; RefreshInlineScene(scene.Id); } catch (Exception error) { ShowError(error); } });
            return;
        }
        if (inlineSceneKind == "alternateCommand")
        {
            var headers = scene.AlternateHeaders ?? [];
            if (headers.Count == 0) { rightToolContent.AddView(new TextView(this) { Text = "No alternate headers are decoded for this scene." }); return; }
            var headerIndexEditor = NumberEditor(rightToolContent, "Alternate header index", inlineAlternateHeaderIndex.ToString(CultureInfo.InvariantCulture));
            AddPanelButton(rightToolContent, "Load alternate header", () => { try { inlineAlternateHeaderIndex = ParseInt(headerIndexEditor); SetRightTab("Scene"); } catch (Exception error) { ShowError(error); } });
            var header = headers.FirstOrDefault(item => item.Index == inlineAlternateHeaderIndex) ?? headers[0]; inlineAlternateHeaderIndex = header.Index;
            var commands = header.Commands ?? []; inlineAlternateCommandIndex = Math.Clamp(inlineAlternateCommandIndex, 0, Math.Max(commands.Count - 1, 0));
            var commandPanel = new LinearLayout(this) { Orientation = Orientation.Vertical }; commandPanel.SetPadding(Dp(4), Dp(4), Dp(4), Dp(4));
            int commandIndex = commands.Count == 0 ? 0 : inlineAlternateCommandIndex; var command = commands.Count == 0 ? new RomHeaderCommand(0, 0, 0) : commands[commandIndex];
            var commandEditor = NumberEditor(commandPanel, "Command (hex)", $"0x{command.Command:X2}"); var parameterEditor = NumberEditor(commandPanel, "Parameter", command.Parameter.ToString(CultureInfo.InvariantCulture)); var pointerEditor = NumberEditor(commandPanel, "Value / pointer (hex)", $"0x{command.Pointer:X8}");
            if (commands.Count > 0)
            {
                var commandIndexEditor = NumberEditor(rightToolContent, "Alternate command index", inlineAlternateCommandIndex.ToString(CultureInfo.InvariantCulture));
                AddToolbar(rightToolContent, ("Previous", () => { inlineAlternateCommandIndex = Math.Max(0, inlineAlternateCommandIndex - 1); SetRightTab("Scene"); }), ("Next", () => { inlineAlternateCommandIndex = Math.Min(commands.Count - 1, inlineAlternateCommandIndex + 1); SetRightTab("Scene"); }), ("Load index", () => { try { inlineAlternateCommandIndex = Math.Clamp(ParseInt(commandIndexEditor), 0, commands.Count - 1); SetRightTab("Scene"); } catch (Exception error) { ShowError(error); } }));
            }
            rightToolContent.AddView(commandPanel, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            AddPanelButton(rightToolContent, commands.Count == 0 ? "Append alternate command" : "Apply alternate command", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging alternate command edits."); var updated = new RomHeaderCommand((byte)ParseInt(commandEditor), (byte)ParseInt(parameterEditor), (uint)ParseUInt64(pointerEditor)); if (commands.Count == 0) romWorkspace.InsertAlternateCommandAt(scene.Id, header.Index, 0, updated); else romWorkspace.EditAlternateCommand(scene.Id, header.Index, inlineAlternateCommandIndex, updated); status.Text = commands.Count == 0 ? "Alternate command insertion staged." : $"Alternate header {header.Index:D2} command {inlineAlternateCommandIndex:D2} edit staged."; RefreshInlineScene(scene.Id); } catch (Exception error) { ShowError(error); } });
            if (commands.Count > 0) AddPanelButton(rightToolContent, "Delete final alternate command", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging alternate command edits."); romWorkspace.DeleteAlternateCommand(scene.Id, header.Index, commands.Count - 1); inlineAlternateCommandIndex = Math.Max(0, inlineAlternateCommandIndex - 1); status.Text = "Final alternate command deletion staged."; RefreshInlineScene(scene.Id); } catch (Exception error) { ShowError(error); } });
            return;
        }
        var records = inlineSceneKind switch { "spawn" => scene.SpawnPoints?.Count ?? 0, "transition" => scene.Transitions?.Count ?? 0, "environment" => scene.Environments?.Count ?? 0, "waterbox" => scene.Collision?.Waterboxes?.Count ?? 0, _ => 0 };
        if (records == 0) { rightToolContent.AddView(new TextView(this) { Text = "No decoded records of this type are present in the scene." }); return; }
        inlineSceneIndex = Math.Clamp(inlineSceneIndex, 0, records - 1);
        var indexEditor = NumberEditor(rightToolContent, "Record index", inlineSceneIndex.ToString(CultureInfo.InvariantCulture));
        AddToolbar(rightToolContent, ("Previous", () => { inlineSceneIndex = Math.Max(0, inlineSceneIndex - 1); SetRightTab("Scene"); }), ("Next", () => { inlineSceneIndex = Math.Min(records - 1, inlineSceneIndex + 1); SetRightTab("Scene"); }), ("Load index", () => { try { inlineSceneIndex = Math.Clamp(ParseInt(indexEditor), 0, records - 1); SetRightTab("Scene"); } catch (Exception error) { ShowError(error); } }));
        var panelFields = new LinearLayout(this) { Orientation = Orientation.Vertical }; panelFields.SetPadding(Dp(4), Dp(4), Dp(4), Dp(4));
        if (inlineSceneKind == "spawn")
        {
            var value = scene.SpawnPoints![inlineSceneIndex]; var number = NumberEditor(panelFields, "Spawn number", $"0x{value.Number:X4}"); var x = NumberEditor(panelFields, "X", value.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panelFields, "Y", value.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panelFields, "Z", value.Z.ToString(CultureInfo.InvariantCulture)); var rx = NumberEditor(panelFields, "X rotation", value.RotationX.ToString(CultureInfo.InvariantCulture)); var ry = NumberEditor(panelFields, "Y rotation", value.RotationY.ToString(CultureInfo.InvariantCulture)); var rz = NumberEditor(panelFields, "Z rotation", value.RotationZ.ToString(CultureInfo.InvariantCulture)); var variable = NumberEditor(panelFields, "Variable", $"0x{value.Variable:X4}");
            AddPanelButton(rightToolContent, "Apply spawn", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging scene edits."); romWorkspace.EditSpawn(scene.Id, inlineSceneIndex, new RomSpawnPoint((ushort)ParseInt(number), (short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(rx), (short)ParseInt(ry), (short)ParseInt(rz), (ushort)ParseInt(variable))); status.Text = $"Spawn {inlineSceneIndex:D2} edit staged."; RefreshInlineScene(scene.Id); } catch (Exception error) { ShowError(error); } });
        }
        else if (inlineSceneKind == "transition")
        {
            var value = scene.Transitions![inlineSceneIndex]; var fr = NumberEditor(panelFields, "Front room", value.FrontRoom.ToString(CultureInfo.InvariantCulture)); var fc = NumberEditor(panelFields, "Front camera", value.FrontCamera.ToString(CultureInfo.InvariantCulture)); var br = NumberEditor(panelFields, "Back room", value.BackRoom.ToString(CultureInfo.InvariantCulture)); var bc = NumberEditor(panelFields, "Back camera", value.BackCamera.ToString(CultureInfo.InvariantCulture)); var number = NumberEditor(panelFields, "Transition number", $"0x{value.Number:X4}"); var x = NumberEditor(panelFields, "X", value.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panelFields, "Y", value.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panelFields, "Z", value.Z.ToString(CultureInfo.InvariantCulture)); var rotation = NumberEditor(panelFields, "Y rotation", value.RotationY.ToString(CultureInfo.InvariantCulture)); var variable = NumberEditor(panelFields, "Variable", $"0x{value.Variable:X4}");
            AddPanelButton(rightToolContent, "Apply transition", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging scene edits."); romWorkspace.EditTransition(scene.Id, inlineSceneIndex, new RomTransition((byte)ParseInt(fr), (byte)ParseInt(fc), (byte)ParseInt(br), (byte)ParseInt(bc), (ushort)ParseInt(number), (short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(rotation), (ushort)ParseInt(variable))); status.Text = $"Transition {inlineSceneIndex:D2} edit staged."; RefreshInlineScene(scene.Id); } catch (Exception error) { ShowError(error); } });
        }
        else if (inlineSceneKind == "environment")
        {
            var value = scene.Environments![inlineSceneIndex]; var ambient = NumberEditor(panelFields, "Ambient (hex)", $"0x{value.Ambient:X6}"); var diffuse0 = NumberEditor(panelFields, "Diffuse 0 (hex)", $"0x{value.Diffuse0:X6}"); var direction0 = NumberEditor(panelFields, "Direction 0 (hex)", $"0x{value.Direction0:X6}"); var diffuse1 = NumberEditor(panelFields, "Diffuse 1 (hex)", $"0x{value.Diffuse1:X6}"); var direction1 = NumberEditor(panelFields, "Direction 1 (hex)", $"0x{value.Direction1:X6}"); var fog = NumberEditor(panelFields, "Fog color (hex)", $"0x{value.FogColor:X6}"); var fogDistance = NumberEditor(panelFields, "Fog distance", value.FogDistance.ToString(CultureInfo.InvariantCulture)); var fogUnknown = NumberEditor(panelFields, "Fog unknown", value.FogUnknown.ToString(CultureInfo.InvariantCulture)); var draw = NumberEditor(panelFields, "Draw distance", value.DrawDistance.ToString(CultureInfo.InvariantCulture));
            AddPanelButton(rightToolContent, "Apply environment", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging scene edits."); romWorkspace.EditEnvironment(scene.Id, inlineSceneIndex, new RomEnvironment((uint)ParseUInt64(ambient), (uint)ParseUInt64(diffuse0), (uint)ParseUInt64(direction0), (uint)ParseUInt64(diffuse1), (uint)ParseUInt64(direction1), (uint)ParseUInt64(fog), (ushort)ParseInt(fogDistance), (ushort)ParseInt(fogUnknown), (ushort)ParseInt(draw))); status.Text = $"Environment {inlineSceneIndex:D2} edit staged."; RefreshInlineScene(scene.Id); } catch (Exception error) { ShowError(error); } });
        }
        else
        {
            var value = scene.Collision!.Waterboxes![inlineSceneIndex]; var x = NumberEditor(panelFields, "X", value.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panelFields, "Y", value.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panelFields, "Z", value.Z.ToString(CultureInfo.InvariantCulture)); var xs = NumberEditor(panelFields, "X size", value.XSize.ToString(CultureInfo.InvariantCulture)); var zs = NumberEditor(panelFields, "Z size", value.ZSize.ToString(CultureInfo.InvariantCulture)); var unknown = NumberEditor(panelFields, "Unknown", value.Unknown.ToString(CultureInfo.InvariantCulture)); var properties = NumberEditor(panelFields, "Properties (hex)", $"0x{value.Properties:X8}");
            AddPanelButton(rightToolContent, "Apply waterbox", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging scene edits."); romWorkspace.EditWaterbox(scene.Id, inlineSceneIndex, new RomWaterbox((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(xs), (short)ParseInt(zs), (ushort)ParseInt(unknown), (uint)ParseUInt64(properties))); status.Text = $"Waterbox {inlineSceneIndex:D2} edit staged."; RefreshInlineScene(scene.Id); } catch (Exception error) { ShowError(error); } });
        }
        rightToolContent.AddView(panelFields, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
    }

    private void RefreshInlineScene(int sceneId)
    {
        if (romWorkspace is null) return;
        var current = romWorkspace.Document.Scenes.First(item => item.Id == sceneId); RefreshSceneBrowser(); SetRightTab("Scene");
        if (selectedRoomId >= 0 && current.Rooms.FirstOrDefault(item => item.Id == selectedRoomId) is { } room) OpenRoomInViewport(current, room);
    }

    private void AddInlineRoomInspector(RomScene? scene, RomRoom? room)
    {
        AddSectionHeader(rightToolContent, "INLINE ROOM WORKSPACE");
        if (scene is null || room is null) { rightToolContent.AddView(new TextView(this) { Text = "Select a scene and room to edit settings, objects, and exits here." }); return; }
        AddToolbar(rightToolContent,
            ("Settings", () => { inlineRoomKind = "settings"; inlineRoomIndex = 0; SetRightTab("Room"); }),
            ("Objects", () => { inlineRoomKind = "object"; inlineRoomIndex = 0; SetRightTab("Room"); }),
            ("Exits", () => { inlineRoomKind = "exit"; inlineRoomIndex = 0; SetRightTab("Room"); }));
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(4), Dp(4), Dp(4), Dp(4));
        if (inlineRoomKind == "settings")
        {
            var value = room.Settings ?? new RomRoomSettings(0, 0, 0, 0, 0, 0); var behavior = NumberEditor(panel, "Behavior (hex)", $"0x{value.Behavior:X8}"); var wind = NumberEditor(panel, "Wind (hex)", $"0x{value.Wind:X8}"); var start = NumberEditor(panel, "Start time", value.StartTime.ToString(CultureInfo.InvariantCulture)); var speed = NumberEditor(panel, "Time speed", value.TimeSpeed.ToString(CultureInfo.InvariantCulture)); var skybox = NumberEditor(panel, "Skybox flags", value.SkyboxFlags.ToString(CultureInfo.InvariantCulture)); var echo = NumberEditor(panel, "Echo", value.Echo.ToString(CultureInfo.InvariantCulture));
            rightToolContent.AddView(panel, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)); AddPanelButton(rightToolContent, "Apply room settings", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging room edits."); romWorkspace.EditRoomSettings(scene.Id, room.Id, new RomRoomSettings((uint)ParseUInt64(behavior), (uint)ParseUInt64(wind), (ushort)ParseInt(start), (byte)ParseInt(speed), (byte)ParseInt(skybox), (byte)ParseInt(echo))); status.Text = "Native room settings edit staged."; RefreshInlineRoom(scene.Id, room.Id); } catch (Exception error) { ShowError(error); } }); return;
        }
        int total = inlineRoomKind == "object" ? room.Objects?.Count ?? 0 : room.Exits?.Count ?? 0; if (total == 0) { rightToolContent.AddView(new TextView(this) { Text = "No decoded records of this type are present in the room." }); return; }
        inlineRoomIndex = Math.Clamp(inlineRoomIndex, 0, total - 1); var indexEditor = NumberEditor(rightToolContent, "Record index", inlineRoomIndex.ToString(CultureInfo.InvariantCulture)); AddToolbar(rightToolContent, ("Previous", () => { inlineRoomIndex = Math.Max(0, inlineRoomIndex - 1); SetRightTab("Room"); }), ("Next", () => { inlineRoomIndex = Math.Min(total - 1, inlineRoomIndex + 1); SetRightTab("Room"); }), ("Load index", () => { try { inlineRoomIndex = Math.Clamp(ParseInt(indexEditor), 0, total - 1); SetRightTab("Room"); } catch (Exception error) { ShowError(error); } }));
        if (inlineRoomKind == "object")
        {
            var value = room.Objects![inlineRoomIndex]; var objectId = NumberEditor(panel, "Object ID (hex)", $"0x{value:X4}"); AddPanelButton(rightToolContent, "Apply object", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging room edits."); romWorkspace.EditRoomObject(scene.Id, room.Id, inlineRoomIndex, (ushort)ParseUInt64(objectId)); status.Text = $"Object {inlineRoomIndex:D2} edit staged."; RefreshInlineRoom(scene.Id, room.Id); } catch (Exception error) { ShowError(error); } });
        }
        else
        {
            var value = room.Exits![inlineRoomIndex]; var raw = NumberEditor(panel, "Exit raw (hex)", $"0x{value:X4}"); AddPanelButton(rightToolContent, "Apply room exit", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging room edits."); romWorkspace.EditRoomExit(scene.Id, room.Id, inlineRoomIndex, (ushort)ParseUInt64(raw)); status.Text = $"Room exit {inlineRoomIndex:D2} edit staged."; RefreshInlineRoom(scene.Id, room.Id); } catch (Exception error) { ShowError(error); } });
        }
        rightToolContent.AddView(panel, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
    }

    private void RefreshInlineRoom(int sceneId, int roomId)
    {
        if (romWorkspace is null) return; var current = romWorkspace.Document.Scenes.First(item => item.Id == sceneId); if (current.Rooms.FirstOrDefault(item => item.Id == roomId) is { } room) OpenRoomInViewport(current, room); SetRightTab("Room");
    }

    private void AddInlineActorInspector(RomScene? scene, RomRoom? room)
    {
        var heading = new TextView(this) { Text = "INLINE ACTOR INSPECTOR", TextSize = 12 };
        heading.SetTextColor(Color.Rgb(88, 222, 189)); heading.SetTypeface(global::Android.Graphics.Typeface.Default, global::Android.Graphics.TypefaceStyle.Bold); heading.SetPadding(Dp(4), Dp(12), Dp(4), Dp(4));
        rightToolContent.AddView(heading);
        if (scene is null || room is null)
        {
            var empty = new TextView(this) { Text = "Select a ROM scene and room to edit actors here.", TextSize = 12 };
            empty.SetTextColor(Color.Rgb(191, 203, 218)); empty.SetPadding(Dp(4), Dp(4), Dp(4), Dp(8)); rightToolContent.AddView(empty);
            return;
        }
        for (int index = 0; index < room.Actors.Count; index++)
        {
            int actorIndex = index;
            var row = CompactButton(ActorLabel(room.Actors[index], index), () => { inlineActorIndex = actorIndex; SetRightTab("Actors"); });
            row.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 11); row.Gravity = GravityFlags.Left | GravityFlags.CenterVertical; row.SetPadding(Dp(8), 0, Dp(8), 0);
            row.SetBackgroundColor(actorIndex == inlineActorIndex ? Color.Rgb(208, 126, 45) : Color.Rgb(55, 68, 84));
            rightToolContent.AddView(row, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, Dp(54)));
        }
        if (room.Actors.Count == 0)
        {
            var empty = new TextView(this) { Text = "No actors in this room.", TextSize = 12 };
            empty.SetTextColor(Color.Rgb(191, 203, 218)); empty.SetPadding(Dp(4), Dp(4), Dp(4), Dp(8)); rightToolContent.AddView(empty);
            return;
        }
        inlineActorIndex = Math.Clamp(inlineActorIndex, 0, room.Actors.Count - 1);
        var actor = room.Actors[inlineActorIndex];
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical };
        panel.SetPadding(Dp(4), Dp(8), Dp(4), Dp(4));
        var number = NumberEditor(panel, "Actor ID", $"0x{actor.Number:X4}");
        var x = NumberEditor(panel, "X", actor.X.ToString(CultureInfo.InvariantCulture));
        var y = NumberEditor(panel, "Y", actor.Y.ToString(CultureInfo.InvariantCulture));
        var z = NumberEditor(panel, "Z", actor.Z.ToString(CultureInfo.InvariantCulture));
        var rx = NumberEditor(panel, "X rotation", actor.RotationX.ToString(CultureInfo.InvariantCulture));
        var ry = NumberEditor(panel, "Y rotation", actor.RotationY.ToString(CultureInfo.InvariantCulture));
        var rz = NumberEditor(panel, "Z rotation", actor.RotationZ.ToString(CultureInfo.InvariantCulture));
        var variable = NumberEditor(panel, "Variable", $"0x{actor.Variable:X4}");
        AddActorGizmo(panel, x, y, z, rx, ry, rz);
        rightToolContent.AddView(panel, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
        AddPanelButton(rightToolContent, "Apply actor", () =>
        {
            try
            {
                if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging native actor edits.");
                var updated = new RomActor((ushort)ParseInt(number), (short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(rx), (short)ParseInt(ry), (short)ParseInt(rz), (ushort)ParseInt(variable));
                romWorkspace.EditRoomActor(scene.Id, room.Id, inlineActorIndex, updated);
                status.Text = $"Actor {inlineActorIndex:D2} edit staged in the ROM workspace.";
                var current = romWorkspace.Document.Scenes.First(item => item.Id == scene.Id);
                OpenRoomInViewport(current, current.Rooms[room.Id]); SetRightTab("Actors");
            }
            catch (Exception error) { ShowError(error); }
        });
        AddPanelButton(rightToolContent, "Delete actor", () =>
        {
            try
            {
                if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging native actor edits.");
                romWorkspace.EditRoomActors(scene.Id, room.Id, room.Actors.Where((_, index) => index != inlineActorIndex).ToArray());
                inlineActorIndex = Math.Max(0, inlineActorIndex - 1); status.Text = "Native actor deletion staged in the ROM workspace.";
                var current = romWorkspace.Document.Scenes.First(item => item.Id == scene.Id);
                OpenRoomInViewport(current, current.Rooms[room.Id]); SetRightTab("Actors");
            }
            catch (Exception error) { ShowError(error); }
        });
    }

    private void AddInlineGeometryInspector(RomScene? scene, RomRoom? room)
    {
        var heading = new TextView(this) { Text = "INLINE GEOMETRY INSPECTOR", TextSize = 12 };
        heading.SetTextColor(Color.Rgb(88, 222, 189)); heading.SetTypeface(global::Android.Graphics.Typeface.Default, global::Android.Graphics.TypefaceStyle.Bold); heading.SetPadding(Dp(4), Dp(12), Dp(4), Dp(4));
        rightToolContent.AddView(heading);
        var geometry = room?.Geometry;
        if (scene is null || room is null || geometry is null)
        {
            var empty = new TextView(this) { Text = "Select a decoded room to edit geometry here.", TextSize = 12 };
            empty.SetTextColor(Color.Rgb(191, 203, 218)); empty.SetPadding(Dp(4), Dp(4), Dp(4), Dp(8)); rightToolContent.AddView(empty);
            return;
        }
        AddToolbar(rightToolContent,
            ("Vertices", () => { inlineGeometryKind = "vertex"; inlineGeometryIndex = 0; SetRightTab("Geometry"); }),
            ("Triangles", () => { inlineGeometryKind = "triangle"; inlineGeometryIndex = 0; SetRightTab("Geometry"); }));
        int total = inlineGeometryKind == "vertex" ? geometry.Vertices.Count : geometry.Triangles.Count;
        if (total == 0) { rightToolContent.AddView(new TextView(this) { Text = "No records decoded for this room." }); return; }
        inlineGeometryIndex = Math.Clamp(inlineGeometryIndex, 0, total - 1);
        var indexEditor = NumberEditor(rightToolContent, inlineGeometryKind == "vertex" ? "Vertex index" : "Triangle index", inlineGeometryIndex.ToString(CultureInfo.InvariantCulture));
        AddToolbar(rightToolContent,
            ("Previous", () => { inlineGeometryIndex = Math.Max(0, inlineGeometryIndex - 1); SetRightTab("Geometry"); }),
            ("Next", () => { inlineGeometryIndex = Math.Min(total - 1, inlineGeometryIndex + 1); SetRightTab("Geometry"); }),
            ("Load index", () => { try { inlineGeometryIndex = Math.Clamp(ParseInt(indexEditor), 0, total - 1); SetRightTab("Geometry"); } catch (Exception error) { ShowError(error); } }));
        if (inlineGeometryKind == "vertex")
        {
            var vertex = geometry.Vertices[inlineGeometryIndex]; var fields = new LinearLayout(this) { Orientation = Orientation.Vertical }; fields.SetPadding(Dp(4), Dp(4), Dp(4), Dp(4));
            var x = NumberEditor(fields, "X", vertex.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(fields, "Y", vertex.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(fields, "Z", vertex.Z.ToString(CultureInfo.InvariantCulture));
            var s = NumberEditor(fields, "S", vertex.S.ToString(CultureInfo.InvariantCulture)); var t = NumberEditor(fields, "T", vertex.T.ToString(CultureInfo.InvariantCulture)); var r = NumberEditor(fields, "R", vertex.R.ToString(CultureInfo.InvariantCulture)); var g = NumberEditor(fields, "G", vertex.G.ToString(CultureInfo.InvariantCulture)); var b = NumberEditor(fields, "B", vertex.B.ToString(CultureInfo.InvariantCulture)); var a = NumberEditor(fields, "A", vertex.A.ToString(CultureInfo.InvariantCulture));
            rightToolContent.AddView(fields, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            AddPanelButton(rightToolContent, "Apply vertex", () =>
            {
                try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging native geometry edits."); romWorkspace.EditGeometryVertex(scene.Id, room.Id, inlineGeometryIndex, new RomGeometryVertex((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(s), (short)ParseInt(t), (byte)ParseInt(r), (byte)ParseInt(g), (byte)ParseInt(b), (byte)ParseInt(a))); status.Text = $"Geometry vertex {inlineGeometryIndex:D3} edit staged."; var current = romWorkspace.Document.Scenes.First(item => item.Id == scene.Id); OpenRoomInViewport(current, current.Rooms[room.Id]); SetRightTab("Geometry"); }
                catch (Exception error) { ShowError(error); }
            });
        }
        else
        {
            var triangle = geometry.Triangles[inlineGeometryIndex]; var fields = new LinearLayout(this) { Orientation = Orientation.Vertical }; fields.SetPadding(Dp(4), Dp(4), Dp(4), Dp(4));
            var a = NumberEditor(fields, "A", triangle.A.ToString(CultureInfo.InvariantCulture)); var b = NumberEditor(fields, "B", triangle.B.ToString(CultureInfo.InvariantCulture)); var c = NumberEditor(fields, "C", triangle.C.ToString(CultureInfo.InvariantCulture));
            rightToolContent.AddView(fields, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            AddPanelButton(rightToolContent, "Apply triangle", () =>
            {
                try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging native geometry edits."); romWorkspace.EditGeometryTriangle(scene.Id, room.Id, inlineGeometryIndex, new RomGeometryTriangle(ParseInt(a), ParseInt(b), ParseInt(c))); status.Text = $"Geometry triangle {inlineGeometryIndex:D3} edit staged."; var current = romWorkspace.Document.Scenes.First(item => item.Id == scene.Id); OpenRoomInViewport(current, current.Rooms[room.Id]); SetRightTab("Geometry"); }
                catch (Exception error) { ShowError(error); }
            });
        }
    }

    private void AddInlineCommandInspector(RomScene? scene, RomRoom? room)
    {
        var heading = new TextView(this) { Text = "INLINE DISPLAY-LIST COMMAND INSPECTOR", TextSize = 12 };
        heading.SetTextColor(Color.Rgb(88, 222, 189)); heading.SetTypeface(global::Android.Graphics.Typeface.Default, global::Android.Graphics.TypefaceStyle.Bold); heading.SetPadding(Dp(4), Dp(12), Dp(4), Dp(4));
        rightToolContent.AddView(heading);
        var commands = room?.Geometry?.DisplayListCommands;
        if (scene is null || room is null || commands is not { Count: > 0 })
        {
            var empty = new TextView(this) { Text = "Select a room with decoded display-list commands to edit raw command words here.", TextSize = 12 };
            empty.SetTextColor(Color.Rgb(191, 203, 218)); empty.SetPadding(Dp(4), Dp(4), Dp(4), Dp(8)); rightToolContent.AddView(empty);
            return;
        }
        inlineCommandIndex = Math.Clamp(inlineCommandIndex, 0, commands.Count - 1); var command = commands[inlineCommandIndex];
        var indexEditor = NumberEditor(rightToolContent, "Command index", inlineCommandIndex.ToString(CultureInfo.InvariantCulture));
        AddToolbar(rightToolContent,
            ("Previous", () => { inlineCommandIndex = Math.Max(0, inlineCommandIndex - 1); SetRightTab("Commands"); }),
            ("Next", () => { inlineCommandIndex = Math.Min(commands.Count - 1, inlineCommandIndex + 1); SetRightTab("Commands"); }),
            ("Load index", () => { try { inlineCommandIndex = Math.Clamp(ParseInt(indexEditor), 0, commands.Count - 1); SetRightTab("Commands"); } catch (Exception error) { ShowError(error); } }));
        var label = new TextView(this) { Text = $"Opcode 0x{command.Operation:X2} • room offset 0x{command.SourceOffset:X6}", TextSize = 12 };
        label.SetTextColor(Color.Rgb(191, 203, 218)); label.SetPadding(Dp(4), Dp(4), Dp(4), Dp(4)); rightToolContent.AddView(label);
        var word0 = NumberEditor(rightToolContent, "Word 0 (hex)", $"0x{command.Word0:X8}"); var word1 = NumberEditor(rightToolContent, "Word 1 (hex)", $"0x{command.Word1:X8}");
        AddPanelButton(rightToolContent, "Apply command", () =>
        {
            try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging display-list edits."); romWorkspace.EditGeometryCommand(scene.Id, room.Id, command.SourceOffset, checked((uint)ParseUInt64(word0)), checked((uint)ParseUInt64(word1))); status.Text = $"Display-list command {inlineCommandIndex:D3} edit staged."; var current = romWorkspace.Document.Scenes.First(item => item.Id == scene.Id); OpenRoomInViewport(current, current.Rooms[room.Id]); SetRightTab("Commands"); }
            catch (Exception error) { ShowError(error); }
        });
    }

    private void AddInlineCollisionInspector(RomScene? scene)
    {
        var heading = new TextView(this) { Text = "INLINE COLLISION INSPECTOR", TextSize = 12 };
        heading.SetTextColor(Color.Rgb(88, 222, 189)); heading.SetTypeface(global::Android.Graphics.Typeface.Default, global::Android.Graphics.TypefaceStyle.Bold); heading.SetPadding(Dp(4), Dp(12), Dp(4), Dp(4));
        rightToolContent.AddView(heading);
        var collision = scene?.Collision;
        if (scene is null || collision is null)
        {
            var empty = new TextView(this) { Text = "Select a scene with decoded collision to edit it here.", TextSize = 12 };
            empty.SetTextColor(Color.Rgb(191, 203, 218)); empty.SetPadding(Dp(4), Dp(4), Dp(4), Dp(8)); rightToolContent.AddView(empty);
            return;
        }
        AddToolbar(rightToolContent,
            ("Vertices", () => { inlineCollisionKind = "vertex"; inlineCollisionIndex = 0; SetRightTab("Collision"); }),
            ("Triangles", () => { inlineCollisionKind = "triangle"; inlineCollisionIndex = 0; SetRightTab("Collision"); }),
            ("Surfaces", () => { inlineCollisionKind = "surface"; inlineCollisionIndex = 0; SetRightTab("Collision"); }),
            ("Cameras", () => { inlineCollisionKind = "camera"; inlineCollisionIndex = 0; SetRightTab("Collision"); }),
            ("Camera paths", () => { inlineCollisionKind = "cameraPath"; inlineCameraPathIndex = 0; SetRightTab("Collision"); }));
        int total = inlineCollisionKind switch { "triangle" => collision.Triangles.Count, "surface" => collision.SurfaceTypes.Count, "camera" => collision.Cameras?.Count ?? 0, "cameraPath" => collision.Cameras is { Count: > 0 } && collision.Cameras[Math.Clamp(inlineCollisionIndex, 0, collision.Cameras.Count - 1)].PathPoints is { } path ? path.Count : 0, _ => collision.Vertices.Count };
        if (total == 0) { rightToolContent.AddView(new TextView(this) { Text = "No collision records decoded for this scene." }); return; }
        inlineCollisionIndex = Math.Clamp(inlineCollisionIndex, 0, total - 1);
        if (inlineCollisionKind == "cameraPath") inlineCollisionIndex = Math.Clamp(inlineCollisionIndex, 0, (collision.Cameras?.Count ?? 1) - 1);
        var indexEditor = NumberEditor(rightToolContent, inlineCollisionKind == "cameraPath" ? "Path point index" : inlineCollisionKind + " index", (inlineCollisionKind == "cameraPath" ? inlineCameraPathIndex : inlineCollisionIndex).ToString(CultureInfo.InvariantCulture));
        AddToolbar(rightToolContent,
            ("Previous", () => { if (inlineCollisionKind == "cameraPath") inlineCameraPathIndex = Math.Max(0, inlineCameraPathIndex - 1); else inlineCollisionIndex = Math.Max(0, inlineCollisionIndex - 1); SetRightTab("Collision"); }),
            ("Next", () => { if (inlineCollisionKind == "cameraPath") inlineCameraPathIndex = Math.Min(total - 1, inlineCameraPathIndex + 1); else inlineCollisionIndex = Math.Min(total - 1, inlineCollisionIndex + 1); SetRightTab("Collision"); }),
            ("Load index", () => { try { if (inlineCollisionKind == "cameraPath") inlineCameraPathIndex = Math.Clamp(ParseInt(indexEditor), 0, total - 1); else inlineCollisionIndex = Math.Clamp(ParseInt(indexEditor), 0, total - 1); SetRightTab("Collision"); } catch (Exception error) { ShowError(error); } }));
        if (inlineCollisionKind == "camera")
        {
            var value = collision.Cameras![inlineCollisionIndex]; var fields = new LinearLayout(this) { Orientation = Orientation.Vertical }; fields.SetPadding(Dp(4), Dp(4), Dp(4), Dp(4));
            var type = NumberEditor(fields, "Type", value.Type.ToString(CultureInfo.InvariantCulture)); var count = NumberEditor(fields, "Page/path point count", value.DataCount.ToString(CultureInfo.InvariantCulture)); var x = NumberEditor(fields, "X", value.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(fields, "Y", value.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(fields, "Z", value.Z.ToString(CultureInfo.InvariantCulture)); var rx = NumberEditor(fields, "X rotation", value.RotationX.ToString(CultureInfo.InvariantCulture)); var ry = NumberEditor(fields, "Y rotation", value.RotationY.ToString(CultureInfo.InvariantCulture)); var rz = NumberEditor(fields, "Z rotation", value.RotationZ.ToString(CultureInfo.InvariantCulture)); var fov = NumberEditor(fields, "FOV", value.Fov.ToString(CultureInfo.InvariantCulture)); var u1 = NumberEditor(fields, "Unknown 1", value.Unknown1.ToString(CultureInfo.InvariantCulture)); var u2 = NumberEditor(fields, "Unknown 2", value.Unknown2.ToString(CultureInfo.InvariantCulture));
            rightToolContent.AddView(fields, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            AddPanelButton(rightToolContent, "Apply camera", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging camera edits."); romWorkspace.EditCamera(scene.Id, inlineCollisionIndex, new RomCamera((byte)ParseInt(type), (short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(rx), (short)ParseInt(ry), (short)ParseInt(rz), (short)ParseInt(fov), (ushort)ParseInt(u1), (ushort)ParseInt(u2), (short)ParseInt(count), value.PathPoints)); status.Text = $"Camera {inlineCollisionIndex:D2} edit staged."; RefreshInlineCollision(scene.Id); } catch (Exception error) { ShowError(error); } });
        }
        else if (inlineCollisionKind == "cameraPath")
        {
            var camera = collision.Cameras![inlineCollisionIndex]; if (camera.PathPoints is null || camera.PathPoints.Count == 0) { rightToolContent.AddView(new TextView(this) { Text = "The selected camera has no decoded path/page points." }); return; }
            inlineCameraPathIndex = Math.Clamp(inlineCameraPathIndex, 0, camera.PathPoints.Count - 1); var value = camera.PathPoints[inlineCameraPathIndex]; var fields = new LinearLayout(this) { Orientation = Orientation.Vertical }; fields.SetPadding(Dp(4), Dp(4), Dp(4), Dp(4)); var x = NumberEditor(fields, "X", value.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(fields, "Y", value.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(fields, "Z", value.Z.ToString(CultureInfo.InvariantCulture));
            rightToolContent.AddView(new TextView(this) { Text = $"Camera {inlineCollisionIndex:D2} path/page point {inlineCameraPathIndex:D2}", TextSize = 12 }); rightToolContent.AddView(fields, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            AddPanelButton(rightToolContent, "Apply camera path point", () => { try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging camera edits."); var points = camera.PathPoints.ToArray(); points[inlineCameraPathIndex] = new RomCameraPathPoint((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z)); romWorkspace.EditCamera(scene.Id, inlineCollisionIndex, camera with { DataCount = (short)points.Length, PathPoints = points }); status.Text = $"Camera {inlineCollisionIndex:D2} path point {inlineCameraPathIndex:D2} edit staged."; RefreshInlineCollision(scene.Id); } catch (Exception error) { ShowError(error); } });
        }
        else if (inlineCollisionKind == "vertex")
        {
            var vertex = collision.Vertices[inlineCollisionIndex]; var fields = new LinearLayout(this) { Orientation = Orientation.Vertical }; fields.SetPadding(Dp(4), Dp(4), Dp(4), Dp(4));
            var x = NumberEditor(fields, "X", vertex.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(fields, "Y", vertex.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(fields, "Z", vertex.Z.ToString(CultureInfo.InvariantCulture));
            rightToolContent.AddView(fields, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            AddPanelButton(rightToolContent, "Apply collision vertex", () =>
            {
                try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging collision edits."); romWorkspace.EditCollisionVertex(scene.Id, inlineCollisionIndex, new RomCollisionVertex((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z))); status.Text = $"Collision vertex {inlineCollisionIndex:D3} edit staged."; RefreshInlineCollision(scene.Id); }
                catch (Exception error) { ShowError(error); }
            });
        }
        else if (inlineCollisionKind == "triangle")
        {
            var triangle = collision.Triangles[inlineCollisionIndex]; var fields = new LinearLayout(this) { Orientation = Orientation.Vertical }; fields.SetPadding(Dp(4), Dp(4), Dp(4), Dp(4));
            var surface = NumberEditor(fields, "Surface type", triangle.SurfaceType.ToString(CultureInfo.InvariantCulture)); var a = NumberEditor(fields, "A", triangle.A.ToString(CultureInfo.InvariantCulture)); var b = NumberEditor(fields, "B", triangle.B.ToString(CultureInfo.InvariantCulture)); var c = NumberEditor(fields, "C", triangle.C.ToString(CultureInfo.InvariantCulture)); var nx = NumberEditor(fields, "Normal X", triangle.NormalX.ToString(CultureInfo.InvariantCulture)); var ny = NumberEditor(fields, "Normal Y", triangle.NormalY.ToString(CultureInfo.InvariantCulture)); var nz = NumberEditor(fields, "Normal Z", triangle.NormalZ.ToString(CultureInfo.InvariantCulture)); var distance = NumberEditor(fields, "Distance", triangle.Distance.ToString(CultureInfo.InvariantCulture));
            rightToolContent.AddView(fields, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
            AddPanelButton(rightToolContent, "Apply collision triangle", () =>
            {
                try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging collision edits."); romWorkspace.EditCollisionTriangle(scene.Id, inlineCollisionIndex, new RomCollisionTriangle((ushort)ParseInt(surface), (ushort)ParseInt(a), (ushort)ParseInt(b), (ushort)ParseInt(c), (short)ParseInt(nx), (short)ParseInt(ny), (short)ParseInt(nz), (short)ParseInt(distance))); status.Text = $"Collision triangle {inlineCollisionIndex:D3} edit staged."; RefreshInlineCollision(scene.Id); }
                catch (Exception error) { ShowError(error); }
            });
        }
        else
        {
            ulong rawValue = collision.SurfaceTypes[inlineCollisionIndex]; var decoded = RomCollisionSurfaceType.Decode(rawValue);
            var description = new TextView(this) { Text = $"Material {decoded.Material} • floor {decoded.FloorType} • wall {decoded.WallType} • hookshot {(decoded.CanHookshot ? "yes" : "no")}\nEdit the complete packed surface value while preserving unknown bits.", TextSize = 12 };
            description.SetTextColor(Color.Rgb(191, 203, 218)); description.SetPadding(Dp(4), Dp(4), Dp(4), Dp(4)); rightToolContent.AddView(description);
            var raw = NumberEditor(rightToolContent, "Packed surface (hex)", $"0x{rawValue:X16}");
            AddPanelButton(rightToolContent, "Apply collision surface", () =>
            {
                try { if (romWorkspace is null) throw new InvalidOperationException("Open a ROM before staging collision edits."); romWorkspace.EditCollisionSurface(scene.Id, inlineCollisionIndex, ParseUInt64(raw)); status.Text = $"Collision surface {inlineCollisionIndex:D3} edit staged."; RefreshInlineCollision(scene.Id); }
                catch (Exception error) { ShowError(error); }
            });
        }
    }

    private void RefreshInlineCollision(int sceneId)
    {
        if (romWorkspace is null) return;
        var current = romWorkspace.Document.Scenes.First(item => item.Id == sceneId); var room = current.Rooms.FirstOrDefault(item => item.Id == selectedRoomId);
        if (room is not null) OpenRoomInViewport(current, room); else RefreshSceneBrowser();
        SetRightTab("Collision");
    }

    private void SetLeftPanel(bool visible)
    {
        leftPanelVisible = visible;
        leftPanel.Visibility = visible && !viewportFullscreen ? ViewStates.Visible : ViewStates.Gone;
        if (visible && Resources!.DisplayMetrics!.WidthPixels < Dp(700)) SetRightPanel(false);
    }

    private void SetRightPanel(bool visible)
    {
        rightPanelVisible = visible;
        rightPanel.Visibility = visible && !viewportFullscreen ? ViewStates.Visible : ViewStates.Gone;
        if (visible && Resources!.DisplayMetrics!.WidthPixels < Dp(700)) SetLeftPanel(false);
    }

    private void ToggleViewportFullscreen()
    {
        viewportFullscreen = !viewportFullscreen;
        editorChrome.Visibility = viewportFullscreen ? ViewStates.Gone : ViewStates.Visible;
        status.Visibility = viewportFullscreen ? ViewStates.Gone : ViewStates.Visible;
        leftPanel.Visibility = !viewportFullscreen && leftPanelVisible ? ViewStates.Visible : ViewStates.Gone;
        rightPanel.Visibility = !viewportFullscreen && rightPanelVisible ? ViewStates.Visible : ViewStates.Gone;
        restoreChrome.Visibility = viewportFullscreen ? ViewStates.Visible : ViewStates.Gone;
    }

    private RomScene? CurrentScene() => (romWorkspace?.Document ?? rom)?.Scenes.FirstOrDefault(s => s.Id == selectedSceneId);
    private RomRoom? CurrentRoom() => CurrentScene()?.Rooms.FirstOrDefault(r => r.Id == selectedRoomId);
    private void WithScene(Action<RomScene> action)
    {
        var scene = CurrentScene(); if (scene is null) { SetLeftPanel(true); status.Text = "Choose a scene from the left panel first."; return; }
        action(scene);
    }
    private void WithRoom(Action<RomScene, RomRoom> action)
    {
        var scene = CurrentScene(); var room = CurrentRoom(); if (scene is null || room is null) { SetLeftPanel(true); status.Text = "Choose a scene and room from the left panel first."; return; }
        action(scene, room);
    }

    private void ShowTopMenu(string name)
    {
        (string Label, Action Run)[] actions = name switch
        {
            "File" => [("Open ROM", () => Pick(ImportRom)), ("Open project", () => Pick(ImportProject)), ("Open layout", () => Pick(OpenLayout)), ("Save layout", () => { LayoutStorage.SaveAtomic(SavePath, history.Current); status.Text = "Layout saved."; }), ("Verify ROM", VerifyEditedRom), ("Export ROM", ExportEditedRom), ("Export patch", ExportRomPatch), ("Export project", ExportProject), ("Export layout", Export)],
            "Edit" => [("Undo layout", () => { history.Undo(); Changed(); }), ("Redo layout", () => { history.Redo(); Changed(); }), ("Scene properties", () => WithScene(ShowRomScene)), ("Room properties", () => WithRoom(ShowRomRoom))],
            "Extra" => [("Sharp Ocarina workspace", ShowRomWorkspace), ("Actor database", ShowActorBrowser), ("Gameplay", Gameplay), ("Room catalog", () => Pick(ImportCatalog)), ("Import tile", () => Pick(ImportTile)), ("Help", Help)],
            "Window" => [(leftPanelVisible ? "Hide scenes panel" : "Show scenes panel", () => SetLeftPanel(!leftPanelVisible)), (rightPanelVisible ? "Hide tools panel" : "Show tools panel", () => SetRightPanel(!rightPanelVisible)), ("Fullscreen viewport", ToggleViewportFullscreen)],
            "View" => [("Reset camera", () => viewport.ResetCamera()), ("Toggle geometry", () => WithRoom(ToggleNativeGeometry)), ("Toggle collision", () => WithRoom(ToggleNativeCollision)), ("Show/hide actor gizmos", ToggleActorGizmos), ("Fullscreen viewport", ToggleViewportFullscreen)],
            "Scene" => [("Scene browser", () => SetLeftPanel(true)), ("Scene properties", () => WithScene(ShowRomScene)), ("Scene order", ShowSceneOrder), ("Entrances", ShowEntrances), ("Commands", () => WithScene(ShowRomSceneCommands))],
            "Room" => [("Room browser", () => SetLeftPanel(true)), ("Room properties", () => WithRoom(ShowRomRoom)), ("Room order", () => WithScene(ShowRoomOrder)), ("Room settings", () => WithRoom(EditNativeRoomSettings)), ("Import geometry", () => WithRoom(ImportNativeGeometry))],
            "Actors" => [("Actors in room", () => WithRoom(ShowRomRoom)), ("Actor database", ShowActorBrowser), ("Spawns", () => WithScene(ShowRomSpawns)), ("Transitions", () => WithScene(ShowRomTransitions))],
            "Collision" => [("Collision vertices and cameras", () => WithScene(ShowRomCollisionVertices)), ("Waterboxes", () => WithScene(ShowRomWaterboxes))],
            _ => [("Geometry vertices", () => WithRoom(ShowRomGeometryVertices)), ("Import geometry", () => WithRoom(ImportNativeGeometry)), ("Split room into tiles", () => WithRoom(AddRomRoomTile))]
        };
        new AlertDialog.Builder(this)!.SetTitle(name)!.SetItems(actions.Select(a => a.Label).ToArray(), (_, e) => { try { actions[e.Which].Run(); } catch (Exception error) { ShowError(error); } })!.Show();
    }

    private void RefreshSceneBrowser()
    {
        var current = romWorkspace?.Document ?? rom;
        bool hasRom = current is not null;
        bool romTab = leftTab == "ROM";
        sceneList.Visibility = hasRom && romTab ? ViewStates.Visible : ViewStates.Gone;
        nativeRoomList.Visibility = hasRom && romTab ? ViewStates.Visible : ViewStates.Gone;
        roomTitle.Visibility = hasRom && romTab ? ViewStates.Visible : ViewStates.Gone;
        rooms.Visibility = !romTab ? ViewStates.Visible : ViewStates.Gone;
        sceneTitle.Text = hasRom && romTab ? $"Scenes · {current!.Scenes.Count}" : "Layout rooms";
        documentTitle.Text = hasRom ? $"{current!.Profile.Name} · Scene {(selectedSceneId < 0 ? "—" : selectedSceneId.ToString("X2"))}" : "No ROM loaded";
        if (!hasRom) return;
        var scenes = current!.Scenes;
        sceneList.Adapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleListItemActivated1, scenes.Select(s => $"{s.Id:X2}  {s.Name}  ·  {s.Rooms.Count} rooms").ToArray());
        int scenePosition = scenes.ToList().FindIndex(s => s.Id == selectedSceneId);
        if (scenePosition >= 0) sceneList.SetItemChecked(scenePosition, true);
        var scene = CurrentScene();
        roomTitle.Text = scene is null ? "Choose a scene" : $"Rooms · Scene {scene.Id:X2}";
        nativeRoomList.Adapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleListItemActivated1, (scene?.Rooms ?? []).Select(r => $"Room {r.Id:D2}  ·  {r.Actors.Count} actors  ·  {r.Geometry?.Triangles.Count ?? 0} triangles").ToArray());
        if (scene is not null) { int roomPosition = scene.Rooms.ToList().FindIndex(r => r.Id == selectedRoomId); if (roomPosition >= 0) nativeRoomList.SetItemChecked(roomPosition, true); }
    }

    private void OpenSceneInViewport(RomScene scene)
    {
        selectedSceneId = scene.Id; selectedRoomId = -1;
        if (scene.Rooms.Count > 0) OpenRoomInViewport(scene, scene.Rooms.FirstOrDefault(r => r.Geometry?.Triangles.Count > 0) ?? scene.Rooms[0]);
        else { actorOverlay.RemoveAllViews(); viewport.ShowNativeRoom(null, scene.Collision, false, nativeCollisionVisible); inspector.Text = $"Scene {scene.Id:X2} · {scene.Name}\nNo rooms decoded. Use Scene properties for metadata."; RefreshSceneBrowser(); }
        status.Text = $"Scene {scene.Id:X2} · {scene.Name} opened from ROM.";
    }

    private void OpenRoomInViewport(RomScene scene, RomRoom room)
    {
        selectedSceneId = scene.Id; selectedRoomId = room.Id;
        viewport.ShowNativeRoom(room.Geometry, scene.Collision, nativeGeometryVisible, nativeCollisionVisible, GetNativePreviewTexture(room));
        ShowActorOverlay(scene, room);
        inspector.Text = $"Scene {scene.Id:X2} · {scene.Name}\nRoom {room.Id:D2} · {room.Actors.Count} actors · {room.ObjectCount} objects\nGeometry: {room.Geometry?.Triangles.Count ?? 0} triangles · Collision: {scene.Collision?.Triangles.Count ?? 0} triangles";
        RefreshSceneBrowser();
        status.Text = $"Scene {scene.Id:X2} / Room {room.Id:D2} · drag to orbit, pinch to zoom.";
    }

    private RomTextureAsset? GetNativePreviewTexture(RomRoom room)
    {
        if (loadedRomBytes is not { } bytes) return null;
        try { return RomTextureDecoder.ExtractRoomTextures(bytes, room).FirstOrDefault(asset => asset.IsDecoded); }
        catch { return null; }
    }

    private void SelectDefaultRomScene()
    {
        var current = romWorkspace?.Document ?? rom;
        if (current is null) return;
        var scene = current.Scenes.FirstOrDefault(s => s.Rooms.Any(r => r.Geometry?.Triangles.Count > 0)) ?? current.Scenes.FirstOrDefault(s => s.Rooms.Count > 0) ?? current.Scenes.FirstOrDefault();
        if (scene is not null) OpenSceneInViewport(scene); else RefreshSceneBrowser();
        SetLeftPanel(true);
    }

    private void ToggleActorGizmos()
    {
        actorOverlayEnabled = !actorOverlayEnabled;
        var scene = CurrentScene(); var room = CurrentRoom();
        if (scene is not null && room is not null) ShowActorOverlay(scene, room);
        status.Text = actorOverlayEnabled ? "Actor gizmos visible; drag a marker to move it." : "Actor gizmos hidden.";
    }

    private void Pick(int request)
    {
        var intent = new Intent(Intent.ActionOpenDocument);
        intent.AddCategory(Intent.CategoryOpenable); intent.SetType("*/*");
        StartActivityForResult(intent, request);
    }

    private void ShowRomWorkspace()
    {
        if (rom is null) { status.Text = "Open a ROM before opening the SO workspace."; return; }
        string[] categories = ["General / scene settings", "Scenes and rooms", "Actors and objects", "Collision and cameras", "Paths, exits and transitions", "Geometry and materials", "Commands and alternate headers", "Export and verification", "Help and controls"];
        new AlertDialog.Builder(this)!.SetTitle("Sharp Ocarina workspace")!.SetItems(categories, (_, args) =>
        {
            try
            {
                switch (args.Which)
                {
                    case 0: status.Text = "General tools: scene settings, environments, spawns and entrances are inside the scene browser."; ShowRomScenes(); break;
                    case 1: status.Text = "Scenes and rooms workspace opened."; ShowRomScenes(); break;
                    case 2: status.Text = "Actors and objects workspace opened."; ShowActorBrowser(); break;
                    case 3: status.Text = "Collision and cameras workspace opened."; ShowRomScenes(); break;
                    case 4: status.Text = "Paths, exits and transitions workspace opened."; ShowRomScenes(); break;
                    case 5: status.Text = "Geometry and materials workspace opened."; ShowRomScenes(); break;
                    case 6: status.Text = "Commands and alternate headers workspace opened."; ShowRomScenes(); break;
                    case 7: VerifyEditedRom(); break;
                    default: Help(); break;
                }
            }
            catch (Exception error) { ShowError(error); }
        })!.SetNegativeButton("Close", (_, _) => { })!.Show();
    }

    private void OpenSoCategory(string category)
    {
        if (rom is null) { status.Text = $"Open a ROM before using the {category} tab."; return; }
        status.Text = $"SO tab: {category}";
        if (category.StartsWith("Actors", StringComparison.Ordinal)) ShowActorBrowser(); else ShowRomScenes();
    }

    private void ShowActorBrowser()
    {
        if (actorDatabase is null) { status.Text = "The embedded OoT actor catalog is unavailable."; return; }
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical };
        panel.SetPadding(Dp(16), Dp(4), Dp(16), Dp(4));
        var filter = new EditText(this) { Hint = "Search name, debug name, or ID…" }; filter.SetSingleLine(true);
        panel.AddView(filter, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, Dp(52)));
        var category = new Spinner(this);
        var categoryValues = new[] { "All categories" }.Concat(actorDatabase.Find().Select(a => a.Category).Distinct().OrderBy(c => c).Select(c => $"Category {c}")).ToArray();
        var categoryAdapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleSpinnerItem);
        foreach (string value in categoryValues) categoryAdapter.Add(value);
        category.Adapter = categoryAdapter;
        panel.AddView(category, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, Dp(52)));
        var list = new ListView(this) { ChoiceMode = ChoiceMode.Single };
        panel.AddView(list, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, Dp(360)));
        void RefreshCatalog()
        {
            int? selectedCategory = category.SelectedItemPosition <= 0 ? null : int.Parse(categoryValues[category.SelectedItemPosition].Replace("Category ", "", StringComparison.Ordinal), CultureInfo.InvariantCulture);
            var matches = actorDatabase.Find(filter.Text, selectedCategory);
            var actorAdapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleListItem1);
            foreach (string value in matches.Select(a => $"0x{a.Id:X4}  {a.Name}  •  {a.DebugName}  •  object 0x{a.ObjectId:X4}")) actorAdapter.Add(value);
            list.Adapter = actorAdapter;
            list.ItemClick -= CatalogItemClick;
            list.ItemClick += CatalogItemClick;
            void CatalogItemClick(object? sender, AdapterView.ItemClickEventArgs args)
            {
                if (args.Position < matches.Count) ShowActorDefinition(matches[args.Position]);
            }
        }
        filter.TextChanged += (_, _) => RefreshCatalog();
        category.ItemSelected += (_, _) => RefreshCatalog();
        new AlertDialog.Builder(this)!.SetTitle("Sharp Ocarina actor database")!.SetView(panel)!.SetNegativeButton("Back", (_, _) => { if (rom is not null) ShowRomScenes(); })!.Show();
        RefreshCatalog();
    }

    private void ShowActorDefinition(ActorDefinition definition)
    {
        var lines = new List<string> { $"ID: 0x{definition.Id:X4}", $"Name: {definition.Name}", $"Debug name: {definition.DebugName}", $"Object: 0x{definition.ObjectId:X4}", $"Category: {definition.Category}", "", "Variable values:" };
        lines.AddRange(definition.Variables.Count == 0 ? ["  (none documented)"] : definition.Variables.OrderBy(pair => pair.Key).Select(pair => $"  0x{pair.Key:X4}  {pair.Value}"));
        lines.Add("\nProperty masks:");
        lines.AddRange(definition.Properties.Count == 0 ? ["  (none documented)"] : definition.Properties.Select(property => $"  {property.Name}  →  {property.Target}  mask 0x{property.Mask:X4}"));
        new AlertDialog.Builder(this)!.SetTitle("Actor definition")!.SetMessage(string.Join("\n", lines))!.SetPositiveButton("Close", (_, _) => { })!.Show();
    }

    private void Export()
    {
        File.WriteAllText(ExportPath, LayoutStorage.Write(history.Current));
        var intent = new Intent(Intent.ActionCreateDocument);
        intent.AddCategory(Intent.CategoryOpenable); intent.SetType("application/json");
        intent.PutExtra(Intent.ExtraTitle, "Archarina64-layout.json");
        StartActivityForResult(intent, ExportLayout);
    }

    private void ExportRomPatch()
    {
        if (romWorkspace is null) { status.Text = "Open a ROM before exporting native edits."; return; }
        string patchPath = Path.Combine(FilesDir!.AbsolutePath, "archarina64-rom-patch.json");
        File.WriteAllText(patchPath, romWorkspace.WritePatch());
        var intent = new Intent(Intent.ActionCreateDocument); intent.AddCategory(Intent.CategoryOpenable); intent.SetType("application/json"); intent.PutExtra(Intent.ExtraTitle, "archarina64-rom-patch.json");
        StartActivityForResult(intent, ExportRomPatchRequest);
    }

    private void ExportProject()
    {
        if (romWorkspace is null) { status.Text = "Open a ROM or project before exporting a project snapshot."; return; }
        string path = Path.Combine(FilesDir!.AbsolutePath, "archarina64-project.json");
        File.WriteAllText(path, JsonSerializer.Serialize(romWorkspace.Document, new JsonSerializerOptions { WriteIndented = true }));
        var intent = new Intent(Intent.ActionCreateDocument); intent.AddCategory(Intent.CategoryOpenable); intent.SetType("application/json"); intent.PutExtra(Intent.ExtraTitle, "archarina64-project.json"); StartActivityForResult(intent, ExportProjectRequest);
    }

    private void ExportEditedRom()
    {
        if (romWorkspace is null || loadedRomBytes is null) { status.Text = "Open a ROM before exporting edited data."; return; }
        string outputPath = Path.Combine(FilesDir!.AbsolutePath, "archarina64-edited.z64");
        try
        {
            byte[] edited = BuildVerifiedRom();
            File.WriteAllBytes(outputPath, edited);
            status.Text = $"Verified edited ROM: {romWorkspace.Document.Scenes.Count} scenes, {romWorkspace.Document.Scenes.Sum(s => s.Rooms.Count)} rooms.";
        }
        catch (Exception error) { ShowError(error); return; }
        var intent = new Intent(Intent.ActionCreateDocument); intent.AddCategory(Intent.CategoryOpenable); intent.SetType("application/octet-stream"); intent.PutExtra(Intent.ExtraTitle, "archarina64-edited.z64"); StartActivityForResult(intent, ExportRomRequest);
    }

    private void VerifyEditedRom()
    {
        if (romWorkspace is null || loadedRomBytes is null) { status.Text = "Open a ROM before verifying edits."; return; }
        try { BuildVerifiedRom(); status.Text = $"ROM edits verified: {romWorkspace.ActorEdits.Count + romWorkspace.SpawnEdits.Count + romWorkspace.TransitionEdits.Count + romWorkspace.PathEdits.Count + romWorkspace.ExitEdits.Count + romWorkspace.WaterboxEdits.Count + romWorkspace.EnvironmentEdits.Count + romWorkspace.CollisionVertexEdits.Count + romWorkspace.CollisionTriangleEdits.Count + romWorkspace.GeometryVertexEdits.Count + romWorkspace.GeometryTriangleEdits.Count} native changes are writable."; }
        catch (Exception error) { ShowError(error); }
    }

    private byte[] BuildVerifiedRom()
    {
        if (romWorkspace is null || loadedRomBytes is null) throw new InvalidOperationException("No ROM is loaded.");
        byte[] edited = romWorkspace.ApplyToRom(loadedRomBytes);
        var decoded = RomDecoder.Read(edited);
        if (decoded.Scenes.Count != romWorkspace.Document.Scenes.Count) throw new InvalidDataException("Edited ROM verification changed the decoded scene count.");
        return edited;
    }

    protected override async void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (resultCode != Result.Ok || data?.Data is null) return;
        busy = true; status.Text = "Reading document…";
        try
        {
            if (requestCode == ExportLayout)
            {
                using var output = ContentResolver!.OpenOutputStream(data.Data, "wt") ?? throw new IOException("Cannot write this document.");
                using var source = File.OpenRead(ExportPath);
                await source.CopyToAsync(output); await output.FlushAsync();
                status.Text = "Mobile layout exported. Desktop scene export is not available yet.";
                return;
            }
            if (requestCode == ExportRomPatchRequest)
            {
                using var output = ContentResolver!.OpenOutputStream(data.Data, "wt") ?? throw new IOException("Cannot write this document.");
                using var source = File.OpenRead(Path.Combine(FilesDir!.AbsolutePath, "archarina64-rom-patch.json"));
                await source.CopyToAsync(output); await output.FlushAsync(); status.Text = "Native ROM patch exported."; return;
            }
            if (requestCode == ExportRomRequest)
            {
                using var output = ContentResolver!.OpenOutputStream(data.Data, "wt") ?? throw new IOException("Cannot write this document.");
                using var source = File.OpenRead(Path.Combine(FilesDir!.AbsolutePath, "archarina64-edited.z64"));
                await source.CopyToAsync(output); await output.FlushAsync(); status.Text = "Edited uncompressed ROM exported."; return;
            }
            if (requestCode == ExportProjectRequest)
            {
                using var output = ContentResolver!.OpenOutputStream(data.Data, "wt") ?? throw new IOException("Cannot write this document.");
                using var source = File.OpenRead(Path.Combine(FilesDir!.AbsolutePath, "archarina64-project.json"));
                await source.CopyToAsync(output); await output.FlushAsync(); status.Text = "Native scene project snapshot exported."; return;
            }
            using var input = ContentResolver!.OpenInputStream(data.Data) ?? throw new IOException("Cannot read this document.");
            if (requestCode == ImportTexture)
            {
                if (romWorkspace is null || pendingTextureAsset is null || pendingTextureScene < 0 || pendingTextureRoom < 0) throw new InvalidOperationException("Choose a native texture before importing a replacement.");
                using var bitmap = global::Android.Graphics.BitmapFactory.DecodeStream(input) ?? throw new InvalidDataException("The selected file is not a readable bitmap.");
                if (bitmap.Width <= 0 || bitmap.Height <= 0 || bitmap.Width > 1024 || bitmap.Height > 1024) throw new InvalidDataException("Replacement PNG dimensions must be between 1 and 1024 pixels.");
                var argb = new int[bitmap.Width * bitmap.Height]; bitmap.GetPixels(argb, 0, bitmap.Width, 0, 0, bitmap.Width, bitmap.Height); var rgba = new byte[argb.Length * 4];
                for (int index = 0; index < argb.Length; index++) { int color = argb[index]; int offset = index * 4; rgba[offset] = (byte)(color >> 16); rgba[offset + 1] = (byte)(color >> 8); rgba[offset + 2] = (byte)color; rgba[offset + 3] = (byte)(color >> 24); }
                romWorkspace.ReplaceRoomTexture(pendingTextureScene, pendingTextureRoom, pendingTextureAsset with { Width = bitmap.Width, Height = bitmap.Height, Rgba = rgba }); status.Text = bitmap.Width == pendingTextureAsset.Width && bitmap.Height == pendingTextureAsset.Height ? "PNG texture replacement staged in the ROM workspace." : "PNG texture replacement staged with room relocation on export."; pendingTextureAsset = null; pendingTextureScene = pendingTextureRoom = -1; return;
            }
            if (requestCode == ImportGeometry)
            {
                if (romWorkspace is null || pendingGeometryScene < 0 || pendingGeometryRoom < 0) throw new InvalidOperationException("Choose a room before importing geometry.");
                string xml = Encoding.UTF8.GetString(await LayoutStorage.ReadBoundedBytesAsync(input)); var preview = DesktopImport.ReadTile(xml);
                var vertices = preview.Vertices.Select(v => new RomGeometryVertex((short)Math.Clamp(MathF.Round(v.X), short.MinValue, short.MaxValue), (short)Math.Clamp(MathF.Round(v.Y), short.MinValue, short.MaxValue), (short)Math.Clamp(MathF.Round(v.Z), short.MinValue, short.MaxValue), 0, 0, 255, 255, 255, 255)).ToArray();
                var triangles = Enumerable.Range(0, preview.Indices.Length / 3).Select(i => new RomGeometryTriangle(preview.Indices[i * 3], preview.Indices[i * 3 + 1], preview.Indices[i * 3 + 2])).ToArray();
                var importedGeometry = new RomRoomGeometry(vertices, triangles, 1); romWorkspace.ReplaceRoomGeometry(pendingGeometryScene, pendingGeometryRoom, importedGeometry); status.Text = romWorkspace.TopologyDirty ? "Custom geometry imported into the project workspace. Native display-list allocation is still required for this topology change." : romWorkspace.GeometryRebuildEdits.Any(e => e.SceneId == pendingGeometryScene && e.RoomId == pendingGeometryRoom) ? "Custom geometry changed topology; native single-list display-list rebuild is staged." : "Custom geometry matched the room topology and native geometry write-back is staged."; ShowRomRoom(romWorkspace.Document.Scenes.First(s => s.Id == pendingGeometryScene), romWorkspace.Document.Scenes.First(s => s.Id == pendingGeometryScene).Rooms[pendingGeometryRoom]); pendingGeometryScene = pendingGeometryRoom = -1; return;
            }
            if (requestCode == ImportProject)
            {
                byte[] projectBytes = await LayoutStorage.ReadBoundedBytesAsync(input); var imported = JsonSerializer.Deserialize<RomDocument>(projectBytes) ?? throw new InvalidDataException("Project snapshot is empty or invalid.");
                rom = imported; romWorkspace = new RomWorkspace(imported); loadedRomBytes = null; SelectDefaultRomScene(); status.Text = $"Loaded project snapshot: {imported.Scenes.Count} scenes and {imported.Scenes.Sum(s => s.Rooms.Count)} rooms. Reopen the source ROM before native ROM export."; return;
            }
            if (requestCode == ImportRom)
            {
                byte[] romBytes = await RomDecoder.ReadBoundedBytesAsync(input);
                loadedRomBytes = romBytes;
                rom = await Task.Run(() => RomDecoder.Read(romBytes));
                romWorkspace = new RomWorkspace(rom);
                SelectDefaultRomScene();
                status.Text = $"Decoded {rom.Scenes.Count} scenes from {rom.Profile.Name}; {rom.Scenes.Sum(s => s.Rooms.Count)} rooms and {rom.Scenes.Sum(s => s.Rooms.Sum(r => r.Actors.Count))} actors available for native editing.";
                return;
            }
            byte[] document = await LayoutStorage.ReadBoundedBytesAsync(input);
            if (requestCode == ImportTile)
            {
                if (document.Length >= 2 && document[0] == (byte)'P' && document[1] == (byte)'K')
                {
                    var bundle = await Task.Run(() => DesktopImport.ReadBundle(new MemoryStream(document)));
                    AddTile(bundle.Xml, bundle.Textures);
                }
                else
                {
                    string text = Encoding.UTF8.GetString(document);
                    await Task.Run(() => DesktopImport.ReadTile(text));
                    AddTile(text);
                }
            }
            else if (requestCode == OpenLayout)
            {
                var layout = await Task.Run(() => LayoutStorage.Read(Encoding.UTF8.GetString(document)));
                history.Apply(layout); selected = 0; Changed();
            }
            else if (requestCode == ImportCatalog)
            {
                var catalog = await Task.Run(() => DesktopImport.ReadCatalog(Encoding.UTF8.GetString(document)));
                string[] labels = catalog.Rooms.Select(r => r.ToString()).ToArray();
                new AlertDialog.Builder(this)!.SetTitle($"Room catalog • {labels.Length} rooms")!
                    .SetItems(labels, (_, args) => new AlertDialog.Builder(this)!.SetTitle(labels[args.Which])!
                        .SetMessage("This catalog contains room metadata. Import a SectionTile XML from the desktop tile library to preview its geometry.")!
                        .SetPositiveButton("OK", (_, _) => { })!.Show())!
                    .SetPositiveButton("Close", (_, _) => { })!.Show();
                status.Text = $"Read {labels.Length} room entries; {catalog.Diagnostics.Count} source diagnostics. Catalogs do not contain geometry.";
            }
        }
        catch (Exception error) { ShowError(error); }
        finally { busy = false; }
    }

    private void ShowRomScenes()
    {
        if ((romWorkspace?.Document ?? rom) is null) { status.Text = "Open a ROM first."; return; }
        RefreshSceneBrowser(); SetLeftPanel(true);
    }

    private void ShowSceneOrder()
    {
        var current = romWorkspace?.Document ?? rom; if (current is null || romWorkspace is null) return; var scenes = current.Scenes.OrderBy(s => s.Id).ToArray(); var labels = scenes.Select(s => $"{s.Id:X2}: {s.Name}").ToArray();
        new AlertDialog.Builder(this)!.SetTitle("Native scene-table order")!.SetItems(labels, (_, _) => { })!.SetPositiveButton("Reverse order", (_, _) => { try { romWorkspace.ReorderScenes(scenes.Select(s => s.Id).Reverse().ToArray()); status.Text = "Native scene-table reorder staged."; ShowRomScenes(); } catch (Exception error) { ShowError(error); } })!.SetNegativeButton("Back", (_, _) => ShowRomScenes())!.Show();
    }

    private void ShowEntrances()
    {
        var current = romWorkspace?.Document ?? rom; if (current is null) return;
        string[] labels = current.Entrances.Select(e => $"{e.Index:D3}: scene {e.SceneId:X2}  spawn {e.SpawnId:D2}").ToArray();
        var builder = new AlertDialog.Builder(this)!; builder.SetTitle($"ROM entrances • {labels.Length}"); builder.SetItems(labels, (_, args) => EditEntrance(current.Entrances[args.Which])); builder.SetNegativeButton("Back", (_, _) => ShowRomScenes()); builder.Show();
    }

    private void EditEntrance(RomEntrance entrance)
    {
        var workspace = romWorkspace; if (workspace is null) return;
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var scene = NumberEditor(panel, "Destination scene", entrance.SceneId.ToString(CultureInfo.InvariantCulture)); var spawn = NumberEditor(panel, "Spawn ID", entrance.SpawnId.ToString(CultureInfo.InvariantCulture));
        var builder = new AlertDialog.Builder(this)!; builder.SetTitle($"Edit entrance {entrance.Index:D3}"); builder.SetView(panel); builder.SetNegativeButton("Cancel", (_, _) => { }); builder.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditEntrance(entrance.Index, (byte)ParseInt(scene), (byte)ParseInt(spawn)); status.Text = "Native entrance edit staged in the ROM workspace."; ShowEntrances(); } catch (Exception error) { ShowError(error); } }); builder.Show();
    }

    private void ShowRomScene(RomScene scene)
    {
        string[] labels = scene.Rooms.Select(r => $"Room {r.Id:D2}  • {r.Actors.Count} actors  • {r.ObjectCount} objects" + (r.Geometry is not null ? $"  • mesh {r.Geometry.Vertices.Count}v/{r.Geometry.Triangles.Count}t" : (r.HasMesh ? "  • geometry" : "")) + (r.HasCollision ? "  • collision" : "")).Append($"Waterboxes ({scene.Collision?.Waterboxes?.Count ?? 0})").Append($"Environments ({scene.Environments?.Count ?? 0})").Append($"Collision vertices ({scene.Collision?.Vertices.Count ?? 0})").Append($"Spawns ({scene.SpawnPoints?.Count ?? 0})").Append($"Transitions ({scene.Transitions?.Count ?? 0})").Append($"Scene settings ({(scene.Settings is null ? 0 : 1)})").Append($"Alternate headers ({scene.AlternateHeaders?.Count ?? 0})").Append($"Scene commands ({scene.Commands?.Count ?? 0})").Append("Clone scene").Append("Delete scene").ToArray();
        new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} • {scene.Name}")!
            .SetItems(labels, (_, args) => { if (args.Which == scene.Rooms.Count) ShowRomWaterboxes(scene); else if (args.Which == scene.Rooms.Count + 1) ShowRomEnvironments(scene); else if (args.Which == scene.Rooms.Count + 2) ShowRomCollisionVertices(scene); else if (args.Which == scene.Rooms.Count + 3) ShowRomSpawns(scene); else if (args.Which == scene.Rooms.Count + 4) ShowRomTransitions(scene); else if (args.Which == scene.Rooms.Count + 5) EditNativeSceneSettings(scene); else if (args.Which == scene.Rooms.Count + 6) ShowRomAlternateHeaders(scene); else if (args.Which == scene.Rooms.Count + 7) ShowRomSceneCommands(scene); else if (args.Which == scene.Rooms.Count + 8) CloneNativeScene(scene); else if (args.Which == scene.Rooms.Count + 9) DeleteNativeScene(scene); else ShowRomRoom(scene, scene.Rooms[args.Which]); })!
            .SetNeutralButton("Room order", (_, _) => ShowRoomOrder(scene))!
            .SetPositiveButton($"Exits ({scene.Exits?.Count ?? 0})", (_, _) => ShowRomExits(scene))!
            .SetNegativeButton("Back", (_, _) => ShowRomScenes())!.Show();
    }

    private void CloneNativeScene(RomScene scene)
    {
        if (romWorkspace is null) return;
        try { int target = Enumerable.Range(0, romWorkspace.Document.Profile.SceneCount).FirstOrDefault(id => romWorkspace.Document.Scenes.All(existing => existing.Id != id), -1); if (target < 0) throw new InvalidDataException("The native scene table has no empty slot for a clone."); var clone = romWorkspace.CloneScene(scene.Id, target); status.Text = $"Scene {scene.Id:X2} cloned into native scene slot {clone.Id:X2}."; ShowRomScenes(); }
        catch (Exception error) { ShowError(error); }
    }

    private void DeleteNativeScene(RomScene scene)
    {
        if (romWorkspace is null) return;
        try { romWorkspace.DeleteScene(scene.Id); status.Text = $"Scene {scene.Id:X2} deletion staged for native scene-table export."; ShowRomScenes(); }
        catch (Exception error) { ShowError(error); }
    }

    private void ShowRoomOrder(RomScene scene)
    {
        if (romWorkspace is null) return;
        var labels = scene.Rooms.Select((room, index) => $"{index:D2}: Room {room.Id:D2} • {room.Actors.Count} actors • {room.ObjectCount} objects").ToArray();
        new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} room order")!.SetItems(labels, (_, _) => { })!.SetPositiveButton("Reverse order", (_, _) =>
        {
            try { var reordered = romWorkspace.ReorderRooms(scene.Id, Enumerable.Range(0, scene.Rooms.Count).Reverse().ToArray()); status.Text = "Native room table reorder staged."; ShowRomScene(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); }
            catch (Exception error) { ShowError(error); }
        })!.SetNegativeButton("Back", (_, _) => ShowRomScene(scene))!.Show();
    }

    private void ShowRomAlternateHeaders(RomScene scene)
    {
        var headers = scene.AlternateHeaders ?? [];
        string[] labels = headers.Select(h => $"Header {h.Index:D2} • {h.Commands?.Count ?? 0} commands" + (h.HasRooms ? " • rooms" : "") + (h.HasCollision ? " • collision" : "") + (h.HasSettings ? " • settings" : "") + (h.IsClone ? " • clone" : "")).ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} alternate headers");
        builder.SetItems(labels, (_, args) => EditAlternateHeader(scene, headers[args.Which]));
        builder.SetNeutralButton("Clone base", (_, _) => { try { romWorkspace?.CloneAlternateHeader(scene.Id); status.Text = "Alternate header clone staged in the ROM workspace."; ShowRomAlternateHeaders(romWorkspace!.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } });
        builder.SetNegativeButton("Back", (_, _) => ShowRomScene(scene)); builder.Show();
    }

    private void ShowRomSceneCommands(RomScene scene)
    {
        if (scene.Commands is not { } commands) return; var labels = commands.Select((c, i) => $"Command {i:D2} • 0x{c.Command:X2} • param {c.Parameter} • 0x{c.Pointer:X8}").ToArray(); var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} commands"); builder.SetItems(labels, (_, args) => EditRomSceneCommand(scene, args.Which)); builder.SetNeutralButton("Insert", (_, _) => InsertRomSceneCommand(scene)); builder.SetPositiveButton("Delete final", (_, _) => { try { if (commands.Count == 0) throw new InvalidDataException("Scene has no commands to delete."); romWorkspace?.DeleteSceneCommand(scene.Id, commands.Count - 1); status.Text = "Final scene command deletion staged."; ShowRomSceneCommands(romWorkspace!.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } }); builder.SetNegativeButton("Back", (_, _) => ShowRomScene(scene)); builder.Show();
    }

    private void InsertRomSceneCommand(RomScene scene)
    {
        if (romWorkspace is null) return; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var index = NumberEditor(panel, "Insert index", (scene.Commands?.Count ?? 0).ToString(CultureInfo.InvariantCulture)); var id = NumberEditor(panel, "Command", "0x00"); var parameter = NumberEditor(panel, "Parameter", "0"); var pointer = NumberEditor(panel, "Value / pointer", "0x00000000"); new AlertDialog.Builder(this)!.SetTitle("Insert scene command")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Insert", (_, _) => { try { romWorkspace.InsertSceneCommandAt(scene.Id, ParseInt(index), new RomHeaderCommand((byte)ParseInt(id), (byte)ParseInt(parameter), (uint)ParseUInt64(pointer))); status.Text = "Scene command insertion staged in the ROM workspace."; ShowRomSceneCommands(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditRomSceneCommand(RomScene scene, int index)
    {
        if (romWorkspace is null || scene.Commands is not { } commands || index < 0 || index >= commands.Count) return; var command = commands[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var id = NumberEditor(panel, "Command", $"0x{command.Command:X2}"); var parameter = NumberEditor(panel, "Parameter", command.Parameter.ToString(CultureInfo.InvariantCulture)); var pointer = NumberEditor(panel, "Value / pointer", $"0x{command.Pointer:X8}"); new AlertDialog.Builder(this)!.SetTitle($"Edit scene command {index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditSceneCommand(scene.Id, index, new RomHeaderCommand((byte)ParseInt(id), (byte)ParseInt(parameter), (uint)ParseUInt64(pointer))); status.Text = "Scene command edit staged in the ROM workspace."; ShowRomSceneCommands(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditAlternateHeader(RomScene scene, RomAlternateHeader header)
    {
        string commands = string.Join(", ", (header.Commands ?? []).Select(c => $"0x{c.Command:X2}"));
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Alternate header {header.Index:D2}")!.SetMessage($"Pointer: 0x{header.Offset:X8}\nCommands: {commands}\n{(header.IsClone ? "This header is marked as a clone." : "This header has its own command list.")}")!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Delete", (_, _) =>
        {
            try { romWorkspace?.DeleteAlternateHeader(scene.Id, header.Index); status.Text = "Alternate header deletion staged in the ROM workspace."; ShowRomAlternateHeaders(romWorkspace!.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); }
        });
        if (header.Rooms is not null) builder.SetNeutralButton("Rooms", (_, _) => ShowAlternateRooms(scene, header)); else if (header.Commands is not null) builder.SetNeutralButton("Commands", (_, _) => ShowAlternateCommands(scene, header));
        builder.Show();
    }

    private void EditAlternateSceneSettings(RomScene scene, RomAlternateHeader header)
    {
        if (romWorkspace is null || header.Settings is not { } settings) return;
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var movement = NumberEditor(panel, "Camera movement", settings.CameraMovement.ToString(CultureInfo.InvariantCulture)); var worldMap = NumberEditor(panel, "World map", settings.WorldMap.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit alternate header {header.Index:D2} settings")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
        {
            try { romWorkspace.EditAlternateSceneSettings(scene.Id, header.Index, new RomSceneSettings((byte)ParseInt(movement), (byte)ParseInt(worldMap))); status.Text = "Alternate scene settings edit staged in the ROM workspace."; ShowRomAlternateHeaders(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void ShowAlternateRooms(RomScene scene, RomAlternateHeader header)
    {
        var rooms = header.Rooms ?? []; var labels = rooms.Select(r => $"Room {r.Id:D2} • {r.Actors.Count} actors • {r.ObjectCount} objects").Append($"Collision ({header.Collision?.Vertices.Count ?? 0}v/{header.Collision?.Triangles.Count ?? 0}t, {header.Collision?.Cameras?.Count ?? 0} cameras, {header.Collision?.Waterboxes?.Count ?? 0} waterboxes)").Append($"Commands ({header.Commands?.Count ?? 0})").ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Alternate header {header.Index:D2} rooms"); builder.SetItems(labels, (_, args) => { if (args.Which == rooms.Count) ShowAlternateCollision(scene, header); else if (args.Which == rooms.Count + 1) ShowAlternateCommands(scene, header); else ShowAlternateRoom(scene, header, args.Which); }); builder.SetNeutralButton("Room order", (_, _) => ShowAlternateRoomOrder(scene, header)); builder.SetNegativeButton("Back", (_, _) => ShowRomAlternateHeaders(scene)); builder.Show();
    }

    private void ShowAlternateRoomOrder(RomScene scene, RomAlternateHeader header)
    {
        if (romWorkspace is null || header.Rooms is not { } rooms) return; var labels = rooms.Select((room, index) => $"{index:D2}: Room {room.Id:D2} • {room.Actors.Count} actors • {room.ObjectCount} objects").ToArray();
        new AlertDialog.Builder(this)!.SetTitle($"Alternate header {header.Index:D2} room order")!.SetItems(labels, (_, _) => { })!.SetPositiveButton("Reverse order", (_, _) => { try { romWorkspace.ReorderAlternateRooms(scene.Id, header.Index, Enumerable.Range(0, rooms.Count).Reverse().ToArray()); status.Text = "Native alternate room table reorder staged."; var updated = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index); ShowAlternateRooms(scene, updated); } catch (Exception error) { ShowError(error); } })!.SetNegativeButton("Back", (_, _) => ShowAlternateRooms(scene, header))!.Show();
    }

    private void ShowAlternateCommands(RomScene scene, RomAlternateHeader header)
    {
        if (header.Commands is not { } commands) return; var labels = commands.Select((c, i) => $"Command {i:D2} • 0x{c.Command:X2} • param {c.Parameter} • 0x{c.Pointer:X8}").ToArray(); var builder = new AlertDialog.Builder(this)!.SetTitle($"Header {header.Index:D2} commands"); builder.SetItems(labels, (_, args) => EditAlternateCommand(scene, header, args.Which)); builder.SetNeutralButton("Insert", (_, _) => InsertAlternateCommand(scene, header)); builder.SetPositiveButton("Delete final", (_, _) => { try { if (commands.Count == 0) throw new InvalidDataException("Alternate header has no commands to delete."); romWorkspace?.DeleteAlternateCommand(scene.Id, header.Index, commands.Count - 1); status.Text = "Final alternate header command deletion staged."; ShowAlternateCommands(scene, romWorkspace!.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); } }); builder.SetNegativeButton("Back", (_, _) => { if (header.Rooms is not null) ShowAlternateRooms(scene, header); else ShowRomAlternateHeaders(scene); }); builder.Show();
    }

    private void InsertAlternateCommand(RomScene scene, RomAlternateHeader header)
    {
        if (romWorkspace is null) return; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var index = NumberEditor(panel, "Insert index", (header.Commands?.Count ?? 0).ToString(CultureInfo.InvariantCulture)); var id = NumberEditor(panel, "Command", "0x00"); var parameter = NumberEditor(panel, "Parameter", "0"); var pointer = NumberEditor(panel, "Value / pointer", "0x00000000"); new AlertDialog.Builder(this)!.SetTitle($"Insert alternate command")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Insert", (_, _) => { try { romWorkspace.InsertAlternateCommandAt(scene.Id, header.Index, ParseInt(index), new RomHeaderCommand((byte)ParseInt(id), (byte)ParseInt(parameter), (uint)ParseUInt64(pointer))); status.Text = "Alternate header command insertion staged in the ROM workspace."; ShowAlternateCommands(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditAlternateCommand(RomScene scene, RomAlternateHeader header, int index)
    {
        if (romWorkspace is null || header.Commands is not { } commands || index < 0 || index >= commands.Count) return; var command = commands[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var id = NumberEditor(panel, "Command", $"0x{command.Command:X2}"); var parameter = NumberEditor(panel, "Parameter", command.Parameter.ToString(CultureInfo.InvariantCulture)); var pointer = NumberEditor(panel, "Value / pointer", $"0x{command.Pointer:X8}"); new AlertDialog.Builder(this)!.SetTitle($"Edit alternate command {index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditAlternateCommand(scene.Id, header.Index, index, new RomHeaderCommand((byte)ParseInt(id), (byte)ParseInt(parameter), (uint)ParseUInt64(pointer))); status.Text = "Alternate header command edit staged in the ROM workspace."; ShowAlternateCommands(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void ShowAlternateCollision(RomScene scene, RomAlternateHeader header)
    {
        if (header.Collision is not { } collision) return; var vertices = collision.Vertices ?? []; var triangles = collision.Triangles ?? []; var surfaces = collision.SurfaceTypes ?? []; var cameras = collision.Cameras ?? []; var waterboxes = collision.Waterboxes ?? []; var labels = new[] { "Add collision vertex", "Remove last collision vertex", "Add collision triangle", "Remove last collision triangle", "Recalculate normals + bounds", $"Bounds • min ({collision.MinX}, {collision.MinY}, {collision.MinZ}) max ({collision.MaxX}, {collision.MaxY}, {collision.MaxZ})" }.Concat(vertices.Select((v, i) => $"Vertex {i:D3} • ({v.X}, {v.Y}, {v.Z})")).Concat(triangles.Select((t, i) => $"Triangle {i:D3} • {t.A}/{t.B}/{t.C}")).Concat(surfaces.Select((s, i) => { var material = RomCollisionSurfaceType.Decode(s); return $"Surface {i:D3} • 0x{s:X16} • material {material.Material} • floor {material.FloorType} • hookshot {(material.CanHookshot ? "yes" : "no")}"; })).Concat(cameras.Select((c, i) => $"Camera {i:D2} • type {c.Type} • {c.DataCount} page/path points • ({c.X}, {c.Y}, {c.Z})")).Concat(waterboxes.Select((w, i) => $"Waterbox {i:D2} • ({w.X}, {w.Y}, {w.Z}) • {w.XSize}×{w.ZSize}")).ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Header {header.Index:D2} collision"); int vertexStart = 6, triangleStart = vertexStart + vertices.Count, surfaceStart = triangleStart + triangles.Count, cameraStart = surfaceStart + surfaces.Count, waterboxStart = cameraStart + cameras.Count; builder.SetItems(labels, (_, args) => { if (args.Which == 0) AddAlternateCollisionVertex(scene, header); else if (args.Which == 1) RemoveAlternateCollisionVertex(scene, header); else if (args.Which == 2) AddAlternateCollisionTriangle(scene, header); else if (args.Which == 3) RemoveAlternateCollisionTriangle(scene, header); else if (args.Which == 4) RecalculateAlternateCollision(scene, header); else if (args.Which == 5) EditAlternateCollisionBounds(scene, header); else if (args.Which < triangleStart) EditAlternateCollisionVertex(scene, header, args.Which - vertexStart); else if (args.Which < surfaceStart) EditAlternateCollisionTriangle(scene, header, args.Which - triangleStart); else if (args.Which < cameraStart) EditAlternateCollisionSurface(scene, header, args.Which - surfaceStart); else if (args.Which < waterboxStart) EditAlternateCamera(scene, header, args.Which - cameraStart); else EditAlternateWaterbox(scene, header, args.Which - waterboxStart); }); builder.SetNegativeButton("Back", (_, _) => ShowAlternateRooms(scene, header)); builder.Show();
    }

    private void RecalculateAlternateCollision(RomScene scene, RomAlternateHeader header)
    {
        if (romWorkspace is null) return;
        try { romWorkspace.RecalculateAlternateCollision(scene.Id, header.Index); status.Text = "Alternate collision normals and bounds recalculated."; ShowAlternateCollision(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); }
        catch (Exception error) { ShowError(error); }
    }

    private void AddAlternateCollisionVertex(RomScene scene, RomAlternateHeader header)
    {
        if (romWorkspace is null || header.Collision is not { } collision) return; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var x = NumberEditor(panel, "X", "0"); var y = NumberEditor(panel, "Y", "0"); var z = NumberEditor(panel, "Z", "0"); new AlertDialog.Builder(this)!.SetTitle("Add alternate collision vertex")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Add", (_, _) => { try { romWorkspace.ReplaceAlternateCollisionTopology(scene.Id, header.Index, collision.Vertices.Concat([new RomCollisionVertex((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z))]).ToArray(), collision.Triangles, collision.SurfaceTypes.Count == collision.Triangles.Count ? collision.SurfaceTypes : collision.Triangles.Select(_ => 0UL).ToArray()); status.Text = "Alternate collision vertex insertion staged."; ShowAlternateCollision(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void RemoveAlternateCollisionVertex(RomScene scene, RomAlternateHeader header)
    {
        if (romWorkspace is null || header.Collision is not { } collision) return; try { if (collision.Vertices.Count <= 1) throw new InvalidDataException("A collision must retain at least one vertex."); int removed = collision.Vertices.Count - 1; if (collision.Triangles.Any(t => t.A == removed || t.B == removed || t.C == removed)) throw new InvalidDataException("The last alternate collision vertex is still referenced by a triangle."); romWorkspace.ReplaceAlternateCollisionTopology(scene.Id, header.Index, collision.Vertices.Take(removed).ToArray(), collision.Triangles, collision.SurfaceTypes); status.Text = "Alternate collision vertex removal staged."; ShowAlternateCollision(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); }
    }

    private void AddAlternateCollisionTriangle(RomScene scene, RomAlternateHeader header)
    {
        if (romWorkspace is null || header.Collision is not { } collision) return; if (collision.Vertices.Count < 3) { ShowError(new InvalidDataException("Add at least three alternate collision vertices before adding a triangle.")); return; } var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var surface = NumberEditor(panel, "Surface type", "0"); var a = NumberEditor(panel, "A", "0"); var b = NumberEditor(panel, "B", "1"); var c = NumberEditor(panel, "C", "2"); var nx = NumberEditor(panel, "Normal X", "0"); var ny = NumberEditor(panel, "Normal Y", "0"); var nz = NumberEditor(panel, "Normal Z", "0"); var distance = NumberEditor(panel, "Distance", "0"); new AlertDialog.Builder(this)!.SetTitle("Add alternate collision triangle")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Add", (_, _) => { try { var triangle = new RomCollisionTriangle((ushort)ParseInt(surface), (ushort)ParseInt(a), (ushort)ParseInt(b), (ushort)ParseInt(c), (short)ParseInt(nx), (short)ParseInt(ny), (short)ParseInt(nz), (short)ParseInt(distance)); var surfaces = collision.SurfaceTypes.Count == collision.Triangles.Count ? collision.SurfaceTypes.Concat([0UL]).ToArray() : collision.Triangles.Select(_ => 0UL).Concat([0UL]).ToArray(); romWorkspace.ReplaceAlternateCollisionTopology(scene.Id, header.Index, collision.Vertices, collision.Triangles.Concat([triangle]).ToArray(), surfaces); status.Text = "Alternate collision triangle insertion staged."; ShowAlternateCollision(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void RemoveAlternateCollisionTriangle(RomScene scene, RomAlternateHeader header)
    {
        if (romWorkspace is null || header.Collision is not { } collision) return; try { if (collision.Triangles.Count <= 1) throw new InvalidDataException("A collision must retain at least one triangle."); int count = collision.Triangles.Count - 1; var surfaces = collision.SurfaceTypes.Count == collision.Triangles.Count ? collision.SurfaceTypes.Take(count).ToArray() : collision.Triangles.Take(count).Select(_ => 0UL).ToArray(); romWorkspace.ReplaceAlternateCollisionTopology(scene.Id, header.Index, collision.Vertices, collision.Triangles.Take(count).ToArray(), surfaces); status.Text = "Alternate collision triangle removal staged."; ShowAlternateCollision(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); }
    }

    private void EditAlternateCollisionBounds(RomScene scene, RomAlternateHeader header)
    {
        if (romWorkspace is null || header.Collision is not { } collision) return; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var minX = NumberEditor(panel, "Min X", collision.MinX.ToString(CultureInfo.InvariantCulture)); var minY = NumberEditor(panel, "Min Y", collision.MinY.ToString(CultureInfo.InvariantCulture)); var minZ = NumberEditor(panel, "Min Z", collision.MinZ.ToString(CultureInfo.InvariantCulture)); var maxX = NumberEditor(panel, "Max X", collision.MaxX.ToString(CultureInfo.InvariantCulture)); var maxY = NumberEditor(panel, "Max Y", collision.MaxY.ToString(CultureInfo.InvariantCulture)); var maxZ = NumberEditor(panel, "Max Z", collision.MaxZ.ToString(CultureInfo.InvariantCulture)); new AlertDialog.Builder(this)!.SetTitle($"Edit alternate collision bounds")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditAlternateCollisionBounds(scene.Id, header.Index, collision with { MinX = (short)ParseInt(minX), MinY = (short)ParseInt(minY), MinZ = (short)ParseInt(minZ), MaxX = (short)ParseInt(maxX), MaxY = (short)ParseInt(maxY), MaxZ = (short)ParseInt(maxZ) }); status.Text = "Alternate collision bounds edit staged in the ROM workspace."; ShowAlternateCollision(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditAlternateCollisionVertex(RomScene scene, RomAlternateHeader header, int index)
    {
        if (romWorkspace is null || header.Collision?.Vertices is not { } vertices || index < 0 || index >= vertices.Count) return; var vertex = vertices[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var x = NumberEditor(panel, "X", vertex.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", vertex.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", vertex.Z.ToString(CultureInfo.InvariantCulture)); new AlertDialog.Builder(this)!.SetTitle($"Edit alternate collision vertex {index:D3}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditAlternateCollisionVertex(scene.Id, header.Index, index, new RomCollisionVertex((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z))); status.Text = "Alternate collision vertex edit staged in the ROM workspace."; ShowAlternateCollision(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditAlternateCollisionTriangle(RomScene scene, RomAlternateHeader header, int index)
    {
        if (romWorkspace is null || header.Collision?.Triangles is not { } triangles || index < 0 || index >= triangles.Count) return; var triangle = triangles[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var surface = NumberEditor(panel, "Surface type", $"0x{triangle.SurfaceType:X4}"); var a = NumberEditor(panel, "A", triangle.A.ToString(CultureInfo.InvariantCulture)); var b = NumberEditor(panel, "B", triangle.B.ToString(CultureInfo.InvariantCulture)); var c = NumberEditor(panel, "C", triangle.C.ToString(CultureInfo.InvariantCulture)); var nx = NumberEditor(panel, "Normal X", triangle.NormalX.ToString(CultureInfo.InvariantCulture)); var ny = NumberEditor(panel, "Normal Y", triangle.NormalY.ToString(CultureInfo.InvariantCulture)); var nz = NumberEditor(panel, "Normal Z", triangle.NormalZ.ToString(CultureInfo.InvariantCulture)); var distance = NumberEditor(panel, "Distance", triangle.Distance.ToString(CultureInfo.InvariantCulture)); new AlertDialog.Builder(this)!.SetTitle($"Edit alternate collision triangle {index:D3}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditAlternateCollisionTriangle(scene.Id, header.Index, index, new RomCollisionTriangle((ushort)ParseInt(surface), (ushort)ParseInt(a), (ushort)ParseInt(b), (ushort)ParseInt(c), (short)ParseInt(nx), (short)ParseInt(ny), (short)ParseInt(nz), (short)ParseInt(distance))); status.Text = "Alternate collision triangle edit staged in the ROM workspace."; ShowAlternateCollision(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditAlternateCollisionSurface(RomScene scene, RomAlternateHeader header, int index)
    {
        if (romWorkspace is null || header.Collision?.SurfaceTypes is not { } surfaces || index < 0 || index >= surfaces.Count) return; var decoded = RomCollisionSurfaceType.Decode(surfaces[index]); var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var bgCamera = NumberEditor(panel, "Background camera", decoded.BgCameraIndex.ToString(CultureInfo.InvariantCulture)); var exit = NumberEditor(panel, "Scene exit", decoded.ExitIndex.ToString(CultureInfo.InvariantCulture)); var floor = NumberEditor(panel, "Floor type", decoded.FloorType.ToString(CultureInfo.InvariantCulture)); var unknown18 = NumberEditor(panel, "Unknown 18", decoded.Unknown18.ToString(CultureInfo.InvariantCulture)); var wall = NumberEditor(panel, "Wall type", decoded.WallType.ToString(CultureInfo.InvariantCulture)); var floorProperty = NumberEditor(panel, "Floor property", decoded.FloorProperty.ToString(CultureInfo.InvariantCulture)); var soft = NumberEditor(panel, "Soft (0/1)", decoded.IsSoft ? "1" : "0"); var horse = NumberEditor(panel, "Horse blocked (0/1)", decoded.IsHorseBlocked ? "1" : "0"); var material = NumberEditor(panel, "Material", decoded.Material.ToString(CultureInfo.InvariantCulture)); var floorEffect = NumberEditor(panel, "Floor effect", decoded.FloorEffect.ToString(CultureInfo.InvariantCulture)); var light = NumberEditor(panel, "Light setting", decoded.LightSetting.ToString(CultureInfo.InvariantCulture)); var echo = NumberEditor(panel, "Echo", decoded.Echo.ToString(CultureInfo.InvariantCulture)); var hookshot = NumberEditor(panel, "Hookshot allowed (0/1)", decoded.CanHookshot ? "1" : "0"); var conveyorSpeed = NumberEditor(panel, "Conveyor speed", decoded.ConveyorSpeed.ToString(CultureInfo.InvariantCulture)); var conveyorDirection = NumberEditor(panel, "Conveyor direction", decoded.ConveyorDirection.ToString(CultureInfo.InvariantCulture)); var unknown27 = NumberEditor(panel, "Unknown 27 (0/1)", decoded.Unknown27 ? "1" : "0"); new AlertDialog.Builder(this)!.SetTitle($"Edit alternate collision surface {index:D3}")!.SetMessage("OoT SURFACETYPE0 and SURFACETYPE1 fields")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { var value = new RomCollisionSurfaceType(decoded.Raw, (byte)ParseInt(bgCamera), (byte)ParseInt(exit), (byte)ParseInt(floor), (byte)ParseInt(unknown18), (byte)ParseInt(wall), (byte)ParseInt(floorProperty), ParseInt(soft) != 0, ParseInt(horse) != 0, (byte)ParseInt(material), (byte)ParseInt(floorEffect), (byte)ParseInt(light), (byte)ParseInt(echo), ParseInt(hookshot) != 0, (byte)ParseInt(conveyorSpeed), (byte)ParseInt(conveyorDirection), ParseInt(unknown27) != 0).Encode(); romWorkspace.EditAlternateCollisionSurface(scene.Id, header.Index, index, value); status.Text = "Alternate collision material flags staged in the ROM workspace."; ShowAlternateCollision(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditAlternateCamera(RomScene scene, RomAlternateHeader header, int index)
    {
        if (romWorkspace is null || header.Collision?.Cameras is not { } cameras || index < 0 || index >= cameras.Count) return; var camera = cameras[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var type = NumberEditor(panel, "Type", camera.Type.ToString(CultureInfo.InvariantCulture)); var count = NumberEditor(panel, "Page/path point count", camera.DataCount.ToString(CultureInfo.InvariantCulture)); var x = NumberEditor(panel, "X", camera.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", camera.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", camera.Z.ToString(CultureInfo.InvariantCulture)); var rx = NumberEditor(panel, "X rotation", camera.RotationX.ToString(CultureInfo.InvariantCulture)); var ry = NumberEditor(panel, "Y rotation", camera.RotationY.ToString(CultureInfo.InvariantCulture)); var rz = NumberEditor(panel, "Z rotation", camera.RotationZ.ToString(CultureInfo.InvariantCulture)); var fov = NumberEditor(panel, "FOV", camera.Fov.ToString(CultureInfo.InvariantCulture)); var u1 = NumberEditor(panel, "Unknown 1", camera.Unknown1.ToString(CultureInfo.InvariantCulture)); var u2 = NumberEditor(panel, "Unknown 2", camera.Unknown2.ToString(CultureInfo.InvariantCulture)); var builder = new AlertDialog.Builder(this)!.SetTitle($"Edit alternate camera {index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditAlternateCamera(scene.Id, header.Index, index, new RomCamera((byte)ParseInt(type), (short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(rx), (short)ParseInt(ry), (short)ParseInt(rz), (short)ParseInt(fov), (ushort)ParseInt(u1), (ushort)ParseInt(u2), (short)ParseInt(count), camera.PathPoints)); status.Text = "Alternate collision camera page edit staged in the ROM workspace."; ShowAlternateCollision(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); } }); if (camera.PathPoints is { Count: > 0 }) builder.SetNeutralButton("Edit path", (_, _) => ShowAlternateCameraPath(scene, header, index)); builder.Show();
    }

    private void ShowAlternateCameraPath(RomScene scene, RomAlternateHeader header, int cameraIndex)
    {
        if (romWorkspace is null || header.Collision?.Cameras is not { } cameras || cameraIndex < 0 || cameraIndex >= cameras.Count || cameras[cameraIndex].PathPoints is not { } points) return;
        var labels = points.Select((point, index) => $"Point {index:D2} • ({point.X}, {point.Y}, {point.Z})").ToArray(); var builder = new AlertDialog.Builder(this)!.SetTitle($"Header {header.Index:D2} camera {cameraIndex:D2} path points"); builder.SetItems(labels, (_, args) => EditAlternateCameraPathPoint(scene, header, cameraIndex, args.Which)); builder.SetNegativeButton("Back", (_, _) => EditAlternateCamera(scene, header, cameraIndex)); builder.Show();
    }

    private void EditAlternateCameraPathPoint(RomScene scene, RomAlternateHeader header, int cameraIndex, int pointIndex)
    {
        if (romWorkspace is null || header.Collision?.Cameras is not { } cameras || cameraIndex < 0 || cameraIndex >= cameras.Count || cameras[cameraIndex].PathPoints is not { } points || pointIndex < 0 || pointIndex >= points.Count) return;
        var point = points[pointIndex]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var x = NumberEditor(panel, "X", point.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", point.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", point.Z.ToString(CultureInfo.InvariantCulture)); new AlertDialog.Builder(this)!.SetTitle($"Edit alternate camera {cameraIndex:D2} path point {pointIndex:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { var updated = points.ToArray(); updated[pointIndex] = new RomCameraPathPoint((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z)); var camera = cameras[cameraIndex] with { DataCount = (short)updated.Length, PathPoints = updated }; romWorkspace.EditAlternateCamera(scene.Id, header.Index, cameraIndex, camera); status.Text = "Alternate camera path point edit staged in the ROM workspace."; var updatedScene = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); ShowAlternateCameraPath(updatedScene, updatedScene.AlternateHeaders!.First(h => h.Index == header.Index), cameraIndex); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditAlternateWaterbox(RomScene scene, RomAlternateHeader header, int index)
    {
        if (romWorkspace is null || header.Collision?.Waterboxes is not { } waterboxes || index < 0 || index >= waterboxes.Count) return; var waterbox = waterboxes[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var x = NumberEditor(panel, "X", waterbox.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", waterbox.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", waterbox.Z.ToString(CultureInfo.InvariantCulture)); var xs = NumberEditor(panel, "X size", waterbox.XSize.ToString(CultureInfo.InvariantCulture)); var zs = NumberEditor(panel, "Z size", waterbox.ZSize.ToString(CultureInfo.InvariantCulture)); var unknown = NumberEditor(panel, "Unknown", waterbox.Unknown.ToString(CultureInfo.InvariantCulture)); var properties = NumberEditor(panel, "Properties", $"0x{waterbox.Properties:X8}"); new AlertDialog.Builder(this)!.SetTitle($"Edit alternate waterbox {index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditAlternateWaterbox(scene.Id, header.Index, index, new RomWaterbox((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(xs), (short)ParseInt(zs), (ushort)ParseInt(unknown), (uint)ParseUInt64(properties))); status.Text = "Alternate collision waterbox edit staged in the ROM workspace."; ShowAlternateCollision(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void ShowAlternateRoom(RomScene scene, RomAlternateHeader header, int roomId)
    {
        if (header.Rooms is not { } rooms || roomId < 0 || roomId >= rooms.Count) return; var room = rooms[roomId]; var labelList = new List<string> { $"Actors ({room.Actors.Count})", $"Objects ({room.Objects?.Count ?? 0})", "Room settings", $"Room exits ({room.Exits?.Count ?? 0})" }; if (room.Geometry is not null) labelList.Add($"Geometry ({room.Geometry.Vertices.Count} vertices)"); int cloneIndex = labelList.Count; labelList.Add("Clone room"); int deleteIndex = labelList.Count; labelList.Add("Delete room"); string[] labels = labelList.ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Header {header.Index:D2} room {roomId:D2}"); builder.SetItems(labels, (_, args) => { if (args.Which == 0) ShowAlternateActors(scene, header, roomId); else if (args.Which == 1) ShowAlternateObjects(scene, header, roomId); else if (args.Which == 2) EditAlternateRoomSettings(scene, header, roomId); else if (args.Which == 3) EditAlternateRoomExits(scene, header, roomId); else if (room.Geometry is not null && args.Which == 4) ShowAlternateGeometry(scene, header, roomId); else if (args.Which == cloneIndex) CloneAlternateRoom(scene, header, roomId); else if (args.Which == deleteIndex) DeleteAlternateRoom(scene, header, roomId); }); builder.SetNegativeButton("Back", (_, _) => ShowAlternateRooms(scene, header)); builder.Show();
    }

    private void CloneAlternateRoom(RomScene scene, RomAlternateHeader header, int roomId)
    {
        if (romWorkspace is null) return; try { var clone = romWorkspace.CloneAlternateRoom(scene.Id, header.Index, roomId); status.Text = $"Alternate room {roomId:D2} cloned as room {clone.Id:D2}; native allocation will occur on export."; var updated = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index); ShowAlternateRooms(scene, updated); } catch (Exception error) { ShowError(error); }
    }

    private void DeleteAlternateRoom(RomScene scene, RomAlternateHeader header, int roomId)
    {
        if (romWorkspace is null) return; try { romWorkspace.DeleteAlternateRoom(scene.Id, header.Index, roomId); status.Text = $"Alternate room {roomId:D2} deleted; native room table rewrite is staged for export."; var updated = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index); ShowAlternateRooms(scene, updated); } catch (Exception error) { ShowError(error); }
    }

    private void ShowAlternateGeometry(RomScene scene, RomAlternateHeader header, int roomId)
    {
        if (header.Rooms is not { } rooms || roomId < 0 || roomId >= rooms.Count || rooms[roomId].Geometry is not { } geometry) return; string[] labels = geometry.Vertices.Select((v, i) => $"Vertex {i:D3} • ({v.X}, {v.Y}, {v.Z})").Concat(geometry.Triangles.Select((t, i) => $"Triangle {i:D3} • {t.A}/{t.B}/{t.C}")).ToArray(); var builder = new AlertDialog.Builder(this)!.SetTitle($"Header {header.Index:D2} room {roomId:D2} geometry"); builder.SetItems(labels, (_, args) => { if (args.Which < geometry.Vertices.Count) EditAlternateGeometryVertex(scene, header, roomId, args.Which); else EditAlternateGeometryTriangle(scene, header, roomId, args.Which - geometry.Vertices.Count); }); builder.SetNegativeButton("Back", (_, _) => ShowAlternateRoom(scene, header, roomId)); builder.Show();
    }

    private void EditAlternateGeometryVertex(RomScene scene, RomAlternateHeader header, int roomId, int index)
    {
        if (romWorkspace is null || header.Rooms is not { } rooms || roomId < 0 || roomId >= rooms.Count || rooms[roomId].Geometry is not { } geometry || index < 0 || index >= geometry.Vertices.Count) return; var vertex = geometry.Vertices[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var x = NumberEditor(panel, "X", vertex.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", vertex.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", vertex.Z.ToString(CultureInfo.InvariantCulture)); new AlertDialog.Builder(this)!.SetTitle($"Edit alternate vertex {index:D3}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditAlternateGeometryVertex(scene.Id, header.Index, roomId, index, vertex with { X = (short)ParseInt(x), Y = (short)ParseInt(y), Z = (short)ParseInt(z) }); status.Text = "Alternate geometry vertex edit staged in the ROM workspace."; ShowAlternateGeometry(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index), roomId); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditAlternateGeometryTriangle(RomScene scene, RomAlternateHeader header, int roomId, int index)
    {
        if (romWorkspace is null || header.Rooms is not { } rooms || roomId < 0 || roomId >= rooms.Count || rooms[roomId].Geometry is not { } geometry || index < 0 || index >= geometry.Triangles.Count) return; var triangle = geometry.Triangles[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var a = NumberEditor(panel, "A", triangle.A.ToString(CultureInfo.InvariantCulture)); var b = NumberEditor(panel, "B", triangle.B.ToString(CultureInfo.InvariantCulture)); var c = NumberEditor(panel, "C", triangle.C.ToString(CultureInfo.InvariantCulture)); new AlertDialog.Builder(this)!.SetTitle($"Edit alternate triangle {index:D3}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditAlternateGeometryTriangle(scene.Id, header.Index, roomId, index, new RomGeometryTriangle(ParseInt(a), ParseInt(b), ParseInt(c))); status.Text = "Alternate geometry triangle edit staged in the ROM workspace."; ShowAlternateGeometry(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index), roomId); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void ShowAlternateObjects(RomScene scene, RomAlternateHeader header, int roomId)
    {
        if (romWorkspace is null || header.Rooms is not { } rooms || roomId < 0 || roomId >= rooms.Count) return; var objects = rooms[roomId].Objects ?? []; string[] labels = objects.Select((o, i) => $"Object {i:D2} • 0x{o:X4}").ToArray(); var builder = new AlertDialog.Builder(this)!.SetTitle($"Header {header.Index:D2} room {roomId:D2} objects"); builder.SetItems(labels, (_, args) => EditAlternateObject(scene, header, roomId, args.Which)); builder.SetPositiveButton("Add object", (_, _) => AddAlternateObject(scene, header, roomId)); builder.SetNeutralButton("Remove last", (_, _) => { if (objects.Count == 0) return; try { romWorkspace.EditAlternateRoomObjects(scene.Id, header.Index, roomId, objects.Take(objects.Count - 1).ToArray()); status.Text = "Alternate-header room object removal staged."; var current = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index); ShowAlternateObjects(scene, current, roomId); } catch (Exception error) { ShowError(error); } }); builder.SetNegativeButton("Back", (_, _) => ShowAlternateRoom(scene, header, roomId)); builder.Show();
    }

    private void AddAlternateObject(RomScene scene, RomAlternateHeader header, int roomId)
    {
        if (romWorkspace is null || header.Rooms is not { } rooms || roomId < 0 || roomId >= rooms.Count) return;
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var editor = NumberEditor(panel, "Object ID (hex)", "0x0000");
        new AlertDialog.Builder(this)!.SetTitle("Add alternate room object")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Add", (_, _) =>
        {
            try { var objects = (rooms[roomId].Objects ?? []).Concat([(ushort)ParseInt(editor)]).ToArray(); romWorkspace.EditAlternateRoomObjects(scene.Id, header.Index, roomId, objects); status.Text = "Alternate-header room object insertion staged."; var current = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index); ShowAlternateObjects(scene, current, roomId); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void EditAlternateObject(RomScene scene, RomAlternateHeader header, int roomId, int index)
    {
        if (romWorkspace is null || header.Rooms is not { } rooms || roomId < 0 || roomId >= rooms.Count || rooms[roomId].Objects is not { } objects || index < 0 || index >= objects.Count) return; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var editor = NumberEditor(panel, "Object ID", $"0x{objects[index]:X4}"); new AlertDialog.Builder(this)!.SetTitle($"Edit alternate object {index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditAlternateRoomObject(scene.Id, header.Index, roomId, index, (ushort)ParseInt(editor)); status.Text = "Alternate object edit staged in the ROM workspace."; ShowAlternateRoom(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index), roomId); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditAlternateRoomSettings(RomScene scene, RomAlternateHeader header, int roomId)
    {
        if (romWorkspace is null || header.Rooms is not { } rooms || roomId < 0 || roomId >= rooms.Count || rooms[roomId].Settings is not { } settings) return; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var behavior = NumberEditor(panel, "Behavior", $"0x{settings.Behavior:X8}"); var wind = NumberEditor(panel, "Wind", $"0x{settings.Wind:X8}"); var start = NumberEditor(panel, "Start time", settings.StartTime.ToString(CultureInfo.InvariantCulture)); var speed = NumberEditor(panel, "Time speed", settings.TimeSpeed.ToString(CultureInfo.InvariantCulture)); var skybox = NumberEditor(panel, "Skybox flags", settings.SkyboxFlags.ToString(CultureInfo.InvariantCulture)); var echo = NumberEditor(panel, "Echo", settings.Echo.ToString(CultureInfo.InvariantCulture)); new AlertDialog.Builder(this)!.SetTitle($"Header {header.Index:D2} room {roomId:D2} settings")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditAlternateRoomSettings(scene.Id, header.Index, roomId, new RomRoomSettings((uint)ParseInt(behavior), (uint)ParseInt(wind), (ushort)ParseInt(start), (byte)ParseInt(speed), (byte)ParseInt(skybox), (byte)ParseInt(echo))); status.Text = "Alternate room settings edit staged in the ROM workspace."; ShowAlternateRoom(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index), roomId); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditAlternateRoomExits(RomScene scene, RomAlternateHeader header, int roomId)
    {
        if (romWorkspace is null || header.Rooms is not { } rooms || roomId < 0 || roomId >= rooms.Count || rooms[roomId].Exits is not { } exits) return; string[] labels = exits.Select((e, i) => $"Exit {i:D2} • 0x{e:X4}").ToArray(); var builder = new AlertDialog.Builder(this)!.SetTitle($"Header {header.Index:D2} room {roomId:D2} exits"); builder.SetItems(labels, (_, args) => { var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var raw = NumberEditor(panel, "Exit word", $"0x{exits[args.Which]:X4}"); new AlertDialog.Builder(this)!.SetTitle($"Edit alternate exit {args.Which:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditAlternateRoomExit(scene.Id, header.Index, roomId, args.Which, (ushort)ParseInt(raw)); status.Text = "Alternate room exit edit staged in the ROM workspace."; ShowAlternateRoom(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index), roomId); } catch (Exception error) { ShowError(error); } }).Show(); }); builder.SetNegativeButton("Back", (_, _) => ShowAlternateRoom(scene, header, roomId)); builder.Show();
    }

    private void ShowAlternateActors(RomScene scene, RomAlternateHeader header, int roomId)
    {
        if (romWorkspace is null || header.Rooms is not { } rooms || roomId < 0 || roomId >= rooms.Count) return; var actors = rooms[roomId].Actors; string[] labels = actors.Select((a, i) => $"Actor {i:D2} • 0x{a.Number:X4} • ({a.X}, {a.Y}, {a.Z})").ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Header {header.Index:D2} room {roomId:D2} actors"); builder.SetItems(labels, (_, args) => EditAlternateActor(scene, header, roomId, args.Which)); builder.SetPositiveButton("Add actor", (_, _) => AddAlternateActor(scene, header, roomId)); builder.SetNeutralButton("Remove last", (_, _) => { if (actors.Count == 0) return; try { romWorkspace.EditAlternateRoomActors(scene.Id, header.Index, roomId, actors.Take(actors.Count - 1).ToArray()); status.Text = "Alternate-header room actor removal staged."; var current = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index); ShowAlternateActors(scene, current, roomId); } catch (Exception error) { ShowError(error); } }); builder.SetNegativeButton("Back", (_, _) => ShowAlternateRooms(scene, header)); builder.Show();
    }

    private void AddAlternateActor(RomScene scene, RomAlternateHeader header, int roomId)
    {
        if (romWorkspace is null || header.Rooms is not { } rooms || roomId < 0 || roomId >= rooms.Count) return;
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var number = NumberEditor(panel, "Actor ID (hex)", "0x0000"); var x = NumberEditor(panel, "X", "0"); var y = NumberEditor(panel, "Y", "0"); var z = NumberEditor(panel, "Z", "0"); var variable = NumberEditor(panel, "Variable", "0");
        new AlertDialog.Builder(this)!.SetTitle("Add alternate room actor")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Add", (_, _) =>
        {
            try { var actor = new RomActor((ushort)ParseInt(number), (short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), 0, 0, 0, (ushort)ParseInt(variable)); romWorkspace.EditAlternateRoomActors(scene.Id, header.Index, roomId, rooms[roomId].Actors.Concat([actor]).ToArray()); status.Text = "Alternate-header room actor insertion staged."; var current = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index); ShowAlternateActors(scene, current, roomId); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void EditAlternateActor(RomScene scene, RomAlternateHeader header, int roomId, int actorIndex)
    {
        if (romWorkspace is null || header.Rooms is not { } rooms || roomId < 0 || roomId >= rooms.Count || actorIndex < 0 || actorIndex >= rooms[roomId].Actors.Count) return; var actor = rooms[roomId].Actors[actorIndex]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var x = NumberEditor(panel, "X", actor.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", actor.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", actor.Z.ToString(CultureInfo.InvariantCulture)); var variable = NumberEditor(panel, "Variable", actor.Variable.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit alternate actor {actorIndex:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditAlternateRoomActor(scene.Id, header.Index, roomId, actorIndex, actor with { X = (short)ParseInt(x), Y = (short)ParseInt(y), Z = (short)ParseInt(z), Variable = (ushort)ParseInt(variable) }); status.Text = "Alternate-header actor edit staged in the ROM workspace."; ShowAlternateActors(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).AlternateHeaders!.First(h => h.Index == header.Index), roomId); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditNativeSceneSettings(RomScene scene)
    {
        if (romWorkspace is null || scene.Settings is not { } settings) return;
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var cameraMovement = NumberEditor(panel, "Camera movement", settings.CameraMovement.ToString(CultureInfo.InvariantCulture));
        var worldMap = NumberEditor(panel, "World map", settings.WorldMap.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit scene {scene.Id:X2} settings")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
        {
            try { romWorkspace.EditSceneSettings(scene.Id, new RomSceneSettings((byte)ParseInt(cameraMovement), (byte)ParseInt(worldMap))); status.Text = "Native scene settings edit staged in the ROM workspace."; ShowRomScene(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void ShowRomTransitions(RomScene scene)
    {
        var transitions = scene.Transitions ?? []; string[] labels = transitions.Select((t, i) => $"Transition {i:D2} • rooms {t.FrontRoom}/{t.BackRoom} • cameras {t.FrontCamera}/{t.BackCamera} • ({t.X}, {t.Y}, {t.Z})").ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} transitions"); builder.SetItems(labels, (_, args) => EditNativeTransition(scene, args.Which)); builder.SetNegativeButton("Back", (_, _) => ShowRomScene(scene)); builder.Show();
    }

    private void EditNativeTransition(RomScene scene, int index)
    {
        if (romWorkspace is null || scene.Transitions is not { } transitions || index < 0 || index >= transitions.Count) return; var transition = transitions[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var fr = NumberEditor(panel, "Front room", transition.FrontRoom.ToString(CultureInfo.InvariantCulture)); var fc = NumberEditor(panel, "Front camera", transition.FrontCamera.ToString(CultureInfo.InvariantCulture)); var br = NumberEditor(panel, "Back room", transition.BackRoom.ToString(CultureInfo.InvariantCulture)); var bc = NumberEditor(panel, "Back camera", transition.BackCamera.ToString(CultureInfo.InvariantCulture)); var number = NumberEditor(panel, "Transition number", transition.Number.ToString(CultureInfo.InvariantCulture)); var x = NumberEditor(panel, "X", transition.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", transition.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", transition.Z.ToString(CultureInfo.InvariantCulture)); var rotation = NumberEditor(panel, "Y rotation", transition.RotationY.ToString(CultureInfo.InvariantCulture)); var variable = NumberEditor(panel, "Variable", transition.Variable.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit transition {index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditTransition(scene.Id, index, new RomTransition((byte)ParseInt(fr), (byte)ParseInt(fc), (byte)ParseInt(br), (byte)ParseInt(bc), (ushort)ParseInt(number), (short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(rotation), (ushort)ParseInt(variable))); status.Text = "Native transition edit staged in the ROM workspace."; ShowRomTransitions(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void ShowRomWaterboxes(RomScene scene)
    {
        var waterboxes = scene.Collision?.Waterboxes ?? [];
        string[] labels = waterboxes.Select((w, i) => $"Waterbox {i:D2} • ({w.X}, {w.Y}, {w.Z}) • size ({w.XSize}, {w.ZSize}) • properties 0x{w.Properties:X8}").ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} waterboxes");
        builder.SetItems(labels, (_, args) => EditNativeWaterbox(scene, args.Which)); builder.SetNegativeButton("Back", (_, _) => ShowRomScene(scene)); builder.Show();
    }

    private void EditNativeWaterbox(RomScene scene, int index)
    {
        if (romWorkspace is null || scene.Collision?.Waterboxes is not { } waterboxes || index < 0 || index >= waterboxes.Count) return;
        var waterbox = waterboxes[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var x = NumberEditor(panel, "X", waterbox.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", waterbox.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", waterbox.Z.ToString(CultureInfo.InvariantCulture));
        var xs = NumberEditor(panel, "X size", waterbox.XSize.ToString(CultureInfo.InvariantCulture)); var zs = NumberEditor(panel, "Z size", waterbox.ZSize.ToString(CultureInfo.InvariantCulture)); var unknown = NumberEditor(panel, "Unknown", waterbox.Unknown.ToString(CultureInfo.InvariantCulture)); var properties = NumberEditor(panel, "Packed properties", $"0x{waterbox.Properties:X8}");
        new AlertDialog.Builder(this)!.SetTitle($"Edit waterbox {index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
        {
            try { romWorkspace!.EditWaterbox(scene.Id, index, new RomWaterbox((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(xs), (short)ParseInt(zs), (ushort)ParseInt(unknown), (uint)ParseInt(properties))); status.Text = "Native waterbox edit staged in the ROM workspace."; ShowRomWaterboxes(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void ShowRomEnvironments(RomScene scene)
    {
        var environments = scene.Environments ?? [];
        string[] labels = environments.Select((e, i) => $"Environment {i:D2} • ambient 0x{e.Ambient:X6} • fog 0x{e.FogColor:X6} • draw {e.DrawDistance}").ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} environments");
        builder.SetItems(labels, (_, args) => EditNativeEnvironment(scene, args.Which)); builder.SetNegativeButton("Back", (_, _) => ShowRomScene(scene)); builder.Show();
    }

    private void EditNativeEnvironment(RomScene scene, int index)
    {
        if (romWorkspace is null || scene.Environments is not { } environments || index < 0 || index >= environments.Count) return;
        var environment = environments[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var ambient = NumberEditor(panel, "Ambient RGB", $"0x{environment.Ambient:X6}"); var diffuse0 = NumberEditor(panel, "Diffuse 0 RGB", $"0x{environment.Diffuse0:X6}"); var direction0 = NumberEditor(panel, "Direction 0 RGB", $"0x{environment.Direction0:X6}"); var diffuse1 = NumberEditor(panel, "Diffuse 1 RGB", $"0x{environment.Diffuse1:X6}"); var direction1 = NumberEditor(panel, "Direction 1 RGB", $"0x{environment.Direction1:X6}"); var fog = NumberEditor(panel, "Fog RGB", $"0x{environment.FogColor:X6}"); var fogDistance = NumberEditor(panel, "Fog distance", environment.FogDistance.ToString(CultureInfo.InvariantCulture)); var fogUnknown = NumberEditor(panel, "Fog unknown", environment.FogUnknown.ToString(CultureInfo.InvariantCulture)); var draw = NumberEditor(panel, "Draw distance", environment.DrawDistance.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit environment {index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
        {
            try { romWorkspace.EditEnvironment(scene.Id, index, new RomEnvironment((uint)ParseInt(ambient), (uint)ParseInt(diffuse0), (uint)ParseInt(direction0), (uint)ParseInt(diffuse1), (uint)ParseInt(direction1), (uint)ParseInt(fog), (ushort)ParseInt(fogDistance), (ushort)ParseInt(fogUnknown), (ushort)ParseInt(draw))); status.Text = "Native environment edit staged in the ROM workspace."; ShowRomEnvironments(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void ShowRomCollisionVertices(RomScene scene)
    {
        var vertices = scene.Collision?.Vertices ?? [];
        var triangles = scene.Collision?.Triangles ?? []; var surfaces = scene.Collision?.SurfaceTypes ?? []; var cameras = scene.Collision?.Cameras ?? [];
        var collision = scene.Collision; string[] labels = new[] { "Add collision vertex", "Remove last collision vertex", "Add collision triangle", "Remove last collision triangle", "Recalculate normals + bounds", $"Bounds • min ({collision?.MinX}, {collision?.MinY}, {collision?.MinZ}) max ({collision?.MaxX}, {collision?.MaxY}, {collision?.MaxZ})" }.Concat(vertices.Select((v, i) => $"Vertex {i:D3} • ({v.X}, {v.Y}, {v.Z})")).Concat(triangles.Select((t, i) => $"Triangle {i:D3} • {t.A},{t.B},{t.C} • surface {t.SurfaceType} • normal {t.NormalX},{t.NormalY},{t.NormalZ}")).Concat(surfaces.Select((s, i) => { var material = RomCollisionSurfaceType.Decode(s); return $"Surface {i:D3} • 0x{s:X16} • material {material.Material} • floor {material.FloorType} • hookshot {(material.CanHookshot ? "yes" : "no")}"; })).Concat(cameras.Select((c, i) => $"Camera {i:D2} • type {c.Type} • {c.DataCount} page/path points • ({c.X}, {c.Y}, {c.Z}) • FOV {c.Fov}")).ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} collision vertices");
        int vertexStart = 6, triangleStart = vertexStart + vertices.Count, surfaceStart = triangleStart + triangles.Count, cameraStart = surfaceStart + surfaces.Count;
        builder.SetItems(labels, (_, args) => { if (args.Which == 0) AddNativeCollisionVertex(scene); else if (args.Which == 1) RemoveNativeCollisionVertex(scene); else if (args.Which == 2) AddNativeCollisionTriangle(scene); else if (args.Which == 3) RemoveNativeCollisionTriangle(scene); else if (args.Which == 4) RecalculateNativeCollision(scene); else if (args.Which == 5) EditNativeCollisionBounds(scene); else if (args.Which < triangleStart) EditNativeCollisionVertex(scene, args.Which - vertexStart); else if (args.Which < surfaceStart) EditNativeCollisionTriangle(scene, args.Which - triangleStart); else if (args.Which < cameraStart) EditNativeCollisionSurface(scene, args.Which - surfaceStart); else EditNativeCamera(scene, args.Which - cameraStart); }); builder.SetNeutralButton("Material DB", (_, _) => ShowNativeCollisionMaterialDatabase(scene)); builder.SetNegativeButton("Back", (_, _) => ShowRomScene(scene)); builder.Show();
    }

    private void ShowNativeCollisionMaterialDatabase(RomScene scene)
    {
        var surfaces = scene.Collision?.SurfaceTypes ?? []; var groups = surfaces.Select((raw, index) => (raw, index, decoded: RomCollisionSurfaceType.Decode(raw))).GroupBy(item => (item.decoded.Material, item.decoded.FloorType, item.decoded.WallType, item.decoded.FloorProperty, item.decoded.FloorEffect, item.decoded.CanHookshot, item.decoded.IsSoft, item.decoded.IsHorseBlocked)).ToArray();
        var labels = groups.Select((group, index) => $"Material {index:D2} • id {group.Key.Material} • floor {group.Key.FloorType} • wall {group.Key.WallType} • effect {group.Key.FloorEffect} • {group.Count()} surfaces • hookshot {(group.Key.CanHookshot ? "yes" : "no")} ").ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} collision material database"); builder.SetItems(labels, (_, args) => EditNativeCollisionSurface(scene, groups[args.Which].First().index)); builder.SetMessage(labels.Length == 0 ? "No collision surface records are decoded." : "Each row groups surfaces by decoded OoT material and gameplay flags. Select a row to edit the native field set."); builder.SetNegativeButton("Back", (_, _) => ShowRomCollisionVertices(scene)); builder.Show();
    }

    private void RecalculateNativeCollision(RomScene scene)
    {
        if (romWorkspace is null) return;
        try { romWorkspace.RecalculateCollision(scene.Id); status.Text = "Collision normals and bounds recalculated."; ShowRomCollisionVertices(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); }
        catch (Exception error) { ShowError(error); }
    }

    private void AddNativeCollisionVertex(RomScene scene)
    {
        if (romWorkspace is null || scene.Collision is not { } collision) return;
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var x = NumberEditor(panel, "X", "0"); var y = NumberEditor(panel, "Y", "0"); var z = NumberEditor(panel, "Z", "0");
        new AlertDialog.Builder(this)!.SetTitle("Add collision vertex")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Add", (_, _) => { try { romWorkspace.ReplaceCollisionTopology(scene.Id, collision.Vertices.Concat([new RomCollisionVertex((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z))]).ToArray(), collision.Triangles, collision.SurfaceTypes.Count == collision.Triangles.Count ? collision.SurfaceTypes : collision.Triangles.Select(_ => 0UL).ToArray()); status.Text = "Native collision vertex insertion staged."; ShowRomCollisionVertices(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void RemoveNativeCollisionVertex(RomScene scene)
    {
        if (romWorkspace is null || scene.Collision is not { } collision) return;
        try { if (collision.Vertices.Count <= 1) throw new InvalidDataException("A collision must retain at least one vertex."); int removed = collision.Vertices.Count - 1; if (collision.Triangles.Any(t => t.A == removed || t.B == removed || t.C == removed)) throw new InvalidDataException("The last collision vertex is still referenced by a triangle."); romWorkspace.ReplaceCollisionTopology(scene.Id, collision.Vertices.Take(removed).ToArray(), collision.Triangles, collision.SurfaceTypes); status.Text = "Native collision vertex removal staged."; ShowRomCollisionVertices(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); }
    }

    private void AddNativeCollisionTriangle(RomScene scene)
    {
        if (romWorkspace is null || scene.Collision is not { } collision) return;
        if (collision.Vertices.Count < 3) { ShowError(new InvalidDataException("Add at least three collision vertices before adding a triangle.")); return; }
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var surface = NumberEditor(panel, "Surface type", "0"); var a = NumberEditor(panel, "A", "0"); var b = NumberEditor(panel, "B", "1"); var c = NumberEditor(panel, "C", "2"); var nx = NumberEditor(panel, "Normal X", "0"); var ny = NumberEditor(panel, "Normal Y", "0"); var nz = NumberEditor(panel, "Normal Z", "0"); var distance = NumberEditor(panel, "Distance", "0");
        new AlertDialog.Builder(this)!.SetTitle("Add collision triangle")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Add", (_, _) => { try { var triangle = new RomCollisionTriangle((ushort)ParseInt(surface), (ushort)ParseInt(a), (ushort)ParseInt(b), (ushort)ParseInt(c), (short)ParseInt(nx), (short)ParseInt(ny), (short)ParseInt(nz), (short)ParseInt(distance)); var surfaces = collision.SurfaceTypes.Count == collision.Triangles.Count ? collision.SurfaceTypes.Concat([0UL]).ToArray() : collision.Triangles.Select(_ => 0UL).Concat([0UL]).ToArray(); romWorkspace.ReplaceCollisionTopology(scene.Id, collision.Vertices, collision.Triangles.Concat([triangle]).ToArray(), surfaces); status.Text = "Native collision triangle insertion staged."; ShowRomCollisionVertices(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void RemoveNativeCollisionTriangle(RomScene scene)
    {
        if (romWorkspace is null || scene.Collision is not { } collision) return;
        try { if (collision.Triangles.Count <= 1) throw new InvalidDataException("A collision must retain at least one triangle."); int count = collision.Triangles.Count - 1; var surfaces = collision.SurfaceTypes.Count == collision.Triangles.Count ? collision.SurfaceTypes.Take(count).ToArray() : collision.Triangles.Take(count).Select(_ => 0UL).ToArray(); romWorkspace.ReplaceCollisionTopology(scene.Id, collision.Vertices, collision.Triangles.Take(count).ToArray(), surfaces); status.Text = "Native collision triangle removal staged."; ShowRomCollisionVertices(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); }
    }

    private void EditNativeCollisionBounds(RomScene scene)
    {
        if (romWorkspace is null || scene.Collision is not { } collision) return; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var minX = NumberEditor(panel, "Min X", collision.MinX.ToString(CultureInfo.InvariantCulture)); var minY = NumberEditor(panel, "Min Y", collision.MinY.ToString(CultureInfo.InvariantCulture)); var minZ = NumberEditor(panel, "Min Z", collision.MinZ.ToString(CultureInfo.InvariantCulture)); var maxX = NumberEditor(panel, "Max X", collision.MaxX.ToString(CultureInfo.InvariantCulture)); var maxY = NumberEditor(panel, "Max Y", collision.MaxY.ToString(CultureInfo.InvariantCulture)); var maxZ = NumberEditor(panel, "Max Z", collision.MaxZ.ToString(CultureInfo.InvariantCulture)); new AlertDialog.Builder(this)!.SetTitle("Edit collision bounds")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditCollisionBounds(scene.Id, collision with { MinX = (short)ParseInt(minX), MinY = (short)ParseInt(minY), MinZ = (short)ParseInt(minZ), MaxX = (short)ParseInt(maxX), MaxY = (short)ParseInt(maxY), MaxZ = (short)ParseInt(maxZ) }); status.Text = "Native collision bounds edit staged in the ROM workspace."; ShowRomCollisionVertices(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditNativeCamera(RomScene scene, int index)
    {
        if (romWorkspace is null || scene.Collision?.Cameras is not { } cameras || index < 0 || index >= cameras.Count) return; var camera = cameras[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var type = NumberEditor(panel, "Type", camera.Type.ToString(CultureInfo.InvariantCulture)); var count = NumberEditor(panel, "Page/path point count", camera.DataCount.ToString(CultureInfo.InvariantCulture)); var x = NumberEditor(panel, "X", camera.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", camera.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", camera.Z.ToString(CultureInfo.InvariantCulture)); var rx = NumberEditor(panel, "X rotation", camera.RotationX.ToString(CultureInfo.InvariantCulture)); var ry = NumberEditor(panel, "Y rotation", camera.RotationY.ToString(CultureInfo.InvariantCulture)); var rz = NumberEditor(panel, "Z rotation", camera.RotationZ.ToString(CultureInfo.InvariantCulture)); var fov = NumberEditor(panel, "FOV", camera.Fov.ToString(CultureInfo.InvariantCulture)); var u1 = NumberEditor(panel, "Unknown 1", camera.Unknown1.ToString(CultureInfo.InvariantCulture)); var u2 = NumberEditor(panel, "Unknown 2", camera.Unknown2.ToString(CultureInfo.InvariantCulture));
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Edit camera {index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditCamera(scene.Id, index, new RomCamera((byte)ParseInt(type), (short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(rx), (short)ParseInt(ry), (short)ParseInt(rz), (short)ParseInt(fov), (ushort)ParseInt(u1), (ushort)ParseInt(u2), (short)ParseInt(count), camera.PathPoints)); status.Text = "Native collision camera page edit staged in the ROM workspace."; ShowRomCollisionVertices(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } }); if (camera.PathPoints is { Count: > 0 }) builder.SetNeutralButton("Edit path", (_, _) => ShowNativeCameraPath(scene, index)); builder.Show();
    }

    private void ShowNativeCameraPath(RomScene scene, int cameraIndex)
    {
        if (romWorkspace is null || scene.Collision?.Cameras is not { } cameras || cameraIndex < 0 || cameraIndex >= cameras.Count || cameras[cameraIndex].PathPoints is not { } points) return;
        var labels = points.Select((point, index) => $"Point {index:D2} • ({point.X}, {point.Y}, {point.Z})").ToArray(); var builder = new AlertDialog.Builder(this)!.SetTitle($"Camera {cameraIndex:D2} path points"); builder.SetItems(labels, (_, args) => EditNativeCameraPathPoint(scene, cameraIndex, args.Which)); builder.SetNegativeButton("Back", (_, _) => EditNativeCamera(scene, cameraIndex)); builder.Show();
    }

    private void EditNativeCameraPathPoint(RomScene scene, int cameraIndex, int pointIndex)
    {
        if (romWorkspace is null || scene.Collision?.Cameras is not { } cameras || cameraIndex < 0 || cameraIndex >= cameras.Count || cameras[cameraIndex].PathPoints is not { } points || pointIndex < 0 || pointIndex >= points.Count) return;
        var point = points[pointIndex]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; var x = NumberEditor(panel, "X", point.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", point.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", point.Z.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit camera {cameraIndex:D2} path point {pointIndex:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { var updated = points.ToArray(); updated[pointIndex] = new RomCameraPathPoint((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z)); var camera = cameras[cameraIndex] with { DataCount = (short)updated.Length, PathPoints = updated }; romWorkspace.EditCamera(scene.Id, cameraIndex, camera); status.Text = "Native camera path point edit staged in the ROM workspace."; ShowNativeCameraPath(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id), cameraIndex); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditNativeCollisionVertex(RomScene scene, int index)
    {
        if (romWorkspace is null || scene.Collision?.Vertices is not { } vertices || index < 0 || index >= vertices.Count) return;
        var vertex = vertices[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var x = NumberEditor(panel, "X", vertex.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", vertex.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", vertex.Z.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit collision vertex {index:D3}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
        {
            try { romWorkspace.EditCollisionVertex(scene.Id, index, new RomCollisionVertex((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z))); status.Text = "Native collision vertex edit staged in the ROM workspace."; ShowRomCollisionVertices(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void EditNativeCollisionTriangle(RomScene scene, int index)
    {
        if (romWorkspace is null || scene.Collision?.Triangles is not { } triangles || index < 0 || index >= triangles.Count) return;
        var triangle = triangles[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var surface = NumberEditor(panel, "Surface index", triangle.SurfaceType.ToString(CultureInfo.InvariantCulture)); var a = NumberEditor(panel, "A", triangle.A.ToString(CultureInfo.InvariantCulture)); var b = NumberEditor(panel, "B", triangle.B.ToString(CultureInfo.InvariantCulture)); var c = NumberEditor(panel, "C", triangle.C.ToString(CultureInfo.InvariantCulture)); var nx = NumberEditor(panel, "Normal X", triangle.NormalX.ToString(CultureInfo.InvariantCulture)); var ny = NumberEditor(panel, "Normal Y", triangle.NormalY.ToString(CultureInfo.InvariantCulture)); var nz = NumberEditor(panel, "Normal Z", triangle.NormalZ.ToString(CultureInfo.InvariantCulture)); var distance = NumberEditor(panel, "Distance", triangle.Distance.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit collision triangle {index:D3}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
        {
            try { romWorkspace.EditCollisionTriangle(scene.Id, index, new RomCollisionTriangle((ushort)ParseInt(surface), (ushort)ParseInt(a), (ushort)ParseInt(b), (ushort)ParseInt(c), (short)ParseInt(nx), (short)ParseInt(ny), (short)ParseInt(nz), (short)ParseInt(distance))); status.Text = "Native collision triangle edit staged in the ROM workspace."; ShowRomCollisionVertices(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void EditNativeCollisionSurface(RomScene scene, int index)
    {
        if (romWorkspace is null || scene.Collision?.SurfaceTypes is not { } surfaces || index < 0 || index >= surfaces.Count) return;
        var decoded = RomCollisionSurfaceType.Decode(surfaces[index]); var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var bgCamera = NumberEditor(panel, "Background camera", decoded.BgCameraIndex.ToString(CultureInfo.InvariantCulture)); var exit = NumberEditor(panel, "Scene exit", decoded.ExitIndex.ToString(CultureInfo.InvariantCulture)); var floor = NumberEditor(panel, "Floor type", decoded.FloorType.ToString(CultureInfo.InvariantCulture)); var unknown18 = NumberEditor(panel, "Unknown 18", decoded.Unknown18.ToString(CultureInfo.InvariantCulture)); var wall = NumberEditor(panel, "Wall type", decoded.WallType.ToString(CultureInfo.InvariantCulture)); var floorProperty = NumberEditor(panel, "Floor property", decoded.FloorProperty.ToString(CultureInfo.InvariantCulture)); var soft = NumberEditor(panel, "Soft (0/1)", decoded.IsSoft ? "1" : "0"); var horse = NumberEditor(panel, "Horse blocked (0/1)", decoded.IsHorseBlocked ? "1" : "0"); var material = NumberEditor(panel, "Material", decoded.Material.ToString(CultureInfo.InvariantCulture)); var floorEffect = NumberEditor(panel, "Floor effect", decoded.FloorEffect.ToString(CultureInfo.InvariantCulture)); var light = NumberEditor(panel, "Light setting", decoded.LightSetting.ToString(CultureInfo.InvariantCulture)); var echo = NumberEditor(panel, "Echo", decoded.Echo.ToString(CultureInfo.InvariantCulture)); var hookshot = NumberEditor(panel, "Hookshot allowed (0/1)", decoded.CanHookshot ? "1" : "0"); var conveyorSpeed = NumberEditor(panel, "Conveyor speed", decoded.ConveyorSpeed.ToString(CultureInfo.InvariantCulture)); var conveyorDirection = NumberEditor(panel, "Conveyor direction", decoded.ConveyorDirection.ToString(CultureInfo.InvariantCulture)); var unknown27 = NumberEditor(panel, "Unknown 27 (0/1)", decoded.Unknown27 ? "1" : "0");
        new AlertDialog.Builder(this)!.SetTitle($"Edit collision surface {index:D3}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
        {
            try { var value = new RomCollisionSurfaceType(decoded.Raw, (byte)ParseInt(bgCamera), (byte)ParseInt(exit), (byte)ParseInt(floor), (byte)ParseInt(unknown18), (byte)ParseInt(wall), (byte)ParseInt(floorProperty), ParseInt(soft) != 0, ParseInt(horse) != 0, (byte)ParseInt(material), (byte)ParseInt(floorEffect), (byte)ParseInt(light), (byte)ParseInt(echo), ParseInt(hookshot) != 0, (byte)ParseInt(conveyorSpeed), (byte)ParseInt(conveyorDirection), ParseInt(unknown27) != 0).Encode(); romWorkspace.EditCollisionSurface(scene.Id, index, value); status.Text = "Native collision material flags staged in the ROM workspace."; ShowRomCollisionVertices(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void ShowRomSpawns(RomScene scene)
    {
        var spawns = scene.SpawnPoints ?? []; string[] labels = spawns.Select((s, i) => $"Spawn {i:D2} • number {s.Number} • ({s.X}, {s.Y}, {s.Z}) • rot {s.RotationY}").ToArray(); var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} spawns"); builder.SetItems(labels, (_, args) => EditNativeSpawn(scene, args.Which)); builder.SetNegativeButton("Back", (_, _) => ShowRomScene(scene)); builder.Show();
    }

    private void EditNativeSpawn(RomScene scene, int index)
    {
        if (romWorkspace is null || scene.SpawnPoints is not { } spawns || index < 0 || index >= spawns.Count) return; var spawn = spawns[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var number = NumberEditor(panel, "Spawn number", spawn.Number.ToString(CultureInfo.InvariantCulture)); var x = NumberEditor(panel, "X", spawn.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", spawn.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", spawn.Z.ToString(CultureInfo.InvariantCulture)); var rx = NumberEditor(panel, "X rotation", spawn.RotationX.ToString(CultureInfo.InvariantCulture)); var ry = NumberEditor(panel, "Y rotation", spawn.RotationY.ToString(CultureInfo.InvariantCulture)); var rz = NumberEditor(panel, "Z rotation", spawn.RotationZ.ToString(CultureInfo.InvariantCulture)); var variable = NumberEditor(panel, "Variable", spawn.Variable.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit spawn {index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditSpawn(scene.Id, index, new RomSpawnPoint((ushort)ParseInt(number), (short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(rx), (short)ParseInt(ry), (short)ParseInt(rz), (ushort)ParseInt(variable))); status.Text = "Native spawn edit staged in the ROM workspace."; ShowRomSpawns(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void ShowRomExits(RomScene scene)
    {
        var exits = scene.Exits ?? [];
        string[] labels = exits.Select(e => $"Exit {e.Index:D2} • room {e.RoomId:D2} • spawn {e.SpawnId:D3} • raw 0x{e.Raw:X4}").ToArray();
        var builder = new AlertDialog.Builder(this)!; builder.SetTitle($"Scene {scene.Id:X2} exits");
        builder.SetItems(labels, (_, args) => EditNativeExit(scene, exits[args.Which]));
        builder.SetNegativeButton("Back", (_, _) => ShowRomScene(scene)); builder.Show();
    }

    private void EditNativeExit(RomScene scene, RomExit exit)
    {
        var workspace = romWorkspace; if (workspace is null) return;
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var raw = NumberEditor(panel, "Raw exit value (hex or decimal)", $"0x{exit.Raw:X4}");
        new AlertDialog.Builder(this)!.SetTitle($"Edit exit {exit.Index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
        {
            try { workspace.EditExit(scene.Id, exit.Index, (ushort)ParseInt(raw)); status.Text = "Native exit edit staged in the ROM workspace."; ShowRomExits(workspace.Document.Scenes.First(s => s.Id == scene.Id)); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void ShowRomPaths(RomScene scene)
    {
        var paths = scene.Paths ?? [];
        string[] labels = paths.Select(p => $"Path {p.Id:D2} • {p.Points.Count} waypoints").ToArray();
        var builder = new AlertDialog.Builder(this)!; builder.SetTitle($"Scene {scene.Id:X2} paths");
        builder.SetItems(labels, (_, args) => EditNativePath(scene, paths[args.Which]));
        builder.SetPositiveButton("Add path", (_, _) => { try { romWorkspace!.ReplacePaths(scene.Id, paths.Concat([new RomPath(paths.Count, [new RomPathPoint(0, 0, 0)])]).ToArray()); status.Text = "Native path insertion staged in the ROM workspace."; ShowRomPaths(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } });
        builder.SetNeutralButton("Remove last", (_, _) => { try { if (paths.Count <= 1) throw new InvalidDataException("A scene must retain at least one path."); romWorkspace!.ReplacePaths(scene.Id, paths.Take(paths.Count - 1).ToArray()); status.Text = "Native path deletion staged in the ROM workspace."; ShowRomPaths(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } });
        builder.SetNegativeButton("Back", (_, _) => ShowRomScene(scene)); builder.Show();
    }

    private void EditNativePath(RomScene scene, RomPath path)
    {
        if (romWorkspace is null) return;
        var labels = path.Points.Select((p, i) => $"{i:D2}: ({p.X}, {p.Y}, {p.Z})").ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Path {path.Id:D2} waypoints")!.SetItems(labels, (_, args) =>
        {
            var point = path.Points[args.Which]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
            var x = NumberEditor(panel, "X", point.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", point.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", point.Z.ToString(CultureInfo.InvariantCulture));
            new AlertDialog.Builder(this)!.SetTitle($"Edit path {path.Id:D2} waypoint {args.Which:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
            {
                try { romWorkspace!.EditPathPoint(scene.Id, path.Id, args.Which, new RomPathPoint((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z))); status.Text = "Native path waypoint edit staged in the ROM workspace."; ShowRomPaths(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); }
                catch (Exception error) { ShowError(error); }
            }).Show();
        });
        builder.SetPositiveButton("Add waypoint", (_, _) => { try { var updated = path.Points.Concat([new RomPathPoint(0, 0, 0)]).ToArray(); romWorkspace.ReplacePaths(scene.Id, (scene.Paths ?? []).Select(item => item.Id == path.Id ? item with { Points = updated } : item).ToArray()); status.Text = "Native path waypoint insertion staged in the ROM workspace."; ShowRomPaths(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } });
        builder.SetNeutralButton("Remove last", (_, _) => { try { if (path.Points.Count <= 1) throw new InvalidDataException("A path must retain at least one waypoint."); var updated = path.Points.Take(path.Points.Count - 1).ToArray(); romWorkspace.ReplacePaths(scene.Id, (scene.Paths ?? []).Select(item => item.Id == path.Id ? item with { Points = updated } : item).ToArray()); status.Text = "Native path waypoint deletion staged in the ROM workspace."; ShowRomPaths(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); } catch (Exception error) { ShowError(error); } });
        builder.SetNegativeButton("Back", (_, _) => ShowRomScene(scene)).Show();
    }

    private void ShowRomRoom(RomScene scene, RomRoom room)
    {
        OpenRoomInViewport(scene, room);
        string actors = room.Actors.Count == 0 ? "No actors" : string.Join("\n", room.Actors.Select((a, i) => ActorLabel(a, i)));
        var builder = new AlertDialog.Builder(this)!;
        builder.SetTitle($"Scene {scene.Id:X2} / Room {room.Id:D2}");
        builder.SetMessage($"ROM range: 0x{room.Start:X8}–0x{room.End:X8}\nGeometry command: {room.HasMesh}\nDecoded mesh: {(room.Geometry is null ? "none" : $"{room.Geometry.Vertices.Count} vertices, {room.Geometry.Triangles.Count} triangles, {room.Geometry.DisplayLists} display lists")}\nCollision command: {room.HasCollision}\nObjects: {room.ObjectCount}\n\nActors\n{actors}");
        builder.SetPositiveButton("Close", (_, _) => { });
        var actorLabels = room.Actors.Select((a, i) => ActorLabel(a, i)).Append($"Geometry vertices ({room.Geometry?.Vertices.Count ?? 0})").Append($"Objects ({room.Objects?.Count ?? room.ObjectCount})").Append($"Room exits ({room.Exits?.Count ?? 0})").Append("Room settings").Append("Split room geometry into tiles").Append("Import custom geometry").Append("Clone room").Append("Delete room").Append("Multi-actor group transform").Append(actorSelectionMode ? "Stop actor selection" : "Select actors on viewport").Append("Clear actor selection").Append(collisionOverlayEnabled ? "Hide collision vertex overlay" : "Show collision vertex overlay").Append(collisionTriangleOverlayEnabled ? "Hide collision triangle overlay" : "Show collision triangle overlay").Append(geometryOverlayEnabled ? "Hide geometry picking overlay" : "Show geometry picking overlay").Append(nativeGeometryVisible ? "Hide room geometry render" : "Show room geometry render").Append(nativeCollisionVisible ? "Hide collision render" : "Show collision render").ToArray();
        if (room.Actors.Count > 0 || room.Geometry is not null || room.ObjectCount > 0 || room.Exits?.Count > 0 || scene.Collision is not null) builder.SetItems(actorLabels, (_, args) => { if (args.Which == room.Actors.Count) ShowRomGeometryVertices(scene, room); else if (args.Which == room.Actors.Count + 1) ShowRomObjects(scene, room); else if (args.Which == room.Actors.Count + 2) ShowRomRoomExits(scene, room); else if (args.Which == room.Actors.Count + 3) EditNativeRoomSettings(scene, room); else if (args.Which == room.Actors.Count + 4) AddRomRoomTile(scene, room); else if (args.Which == room.Actors.Count + 5) ImportNativeGeometry(scene, room); else if (args.Which == room.Actors.Count + 6) CloneNativeRoom(scene, room); else if (args.Which == room.Actors.Count + 7) DeleteNativeRoom(scene, room); else if (args.Which == room.Actors.Count + 8) ShowActorGroupTransform(scene, room); else if (args.Which == room.Actors.Count + 9) ToggleActorSelection(scene, room); else if (args.Which == room.Actors.Count + 10) ClearActorSelection(scene, room); else if (args.Which == room.Actors.Count + 11) ToggleCollisionOverlay(scene, room); else if (args.Which == room.Actors.Count + 12) ToggleCollisionTriangleOverlay(scene, room); else if (args.Which == room.Actors.Count + 13) ToggleGeometryOverlay(scene, room); else if (args.Which == room.Actors.Count + 14) ToggleNativeGeometry(scene, room); else if (args.Which == room.Actors.Count + 15) ToggleNativeCollision(scene, room); else EditNativeActor(scene, room, args.Which); });
        builder.SetPositiveButton("Add actor", (_, _) => AddNativeActor(scene, room));
        builder.SetNeutralButton("Remove last", (_, _) => { if (room.Actors.Count == 0 || romWorkspace is null) return; try { romWorkspace.EditRoomActors(scene.Id, room.Id, room.Actors.Take(room.Actors.Count - 1).ToArray()); status.Text = "Native room actor removal staged."; var current = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); ShowRomRoom(current, current.Rooms[room.Id]); } catch (Exception error) { ShowError(error); } });
        builder.Show();
    }

    private void CloneNativeRoom(RomScene scene, RomRoom room)
    {
        if (romWorkspace is null) return;
        new AlertDialog.Builder(this)!.SetTitle($"Clone room {room.Id:D2}")!
            .SetItems(["Insert before", "Insert after", "Append to scene"], (_, args) =>
            {
                try
                {
                    int index = args.Which == 0 ? room.Id : args.Which == 1 ? room.Id + 1 : scene.Rooms.Count;
                    var clone = romWorkspace.InsertRoomClone(scene.Id, room.Id, index);
                    status.Text = $"Room {room.Id:D2} cloned at room {clone.Id:D2}; native room allocation and routing updates are staged for export.";
                    ShowRomScene(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id));
                }
                catch (Exception error) { ShowError(error); }
            })!
            .SetNegativeButton("Back", (_, _) => ShowRomRoom(scene, room))!.Show();
    }

    private void ImportNativeGeometry(RomScene scene, RomRoom room)
    {
        pendingGeometryScene = scene.Id; pendingGeometryRoom = room.Id; Pick(ImportGeometry);
    }

    private void DeleteNativeRoom(RomScene scene, RomRoom room)
    {
        if (romWorkspace is null) return;
        try { romWorkspace.DeleteRoom(scene.Id, room.Id); status.Text = $"Room {room.Id:D2} deleted; native room table rewrite is staged for export."; ShowRomScene(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id)); }
        catch (Exception error) { ShowError(error); }
    }

    private void AddNativeActor(RomScene scene, RomRoom room)
    {
        if (romWorkspace is null) return;
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var number = NumberEditor(panel, "Actor ID (hex)", "0x0000"); var x = NumberEditor(panel, "X", "0"); var y = NumberEditor(panel, "Y", "0"); var z = NumberEditor(panel, "Z", "0"); var variable = NumberEditor(panel, "Variable (hex)", "0x0000");
        new AlertDialog.Builder(this)!.SetTitle("Add native room actor")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Add", (_, _) =>
        {
            try { var actor = new RomActor((ushort)ParseInt(number), (short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), 0, 0, 0, (ushort)ParseInt(variable)); romWorkspace.EditRoomActors(scene.Id, room.Id, room.Actors.Concat([actor]).ToArray()); status.Text = "Native room actor insertion staged."; var current = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); ShowRomRoom(current, current.Rooms[room.Id]); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void ShowActorGroupTransform(RomScene scene, RomRoom room)
    {
        if (romWorkspace is null || room.Actors.Count == 0) { ShowError(new InvalidDataException("This room has no actors to transform.")); return; }
        var selectedIndexes = selectedActorKeys.Where(key => key.SceneId == scene.Id && key.RoomId == room.Id).Select(key => key.ActorIndex).Where(index => index >= 0 && index < room.Actors.Count).Order().ToArray();
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var selection = NumberEditor(panel, "Actor indexes (comma separated or all)", selectedIndexes.Length > 0 ? string.Join(",", selectedIndexes) : "all"); var dx = NumberEditor(panel, "Move X", "0"); var dy = NumberEditor(panel, "Move Y", "0"); var dz = NumberEditor(panel, "Move Z", "0"); var rx = NumberEditor(panel, "Rotate X (degrees)", "0"); var ry = NumberEditor(panel, "Rotate Y (degrees)", "0"); var rz = NumberEditor(panel, "Rotate Z (degrees)", "0"); var scale = NumberEditor(panel, "Scale (%)", "100");
        new AlertDialog.Builder(this)!.SetTitle($"Group transform • {room.Actors.Count} actors")!.SetMessage("Use actor indexes such as 0,2,3 or enter all. Move values are native world units; rotation is around the selected group center; scale is a percentage around that center.")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
        {
            try
            {
                string rawSelection = (selection.Text ?? "all").Trim(); var indexes = rawSelection.Equals("all", StringComparison.OrdinalIgnoreCase) ? Enumerable.Range(0, room.Actors.Count).ToArray() : rawSelection.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(ParseIndex).Distinct().ToArray();
                if (indexes.Length == 0) throw new InvalidDataException("Actor selection cannot be empty."); int moveX = ParseInt(dx), moveY = ParseInt(dy), moveZ = ParseInt(dz); int rotateX = RomActorTransforms.BinangFromDegrees(ParseFloat(rx)), rotateY = RomActorTransforms.BinangFromDegrees(ParseFloat(ry)), rotateZ = RomActorTransforms.BinangFromDegrees(ParseFloat(rz)); float scaleValue = ParseFloat(scale) / 100f; var updated = RomActorTransforms.Apply(room.Actors, indexes, moveX, moveY, moveZ, rotateX, rotateY, rotateZ, scaleValue); romWorkspace.EditRoomActors(scene.Id, room.Id, updated); status.Text = $"Group transform staged for {indexes.Length} actor(s)."; var current = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); ShowRomRoom(current, current.Rooms[room.Id]);
            }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void ToggleActorSelection(RomScene scene, RomRoom room)
    {
        actorSelectionMode = !actorSelectionMode;
        if (actorSelectionMode) actorOverlayEnabled = true;
        selectedActorKeys.RemoveWhere(key => key.SceneId != scene.Id || key.RoomId != room.Id || key.ActorIndex < 0 || key.ActorIndex >= room.Actors.Count);
        status.Text = actorSelectionMode ? "Actor selection mode enabled. Tap viewport markers to select or deselect them." : $"Actor selection mode closed with {selectedActorKeys.Count} selected actor(s).";
        ShowActorOverlay(scene, room);
    }

    private void ToggleActorSelection(RomScene scene, RomRoom room, int actorIndex)
    {
        var key = (scene.Id, room.Id, actorIndex);
        if (!selectedActorKeys.Add(key)) selectedActorKeys.Remove(key);
        status.Text = $"Selected {selectedActorKeys.Count} actor(s).";
        ShowActorOverlay(scene, room);
    }

    private void ClearActorSelection(RomScene scene, RomRoom room)
    {
        selectedActorKeys.RemoveWhere(key => key.SceneId == scene.Id && key.RoomId == room.Id);
        status.Text = "Actor selection cleared.";
        ShowActorOverlay(scene, room);
    }

    private void ToggleCollisionOverlay(RomScene scene, RomRoom room)
    {
        collisionOverlayEnabled = !collisionOverlayEnabled;
        status.Text = collisionOverlayEnabled ? "Collision vertex overlay enabled. Tap a C marker to edit its native vertex." : "Collision vertex overlay hidden.";
        ShowActorOverlay(scene, room);
    }

    private void ToggleCollisionTriangleOverlay(RomScene scene, RomRoom room)
    {
        collisionTriangleOverlayEnabled = !collisionTriangleOverlayEnabled;
        status.Text = collisionTriangleOverlayEnabled ? "Collision triangle overlay enabled. Tap a T marker to edit its native triangle." : "Collision triangle overlay hidden.";
        ShowActorOverlay(scene, room);
    }

    private void ToggleGeometryOverlay(RomScene scene, RomRoom room)
    {
        geometryOverlayEnabled = !geometryOverlayEnabled;
        status.Text = geometryOverlayEnabled ? "Geometry picking overlay enabled. Tap V or G markers to edit native geometry." : "Geometry picking overlay hidden.";
        ShowActorOverlay(scene, room);
    }

    private void ToggleNativeGeometry(RomScene scene, RomRoom room)
    {
        nativeGeometryVisible = !nativeGeometryVisible;
        status.Text = nativeGeometryVisible ? "Native room geometry render enabled." : "Native room geometry render hidden.";
        viewport.ShowNativeRoom(room.Geometry, scene.Collision, nativeGeometryVisible, nativeCollisionVisible, GetNativePreviewTexture(room));
        ShowActorOverlay(scene, room);
    }

    private void ToggleNativeCollision(RomScene scene, RomRoom room)
    {
        nativeCollisionVisible = !nativeCollisionVisible;
        status.Text = nativeCollisionVisible ? "Native collision render enabled." : "Native collision render hidden.";
        viewport.ShowNativeRoom(room.Geometry, scene.Collision, nativeGeometryVisible, nativeCollisionVisible, GetNativePreviewTexture(room));
        ShowActorOverlay(scene, room);
    }

    private static int ParseIndex(string value) => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

    private void ShowActorOverlay(RomScene scene, RomRoom room)
    {
        if (actorOverlay is null) return;
        actorOverlay.RemoveAllViews();
        if (!actorOverlayEnabled && !collisionOverlayEnabled && !collisionTriangleOverlayEnabled && !geometryOverlayEnabled) return;
        if (room.Actors.Count == 0 && !collisionOverlayEnabled && !collisionTriangleOverlayEnabled && !geometryOverlayEnabled) return;
        actorOverlay.Post(() =>
        {
            int width = Math.Max(actorOverlay.Width, 1), height = Math.Max(actorOverlay.Height, 1);
            int minX = room.Actors.Count > 0 ? room.Actors.Min(a => (int)a.X) : scene.Collision?.MinX ?? -1, maxX = room.Actors.Count > 0 ? room.Actors.Max(a => (int)a.X) : scene.Collision?.MaxX ?? 1, minZ = room.Actors.Count > 0 ? room.Actors.Min(a => (int)a.Z) : scene.Collision?.MinZ ?? -1, maxZ = room.Actors.Count > 0 ? room.Actors.Max(a => (int)a.Z) : scene.Collision?.MaxZ ?? 1; var camera = scene.Collision?.Cameras?.FirstOrDefault(); float aspect = width / (float)height;
            foreach (var item in room.Actors.Select((actor, index) => (actor, index)))
            {
                bool isSelected = selectedActorKeys.Contains((scene.Id, room.Id, item.index)); var projected = camera is null ? null : RomCameraProjection.Project(camera, item.actor, aspect); var marker = new Button(this) { Text = projected is { } view ? $"{(isSelected ? "◎" : "")}A{item.index:D2}\n{view.ScreenX:0.##},{view.ScreenY:0.##}" : $"{(isSelected ? "◎" : "")}A{item.index:D2}", TextSize = 10 };
                marker.SetAllCaps(false); marker.SetTextColor(Color.White); marker.SetBackgroundColor(isSelected ? Color.Rgb(208, 126, 45) : Color.Rgb(40, 120, 190));
                int left = projected is { } screen ? (int)Math.Clamp((screen.ScreenX + 1f) * 0.5f * Math.Max(width - Dp(58), 1), 0, Math.Max(width - Dp(58), 0)) : (int)((item.actor.X - minX) / (float)Math.Max(maxX - minX, 1) * Math.Max(width - Dp(58), 1));
                int top = projected is { } screenView ? (int)Math.Clamp((1f - screenView.ScreenY) * 0.5f * Math.Max(height - Dp(42), 1), 0, Math.Max(height - Dp(42), 0)) : (int)((item.actor.Z - minZ) / (float)Math.Max(maxZ - minZ, 1) * Math.Max(height - Dp(42), 1));
                var lp = new FrameLayout.LayoutParams(Dp(58), Dp(42)) { LeftMargin = left, TopMargin = top };
                marker.LayoutParameters = lp;
                float downX = 0, downY = 0; bool moved = false;
                marker.Touch += (_, e) =>
                {
                    if (e.Event is null) return;
                    if (e.Event.ActionMasked == MotionEventActions.Down) { downX = e.Event.RawX; downY = e.Event.RawY; moved = false; }
                    else if (e.Event.ActionMasked == MotionEventActions.Move)
                    {
                        float dx = e.Event.RawX - downX, dy = e.Event.RawY - downY; if (Math.Abs(dx) + Math.Abs(dy) > 4) moved = true;
                        lp.LeftMargin = Math.Clamp(lp.LeftMargin + (int)dx, 0, Math.Max(width - Dp(58), 0)); lp.TopMargin = Math.Clamp(lp.TopMargin + (int)dy, 0, Math.Max(height - Dp(42), 0)); marker.LayoutParameters = lp; downX = e.Event.RawX; downY = e.Event.RawY;
                    }
                    else if (e.Event.ActionMasked == MotionEventActions.Up)
                    {
                        if (moved && romWorkspace is not null)
                        {
                            var movedWorld = camera is { } activeCamera && projected is { } activeProjection ? RomCameraProjection.UnprojectAtDepth(activeCamera, lp.LeftMargin / (float)Math.Max(width - Dp(58), 1) * 2f - 1f, 1f - lp.TopMargin / (float)Math.Max(height - Dp(42), 1) * 2f, activeProjection.Depth, aspect) : new System.Numerics.Vector3(minX + lp.LeftMargin / (float)Math.Max(width - Dp(58), 1) * Math.Max(maxX - minX, 1), item.actor.Y, minZ + lp.TopMargin / (float)Math.Max(height - Dp(42), 1) * Math.Max(maxZ - minZ, 1));
                            short x = (short)Math.Clamp(MathF.Round(movedWorld.X), short.MinValue, short.MaxValue); short z = (short)Math.Clamp(MathF.Round(movedWorld.Z), short.MinValue, short.MaxValue);
                            romWorkspace.EditRoomActor(scene.Id, room.Id, item.index, item.actor with { X = x, Z = z }); status.Text = $"Actor {item.index:D2} moved to ({x}, {item.actor.Y}, {z}) and staged."; ShowActorOverlay(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).Rooms[room.Id]);
                        }
                        else if (actorSelectionMode) ToggleActorSelection(scene, room, item.index);
                        else EditNativeActor(scene, room, item.index);
                    }
                    e.Handled = true;
                };
                actorOverlay.AddView(marker);
                var yUp = new Button(this) { Text = "Y+", TextSize = 9 }; yUp.SetAllCaps(false); yUp.SetPadding(0, 0, 0, 0);
                yUp.SetOnClickListener(new ClickListener(() => { if (romWorkspace is null) return; var updated = item.actor with { Y = (short)Math.Clamp(item.actor.Y + 10, short.MinValue, short.MaxValue) }; romWorkspace.EditRoomActor(scene.Id, room.Id, item.index, updated); status.Text = $"Actor {item.index:D2} Y increased to {updated.Y}."; ShowActorOverlay(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).Rooms[room.Id]); }));
                yUp.LayoutParameters = new FrameLayout.LayoutParams(Dp(34), Dp(21)) { LeftMargin = left + Dp(58), TopMargin = top };
                actorOverlay.AddView(yUp);
                var yDown = new Button(this) { Text = "Y−", TextSize = 9 }; yDown.SetAllCaps(false); yDown.SetPadding(0, 0, 0, 0);
                yDown.SetOnClickListener(new ClickListener(() => { if (romWorkspace is null) return; var updated = item.actor with { Y = (short)Math.Clamp(item.actor.Y - 10, short.MinValue, short.MaxValue) }; romWorkspace.EditRoomActor(scene.Id, room.Id, item.index, updated); status.Text = $"Actor {item.index:D2} Y decreased to {updated.Y}."; ShowActorOverlay(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).Rooms[room.Id]); }));
                yDown.LayoutParameters = new FrameLayout.LayoutParams(Dp(34), Dp(21)) { LeftMargin = left + Dp(58), TopMargin = top + Dp(21) };
                actorOverlay.AddView(yDown);
                AddActorNudgeButton(scene, room, item.index, "X−", -10, 0, 0, left - Dp(34), top + Dp(10), width, height);
                AddActorNudgeButton(scene, room, item.index, "X+", 10, 0, 0, left + Dp(92), top + Dp(10), width, height);
                AddActorNudgeButton(scene, room, item.index, "Z−", 0, 0, -10, left + Dp(12), top - Dp(21), width, height);
                AddActorNudgeButton(scene, room, item.index, "Z+", 0, 0, 10, left + Dp(12), top + Dp(42), width, height);
                AddActorRotationButton(scene, room, item.index, "RX−", -0x4000, 0, 0, left, top - Dp(42), width, height);
                AddActorRotationButton(scene, room, item.index, "RX+", 0x4000, 0, 0, left + Dp(34), top - Dp(42), width, height);
                AddActorRotationButton(scene, room, item.index, "RY−", 0, -0x4000, 0, left - Dp(34), top - Dp(42), width, height);
                AddActorRotationButton(scene, room, item.index, "RY+", 0, 0x4000, 0, left + Dp(92), top - Dp(42), width, height);
                AddActorRotationButton(scene, room, item.index, "RZ−", 0, 0, -0x4000, left, top + Dp(63), width, height);
                AddActorRotationButton(scene, room, item.index, "RZ+", 0, 0, 0x4000, left + Dp(34), top + Dp(63), width, height);
            }
            if (collisionOverlayEnabled && scene.Collision?.Vertices is { } collisionVertices)
            {
                var collision = scene.Collision; int cMinX = collision.MinX, cMaxX = collision.MaxX, cMinZ = collision.MinZ, cMaxZ = collision.MaxZ;
                foreach (var item in collisionVertices.Select((vertex, index) => (vertex, index)))
                {
                    var projected = camera is null ? null : RomCameraProjection.Project(camera, item.vertex, aspect); int left = projected is { } screen ? (int)Math.Clamp((screen.ScreenX + 1f) * 0.5f * Math.Max(width - Dp(34), 1), 0, Math.Max(width - Dp(34), 0)) : (int)((item.vertex.X - cMinX) / (float)Math.Max(cMaxX - cMinX, 1) * Math.Max(width - Dp(34), 1)); int top = projected is { } screenView ? (int)Math.Clamp((1f - screenView.ScreenY) * 0.5f * Math.Max(height - Dp(24), 1), 0, Math.Max(height - Dp(24), 0)) : (int)((item.vertex.Z - cMinZ) / (float)Math.Max(cMaxZ - cMinZ, 1) * Math.Max(height - Dp(24), 1));
                    var marker = new Button(this) { Text = $"C{item.index:D2}", TextSize = 8 }; marker.SetAllCaps(false); marker.SetPadding(0, 0, 0, 0); marker.SetTextColor(Color.White); marker.SetBackgroundColor(Color.Rgb(180, 58, 66)); marker.SetOnClickListener(new ClickListener(() => EditNativeCollisionVertex(scene, item.index))); marker.LayoutParameters = new FrameLayout.LayoutParams(Dp(34), Dp(24)) { LeftMargin = left, TopMargin = top }; actorOverlay.AddView(marker);
                }
            }
            if (collisionTriangleOverlayEnabled && scene.Collision is { } triangleCollision && triangleCollision.Triangles is { } collisionTriangles && triangleCollision.Vertices is { } triangleVertices)
            {
                foreach (var item in collisionTriangles.Select((triangle, index) => (triangle, index)))
                {
                    if (item.triangle.A >= triangleVertices.Count || item.triangle.B >= triangleVertices.Count || item.triangle.C >= triangleVertices.Count) continue;
                    var a = triangleVertices[item.triangle.A]; var b = triangleVertices[item.triangle.B]; var c = triangleVertices[item.triangle.C]; var center = new System.Numerics.Vector3((a.X + b.X + c.X) / 3f, (a.Y + b.Y + c.Y) / 3f, (a.Z + b.Z + c.Z) / 3f); var projected = camera is null ? null : RomCameraProjection.Project(camera, center, aspect); int left = projected is { } screen ? (int)Math.Clamp((screen.ScreenX + 1f) * 0.5f * Math.Max(width - Dp(34), 1), 0, Math.Max(width - Dp(34), 0)) : (int)((center.X - triangleCollision.MinX) / (float)Math.Max(triangleCollision.MaxX - triangleCollision.MinX, 1) * Math.Max(width - Dp(34), 1)); int top = projected is { } screenView ? (int)Math.Clamp((1f - screenView.ScreenY) * 0.5f * Math.Max(height - Dp(24), 1), 0, Math.Max(height - Dp(24), 0)) : (int)((center.Z - triangleCollision.MinZ) / (float)Math.Max(triangleCollision.MaxZ - triangleCollision.MinZ, 1) * Math.Max(height - Dp(24), 1));
                    var marker = new Button(this) { Text = $"T{item.index:D2}", TextSize = 8 }; marker.SetAllCaps(false); marker.SetPadding(0, 0, 0, 0); marker.SetTextColor(Color.White); marker.SetBackgroundColor(Color.Rgb(116, 74, 184)); marker.SetOnClickListener(new ClickListener(() => EditNativeCollisionTriangle(scene, item.index))); marker.LayoutParameters = new FrameLayout.LayoutParams(Dp(34), Dp(24)) { LeftMargin = left, TopMargin = top }; actorOverlay.AddView(marker);
                }
            }
            if (geometryOverlayEnabled && room.Geometry is { } roomGeometry)
            {
                var geometryPoints = roomGeometry.Vertices; int gMinX = geometryPoints.Count == 0 ? -1 : geometryPoints.Min(v => (int)v.X), gMaxX = geometryPoints.Count == 0 ? 1 : geometryPoints.Max(v => (int)v.X), gMinZ = geometryPoints.Count == 0 ? -1 : geometryPoints.Min(v => (int)v.Z), gMaxZ = geometryPoints.Count == 0 ? 1 : geometryPoints.Max(v => (int)v.Z);
                foreach (var item in geometryPoints.Select((vertex, index) => (vertex, index)))
                {
                    var projected = camera is null ? null : RomCameraProjection.Project(camera, new System.Numerics.Vector3(item.vertex.X, item.vertex.Y, item.vertex.Z), aspect); int left = projected is { } screen ? (int)Math.Clamp((screen.ScreenX + 1f) * 0.5f * Math.Max(width - Dp(34), 1), 0, Math.Max(width - Dp(34), 0)) : (int)((item.vertex.X - gMinX) / (float)Math.Max(gMaxX - gMinX, 1) * Math.Max(width - Dp(34), 1)); int top = projected is { } screenView ? (int)Math.Clamp((1f - screenView.ScreenY) * 0.5f * Math.Max(height - Dp(24), 1), 0, Math.Max(height - Dp(24), 0)) : (int)((item.vertex.Z - gMinZ) / (float)Math.Max(gMaxZ - gMinZ, 1) * Math.Max(height - Dp(24), 1));
                    var marker = new Button(this) { Text = $"V{item.index:D2}", TextSize = 8 }; marker.SetAllCaps(false); marker.SetPadding(0, 0, 0, 0); marker.SetTextColor(Color.White); marker.SetBackgroundColor(Color.Rgb(38, 150, 130)); marker.SetOnClickListener(new ClickListener(() => EditNativeGeometryVertex(scene, room, item.index))); marker.LayoutParameters = new FrameLayout.LayoutParams(Dp(34), Dp(24)) { LeftMargin = left, TopMargin = top }; actorOverlay.AddView(marker);
                }
                foreach (var item in roomGeometry.Triangles.Select((triangle, index) => (triangle, index)))
                {
                    if (item.triangle.A < 0 || item.triangle.B < 0 || item.triangle.C < 0 || item.triangle.A >= geometryPoints.Count || item.triangle.B >= geometryPoints.Count || item.triangle.C >= geometryPoints.Count) continue;
                    var a = geometryPoints[item.triangle.A]; var b = geometryPoints[item.triangle.B]; var c = geometryPoints[item.triangle.C]; var center = new System.Numerics.Vector3((a.X + b.X + c.X) / 3f, (a.Y + b.Y + c.Y) / 3f, (a.Z + b.Z + c.Z) / 3f); var projected = camera is null ? null : RomCameraProjection.Project(camera, center, aspect); int left = projected is { } screen ? (int)Math.Clamp((screen.ScreenX + 1f) * 0.5f * Math.Max(width - Dp(34), 1), 0, Math.Max(width - Dp(34), 0)) : (int)((center.X - gMinX) / (float)Math.Max(gMaxX - gMinX, 1) * Math.Max(width - Dp(34), 1)); int top = projected is { } screenView ? (int)Math.Clamp((1f - screenView.ScreenY) * 0.5f * Math.Max(height - Dp(24), 1), 0, Math.Max(height - Dp(24), 0)) : (int)((center.Z - gMinZ) / (float)Math.Max(gMaxZ - gMinZ, 1) * Math.Max(height - Dp(24), 1));
                    var marker = new Button(this) { Text = $"G{item.index:D2}", TextSize = 8 }; marker.SetAllCaps(false); marker.SetPadding(0, 0, 0, 0); marker.SetTextColor(Color.White); marker.SetBackgroundColor(Color.Rgb(34, 120, 96)); marker.SetOnClickListener(new ClickListener(() => EditNativeGeometryTriangle(scene, room, item.index))); marker.LayoutParameters = new FrameLayout.LayoutParams(Dp(34), Dp(24)) { LeftMargin = left, TopMargin = top }; actorOverlay.AddView(marker);
                }
            }
        });
    }

    private void AddActorNudgeButton(RomScene scene, RomRoom room, int actorIndex, string label, int dx, int dy, int dz, int left, int top, int width, int height)
    {
        if (actorOverlay is null || romWorkspace is null) return; var button = new Button(this) { Text = label, TextSize = 9 }; button.SetAllCaps(false); button.SetPadding(0, 0, 0, 0); button.SetOnClickListener(new ClickListener(() => { try { var current = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); var actor = current.Rooms[room.Id].Actors[actorIndex]; var updated = actor with { X = (short)Math.Clamp(actor.X + dx, short.MinValue, short.MaxValue), Y = (short)Math.Clamp(actor.Y + dy, short.MinValue, short.MaxValue), Z = (short)Math.Clamp(actor.Z + dz, short.MinValue, short.MaxValue) }; romWorkspace.EditRoomActor(scene.Id, room.Id, actorIndex, updated); status.Text = $"Actor {actorIndex:D2} moved by ({dx}, {dy}, {dz})."; ShowActorOverlay(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).Rooms[room.Id]); } catch (Exception error) { ShowError(error); } })); button.LayoutParameters = new FrameLayout.LayoutParams(Dp(34), Dp(21)) { LeftMargin = Math.Clamp(left, 0, Math.Max(width - Dp(34), 0)), TopMargin = Math.Clamp(top, 0, Math.Max(height - Dp(21), 0)) }; actorOverlay.AddView(button);
    }

    private void AddActorRotationButton(RomScene scene, RomRoom room, int actorIndex, string label, int rotateX, int rotateY, int rotateZ, int left, int top, int width, int height)
    {
        if (actorOverlay is null || romWorkspace is null) return; var button = new Button(this) { Text = label, TextSize = 8 }; button.SetAllCaps(false); button.SetPadding(0, 0, 0, 0); button.SetOnClickListener(new ClickListener(() => { try { var current = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); var actor = current.Rooms[room.Id].Actors[actorIndex]; var updated = actor with { RotationX = unchecked((short)(actor.RotationX + rotateX)), RotationY = unchecked((short)(actor.RotationY + rotateY)), RotationZ = unchecked((short)(actor.RotationZ + rotateZ)) }; romWorkspace.EditRoomActor(scene.Id, room.Id, actorIndex, updated); status.Text = $"Actor {actorIndex:D2} rotated by ({rotateX}, {rotateY}, {rotateZ}) BINANG."; ShowActorOverlay(scene, romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).Rooms[room.Id]); } catch (Exception error) { ShowError(error); } })); button.LayoutParameters = new FrameLayout.LayoutParams(Dp(34), Dp(21)) { LeftMargin = Math.Clamp(left, 0, Math.Max(width - Dp(34), 0)), TopMargin = Math.Clamp(top, 0, Math.Max(height - Dp(21), 0)) }; actorOverlay.AddView(button);
    }

    private sealed class ClickListener(Action click) : Java.Lang.Object, View.IOnClickListener
    {
        public void OnClick(View? v) => click();
    }

    private void ShowRomObjects(RomScene scene, RomRoom room)
    {
        var objects = room.Objects ?? []; string[] labels = objects.Select((id, i) => $"Object {i:D2} • 0x{id:X4}").ToArray(); var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} / Room {room.Id:D2} objects"); builder.SetItems(labels, (_, args) => EditNativeObject(scene, room, args.Which));
        builder.SetPositiveButton("Add object", (_, _) => AddNativeObject(scene, room));
        builder.SetNeutralButton("Remove last", (_, _) => { if (objects.Count == 0) return; try { var updated = objects.Take(objects.Count - 1).ToArray(); romWorkspace!.EditRoomObjects(scene.Id, room.Id, updated); status.Text = "Native room object removal staged."; var current = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); ShowRomObjects(current, current.Rooms[room.Id]); } catch (Exception error) { ShowError(error); } });
        builder.SetNegativeButton("Back", (_, _) => ShowRomRoom(scene, room)); builder.Show();
    }

    private void AddNativeObject(RomScene scene, RomRoom room)
    {
        if (romWorkspace is null) return;
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var editor = NumberEditor(panel, "Object ID (hex)", "0x0000");
        new AlertDialog.Builder(this)!.SetTitle("Add native room object")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Add", (_, _) =>
        {
            try { var objects = (room.Objects ?? []).Concat([(ushort)ParseInt(editor)]).ToArray(); romWorkspace.EditRoomObjects(scene.Id, room.Id, objects); status.Text = "Native room object insertion staged."; var current = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); ShowRomObjects(current, current.Rooms[room.Id]); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void ShowRomRoomExits(RomScene scene, RomRoom room)
    {
        var exits = room.Exits ?? []; var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} / Room {room.Id:D2} exits"); builder.SetItems(exits.Select((raw, i) => $"Room exit {i:D2} • raw 0x{raw:X4} • room {(raw >> 9) & 0x3F:D2} • spawn {raw & 0x1FF:D3}").ToArray(), (_, args) => EditNativeRoomExit(scene, room, args.Which)); builder.SetNegativeButton("Back", (_, _) => ShowRomRoom(scene, room)); builder.Show();
    }

    private void EditNativeRoomExit(RomScene scene, RomRoom room, int index)
    {
        if (romWorkspace is null || room.Exits is not { } exits || index < 0 || index >= exits.Count) return; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var raw = NumberEditor(panel, "Raw exit (hex)", $"0x{exits[index]:X4}");
        new AlertDialog.Builder(this)!.SetTitle($"Edit room exit {index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditRoomExit(scene.Id, room.Id, index, (ushort)ParseInt(raw)); status.Text = "Native room exit edit staged in the ROM workspace."; var updated = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); ShowRomRoomExits(updated, updated.Rooms[room.Id]); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditNativeRoomSettings(RomScene scene, RomRoom room)
    {
        if (romWorkspace is null || room.Settings is not { } settings) return; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var behavior = NumberEditor(panel, "Behavior word (hex)", $"0x{settings.Behavior:X8}"); var wind = NumberEditor(panel, "Wind word (hex)", $"0x{settings.Wind:X8}"); var startTime = NumberEditor(panel, "Start time", settings.StartTime.ToString(CultureInfo.InvariantCulture)); var timeSpeed = NumberEditor(panel, "Time speed", settings.TimeSpeed.ToString(CultureInfo.InvariantCulture)); var skybox = NumberEditor(panel, "Skybox flags", settings.SkyboxFlags.ToString(CultureInfo.InvariantCulture)); var echo = NumberEditor(panel, "Echo", settings.Echo.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} / Room {room.Id:D2} settings")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditRoomSettings(scene.Id, room.Id, new RomRoomSettings((uint)ParseInt(behavior), (uint)ParseInt(wind), (ushort)ParseInt(startTime), (byte)ParseInt(timeSpeed), (byte)ParseInt(skybox), (byte)ParseInt(echo))); status.Text = "Native room settings edit staged in the ROM workspace."; var updated = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); ShowRomRoom(updated, updated.Rooms[room.Id]); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditNativeObject(RomScene scene, RomRoom room, int index)
    {
        if (romWorkspace is null || room.Objects is not { } objects || index < 0 || index >= objects.Count) return;
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var editor = NumberEditor(panel, "Object ID", objects[index].ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit object {index:D2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditRoomObject(scene.Id, room.Id, index, (ushort)ParseInt(editor)); status.Text = "Native room object edit staged in the ROM workspace."; var updated = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); ShowRomObjects(updated, updated.Rooms[room.Id]); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void AddRomRoomTile(RomScene scene, RomRoom room)
    {
        if (romWorkspace is null) return;
        try
        {
            var extracted = RomTileExporter.ExportTiles(romWorkspace.Document, scene.Id, room.Id);
            if (extracted.Count == 0) throw new InvalidDataException("Room geometry produced no tiles.");
            float grid = DesktopImport.ReadTile(extracted[0].Xml).Grid;
            var additions = extracted.Select(tile => new LayoutTile { SourceXml = tile.Xml, X = tile.CellX * grid, Z = tile.CellZ * grid }).ToArray();
            history.Apply(history.Current with { Tiles = [.. history.Current.Tiles, .. additions] }); selected = history.Current.Tiles.Length - 1; Changed();
            status.Text = $"Split Scene {scene.Id:X2} Room {room.Id:D2} into {extracted.Count} positioned mobile tiles.";
        }
        catch (Exception error) { ShowError(error); }
    }

    private void ShowRomGeometryVertices(RomScene scene, RomRoom room)
    {
        var vertices = room.Geometry?.Vertices ?? [];
        var triangles = room.Geometry?.Triangles ?? []; string[] labels = vertices.Select((v, i) => $"Vertex {i:D3} • ({v.X}, {v.Y}, {v.Z}) • color {v.R},{v.G},{v.B},{v.A}").Concat(triangles.Select((t, i) => $"Triangle {i:D3} • {t.A},{t.B},{t.C}")).ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} / Room {room.Id:D2} geometry"); builder.SetItems(labels, (_, args) => { if (args.Which < vertices.Count) EditNativeGeometryVertex(scene, room, args.Which); else EditNativeGeometryTriangle(scene, room, args.Which - vertices.Count); }); builder.SetNeutralButton("Materials", (_, _) => ShowNativeGeometryMaterials(scene, room)); builder.SetPositiveButton("Display lists", (_, _) => ShowNativeDisplayListSources(scene, room)); builder.SetNegativeButton("Back", (_, _) => ShowRomRoom(scene, room)); builder.Show();
    }

    private void ShowNativeGeometryMaterials(RomScene scene, RomRoom room)
    {
        var vertices = room.Geometry?.Vertices ?? []; var groups = vertices.Select((vertex, index) => (vertex, index)).GroupBy(item => (R: item.vertex.R, G: item.vertex.G, B: item.vertex.B, A: item.vertex.A)).ToArray();
        var labels = groups.Select((group, index) => { short minS = group.Min(item => item.vertex.S), maxS = group.Max(item => item.vertex.S), minT = group.Min(item => item.vertex.T), maxT = group.Max(item => item.vertex.T); return $"Material {index:D2} • RGBA {group.Key.R},{group.Key.G},{group.Key.B},{group.Key.A} • {group.Count()} vertices • UV {minS},{minT}–{maxS},{maxT}"; }).ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} / Room {room.Id:D2} native materials"); builder.SetItems(labels, (_, args) => EditNativeGeometryMaterial(scene, room, groups[args.Which].ToArray())); builder.SetMessage(labels.Length == 0 ? "No decoded vertex materials are available." : "Materials are decoded native vertex colors. UVs are shown for context and are preserved when colors are replaced."); builder.SetNegativeButton("Back", (_, _) => ShowRomGeometryVertices(scene, room)); builder.Show();
    }

    private void EditNativeGeometryMaterial(RomScene scene, RomRoom room, (RomGeometryVertex vertex, int index)[] members)
    {
        if (romWorkspace is null || members.Length == 0) return; var first = members[0].vertex; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var r = NumberEditor(panel, "R", first.R.ToString(CultureInfo.InvariantCulture)); var g = NumberEditor(panel, "G", first.G.ToString(CultureInfo.InvariantCulture)); var b = NumberEditor(panel, "B", first.B.ToString(CultureInfo.InvariantCulture)); var a = NumberEditor(panel, "A", first.A.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Replace material on {members.Length} vertices")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
        {
            try
            {
                byte red = checked((byte)ParseInt(r)), green = checked((byte)ParseInt(g)), blue = checked((byte)ParseInt(b)), alpha = checked((byte)ParseInt(a));
                foreach (var member in members) { var current = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).Rooms[room.Id].Geometry!.Vertices[member.index]; romWorkspace.EditGeometryVertex(scene.Id, room.Id, member.index, current with { R = red, G = green, B = blue, A = alpha }); }
                status.Text = $"Native material replacement staged for {members.Length} geometry vertices."; var updated = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); ShowNativeGeometryMaterials(scene, updated.Rooms[room.Id]);
            }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void ShowNativeDisplayListSources(RomScene scene, RomRoom room)
    {
        var geometry = room.Geometry; var commands = geometry?.DisplayListCommands ?? []; var labels = commands.Count > 0 ? commands.Select((command, index) => $"Command {index:D3} • {DisplayListOpcodeName(command.Operation)} • 0x{command.Word0:X8} 0x{command.Word1:X8} • room offset 0x{command.SourceOffset:X6}").ToArray() : (geometry?.DisplayListOffsets ?? []).Select((offset, index) => $"Display list {index:D2} • room offset 0x{offset:X6}").ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} / Room {room.Id:D2} display lists"); builder.SetItems(labels, (_, args) => { if (commands.Count > 0) EditNativeDisplayListCommand(scene, room, args.Which); }); builder.SetMessage(labels.Length == 0 ? "No display-list sources were decoded." : "Tap a decoded command to edit its two raw 32-bit words. Writes are bounds-checked and preserve the command offset."); builder.SetNeutralButton("Textures", (_, _) => ShowNativeTextureCommands(scene, room)); builder.SetPositiveButton("Geometry", (_, _) => ShowRomGeometryVertices(scene, room))!.SetNegativeButton("Back", (_, _) => ShowRomRoom(scene, room))!.Show();
    }

    private void ShowNativeTextureCommands(RomScene scene, RomRoom room)
    {
        var commands = (room.Geometry?.DisplayListCommands ?? []).Where(command => IsTextureOpcode(command.Operation)).ToArray();
        var assets = loadedRomBytes is { } bytes ? RomTextureDecoder.ExtractRoomTextures(bytes, room) : [];
        var labels = commands.Select((command, index) =>
        {
            var asset = assets.FirstOrDefault(item => item.SourceOffset == command.SourceOffset);
            string preview = asset is { IsDecoded: true } ? $" • preview {asset.Width}×{asset.Height}" : asset is { Error: not null } ? $" • {asset.Error}" : "";
            return $"Texture command {index:D2} • {TextureCommandDescription(command)}{preview}";
        }).ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Scene {scene.Id:X2} / Room {room.Id:D2} texture commands");
        builder.SetItems(labels, (_, args) =>
        {
            var command = commands[args.Which]; var asset = assets.FirstOrDefault(item => item.SourceOffset == command.SourceOffset);
            if (asset is { IsDecoded: true }) ShowNativeTexturePreview(scene, room, asset); else EditNativeDisplayListCommand(scene, room, Array.IndexOf(room.Geometry!.DisplayListCommands!.ToArray(), command));
        });
        builder.SetMessage(labels.Length == 0 ? "No native texture commands were decoded from this room." : "Tap a decoded texture with a preview size to view its native pixels. Other entries open the bounded raw-word editor. Supported previews include RGBA, IA, I, and CI textures with loaded RGBA16 palettes; texture relocation remains guarded."); builder.SetNegativeButton("Back", (_, _) => ShowNativeDisplayListSources(scene, room)); builder.Show();
    }

    private static bool IsTextureOpcode(byte opcode) => opcode is 0xF0 or 0xF2 or 0xF3 or 0xF4 or 0xF5 or 0xFD;
    private static string DisplayListOpcodeName(byte opcode) => opcode switch { 0x01 => "G_VTX", 0x06 => "G_DL", 0xBF => "G_TRI1", 0xB1 => "G_TRI2", 0xB8 => "G_ENDDL", 0xF0 => "G_LOADTLUT", 0xF2 => "G_SETTILESIZE", 0xF3 => "G_LOADBLOCK", 0xF4 => "G_LOADTILE", 0xF5 => "G_SETTILE", 0xFD => "G_SETTIMG", _ => $"OP_{opcode:X2}" };
    private static string TextureCommandDescription(RomDisplayListCommand command)
    {
        if (!RomTextureCommandDecoder.TryDecode(command, out var info)) return $"{DisplayListOpcodeName(command.Operation)} • 0x{command.Word0:X8} 0x{command.Word1:X8} • offset 0x{command.SourceOffset:X6}";
        string details = info.Kind == "G_SETTIMG" ? $"{info.FormatName} {info.SizeName} {info.Width}px • seg {info.Segment:X2}:0x{info.Address:X6}" : info.Kind == "G_LOADTLUT" ? $"{info.Dxt + 1} palette entries" : $"{info.FormatName} {info.SizeName} • UV {info.Uls},{info.Ult}–{info.Lrs},{info.Lrt}";
        return $"{info.Kind} • {details} • 0x{command.Word0:X8} 0x{command.Word1:X8} • offset 0x{command.SourceOffset:X6}";
    }

    private void ShowNativeTexturePreview(RomScene scene, RomRoom room, RomTextureAsset asset)
    {
        var bitmap = global::Android.Graphics.Bitmap.CreateBitmap(asset.Width, asset.Height, global::Android.Graphics.Bitmap.Config.Argb8888!);
        var pixels = new int[asset.Width * asset.Height];
        for (int index = 0; index < pixels.Length; index++)
        {
            int offset = index * 4; pixels[index] = global::Android.Graphics.Color.Argb(asset.Rgba[offset + 3], asset.Rgba[offset], asset.Rgba[offset + 1], asset.Rgba[offset + 2]);
        }
        bitmap.SetPixels(pixels, 0, asset.Width, 0, 0, asset.Width, asset.Height);
        var image = new ImageView(this); image.SetAdjustViewBounds(true); image.SetPadding(Dp(12), Dp(12), Dp(12), Dp(12)); image.SetImageBitmap(bitmap);
        new AlertDialog.Builder(this)!.SetTitle($"Native texture preview • Scene {scene.Id:X2} / Room {room.Id:D2}")!.SetMessage($"{asset.Format} {asset.Size} • {asset.Width}×{asset.Height} • segment 0x{asset.Segment:X2}:0x{asset.Address:X6}")!.SetView(image)!.SetNegativeButton("Replace PNG", (_, _) => { pendingTextureScene = scene.Id; pendingTextureRoom = room.Id; pendingTextureAsset = asset; Pick(ImportTexture); })!.SetNeutralButton("Replace color", (_, _) => EditNativeTextureSolidColor(scene, room, asset))!.SetPositiveButton("Close", (_, _) => { }).Show();
    }

    private void EditNativeTextureSolidColor(RomScene scene, RomRoom room, RomTextureAsset asset)
    {
        if (romWorkspace is null) return;
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var red = NumberEditor(panel, "Red", "255"); var green = NumberEditor(panel, "Green", "255"); var blue = NumberEditor(panel, "Blue", "255"); var alpha = NumberEditor(panel, "Alpha", "255");
        new AlertDialog.Builder(this)!.SetTitle("Replace native texture with solid color")!.SetMessage("This keeps the original format, dimensions, and room allocation. Larger texture replacement and table relocation remain guarded.")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Stage replacement", (_, _) =>
        {
            try
            {
                byte r = checked((byte)ParseInt(red)), g = checked((byte)ParseInt(green)), b = checked((byte)ParseInt(blue)), a = checked((byte)ParseInt(alpha)); var pixels = new byte[asset.Width * asset.Height * 4];
                for (int index = 0; index < asset.Width * asset.Height; index++) { int offset = index * 4; pixels[offset] = r; pixels[offset + 1] = g; pixels[offset + 2] = b; pixels[offset + 3] = a; }
                romWorkspace.ReplaceRoomTexture(scene.Id, room.Id, asset with { Rgba = pixels }); status.Text = "Fixed-size native texture replacement staged in the ROM workspace.";
            }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void EditNativeDisplayListCommand(RomScene scene, RomRoom room, int index)
    {
        if (romWorkspace is null || room.Geometry?.DisplayListCommands is not { } commands || index < 0 || index >= commands.Count) return;
        var command = commands[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var word0 = NumberEditor(panel, "Word 0 (hex)", $"0x{command.Word0:X8}"); var word1 = NumberEditor(panel, "Word 1 (hex)", $"0x{command.Word1:X8}");
        new AlertDialog.Builder(this)!.SetTitle($"Edit display-list command {index:D3}")!.SetMessage($"Room offset 0x{command.SourceOffset:X6} • opcode 0x{command.Operation:X2}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) => { try { romWorkspace.EditGeometryCommand(scene.Id, room.Id, command.SourceOffset, checked((uint)ParseUInt64(word0)), checked((uint)ParseUInt64(word1))); status.Text = "Native display-list command words staged in the ROM workspace."; var updated = romWorkspace.Document.Scenes.First(s => s.Id == scene.Id); ShowNativeDisplayListSources(updated, updated.Rooms[room.Id]); } catch (Exception error) { ShowError(error); } }).Show();
    }

    private void EditNativeGeometryVertex(RomScene scene, RomRoom room, int index)
    {
        if (romWorkspace is null || room.Geometry?.Vertices is not { } vertices || index < 0 || index >= vertices.Count) return;
        var vertex = vertices[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var x = NumberEditor(panel, "X", vertex.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", vertex.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", vertex.Z.ToString(CultureInfo.InvariantCulture)); var s = NumberEditor(panel, "S", vertex.S.ToString(CultureInfo.InvariantCulture)); var t = NumberEditor(panel, "T", vertex.T.ToString(CultureInfo.InvariantCulture)); var r = NumberEditor(panel, "R", vertex.R.ToString(CultureInfo.InvariantCulture)); var g = NumberEditor(panel, "G", vertex.G.ToString(CultureInfo.InvariantCulture)); var b = NumberEditor(panel, "B", vertex.B.ToString(CultureInfo.InvariantCulture)); var a = NumberEditor(panel, "A", vertex.A.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit geometry vertex {index:D3}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
        {
            try { romWorkspace.EditGeometryVertex(scene.Id, room.Id, index, new RomGeometryVertex((short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(s), (short)ParseInt(t), (byte)ParseInt(r), (byte)ParseInt(g), (byte)ParseInt(b), (byte)ParseInt(a))); status.Text = "Native room geometry vertex edit staged in the ROM workspace."; var updatedScene = romWorkspace.Document.Scenes.First(sc => sc.Id == scene.Id); ShowRomGeometryVertices(updatedScene, updatedScene.Rooms[room.Id]); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void EditNativeGeometryTriangle(RomScene scene, RomRoom room, int index)
    {
        if (romWorkspace is null || room.Geometry?.Triangles is not { } triangles || index < 0 || index >= triangles.Count) return;
        var triangle = triangles[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4)); var a = NumberEditor(panel, "A", triangle.A.ToString(CultureInfo.InvariantCulture)); var b = NumberEditor(panel, "B", triangle.B.ToString(CultureInfo.InvariantCulture)); var c = NumberEditor(panel, "C", triangle.C.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit geometry triangle {index:D3}")!.SetView(panel)!.SetNegativeButton("Cancel", (_, _) => { })!.SetPositiveButton("Apply", (_, _) =>
        {
            try { romWorkspace.EditGeometryTriangle(scene.Id, room.Id, index, new RomGeometryTriangle(ParseInt(a), ParseInt(b), ParseInt(c))); status.Text = "Native geometry triangle edit staged in the ROM workspace."; var updatedScene = romWorkspace.Document.Scenes.First(sc => sc.Id == scene.Id); ShowRomGeometryVertices(updatedScene, updatedScene.Rooms[room.Id]); }
            catch (Exception error) { ShowError(error); }
        }).Show();
    }

    private void EditNativeActor(RomScene scene, RomRoom room, int index)
    {
        if (romWorkspace is null || index < 0 || index >= room.Actors.Count) return;
        var actor = room.Actors[index]; var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var number = NumberEditor(panel, "Actor ID", actor.Number.ToString(CultureInfo.InvariantCulture));
        var x = NumberEditor(panel, "X", actor.X.ToString(CultureInfo.InvariantCulture)); var y = NumberEditor(panel, "Y", actor.Y.ToString(CultureInfo.InvariantCulture)); var z = NumberEditor(panel, "Z", actor.Z.ToString(CultureInfo.InvariantCulture));
        var rx = NumberEditor(panel, "X rotation", actor.RotationX.ToString(CultureInfo.InvariantCulture)); var ry = NumberEditor(panel, "Y rotation", actor.RotationY.ToString(CultureInfo.InvariantCulture)); var rz = NumberEditor(panel, "Z rotation", actor.RotationZ.ToString(CultureInfo.InvariantCulture));
        var variable = NumberEditor(panel, "Variable", actor.Variable.ToString(CultureInfo.InvariantCulture));
        AddActorGizmo(panel, x, y, z, rx, ry, rz);
        var builder = new AlertDialog.Builder(this)!;
        builder.SetTitle($"Edit native actor • {ActorLabel(actor, index)}"); builder.SetView(panel);
        builder.SetNegativeButton("Cancel", (_, _) => { }); builder.SetPositiveButton("Apply", (_, _) =>
            {
                try
                {
                    var updated = new RomActor((ushort)ParseInt(number), (short)ParseInt(x), (short)ParseInt(y), (short)ParseInt(z), (short)ParseInt(rx), (short)ParseInt(ry), (short)ParseInt(rz), (ushort)ParseInt(variable));
                    romWorkspace.EditRoomActor(scene.Id, room.Id, index, updated); status.Text = "Native actor edit staged in the ROM workspace."; ShowRomRoom(romWorkspace.Document.Scenes.First(s => s.Id == scene.Id), romWorkspace.Document.Scenes.First(s => s.Id == scene.Id).Rooms[room.Id]);
                }
                catch (Exception error) { ShowError(error); }
            });
        builder.Show();
    }

    private string ActorLabel(RomActor actor, int index)
    {
        if (actorDatabase?.TryDescribe(actor, out var description) == true)
        {
            string flags = string.Join(", ", description.Properties.Where(p => p.Name.Contains("Flag", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)).Select(p => $"{p.Name}={p.Value}"));
            string variable = description.Variables.TryGetValue(actor.Variable, out var note) ? $" • {note}" : "";
            return $"{index:D2}: {description.Name} (0x{actor.Number:X4})  ({actor.X}, {actor.Y}, {actor.Z})  rotY {actor.RotationY}  var 0x{actor.Variable:X4}" + (flags.Length == 0 ? variable : $" • {flags}{variable}");
        }
        return $"{index:D2}: 0x{actor.Number:X4}  ({actor.X}, {actor.Y}, {actor.Z})  rotY {actor.RotationY}  var 0x{actor.Variable:X4}";
    }

    private void AddActorGizmo(LinearLayout panel, EditText x, EditText y, EditText z, EditText rx, EditText ry, EditText rz)
    {
        var label = new TextView(this) { Text = "ACTOR GIZMO  •  step 10 units / 90° rotation", TextSize = 11 };
        label.SetTextColor(Color.Rgb(88, 222, 189)); panel.AddView(label);
        void Nudge(EditText field, int amount) { field.Text = (ParseInt(field) + amount).ToString(CultureInfo.InvariantCulture); }
        AddToolbar(panel, ("X −", () => Nudge(x, -10)), ("X +", () => Nudge(x, 10)), ("Y −", () => Nudge(y, -10)), ("Y +", () => Nudge(y, 10)), ("Z −", () => Nudge(z, -10)), ("Z +", () => Nudge(z, 10)));
        AddToolbar(panel, ("Rot X", () => Nudge(rx, 0x4000)), ("Rot Y", () => Nudge(ry, 0x4000)), ("Rot Z", () => Nudge(rz, 0x4000)));
    }

    private void AddTile(string xml, IReadOnlyDictionary<string, byte[]>? textures = null)
    {
        DesktopImport.ReadTile(xml);
        var embedded = textures?.ToDictionary(pair => pair.Key, pair => Convert.ToBase64String(pair.Value), StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        history.Apply(history.Current with { Tiles = [.. history.Current.Tiles, new LayoutTile { SourceXml = xml, EmbeddedTextures = embedded }] });
        selected = history.Current.Tiles.Length - 1; Changed();
    }

    private void Edit(Func<LayoutTile, LayoutTile> transform)
    {
        if (selected < 0 || selected >= history.Current.Tiles.Length) return;
        var tiles = history.Current.Tiles.ToArray(); tiles[selected] = transform(tiles[selected]);
        history.Apply(history.Current with { Tiles = tiles }); Changed();
    }

    private void Move(int x, int z)
    {
        if (selected < 0 || selected >= history.Current.Tiles.Length) return;
        float grid = DesktopImport.ReadTile(history.Current.Tiles[selected].SourceXml).Grid;
        Edit(t => t with { X = t.X + x * grid, Z = t.Z + z * grid });
    }

    private void SnapSelected()
    {
        if (selected < 0 || selected >= history.Current.Tiles.Length) return;
        float grid = DesktopImport.ReadTile(history.Current.Tiles[selected].SourceXml).Grid;
        Edit(t => t with { X = MathF.Round(t.X / grid) * grid, Y = MathF.Round(t.Y / grid) * grid, Z = MathF.Round(t.Z / grid) * grid });
    }

    private void Remove()
    {
        if (selected < 0) return;
        history.Apply(history.Current with { Tiles = history.Current.Tiles.Where((_, i) => i != selected).ToArray() }); Changed();
    }

    private void Changed()
    {
        Refresh();
        LayoutStorage.SaveAtomic(SavePath, history.Current);
        status.Text = "Saved on device • bundled textures preview; gameplay editing is pending.";
    }

    #pragma warning disable CS8602
    private void Gameplay()
    {
        if (selected < 0 || selected >= history.Current.Tiles.Length) { status.Text = "Select a room before opening Gameplay."; return; }
        var tile = history.Current.Tiles[selected];
        var summary = GameplayEditor.Read(tile.SourceXml);
        var labels = summary.Actors.Select(actor => $"{actor.Role}: {actor.Name}\n  {actor.Id}  @ ({actor.X:0}, {actor.Y:0}, {actor.Z:0})  var {actor.Variable}  flags {actor.Flags.Count}").Concat(
            summary.Links.Select(link => $"LINK {link.Kind}: {link.SourceId} → {link.TargetId} {link.Parameter}")) .ToArray();
        var builder = new AlertDialog.Builder(this)!.SetTitle($"Gameplay • room {summary.Room} • {summary.Actors.Count} actors, {summary.Links.Count} links");
        builder.SetItems(labels, (_, args) => { if (args.Which < summary.Actors.Count) EditActor(selected, summary.Actors[args.Which]); });
        builder.SetNeutralButton("Add logic link", (_, _) => AddLogicLink(selected, summary));
        builder.SetPositiveButton("Close", (_, _) => { });
        builder.Show();
        status.Text = $"Gameplay visible: {summary.Actors.Count} actors, {summary.Links.Count} logic links, {summary.Actors.Sum(a => a.Flags.Count)} flags.";
    }

    private void EditActor(int tileIndex, GameplayActor actor)
    {
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical };
        panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var x = NumberEditor(panel, "X", actor.X.ToString(CultureInfo.InvariantCulture));
        var y = NumberEditor(panel, "Y", actor.Y.ToString(CultureInfo.InvariantCulture));
        var z = NumberEditor(panel, "Z", actor.Z.ToString(CultureInfo.InvariantCulture));
        var rotation = NumberEditor(panel, "Y rotation", actor.RotationY.ToString(CultureInfo.InvariantCulture));
        var variable = NumberEditor(panel, "Variable", actor.Variable.ToString(CultureInfo.InvariantCulture));
        EditText? flag = null;
        if (actor.Flags.Count > 0) flag = NumberEditor(panel, "First flag value", actor.Flags[0].Value.ToString(CultureInfo.InvariantCulture));
        new AlertDialog.Builder(this)!.SetTitle($"Edit actor • {actor.Name}")!.SetMessage($"Role: {actor.Role}\nID: {actor.Id}\nTransition: {actor.IsTransition}\nRequirements: {actor.Flags.Count} flag record(s)")!.SetView(panel)!
            .SetNegativeButton("Cancel", (_, _) => { })!
            .SetPositiveButton("Apply", (_, _) =>
            {
                try
                {
                    float px = ParseFloat(x), py = ParseFloat(y), pz = ParseFloat(z); int ry = ParseInt(rotation), v = ParseInt(variable); int? fv = flag == null ? null : ParseInt(flag);
                    var tiles = history.Current.Tiles.ToArray(); var current = tiles[tileIndex];
                    tiles[tileIndex] = current with { SourceXml = GameplayEditor.UpdateActor(current.SourceXml, actor.Id, px, py, pz, ry, v, fv) };
                    history.Apply(history.Current with { Tiles = tiles }); selected = tileIndex; Changed();
                }
                catch (Exception error) { ShowError(error); }
            }).Show();
    }

    private void AddLogicLink(int tileIndex, GameplaySummary summary)
    {
        var panel = new LinearLayout(this) { Orientation = Orientation.Vertical }; panel.SetPadding(Dp(20), Dp(4), Dp(20), Dp(4));
        var source = NumberEditor(panel, "Source actor ID", summary.Actors.FirstOrDefault()?.Id ?? "");
        var target = NumberEditor(panel, "Target actor ID", summary.Actors.Skip(1).FirstOrDefault()?.Id ?? "");
        var kind = NumberEditor(panel, "Link kind", "Activates");
        var parameter = NumberEditor(panel, "Parameter", "");
        new AlertDialog.Builder(this)!.SetTitle("Add visual logic link")!.SetMessage("Choose actor IDs from the Gameplay list. The link is stored in the tile package and remains reviewable on desktop.")!.SetView(panel)!
            .SetNegativeButton("Cancel", (_, _) => { })!
            .SetPositiveButton("Add", (_, _) =>
            {
                try
                {
                    var tiles = history.Current.Tiles.ToArray(); var current = tiles[tileIndex];
                    tiles[tileIndex] = current with { SourceXml = GameplayEditor.AddLink(current.SourceXml, source.Text ?? "", target.Text ?? "", kind.Text ?? "Activates", parameter.Text ?? "") };
                    history.Apply(history.Current with { Tiles = tiles }); selected = tileIndex; Changed();
                }
                catch (Exception error) { ShowError(error); }
            }).Show();
    }

    private EditText NumberEditor(LinearLayout panel, string hint, string value)
    {
        var edit = new EditText(this) { Hint = hint, Text = value }; edit.SetSingleLine(true); edit.SetSelectAllOnFocus(true); panel.AddView(edit); return edit;
    }
    private static float ParseFloat(EditText edit) => float.Parse(edit.Text ?? "0", NumberStyles.Float, CultureInfo.InvariantCulture);
    private static int ParseInt(EditText edit) { string value = (edit.Text ?? "0").Trim(); return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? int.Parse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture) : int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture); }
    private static ulong ParseUInt64(EditText edit) { string value = (edit.Text ?? "0").Trim(); return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? ulong.Parse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture) : ulong.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture); }
    #pragma warning restore CS8602

    private void Refresh()
    {
        selected = Math.Clamp(selected, -1, history.Current.Tiles.Length - 1);
        if (selected < 0 && history.Current.Tiles.Length > 0) selected = 0;
        var labels = history.Current.Tiles.Select((t, i) => $"{i + 1}. {DesktopImport.ReadTile(t.SourceXml).Name}").ToArray();
        rooms.Adapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleListItemActivated1, labels);
        if (selected >= 0)
        {
            rooms.SetItemChecked(selected, true);
            var tile = history.Current.Tiles[selected];
            var preview = DesktopImport.ReadTile(tile.SourceXml);
            inspector.Text = $"Room {selected + 1}  •  X {tile.X:0} / Y {tile.Y:0} / Z {tile.Z:0}  •  {tile.QuarterTurns * 90}°\n{preview.TextureReferences.Length} texture reference(s) • material colors active";
        }
        else inspector.Text = "Empty layout • import a desktop tile or add a demo room";
        if ((romWorkspace?.Document ?? rom) is null) { viewport.ShowLayout(history.Current, selected); actorOverlay.RemoveAllViews(); }
        else { var scene = CurrentScene(); var room = CurrentRoom(); if (scene is not null && room is not null) OpenRoomInViewport(scene, room); }
        RefreshSceneBrowser();
    }

    private void Help() => new AlertDialog.Builder(this)!.SetTitle("Archarina64 Android • first port")!
        .SetMessage("Import tile: open a SectionTile XML copied from the desktop tile library for material-color preview, or open a local .archtile bundle made by scripts/make-tile-bundle.ps1 to include PNG textures. The bundle preview uses its first available PNG and UVs; other materials use desktop colors. Original XML and embedded PNGs are retained.\n\nROM workflow: open a supported OoT z64 ROM to decode scenes, rooms, actors, spawns, transitions, paths, collision and room geometry. Browse a scene and edit native records, then use Verify ROM before exporting an edited ROM or JSON patch. Verification re-applies the staged changes and decodes the result before it is offered for export. Keep an original ROM backup.\n\nTouch: drag the viewport to orbit, pinch to zoom. Select a room in the list. Rotate turns 90°; X/Z move by the tile grid. Snap rounds the selected tile transform to its grid. Remove deletes the selected placement. Undo/Redo reverse edits. Reset view resets the camera. Add demo adds synthetic geometry.\n\nChanges save automatically on this device; Save retries a manual save. Open/Export layout exchange mobile JSON layouts and embedded tile XML, not desktop scene projects. Opening another layout can be undone.\n\nThe native ROM editor is still an early port: asset extraction, full display-list topology editing, simultaneous texture materials, socket snapping, and complete game-code export remain pending.")!
        .SetPositiveButton("Close", (_, _) => { })!.Show();

    private void ShowError(Exception error)
    {
        status.Text = "Could not complete action: " + error.Message;
        global::Android.Util.Log.Warn("Archarina64", error.ToString());
    }

    protected override void OnSaveInstanceState(Bundle outState) { outState.PutInt("selected", selected); base.OnSaveInstanceState(outState); }
    protected override void OnPause() { viewport.OnPause(); base.OnPause(); }
    protected override void OnResume() { base.OnResume(); viewport?.OnResume(); }

    private sealed class InsetsListener(int padding) : Java.Lang.Object, View.IOnApplyWindowInsetsListener
    {
        public WindowInsets OnApplyWindowInsets(View? view, WindowInsets? insets)
        {
            if (insets is null) return null!;
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var bars = insets.GetInsets(WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout())!;
                view?.SetPadding(padding + bars.Left, padding + bars.Top, padding + bars.Right, padding + bars.Bottom);
            }
            else
            {
                view?.SetPadding(padding + insets.SystemWindowInsetLeft, padding + insets.SystemWindowInsetTop, padding + insets.SystemWindowInsetRight, padding + insets.SystemWindowInsetBottom);
            }
            return insets;
        }
    }
}
