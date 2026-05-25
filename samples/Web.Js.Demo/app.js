import {
  systemApi,
  setAccessToken,
  getAccessToken,
  apiEndpoint,
} from './bee-api-client.js';

const $ = (id) => document.getElementById(id);

$('endpoint').textContent = apiEndpoint;

function log(label, payload) {
  const out = $('result');
  const ts = new Date().toLocaleTimeString();
  const body = typeof payload === 'string' ? payload : JSON.stringify(payload, null, 2);
  out.textContent = `[${ts}] ${label}\n${body}\n\n` + out.textContent;
}

function logError(label, err) {
  const out = $('result');
  const ts = new Date().toLocaleTimeString();
  const detail = err.code ? `code ${err.code}: ${err.message}` : err.message;
  out.textContent = `[${ts}] ${label} ERROR\n${detail}\n\n` + out.textContent;
}

$('btn-login').addEventListener('click', async () => {
  try {
    const result = await systemApi.login($('userId').value, $('password').value);
    setAccessToken(result.accessToken);
    $('token-display').textContent = result.accessToken;
    log('Login', result);
  } catch (err) {
    logError('Login', err);
  }
});

$('btn-ping').addEventListener('click', async () => {
  try {
    log('Ping', await systemApi.ping());
  } catch (err) {
    logError('Ping', err);
  }
});

$('btn-clear').addEventListener('click', () => {
  $('result').textContent = '(已清除)';
});
