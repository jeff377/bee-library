// FormDefinition-driven renderer for Bee.NET FormLayout / FormSchema.
//
// Takes a FormLayout JSON tree and produces a working HTML form into a target
// container. Supports binding DataSet -> form and collecting form -> DataSet
// with RowState management.
//
// Scope (per plan-web-js-demo-formdef-rendering.md):
//   - Master sections rendered via CSS Grid using LayoutField.rowSpan/columnSpan
//   - Detail tables rendered read-only (no row add/edit/delete)
//   - Control types: TextEdit / DateEdit / YearMonthEdit / CheckEdit / MemoEdit /
//     DropDownEdit (no data source) / ButtonEdit (placeholder button)
//   - Client-side validation intentionally NOT implemented — relies on server
//     Save returning RpcError

// ---- ControlType -> HTML element factory ----------------------------------

const CONTROL_FACTORIES = {
  TextEdit: (field) => {
    const input = document.createElement('input');
    input.type = 'text';
    input.dataset.fieldName = field.fieldName;
    return input;
  },
  MemoEdit: (field) => {
    const ta = document.createElement('textarea');
    ta.rows = 3;
    ta.dataset.fieldName = field.fieldName;
    return ta;
  },
  DateEdit: (field) => {
    const input = document.createElement('input');
    input.type = 'date';
    input.dataset.fieldName = field.fieldName;
    return input;
  },
  YearMonthEdit: (field) => {
    const input = document.createElement('input');
    input.type = 'month';
    input.dataset.fieldName = field.fieldName;
    return input;
  },
  CheckEdit: (field) => {
    const input = document.createElement('input');
    input.type = 'checkbox';
    input.dataset.fieldName = field.fieldName;
    return input;
  },
  DropDownEdit: (field) => {
    // Placeholder — real data source binding is out of scope for this demo.
    const sel = document.createElement('select');
    sel.dataset.fieldName = field.fieldName;
    const opt = document.createElement('option');
    opt.value = '';
    opt.textContent = '(no data source)';
    sel.appendChild(opt);
    return sel;
  },
  ButtonEdit: (field) => {
    // Placeholder — wraps text input with a non-functional button.
    const wrap = document.createElement('span');
    wrap.style.display = 'inline-flex';
    wrap.style.gap = '0.3em';
    const input = document.createElement('input');
    input.type = 'text';
    input.dataset.fieldName = field.fieldName;
    wrap.appendChild(input);
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.textContent = '...';
    btn.disabled = true;
    btn.title = 'ButtonEdit lookup not wired in this demo';
    wrap.appendChild(btn);
    // Expose the actual input for value get/set.
    wrap._beeInput = input;
    return wrap;
  },
};

function getInputFromControl(el) {
  // ButtonEdit wraps the input inside a span — unwrap.
  return el._beeInput || el;
}

// ---- Value conversion (DataSet field <-> HTML input) -----------------------

function setInputValue(input, value) {
  if (value === null || value === undefined) {
    if (input.type === 'checkbox') {
      input.checked = false;
    } else {
      input.value = '';
    }
    return;
  }
  if (input.type === 'checkbox') {
    input.checked = Boolean(value);
  } else if (input.type === 'date') {
    // DataSet date strings come as "2024-03-01 00:00:00" or "2024-03-01T00:00:00".
    input.value = String(value).slice(0, 10);
  } else if (input.type === 'month') {
    input.value = String(value).slice(0, 7);
  } else {
    input.value = String(value);
  }
}

function readInputValue(input, column) {
  if (input.type === 'checkbox') return input.checked;
  if (input.value === '') return null;
  if (input.type === 'date') {
    // Match GetNewData's ISO-like format so server-side equality works.
    return input.value + 'T00:00:00';
  }
  if (input.type === 'month') {
    return input.value + '-01T00:00:00';
  }
  if (column && (column.type === 'Integer' || column.type === 'Long')) {
    const n = Number(input.value);
    return Number.isFinite(n) ? n : input.value;
  }
  return input.value;
}

// ---- Master section rendering ---------------------------------------------

function renderField(field) {
  const cell = document.createElement('div');
  cell.style.gridRow = `span ${field.rowSpan || 1}`;
  cell.style.gridColumn = `span ${field.columnSpan || 1}`;
  cell.style.display = 'flex';
  cell.style.flexDirection = 'column';
  cell.style.gap = '0.2em';

  const label = document.createElement('label');
  label.textContent = field.caption || field.fieldName;
  label.style.fontSize = '0.85em';
  label.style.color = '#555';

  const factory = CONTROL_FACTORIES[field.controlType] || CONTROL_FACTORIES.TextEdit;
  const control = factory(field);
  control.style.width = '100%';
  control.style.boxSizing = 'border-box';
  if (field.visible === false) cell.style.display = 'none';

  cell.appendChild(label);
  cell.appendChild(control);
  return { cell, control };
}

