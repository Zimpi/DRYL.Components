using DRYL.Components;

namespace DRYL.Components.Demo;

/// <summary>
/// A single documented component on the DRYL website. One entry feeds the sidebar,
/// the Ctrl+K command palette and the <c>/components</c> overview grid — so adding a
/// component here updates every surface at once and they can never drift apart.
/// </summary>
/// <param name="Title">Display label used in the sidebar, search and overview card (e.g. "Button").</param>
/// <param name="Slug">Route the demo page lives at, without a leading slash (e.g. "buttons").</param>
/// <param name="Category">Sidebar group / category the entry is shown under.</param>
/// <param name="ClassName">DRYL type name (e.g. "DrylButton"). Null for composite demo pages that show several components.</param>
/// <param name="Folder">Library source folder the type lives in (used to build the "View source" link).</param>
/// <param name="Ai">Whether the component supports the shared <c>Ai</c> (AiState) parameter.</param>
/// <param name="Summary">One-line description (≤ ~12 words) shown in search and overview cards.</param>
/// <param name="Icon">DrylIcon name shown in the sidebar and search row.</param>
public sealed record ComponentDocEntry(
    string Title,
    string Slug,
    string Category,
    string? ClassName,
    string Folder,
    bool Ai,
    string Summary,
    string Icon);

/// <summary>
/// The single source of truth for every component documented on the DRYL website.
/// </summary>
public static class ComponentCatalog
{
    /// <summary>Base URL of the public repository, used for "View source" links.</summary>
    public const string RepoUrl = "https://github.com/Zimpi/DRYL.Components";

    /// <summary>Category display order in the sidebar and overview.</summary>
    public static readonly IReadOnlyList<string> CategoryOrder =
    [
        "Actions", "Surfaces", "Navigation", "Data",
        "Inputs", "Layout", "Feedback", "Intelligence",
    ];

