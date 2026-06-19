using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.DemoCenter.Modules;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;

namespace Avalonia.DemoCenter
{
    /// <summary>
    /// Demo Center shell: a global toolbar (theme + FormMode), a navigation tree
    /// (Category → Control → Scenario), and a Demo / Source tab pair. The Demo tab hosts
    /// the live scenario view; the Source tab shows the module's real embedded source.
    /// FormMode is set on the tab host, so the toolbar's FormMode switch drives every
    /// editor in the active scenario live.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Built scenario views are cached so re-selecting a node preserves its state.
        private readonly Dictionary<IDemoModule, Control> _viewCache = [];
        private bool _initialized;

        /// <summary>
        /// Initializes the shell, populates the FormMode selector and the navigation tree,
        /// and selects the first scenario.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            FormModeCombo.ItemsSource = Enum.GetValues<SingleFormMode>();
            FormModeCombo.SelectedItem = SingleFormMode.Edit;

            BuildNavTree();
            _initialized = true;

            SelectFirstScenario();
        }

        private void BuildNavTree()
        {
            foreach (var categoryGroup in DemoModuleRegistry.Modules.GroupBy(m => m.Category))
            {
                var categoryNode = new TreeViewItem { Header = categoryGroup.Key, IsExpanded = true };
                foreach (var controlGroup in categoryGroup.GroupBy(m => m.ControlName))
                {
                    var controlNode = new TreeViewItem { Header = controlGroup.Key, IsExpanded = true };
                    foreach (var module in controlGroup)
                    {
                        controlNode.Items.Add(new TreeViewItem
                        {
                            Header = module.ScenarioTitle,
                            Tag = module,
                        });
                    }
                    categoryNode.Items.Add(controlNode);
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

            ScenarioTitleText.Text = $"{module.ControlName} — {module.ScenarioTitle}";
            ScenarioDescText.Text = module.Description;
            DemoHost.Content = view;
            SourceText.Text = module.GetSourceText();
        }

        private void OnFormModeChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (!_initialized || FormModeCombo.SelectedItem is not SingleFormMode formMode)
                return;

            // The host is the ambient FormScope ancestor of every scenario view, so
            // setting FormMode here propagates down to all bound editors live.
            FormScope.SetFormMode(ScenarioHost, formMode);
        }

        private void OnThemeToggled(object? sender, RoutedEventArgs e)
        {
            if (global::Avalonia.Application.Current is { } app)
                app.RequestedThemeVariant = ThemeToggle.IsChecked == true ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}
