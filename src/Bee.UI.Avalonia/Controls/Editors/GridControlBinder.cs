using Bee.UI.Avalonia.DataObjects;

namespace Bee.UI.Avalonia.Controls.Editors
{
    /// <summary>
    /// Binding engine held by <see cref="GridControl"/>. Mirrors the
    /// <see cref="FieldEditorBinder"/> ambient/attach state machine, but a table
    /// binder has different duties — it resolves a detail <c>DataTable</c> from the
    /// data object instead of reading/writing a field value, and it never needs
    /// write-back echo suppression because the read-only grid does not write.
    /// </summary>
    internal sealed class GridControlBinder
    {
        private readonly GridControl _owner;
        private bool _attached;
        private bool _updatingBinding;
        private bool _boundFromAmbient;

        /// <summary>
        /// Initializes a new instance of <see cref="GridControlBinder"/>.
        /// </summary>
        /// <param name="owner">The grid control that forwards lifecycle events here.</param>
        public GridControlBinder(GridControl owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Gets the bound data object, or <c>null</c> when unbound or in list mode.
        /// </summary>
        public FormDataObject? DataObject { get; private set; }

        /// <summary>
        /// Subscribes to <paramref name="dataObject"/>, replacing any current
        /// subscription. Explicit bindings take precedence over the ambient scope
        /// while the grid stays attached.
        /// </summary>
        /// <param name="dataObject">The data object whose dataset holds the bound table.</param>
        public void BindExplicit(FormDataObject dataObject)
        {
            ArgumentNullException.ThrowIfNull(dataObject);
            Unbind();
            BindCore(dataObject, fromAmbient: false);
        }

        /// <summary>
        /// Releases the data object subscription. List-mode binds
        /// (<see cref="GridControl.Bind(Bee.Definition.Layouts.LayoutGrid, System.Data.DataTable?)"/>)
        /// call this because their rows live outside any data object.
        /// </summary>
        public void Unbind()
        {
            if (DataObject is not null)
                DataObject.DataSetReplaced -= OnDataSetReplaced;
            DataObject = null;
            _boundFromAmbient = false;
        }

        /// <summary>
        /// Notifies the binder that the grid attached to the logical tree, enabling
        /// ambient binding through <see cref="FormScope"/>.
        /// </summary>
        public void NotifyAttached()
        {
            _attached = true;
            TryAmbientBind();
        }

        /// <summary>
        /// Notifies the binder that the grid detached from the logical tree. The
        /// subscription is released so grids discarded by a host rebuild do not keep
        /// the <see cref="FormDataObject"/> alive; re-attaching restores ambient
        /// bindings automatically.
        /// </summary>
        public void NotifyDetached()
        {
            _attached = false;
            Unbind();
        }

        /// <summary>
        /// Re-evaluates the ambient binding after the grid's table name or the
        /// ambient <see cref="FormScope.DataObjectProperty"/> changed.
        /// </summary>
        public void OnBindingContextChanged()
        {
            if (_updatingBinding || !_attached) return;
            TryAmbientBind();
        }

        private void TryAmbientBind()
        {
            // An explicit binding wins over the ambient scope.
            if (DataObject is not null && !_boundFromAmbient) return;

            var ambient = _owner.GetValue(FormScope.DataObjectProperty);
            var tableName = _owner.TableName;
            if (DataObject is not null)
            {
                if (ReferenceEquals(DataObject, ambient))
                {
                    // Same scope — the table name changed; re-resolve against it.
                    _owner.RefreshFromDataObject();
                    return;
                }
                Unbind();
            }
            if (ambient is null || string.IsNullOrWhiteSpace(tableName)) return;
            BindCore(ambient, fromAmbient: true);
        }

        private void BindCore(FormDataObject dataObject, bool fromAmbient)
        {
            _updatingBinding = true;
            try
            {
                DataObject = dataObject;
                _boundFromAmbient = fromAmbient;
                dataObject.DataSetReplaced += OnDataSetReplaced;
                if (fromAmbient)
                    _owner.RefreshFromDataObject();
                _owner.SetControlState(_owner.GetValue(FormScope.FormModeProperty));
            }
            finally
            {
                _updatingBinding = false;
            }
        }

        private void OnDataSetReplaced(object? sender, EventArgs e)
        {
            // The whole DataSet was swapped (LoadAsync / NewAsync / ...), so the
            // previously resolved DataTable instance is stale — re-resolve by name.
            _owner.RefreshFromDataObject();
        }
    }
}
