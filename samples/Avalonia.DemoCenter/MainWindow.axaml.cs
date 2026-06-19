using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.DemoCenter.Modules;

namespace Avalonia.DemoCenter
{
    /// <summary>
    /// Demo Center shell: a global toolbar (theme toggle), a navigation tree (theme → case),
    /// and a Demo / Source tab pair. The Demo tab hosts the live scenario view; the Source
    /// tab shows the module's real embedded source. FormMode switching is not global — it
    /// lives inside the "FormMode 顯示狀態" theme so it never drives unrelated demos.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Built scenario views are cached so re-selecting a node preserves its state.
        private readonly Dictionary<IDemoModule, Control> _viewCache = [];

        // Last nav-column width, restored when the tree is shown again.
        private GridLength _savedNavWidth = new(240);

        /// <summary>
        /// Initializes the shell, builds the navigation tree and selects the first scenario.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            BuildNavTree();
            SelectFirstScenario();
        }

        private void BuildNavTree()
        {
            // Two-level tree: theme (Category) → case (Title).
            foreach (var categoryGroup in DemoModuleRegistry.Modules.GroupBy(m => m.Category))
            {
                var categoryNode = new TreeViewItem { Header = categoryGroup.Key, IsExpanded = true };
                foreach (var module in categoryGroup)
                {
                    categoryNode.Items.Add(new TreeViewItem
                    {
                        Header = module.Title,
                        Tag = module,
                    });
                }
                NavTree.Items.Add(categoryNode);
            }
        }

        private void SelectFirstScenario()
        {
            // The first leaf (deepest non-grouping node) is the default scenario.
            var firstLeaf = FindFirstLeaf(NavTree.Items.OfType<TreeViewItem>());
            if (firstLeaf is not null)
                firstLeaf.IsSelected = true;
        }

        private static TreeViewItem? FindFirstLeaf(IEnumerable<TreeViewItem> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Tag is IDemoModule)
                    return node;
                var leaf = FindFirstLeaf(node.Items.OfType<TreeViewItem>());
                if (leaf is not null)
                    return leaf;
            }
            return null;
        }

        private void OnScenarioSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (NavTree.SelectedItem is not TreeViewItem { Tag: IDemoModule module })
                return;

            if (!_viewCache.TryGetValue(module, out var view))
            {
                view = module.BuildView();
                _viewCache[module] = view;
            }

            ScenarioTitleText.Text = $"{module.Category} — {module.Title}";
            ScenarioDescText.Text = module.Description;
            DemoHost.Content = view;
            SourceText.Text = module.GetSourceText();
        }

        private void OnToggleNav(object? sender, RoutedEventArgs e)
        {
            var navColumn = BodyGrid.ColumnDefinitions[0];
            if (NavPanel.IsVisible)
            {
                // Hide: remember the width, collapse the column (MinWidth would otherwise
                // pin it open) and hide the panel + splitter.
                _savedNavWidth = navColumn.Width;
                navColumn.MinWidth = 0;
                navColumn.Width = new GridLength(0);
                NavPanel.IsVisible = false;
                NavSplitter.IsVisible = false;
            }
            else
            {
                navColumn.MinWidth = 160;
                navColumn.Width = _savedNavWidth.IsAbsolute && _savedNavWidth.Value > 0
                    ? _savedNavWidth
                    : new GridLength(240);
                NavPanel.IsVisible = true;
                NavSplitter.IsVisible = true;
            }
        }

        private void OnThemeToggled(object? sender, RoutedEventArgs e)
        {
            if (global::Avalonia.Application.Current is { } app)
                app.RequestedThemeVariant = ThemeToggle.IsChecked == true ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}
