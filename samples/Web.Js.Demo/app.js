import {
  systemApi,
  formApi,
  setAccessToken,
  clearAccessToken,
  apiEndpoint,
  RpcError,
} from './bee-api-client.js';
import { renderFormLayout } from './form-renderer.js';

const $ = (id) => document.getElementById(id);

$('endpoint').textContent = apiEndpoint;

const employee = formApi('Employee');

function log(label, payload) {
  const out = $('result');
  const ts = new Date().toLocaleTimeString();
  const body = typeof payload === 'string' ? payload : JSON.stringify(payload, null, 2);
  out.textContent = `[${ts}] ${label}\n${body}\n\n` + out.textContent;
}

function logError(label, err) {
  const out = $('result');
  const ts = new Date().toLocaleTimeString();
  const codeStr = err instanceof RpcError ? `RpcError code ${err.code}` : 'JS Error';
  out.textContent = `[${ts}] ${label} — ${codeStr}\n${err.message}\n\n` + out.textContent;
}

async function run(label, fn) {
  try {
    const result = await fn();
    log(label, result);
    return result;
  } catch (err) {
    logError(label, err);
    return null;
  }
}

// ---------- Section 1: Login ----------

$('btn-login').addEventListener('click', async () => {
  const result = await run('Login', () =>
    systemApi.login($('userId').value, $('password').value),
  );
  if (result) {
    setAccessToken(result.accessToken);
    $('token-display').textContent = result.accessToken;
  }
});

// ---------- Section 2: Ping ----------

$('btn-ping').addEventListener('click', () => run('Ping', () => systemApi.ping()));

// ---------- Section 3: Enter Company ----------

$('btn-enter-company').addEventListener('click', () =>
  run('EnterCompany', () => systemApi.enterCompany($('companyId').value)),
);

$('btn-leave-company').addEventListener('click', () =>
  run('LeaveCompany', () => systemApi.leaveCompany()),
);

// ---------- Section 4: Employee CRUD ----------

function renderRowList(table) {
  const container = $('rowlist-container');
  if (!table?.rows?.length) {
    container.innerHTML = '<p class="hint">(no rows)</p>';
    return;
  }

  const cols = table.columns.map((c) => c.name);
  const headCells = cols.map((n) => `<th>${n}</th>`).join('');
  const head = `<tr>${headCells}<th>Action</th></tr>`;
  const body = table.rows
    .map((r) => {
      const cells = cols
        .map((n) => {
          const v = r.current[n];
          const display = v === null || v === undefined ? '' : String(v);
          const cls = n === 'sys_rowid' ? 'rowid' : '';
          return `<td class="${cls}">${display}</td>`;
        })
        .join('');
      const rowId = r.current.sys_rowid ?? '';
      const actionCell = `<td><button data-rowid="${rowId}" class="fill-rowid">Fill Row ID</button></td>`;
      return `<tr>${cells}${actionCell}</tr>`;
    })
    .join('');

  container.innerHTML = `<table class="rowlist"><thead>${head}</thead><tbody>${body}</tbody></table>`;

  container.querySelectorAll('button.fill-rowid').forEach((btn) => {
    btn.addEventListener('click', () => {
      $('rowId').value = btn.dataset.rowid;
    });
  });
}

$('btn-getlist').addEventListener('click', async () => {
  const result = await run('Employee.GetList', () => employee.getList());
  if (result?.table) renderRowList(result.table);
});

$('btn-getnewdata').addEventListener('click', () =>
  run('Employee.GetNewData', () => employee.getNewData()),
);

$('btn-getdata').addEventListener('click', () => {
  const rowId = $('rowId').value.trim();
  if (!rowId) {
    log('GetData', '(enter a Row ID first)');
    return;
  }
  run('Employee.GetData', () => employee.getData(rowId));
});