    /// <summary>Every documented component, in the order it should appear within its category.</summary>
    public static readonly IReadOnlyList<ComponentDocEntry> Entries =
    [
        // ── Actions ───────────────────────────────────────────────
        new("Button",          "buttons",          "Actions",  "DrylButton",        "Actions",     true,  "Primary action — 4 variants, sizes, loading, icons.",        "Bolt"),
        new("Button Group",    "button-group",     "Actions",  "DrylButtonGroup",   "Actions",     false, "Segmented buttons — toggle group or clustered actions.",     "Blocks"),
        new("Menu",            "menu",             "Actions",  "DrylMenu",          "Navigation",  false, "Dropdown menu — placements, icons, shortcuts, danger items.","Menu"),
        new("Command Palette", "command-palette",  "Actions",  "DrylCommandPalette","Navigation",  true,  "Ctrl+K launcher — static or async search, AI result panel.", "Command"),

        // ── Surfaces ──────────────────────────────────────────────
        new("Card",     "cards",    "Surfaces", "DrylCard",     "Surfaces", true,  "Glass surface with optional cursor-tracking spotlight.",     "Layers"),
        new("Dialog",   "dialog",   "Surfaces", "DrylDialog",   "Surfaces", true,  "Service-driven glass dialog — focus trap, sizes.",           "Box"),
        new("Toast",    "toasts",   "Surfaces", "DrylToast",    "Surfaces", false, "Service-driven toast stack — auto-dismiss, 6 positions.",    "Bell"),
        new("Chat",     "chat",     "Surfaces", "DrylChat",     "Surfaces", false, "Conversation surface — scrollable log plus pinned composer.","Sparkle"),
        new("Popover",  "popover",  "Surfaces", "DrylPopover",  "Surfaces", false, "Anchored floating panel — portals to body, placements.",     "Layers"),
        new("Markdown",  "markdown", "Surfaces", "DrylMarkdown", "Surfaces", false, "Renders CommonMark + GFM — fenced code, XSS-safe.",         "Type"),

        // ── Navigation ────────────────────────────────────────────
        new("Breadcrumbs", "breadcrumbs", "Navigation", "DrylBreadcrumbs", "Navigation", false, "Hierarchical trail — separators, ellipsis collapse.", "ChevronsRight"),

        // ── Data ──────────────────────────────────────────────────
        new("Badge",            "badges",           "Data", "DrylBadge",           "Data", false, "Neutral · Accent · Success · Warning · Danger, optional dot.","Star"),
        new("Avatar",           "avatar",           "Data", "DrylAvatar",          "Data", false, "Image → initials → icon fallback; sizes, presence dot.",     "User"),
        new("Table",            "tables",           "Data", "DrylTable",           "Data", true,  "Declarative columns — search, sort, filter, paging, more.",  "Grid"),
        new("Pagination",       "pagination",       "Data", "DrylPagination",      "Data", false, "Standalone page navigator with smart-ellipsis range.",       "Dots"),
        new("Tree View",        "tree",             "Data", "DrylTreeView",        "Data", false, "Hierarchical tree — WAI-ARIA keyboard nav, roving tabindex.","Folder"),
        new("Timeline",         "timeline",         "Data", "DrylTimeline",        "Data", true,  "Vertical event rail — variants, timestamps, AI step traces.","Activity"),
        new("Stat",             "stat",             "Data", "DrylStat",            "Data", true,  "KPI card — value, delta chip, optional sparkline slot.",     "Hash"),
        new("Sparkline",        "sparkline",        "Data", "DrylSparkline",       "Data", false, "Inline-SVG trend chart (zero JS) — Line / Area / Bar.",      "Chart"),
        new("Description List", "description-list",  "Data", "DrylDescriptionList", "Data", false, "Semantic key/value view — Stacked or Inline, columns.",     "List"),
        new("Keyboard Key",     "kbd",              "Data", "DrylKbd",             "Data", false, "Keyboard-shortcut chips — single key or chord, pure CSS.",   "Command"),
        new("Code Block",       "code-block",       "Data", "DrylCodeBlock",       "Data", true,  "Glass code surface — server-side highlighting, copy button.","Code"),
        new("Citation",         "citation",         "Data", "DrylCitation",        "Data", false, "Inline [n] source chips with popover — RAG answers.",        "Quote"),
        new("Icons",            "icons",            "Data", "DrylIcon",            "Data", false, "Lucide-based line icon set, currentColor stroked.",          "Palette"),

        // ── Inputs ────────────────────────────────────────────────
        new("Input Text",    "inputs",          "Inputs", "DrylInputText",     "Inputs", true,  "Form-bound text input — leading/trailing icon slots.",       "Type"),
        new("Password",      "input-password",  "Inputs", "DrylInputPassword", "Inputs", false, "Password input with show / hide eye toggle.",                "Lock"),
        new("Number",        "input-number",    "Inputs", "DrylInputNumber",   "Inputs", true,  "Generic numeric input with optional ± stepper.",             "Hash"),
        new("Form Controls", "forms",           "Inputs", null,                "Inputs", false, "Textarea, select, checkbox, toggle — EditForm-integrated.",  "Check"),
        new("Radio Group",   "radio",           "Inputs", "DrylRadioGroup",    "Inputs", false, "Radio group — vertical or horizontal layout.",               "Circle"),
        new("Segmented Control","segmented-control","Inputs","DrylSegmentedControl","Inputs", false, "Exclusive view / mode switch — gliding accent indicator.","Blocks"),
        new("Multi Select",  "multiselect",     "Inputs", "DrylMultiSelect",   "Inputs", true,  "Multi-selection dropdown — removable chips, @bind.",         "List"),
        new("Slider",        "slider",          "Inputs", "DrylSlider",        "Inputs", true,  "Range slider — accent gradient fill.",                       "Sliders"),
        new("File Upload",   "file-upload",     "Inputs", "DrylFileUpload",    "Inputs", true,  "Drag-and-drop or click-to-browse, multiple files.",          "Upload"),
        new("Autocomplete",  "autocomplete",    "Inputs", "DrylAutocomplete",  "Inputs", true,  "Generic combobox — client SearchFunc or async provider.",    "Search"),
        new("Date Picker",   "datepicker",      "Inputs", "DrylDatePicker",    "Inputs", true,  "Calendar panel — keyboard nav, Min/Max, range mode.",        "Calendar"),
        new("Time Picker",   "timepicker",      "Inputs", "DrylTimePicker",    "Inputs", true,  "Time-only picker — scrollable hour/minute, MinuteStep.",     "Calendar"),
        new("Chip Input",    "chip-input",      "Inputs", "DrylChipInput",     "Inputs", true,  "Free-text tag field — Enter / comma to commit, MaxTags.",    "Hash"),
        new("OTP Input",     "otp",             "Inputs", "DrylInputOtp",      "Inputs", true,  "Fixed-box OTP entry — auto-advance, paste-to-fill.",         "Grid"),
        new("Input Mask",    "input-mask",      "Inputs", "DrylInputMask",     "Inputs", true,  "Masked input — Phone / IBAN / Postal / Card / Custom.",      "Hash"),
        new("Rating",        "rating",          "Inputs", "DrylRating",        "Inputs", true,  "Star rating — hover preview, AllowClear, keyboard nav.",     "Star"),
        new("Form Field",    "form-field",      "Inputs", "DrylFormField",     "Inputs", false, "Label + required + hint + inline validation wrapper.",       "Check"),

        // ── Layout ────────────────────────────────────────────────
        new("Expansion",   "expansion",   "Layout", "DrylExpansion",  "Layout", false, "Collapsible disclosure panel with animated chevron.", "ChevronDown"),
        new("Tabs",        "tabs",        "Layout", "DrylTabs",       "Layout", true,  "Tabbed interface — gliding gradient underline.",      "Folder"),
        new("Stepper",     "stepper",     "Layout", "DrylStepper",    "Layout", true,  "Multi-step wizard — horizontal or vertical, AI ring.","ArrowRight"),
        new("Scroll Area", "scroll-area", "Layout", "DrylScrollArea", "Layout", false, "Scrollable region with the thin DRYL scrollbar.",     "Layers"),
        new("Typography",  "typography",  "Layout", "DrylTypo",       "Layout", false, "Type scale primitive — Variant look, As tag, colours.", "Type"),
        new("Stack",       "stack",       "Layout", "DrylStack",      "Layout", false, "Flex layout — direction, gap, align, justify, wrap.",   "Blocks"),
        new("Divider",     "divider",     "Layout", "DrylDivider",    "Layout", false, "Thin rule — horizontal, labelled or vertical.",         "Menu"),

        // ── Feedback ──────────────────────────────────────────────
        new("Alert",          "alerts",         "Feedback", "DrylAlert",         "Feedback", true,  "Feedback banner — 5 variants, optional title, dismissible.","Alert"),
        new("Tooltip",        "tooltip",        "Feedback", "DrylTooltip",       "Feedback", false, "CSS-only hover tooltip — 4 placements, wraps any trigger.", "Info"),
        new("Spinner",        "spinners",       "Feedback", "DrylSpinner",       "Feedback", true,  "Ring / Dots / Pulse — rate adapts to AI state.",            "Refresh"),
        new("Skeleton",       "skeleton",       "Feedback", "DrylSkeleton",      "Feedback", true,  "Placeholder — Streaming shifts shimmer to violet-cyan.",    "Blocks"),
        new("Progress",       "progress",       "Feedback", "DrylProgress",      "Feedback", true,  "Linear bar — determinate / indeterminate, percentage.",     "Activity"),
        new("Empty State",    "empty-state",    "Feedback", "DrylEmptyState",    "Feedback", true,  "No-data placeholder — icon, title, description, action.",   "Search"),
        new("Error Boundary", "error-boundary", "Feedback", "DrylErrorBoundary", "Feedback", true,  "Glass fallback — retry / recover, dev-only detail toggle.", "Alert"),
        new("Notifications",  "notifications",  "Feedback", "DrylNotifications",  "Feedback", true,  "Bell + inbox panel — unread badge, mark-read, AI entries.", "Bell"),

        // ── Intelligence ──────────────────────────────────────────
        new("AI Mode",   "ai",        "Intelligence", "DrylAiIndicator", "AI", true, "Pulsing status pill adapting label and speed to AiState.", "Sparkle"),
        new("Tool Call", "tool-call", "Intelligence", "DrylToolCall",    "AI", true, "Agent tool call — status pill, collapsible JSON args/result.","Wrench"),
    ];

