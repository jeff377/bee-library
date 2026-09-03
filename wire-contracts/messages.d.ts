// Generated from the Bee.NET message types — do not edit by hand.
//
// These describe the JSON shape on the wire, not the CLR declarations: a Guid and a
// DateTime are both strings here, enums are string literal unions (the server writes
// them with JsonStringEnumConverter), and an object-typed member is the discriminated
// envelope this package calls a wire value.

/** A value carrying its type discriminator: `[code, value]`. */
export type WireValue = [number, unknown] | null;

/** A column's shape inside a serialized DataTable. */
export interface DataColumnShape {
  name: string;
  type: string;
  allowNull: boolean;
  readOnly: boolean;
  maxLength: number;
  caption: string;
  defaultValue: unknown;
}

/** A row, carrying its state and the versions that state implies. */
export interface DataRowShape {
  state: 'Unchanged' | 'Added' | 'Modified' | 'Deleted';
  current?: Record<string, unknown>;
  original?: Record<string, unknown>;
}

export interface DataTable {
  tableName: string;
  columns: DataColumnShape[];
  primaryKeys: string[];
  rows: DataRowShape[];
}

export interface DataRelationShape {
  name: string;
  parentTable: string;
  childTable: string;
  parentColumns: string[];
  childColumns: string[];
}

export interface DataSet {
  dataSetName: string;
  tables: DataTable[];
  relations: DataRelationShape[];
}

export type AnomalyKind = 'Error' | 'Timeout' | 'Slow' | 'LargeAffected' | 'LargeResult' | 'Unauthorized' | 'Replay';

export type ApiKeyStatus = 'NotChecked' | 'NotConfigured' | 'NotProvided' | 'Invalid' | 'Valid';

export type ApiKeyType = 'Internal' | 'ThirdParty';

export type ChangeKind = 'Insert' | 'Update' | 'Delete';

export type DefineType = 'SystemSettings' | 'DatabaseSettings' | 'DbCategorySettings' | 'ProgramSettings' | 'TableSchema' | 'FormSchema' | 'FormLayout' | 'Language' | 'PermissionModels' | 'CurrencySettings' | 'UnitSettings' | 'MenuSettings' | 'PluginSettings';

export type LoginEvent = 'LoginSucceeded' | 'LoginFailed' | 'LockedOut' | 'Logout' | 'ServiceSessionCreated';

export type NumberKind = 'None' | 'Quantity' | 'Weight' | 'Amount' | 'Percent' | 'UnitPrice' | 'Cost' | 'ExchangeRate';

export type PackageDelivery = 'Url' | 'Api';

export type PayloadFormat = 'Plain' | 'Encoded' | 'Encrypted';

export type SortDirection = 'Asc' | 'Desc';

export interface AllowedCurrencyItem {
  code?: string;
}

export interface ApiCallContext {
  accessToken: string;
  format: PayloadFormat;
  isLocalCall: boolean;
}

export interface ApiKeySummary {
  contact?: string;
  enabled: boolean;
  expiredAt?: string;
  issuedAt?: string;
  keyType: ApiKeyType;
  sysId?: string;
  sysName?: string;
}

export interface CashRoundingItem {
  currencyCode?: string;
  unit: number;
}

export interface CheckPackageUpdateRequest {
  parameters?: Parameter[];
  queries?: PackageUpdateQuery[];
}

export interface CheckPackageUpdateResponse {
  parameters?: Parameter[];
  updates?: PackageUpdateInfo[];
}

export interface CompanyInfo {
  allowedCurrencies?: AllowedCurrencyItem[];
  cashRounding?: CashRoundingItem[];
  companyDatabaseId?: string;
  companyId?: string;
  companyName?: string;
  customizeId?: string;
  defaultCurrency?: string;
  numberFormats?: NumberFormatItem[];
}

export interface CreateApiKeyRequest {
  contact?: string;
  expiredAt?: string;
  keyType: ApiKeyType;
  parameters?: Parameter[];
  sysId?: string;
  sysName?: string;
}

export interface CreateApiKeyResponse {
  apiKey?: string;
  parameters?: Parameter[];
  sysId?: string;
}

export interface CreateSessionRequest {
  expiresIn: number;
  oneTime: boolean;
  parameters?: Parameter[];
  userID?: string;
}

export interface CreateSessionResponse {
  accessToken: string;
  expiredAt: string;
  parameters?: Parameter[];
}

export interface DeleteRequest {
  parameters?: Parameter[];
  rowId: string;
}

export interface DeleteResponse {
  parameters?: Parameter[];
  rowsAffected: number;
}

export interface DepartmentNode {
  children?: DepartmentNode[];
  deptId?: string;
  deptName?: string;
  managerRowId: string;
  rowId: string;
}

