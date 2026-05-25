import {
  systemApi,
  formApi,
  setAccessToken,
  clearAccessToken,
  apiEndpoint,
  RpcError,
} from './bee-api-client.js';

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
  if (!table || !table.rows || table.rows.length === 0) {
    container.innerHTML = '<p class="hint">(無資料)</p>';
    return;
  }

  const cols = table.columns.map((c) => c.name);
  const head = `<tr>${cols.map((n) => `<th>${n}</th>`).join('')}<th>動作</th></tr>`;
  const body = table.rows
    .map((r, i) => {
      const cells = cols
        .map((n) => {
          const v = r.current[n];
          const display = v === null || v === undefined ? '' : String(v);
          const cls = n === 'SYS_ROWID' ? 'rowid' : '';
          return `<td class="${cls}">${display}</td>`;
        })
        .join('');
      const rowId = r.current.SYS_ROWID ?? '';
      return `<tr>${cells}<td><button data-rowid="${rowId}" class="fill-rowid">填入 RowId</button></td></tr>`;
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
    log('GetData', '(請先輸入 Row ID)');
    return;
  }
  run('Employee.GetData', () => employee.getData(rowId));
});

$('btn-insert-sample').addEventListener('click', async () => {
  // Self-contained Save demo:
  //   1. GetNewData → blank DataSet skeleton with state="Added"
  //   2. Fill SYS_ID / SYS_NAME / HIRE_DATE with a timestamped sample
  //   3. POST back to Save
  const newResult = await run('Insert Sample [1/2] GetNewData', () =>
    employee.getNewData(),
  );
  if (!newResult?.dataSet) return;

  const dataSet = newResult.dataSet;
  const masterTable = dataSet.tables.find((t) => t.tableName === 'Employee');
  if (!masterTable || masterTable.rows.length === 0) {
    log('Insert Sample', '(GetNewData 沒回 Employee table，跳過 Save)');
    return;
  }

  const stamp = new Date().toISOString().replace(/[-:T]/g, '').slice(0, 14);
  const row = masterTable.rows[0];
  row.current.SYS_ID = `J${stamp}`;
  row.current.SYS_NAME = `JS Demo ${new Date().toLocaleTimeString()}`;
  row.current.HIRE_DATE = new Date().toISOString().slice(0, 10) + 'T00:00:00';
  row.current.IS_ACTIVE = true;

  await run('Insert Sample [2/2] Save', () => employee.save(dataSet));
});

$('btn-delete').addEventListener('click', () => {
  const rowId = $('rowId').value.trim();
  if (!rowId) {
    log('Delete', '(請先輸入 Row ID)');
    return;
  }
  run('Employee.Delete', () => employee.delete(rowId));
});

// ---------- Section 5: Logout ----------

$('btn-logout').addEventListener('click', async () => {
  await run('Logout', () => systemApi.logout());
  clearAccessToken();
  $('token-display').textContent = '(已登出)';
});

// ---------- Clear output ----------

$('btn-clear').addEventListener('click', () => {
  $('result').textContent = '(已清除)';
});