    /// <summary>Entries grouped by category in <see cref="CategoryOrder"/> order.</summary>
    public static IEnumerable<IGrouping<string, ComponentDocEntry>> ByCategory() =>
        Entries
            .GroupBy(e => e.Category)
            .OrderBy(g => CategoryIndex(g.Key));

    private static int CategoryIndex(string category)
    {
        for (var i = 0; i < CategoryOrder.Count; i++)
            if (CategoryOrder[i] == category) return i;
        return int.MaxValue;
    }

    /// <summary>Look up a single entry by its route slug (no leading slash).</summary>
    public static ComponentDocEntry? FindBySlug(string slug) =>
        Entries.FirstOrDefault(e =>
            string.Equals(e.Slug, slug.Trim('/'), StringComparison.OrdinalIgnoreCase));

    /// <summary>Public repo "blob" link to a component's source file, or the components folder for composite pages.</summary>
    public static string SourceUrl(ComponentDocEntry entry) =>
        entry.ClassName is null
            ? $"{RepoUrl}/tree/main/DRYL.Components/Components/{entry.Folder}"
            : $"{RepoUrl}/blob/main/DRYL.Components/Components/{entry.Folder}/{entry.ClassName}.razor";

    /// <summary>
    /// Build the command-palette item list from the catalog — every component plus the
    /// Components overview and Get Started shortcuts. Keeps Ctrl+K search always current.
    /// </summary>
    public static CommandItem[] ToCommandItems()
    {
        var items = new List<CommandItem>
        {
            new() { Label = "Home",       Icon = "Home",   Type = CommandItemType.Navigate, Href = "/",                Category = "Pages" },
            new() { Label = "Components",  Icon = "Grid",   Type = CommandItemType.Navigate, Href = "/components",      Category = "Pages" },
            new() { Label = "Get Started", Icon = "Rocket", Type = CommandItemType.Navigate, Href = "/getting-started", Category = "Pages" },
        };

        items.AddRange(Entries.Select(e => new CommandItem
        {
            Label       = e.Title,
            Description = e.Summary,
            Icon        = e.Icon,
            Category    = e.Category,
            Type        = CommandItemType.Navigate,
            Href        = "/" + e.Slug,
        }));

        return [.. items];
    }
}