$('btn-insert-sample').addEventListener('click', async () => {
  // Self-contained Save demo:
  //   1. GetNewData → blank DataSet skeleton with state="Added"
  //   2. Fill sys_id / sys_name / hire_date with a timestamped sample
  //   3. POST back to Save
  const newResult = await run('Insert Sample [1/2] GetNewData', () =>
    employee.getNewData(),
  );
  if (!newResult?.dataSet) return;

  const dataSet = newResult.dataSet;
  const masterTable = dataSet.tables.find((t) => t.tableName === 'Employee');
  if (!masterTable || masterTable.rows.length === 0) {
    log('Insert Sample', '(GetNewData returned no Employee table, skipping Save)');
    return;
  }

  const stamp = new Date().toISOString().replace(/[-:T]/g, '').slice(0, 14);
  const row = masterTable.rows[0];
  row.current.sys_id = `J${stamp}`;
  row.current.sys_name = `JS Demo ${new Date().toLocaleTimeString()}`;
  row.current.hire_date = new Date().toISOString().slice(0, 10) + 'T00:00:00';
  row.current.is_active = true;

  await run('Insert Sample [2/2] Save', () => employee.save(dataSet));
});

$('btn-delete').addEventListener('click', () => {
  const rowId = $('rowId').value.trim();
  if (!rowId) {
    log('Delete', '(enter a Row ID first)');
    return;
  }
  run('Employee.Delete', () => employee.delete(rowId));
});

// ---------- Section 5: FormDefinition-driven rendering ----------

let _formController = null;

$('btn-load-formdef').addEventListener('click', async () => {
  try {
    // Fetch schema + layout in parallel — schema is currently unused by the
    // renderer (no client validation) but is included to demonstrate the
    // typical Promise.all pattern that React/Vue apps would use.
    const [schemaResp, layoutResp] = await Promise.all([
      systemApi.getFormSchema('Employee'),
      systemApi.getFormLayout('Employee'),
    ]);
    log('GetFormSchema + GetFormLayout', {
      schemaProgId: schemaResp.schema?.progId,
      layoutId: schemaResp.schema ? layoutResp.layout?.layoutId : null,
    });
    _formController = renderFormLayout(layoutResp.layout, $('rendered-form'));
  } catch (err) {
    logError('Load Form Definition', err);
  }
});

$('btn-form-new').addEventListener('click', async () => {
  if (!_formController) {
    log('Form New', '(click "Load Form Definition" first)');
    return;
  }
  try {
    const result = await employee.getNewData();
    log('GetNewData (for form)', { rowId: result.dataSet?.tables?.[0]?.rows?.[0]?.current?.sys_rowid });
    _formController.bindDataSet(result.dataSet);
  } catch (err) {
    logError('Form New', err);
  }
});

$('btn-form-load').addEventListener('click', async () => {
  if (!_formController) {
    log('Form Load', '(click "Load Form Definition" first)');
    return;
  }
  const rowId = $('rowId').value.trim();
  if (!rowId) {
    log('Form Load', '(enter a Row ID in section 4 first)');
    return;
  }
  try {
    const result = await employee.getData(rowId);
    log('GetData (for form)', { rowId });
    _formController.bindDataSet(result.dataSet);
  } catch (err) {
    logError('Form Load', err);
  }
});

$('btn-form-save').addEventListener('click', async () => {
  if (!_formController) {
    log('Form Save', '(click "Load Form Definition" first)');
    return;
  }
  try {
    const dataSet = _formController.collectDataSet();
    const result = await employee.save(dataSet);
    log('Form Save', { affectedRows: result.affectedRows });
    // Re-bind the refreshed DataSet so server-side derived values (e.g. sys_no)
    // surface back into the form.
    if (result.dataSet) _formController.bindDataSet(result.dataSet);
  } catch (err) {
    logError('Form Save', err);
  }
});

// ---------- Section 6: Logout ----------

$('btn-logout').addEventListener('click', async () => {
  await run('Logout', () => systemApi.logout());
  clearAccessToken();
  $('token-display').textContent = '(logged out)';
});

// ---------- Clear output ----------

$('btn-clear').addEventListener('click', () => {
  $('result').textContent = '(cleared)';
});