export interface DepartmentTree {
  companyId?: string;
  roots?: DepartmentNode[];
}

export interface EnterCompanyRequest {
  companyId?: string;
  parameters?: Parameter[];
}

export interface EnterCompanyResponse {
  capabilities?: string[];
  company?: CompanyInfo;
  parameters?: Parameter[];
}

export interface ExecFuncRequest {
  funcId?: string;
  parameters?: Parameter[];
}

export interface ExecFuncResponse {
  parameters?: Parameter[];
}

export interface FilterNode {
}

export interface GetAccessLogRequest {
  fromUtc?: string;
  paging?: PagingOptions;
  parameters?: Parameter[];
  progId?: string;
  rowKey?: string;
  toUtc?: string;
  userId?: string;
}

export interface GetApiAnomalyLogRequest {
  fromUtc?: string;
  kind?: AnomalyKind;
  method?: string;
  paging?: PagingOptions;
  parameters?: Parameter[];
  toUtc?: string;
  userId?: string;
}

export interface GetApiAnomalySummaryRequest {
  fromUtc?: string;
  parameters?: Parameter[];
  toUtc?: string;
}

export interface GetChangeDetailRequest {
  parameters?: Parameter[];
  sysRowId: string;
}

export interface GetChangeDetailResponse {
  changeKind: ChangeKind;
  fields?: RecordFieldChange[];
  isSensitive: boolean;
  logTime: string;
  parameters?: Parameter[];
  progId?: string;
  rowKey?: string;
  source?: string;
  sysRowId: string;
  userId?: string;
  userName?: string;
}

export interface GetChangeLogRequest {
  changeKind?: ChangeKind;
  fromUtc?: string;
  paging?: PagingOptions;
  parameters?: Parameter[];
  progId?: string;
  rowKey?: string;
  toUtc?: string;
  userId?: string;
}

export interface GetCommonConfigurationRequest {
  parameters?: Parameter[];
}

export interface GetCommonConfigurationResponse {
  commonConfiguration?: string;
  parameters?: Parameter[];
}

export interface GetCustomizePluginSettingsRequest {
  customizeId?: string;
  parameters?: Parameter[];
}

export interface GetCustomizePluginSettingsResponse {
  parameters?: Parameter[];
  xml?: string;
}

export interface GetDataRequest {
  parameters?: Parameter[];
  rowId: string;
}

export interface GetDataResponse {
  dataSet?: DataSet;
  parameters?: Parameter[];
}

export interface GetDbAnomalyLogRequest {
  databaseId?: string;
  fromUtc?: string;
  kind?: AnomalyKind;
  paging?: PagingOptions;
  parameters?: Parameter[];
  toUtc?: string;
}

export interface GetDbAnomalySummaryRequest {
  fromUtc?: string;
  parameters?: Parameter[];
  toUtc?: string;
}

export interface GetDefineRequest {
  defineType: DefineType;
  keys?: string[];
  parameters?: Parameter[];
}

export interface GetDefineResponse {
  parameters?: Parameter[];
  xml?: string;
}

export interface GetDepartmentTreeRequest {
  parameters?: Parameter[];
}

export interface GetDepartmentTreeResponse {
  parameters?: Parameter[];
  tree?: DepartmentTree;
}

export interface GetFormLayoutRequest {
  layoutId?: string;
  parameters?: Parameter[];
  progId?: string;
}

export interface GetFormLayoutResponse {
  parameters?: Parameter[];
  xml?: string;
}

export interface GetFormSchemaRequest {
  parameters?: Parameter[];
  progId?: string;
}

export interface GetFormSchemaResponse {
  parameters?: Parameter[];
  xml?: string;
}

export interface GetLanguageRequest {
  lang?: string;
  namespace?: string;
  parameters?: Parameter[];
}

export interface GetLanguageResponse {
  parameters?: Parameter[];
  xml?: string;
}

export interface GetListRequest {
  filter?: FilterNode;
  paging?: PagingOptions;
  parameters?: Parameter[];
  selectFields?: string;
  sortFields?: SortField[];
}

export interface GetListResponse {
  paging?: PagingInfo;
  parameters?: Parameter[];
  table?: DataTable;
}

export interface GetLoginLogRequest {
  event?: LoginEvent;
  fromUtc?: string;
  paging?: PagingOptions;
  parameters?: Parameter[];
  toUtc?: string;
  userId?: string;
}

export interface GetLookupRequest {
  paging?: PagingOptions;
  parameters?: Parameter[];
  searchText?: string;
}

export interface GetLookupResponse {
  paging?: PagingInfo;
  parameters?: Parameter[];
  table?: DataTable;
}

