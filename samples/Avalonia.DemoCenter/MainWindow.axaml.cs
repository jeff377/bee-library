using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.DemoCenter.Scenarios;
using Bee.Definition.Layouts;
using Bee.UI.Avalonia.Controls.Editors;

namespace Avalonia.DemoCenter
{
    /// <summary>
    /// Demo Center shell: a global toolbar (theme + FormMode), a navigation tree
    /// (Category → Control → Scenario) and a scenario host. The host carries the
    /// ambient <see cref="Bee.UI.Avalonia.Controls.Editors.FormScope"/> FormMode, so the
    /// toolbar's FormMode switch drives every editor in the active scenario live.
    /// </summary>
    /// <remarks>
    /// Stage 1 registers a single migrated scenario (the editor-vs-native comparison).
    /// Stage 2 replaces the inline registry with the <c>IDemoModule</c> abstraction and
    /// adds the View Source panel.
    /// </remarks>
    public partial class MainWindow : Window
    {
        // Stage 1 registry: a flat list grouped into the nav tree by Category / ControlName.
        private static readonly DemoScenario[] s_scenarios =
        [
            new DemoScenario(
                "Data Editors",
                "原生 vs 繼承 比對",
                "全部編輯器",
                "每個 ControlType 一個區塊：左欄原生 Avalonia 控件、右欄繼承控件（FormScope ambient 綁定）。"
                + "工具列的 FormMode 切換會即時驅動右欄繼承控件的唯讀 / 編輯外觀。",
                EditorsComparisonScenario.Build),
        ];

        // Built scenario views are cached so re-selecting a node preserves its state.
        private readonly Dictionary<DemoScenario, Control> _viewCache = [];
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
            foreach (var categoryGroup in s_scenarios.GroupBy(s => s.Category))
            {
                var categoryNode = new TreeViewItem { Header = categoryGroup.Key, IsExpanded = true };
                foreach (var controlGroup in categoryGroup.GroupBy(s => s.ControlName))
                {
                    var controlNode = new TreeViewItem { Header = controlGroup.Key, IsExpanded = true };
                    foreach (var scenario in controlGroup)
                    {
                        controlNode.Items.Add(new TreeViewItem
                        {
                            Header = scenario.ScenarioTitle,
                            Tag = scenario,
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
                if (node.Tag is DemoScenario)
                    return node;
                var leaf = FindFirstLeaf(node.Items.OfType<TreeViewItem>());
                if (leaf is not null)
                    return leaf;
            }
            return null;
        }

        private void OnScenarioSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (NavTree.SelectedItem is not TreeViewItem { Tag: DemoScenario scenario })
                return;

            if (!_viewCache.TryGetValue(scenario, out var view))
            {
                view = scenario.BuildView();
                _viewCache[scenario] = view;
            }

            ScenarioTitleText.Text = $"{scenario.ControlName} — {scenario.ScenarioTitle}";
            ScenarioDescText.Text = scenario.Description;
            ScenarioHost.Content = view;
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
