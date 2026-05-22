using System.Data;
using Bee.Api.Client.Connectors;
using Bee.Definition;
using Bee.Definition.Forms;
using Bee.Definition.Layouts;
using Bee.Web.Blazor.Server.DataObjects;
using Bee.Web.Blazor.Server.DependencyInjection;
using Microsoft.AspNetCore.Components;

namespace Bee.Web.Blazor.Server.Components
{
    /// <summary>
    /// Code-behind for <c>FormPage.razor</c>. Wires <see cref="DynamicGrid"/>
    /// (list view) to <see cref="DynamicForm"/> (master detail) via a shared
    /// <see cref="FormDataObject"/>: selecting a list row drives
    /// <see cref="FormDataObject.LoadAsync"/>; the toolbar buttons fan out to
    /// <see cref="FormDataObject.NewAsync"/> / <c>SaveAsync</c> / <c>DeleteAsync</c>.
    /// </summary>
    /// <remarks>
    /// FormPage assumes the host has acquired an <c>AccessToken</c> elsewhere
    /// (e.g. via a sign-in page that calls <see cref="SystemApiConnector.LoginAsync"/>)
    /// and supplies it through a cascading parameter. Anonymous use is allowed
    /// (<see cref="AccessToken"/> defaults to <see cref="Guid.Empty"/>); the
    /// backend BO methods being called must then declare
    /// <c>ApiAccessRequirement.Anonymous</c> themselves.
    /// </remarks>
    public partial class FormPage : ComponentBase
    {
        private FormSchema? _schema;
        private FormLayout? _formLayout;
        private LayoutGrid? _listLayout;
        private FormDataObject? _dataObject;
        private DataTable? _listRows;
        private string? _error;
        private bool _isInitializing = true;
        private bool _isBusy;

        /// <summary>
        /// Gets or sets the program identifier (e.g. "Employee"). Drives the
        /// FormSchema lookup and the connector creation.
        /// </summary>
        [Parameter, EditorRequired]
        public string ProgId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the cascading access token. Defaults to
        /// <see cref="Guid.Empty"/> (anonymous).
        /// </summary>
        [CascadingParameter]
        public Guid AccessToken { get; set; }

        [Inject]
        private BeeApiConnectorFactory Factory { get; set; } = default!;

        /// <inheritdoc/>
        protected override async Task OnInitializedAsync()
        {
            if (string.IsNullOrWhiteSpace(ProgId))
            {
                _error = "FormPage.ProgId must be set.";
                _isInitializing = false;
                return;
            }

            try
            {
                var system = Factory.CreateSystemConnector(AccessToken);
                _schema = await system
                    .GetDefineAsync<FormSchema>(DefineType.FormSchema, [ProgId])
                    .ConfigureAwait(true);

                _formLayout = _schema.GetFormLayout();
                _listLayout = _schema.GetListLayout();

                _dataObject = new FormDataObject(_schema, Factory.CreateFormConnector(AccessToken, ProgId));
                await ReloadListAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private async Task ReloadListAsync()
        {
            var connector = Factory.CreateFormConnector(AccessToken, ProgId);
            var response = await connector.GetListAsync().ConfigureAwait(true);
            _listRows = response.Table;
        }

        private async Task OnRowSelectedAsync(Guid rowId)
        {
            if (_dataObject is null) return;
            await RunGuardedAsync(() => _dataObject.LoadAsync(rowId)).ConfigureAwait(true);
        }

        private async Task OnNewAsync()
        {
            if (_dataObject is null) return;
            await RunGuardedAsync(_dataObject.NewAsync).ConfigureAwait(true);
        }

        private async Task OnSaveAsync()
        {
            if (_dataObject is null) return;
            await RunGuardedAsync(async () =>
            {
                await _dataObject.SaveAsync().ConfigureAwait(true);
                await ReloadListAsync().ConfigureAwait(true);
            }).ConfigureAwait(true);
        }

        private async Task OnDeleteAsync()
        {
            if (_dataObject is null) return;
            await RunGuardedAsync(async () =>
            {
                await _dataObject.DeleteAsync().ConfigureAwait(true);
                await ReloadListAsync().ConfigureAwait(true);
            }).ConfigureAwait(true);
        }

        private async Task RunGuardedAsync(Func<Task> action)
        {
            if (_isBusy) return;
            _isBusy = true;
            _error = null;
            try
            {
                await action().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                _error = ex.Message;
            }
            finally
            {
                _isBusy = false;
            }
        }
    }
}