function renderSection(section, columnCount) {
  const wrap = document.createElement('div');
  wrap.style.marginBottom = '1em';

  if (section.showCaption !== false) {
    const h = document.createElement('h3');
    h.textContent = section.caption || section.name;
    h.style.margin = '0.5em 0 0.4em';
    h.style.fontSize = '1em';
    h.style.color = '#1e3a8a';
    wrap.appendChild(h);
  }

  const grid = document.createElement('div');
  grid.style.display = 'grid';
  grid.style.gridTemplateColumns = `repeat(${columnCount || 2}, 1fr)`;
  grid.style.gap = '0.6em 1em';

  const controlsByFieldName = {};
  for (const field of section.fields || []) {
    const { cell, control } = renderField(field);
    grid.appendChild(cell);
    controlsByFieldName[field.fieldName] = control;
  }

  wrap.appendChild(grid);
  return { wrap, controlsByFieldName };
}

// ---- Detail grid rendering (read-only table) ------------------------------

function renderDetailGrid(detail) {
  const wrap = document.createElement('div');
  wrap.style.marginBottom = '1em';

  const h = document.createElement('h3');
  h.textContent = `${detail.caption || detail.tableName} (detail, read-only)`;
  h.style.margin = '0.5em 0 0.4em';
  h.style.fontSize = '1em';
  h.style.color = '#1e3a8a';
  wrap.appendChild(h);

  const table = document.createElement('table');
  table.className = 'rowlist';
  table.dataset.tableName = detail.tableName;
  wrap.appendChild(table);

  const thead = document.createElement('thead');
  const tr = document.createElement('tr');
  for (const col of detail.columns || []) {
    const th = document.createElement('th');
    th.textContent = col.caption || col.fieldName;
    tr.appendChild(th);
  }
  thead.appendChild(tr);
  table.appendChild(thead);

  const tbody = document.createElement('tbody');
  tbody.className = 'detail-rows';
  table.appendChild(tbody);

  return { wrap, table, tbody, columns: detail.columns || [] };
}

// ---- DataSet binding helpers ----------------------------------------------

function bindMasterRow(row, controlsBySection) {
  for (const { controlsByFieldName } of controlsBySection) {
    for (const [fieldName, control] of Object.entries(controlsByFieldName)) {
      const input = getInputFromControl(control);
      const value = row.current[fieldName.toUpperCase()];
      setInputValue(input, value);
    }
  }
}

function populateDetailGrid(grid, dataSet) {
  grid.tbody.innerHTML = '';
  const detailTable = dataSet.tables.find((t) => t.tableName === grid.tableName);
  if (!detailTable?.rows) return;
  for (const r of detailTable.rows) {
    const tr = document.createElement('tr');
    for (const col of grid.columns) {
      const td = document.createElement('td');
      const v = r.current[col.fieldName.toUpperCase()];
      td.textContent = v === null || v === undefined ? '' : String(v);
      tr.appendChild(td);
    }
    grid.tbody.appendChild(tr);
  }
}

// ---- Public API -----------------------------------------------------------

/**
 * Render a FormLayout into the given container.
 * @returns A controller {bindDataSet, collectDataSet, layout} for data ops.
 */
export function renderFormLayout(layout, container) {
  container.innerHTML = '';

  const masterControlsBySection = []; // [{ sectionName, controlsByFieldName }]
  for (const section of layout.sections || []) {
    const { wrap, controlsByFieldName } = renderSection(section, layout.columnCount);
    container.appendChild(wrap);
    masterControlsBySection.push({ sectionName: section.name, controlsByFieldName });
  }

  const detailGrids = []; // [{ tableName, tbody, columns }]
  for (const detail of layout.details || []) {
    const { wrap, tbody, columns } = renderDetailGrid(detail);
    container.appendChild(wrap);
    detailGrids.push({ tableName: detail.tableName, tbody, columns });
  }

  // Hold the most recent DataSet so collectDataSet can preserve untouched rows
  // and restore RowState/sys_rowid for the master row.
  let _currentDataSet = null;

  function bindDataSet(dataSet) {
    _currentDataSet = dataSet;
    if (!dataSet?.tables) return;

    // master is always tables[0] per FormSchema convention
    const masterTable = dataSet.tables[0];
    if (masterTable?.rows?.length > 0) {
      bindMasterRow(masterTable.rows[0], masterControlsBySection);
    }

    // Detail tables: clear + repopulate (read-only).
    for (const grid of detailGrids) {
      populateDetailGrid(grid, dataSet);
    }
  }

  function collectDataSet() {
    if (!_currentDataSet?.tables?.length) {
      throw new Error('No DataSet loaded — call bindDataSet first (via Load or New).');
    }
    // Deep-clone the most recent DataSet so we do not mutate the in-memory copy
    // the caller might still inspect.
    const ds = structuredClone(_currentDataSet);
    const masterTable = ds.tables[0];
    if (!masterTable?.rows?.length) {
      throw new Error('DataSet has no master row to save.');
    }
    const row = masterTable.rows[0];

    // Apply form values back to current. Mark RowState=Modified if state was
    // Unchanged (i.e., this came from GetData rather than GetNewData).
    for (const { controlsByFieldName } of masterControlsBySection) {
      for (const [fieldName, control] of Object.entries(controlsByFieldName)) {
        const input = getInputFromControl(control);
        const upperFieldName = fieldName.toUpperCase();
        const column = masterTable.columns.find((c) => c.name === upperFieldName);
        row.current[upperFieldName] = readInputValue(input, column);
      }
    }
    if (row.state === 'Unchanged') row.state = 'Modified';

    return ds;
  }

  return { layout, bindDataSet, collectDataSet };
}