export interface GetNewDataRequest {
  parameters?: Parameter[];
}

export interface GetNewDataResponse {
  dataSet?: DataSet;
  parameters?: Parameter[];
}

export interface GetPackageRequest {
  appId?: string;
  channel?: string;
  componentId?: string;
  fileId?: string;
  parameters?: Parameter[];
  platform?: string;
  version?: string;
}

export interface GetPackageResponse {
  content?: string;
  fileName?: string;
  fileSize: number;
  packageUrl?: string;
  parameters?: Parameter[];
  sha256?: string;
}

export interface GetTopApiMethodsRequest {
  fromUtc?: string;
  parameters?: Parameter[];
  toUtc?: string;
  topN: number;
}

export interface LeaveCompanyRequest {
  parameters?: Parameter[];
}

export interface LeaveCompanyResponse {
  parameters?: Parameter[];
}

export interface ListApiKeysRequest {
  parameters?: Parameter[];
}

export interface ListApiKeysResponse {
  apiKeys?: ApiKeySummary[];
  parameters?: Parameter[];
}

export interface LogAggregateResponse {
  parameters?: Parameter[];
  table?: DataTable;
}

export interface LogListResponse {
  paging?: PagingInfo;
  parameters?: Parameter[];
  table?: DataTable;
}

export interface LoginRequest {
  clientPublicKey?: string;
  parameters?: Parameter[];
  password?: string;
  userId?: string;
}

export interface LoginResponse {
  accessToken: string;
  apiEncryptionKey?: string;
  expiredAt: string;
  parameters?: Parameter[];
  timeZone?: string;
  userId?: string;
  userName?: string;
}

export interface LogoutRequest {
  parameters?: Parameter[];
}

export interface LogoutResponse {
  parameters?: Parameter[];
}

export interface NumberFormatItem {
  decimals: number;
  kind: NumberKind;
}

export interface PackageUpdateInfo {
  appId?: string;
  componentId?: string;
  delivery: PackageDelivery;
  latestVersion?: string;
  mandatory: boolean;
  packageSize: number;
  packageUrl?: string;
  releaseNotes?: string;
  sha256?: string;
  updateAvailable: boolean;
}

export interface PackageUpdateQuery {
  appId?: string;
  channel?: string;
  componentId?: string;
  currentVersion?: string;
  platform?: string;
}

export interface PagingInfo {
  hasMore: boolean;
  page: number;
  pageSize: number;
  totalCount?: number;
}

export interface PagingOptions {
  includeTotalCount: boolean;
  page: number;
  pageSize: number;
}

export interface Parameter {
  name?: string;
  value?: WireValue;
}

export interface PingRequest {
  clientName?: string;
  parameters?: Parameter[];
  traceId?: string;
}

export interface PingResponse {
  apiKeyStatus: ApiKeyStatus;
  parameters?: Parameter[];
  serverTime: string;
  status?: string;
  traceId?: string;
  version?: string;
}

export interface RecordFieldChange {
  fieldName?: string;
  newValue?: string;
  oldValue?: string;
  rowKey?: string;
  rowState: ChangeKind;
  tableName?: string;
}

export interface SaveCustomizePluginSettingsRequest {
  customizeId?: string;
  parameters?: Parameter[];
  xml?: string;
}

export interface SaveCustomizePluginSettingsResponse {
  parameters?: Parameter[];
  pluginCount: number;
}

export interface SaveDefineRequest {
  defineType: DefineType;
  keys?: string[];
  parameters?: Parameter[];
  xml?: string;
}

export interface SaveDefineResponse {
  parameters?: Parameter[];
}

export interface SaveRequest {
  dataSet?: DataSet;
  parameters?: Parameter[];
}

export interface SaveResponse {
  affectedRows?: string[];
  dataSet?: DataSet;
  parameters?: Parameter[];
}

export interface SetApiKeyEnabledRequest {
  enabled: boolean;
  parameters?: Parameter[];
  sysId?: string;
}

export interface SetApiKeyEnabledResponse {
  enabled: boolean;
  parameters?: Parameter[];
  sysId?: string;
}

export interface SetApiKeyExpiryRequest {
  expiredAt?: string;
  parameters?: Parameter[];
  sysId?: string;
}

export interface SetApiKeyExpiryResponse {
  expiredAt?: string;
  parameters?: Parameter[];
  sysId?: string;
}

export interface SetDeploymentAdminRequest {
  isDeploymentAdmin: boolean;
  parameters?: Parameter[];
  userId?: string;
}

export interface SetDeploymentAdminResponse {
  isDeploymentAdmin: boolean;
  parameters?: Parameter[];
  userId?: string;
}

export interface SortField {
  direction: SortDirection;
  fieldName?: string;
}
