#!/usr/bin/env python3
"""Strict MCP stdio baseline validator. Python 3 standard library only.

Run on Linux/macOS with the intended dotnet environment already sourced. Output
is a new directory outside the repository containing raw stdout/stderr, client
JSONL, decoded events and summary.json. Process cleanup uses POSIX groups.
No host configuration, environment variables or dependency registrations change.
--sample-tools absent validates generated hosts without sample tools; sample
prompts/resources remain required. --self-test needs no host or .NET SDK.
Exit codes: 0 all required checks passed; 1 validation failure; 2 CLI error.
"""
import argparse
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import queue
import re
import signal
import subprocess
import sys
import threading
import time
import unittest

PROTOCOL = '2025-11-25'
WELCOME = '''Welcome to McpServer.Template!

This is a demonstration of a clean, scalable Model Context Protocol (MCP) server\x20
built with .NET 10 and proper architecture.

Features:
- Shared business logic between stdio and HTTP transports
- Strict separation of concerns (Contracts / Application / Infrastructure / MCP Adapter / Hosts)
- Easy addition of tools, prompts, and resources
- Testable, idiomatic .NET code with DI and Options pattern

Available Tools:
- echo: Echoes back your message with a timestamp
- calc_divide: Divides two numbers

Available Prompts:
- greeting: Generates a multilingual greeting message

Available Resources:
- resource://welcome: This welcome message
- resource://status: Dynamic server status information'''


class Failure(Exception):
    def __init__(self, category, detail):
        self.category = category
        super().__init__(detail)


def require(condition, message):
    if not condition:
        raise Failure('semantic_error', message)


def decode_line(raw):
    if not raw.endswith(b'\n'):
        raise Failure('protocol_error', 'Unterminated stdout frame')
    try:
        obj = json.loads(raw.decode('utf-8'), parse_constant=lambda x: (_ for _ in ()).throw(ValueError(x)))
    except (UnicodeError, ValueError) as exc:
        raise Failure('stdout_noise', str(exc)) from exc
    if not isinstance(obj, dict) or obj.get('jsonrpc') != '2.0':
        raise Failure('protocol_error', 'Expected a JSON-RPC 2.0 object')
    if 'method' in obj:
        if not isinstance(obj['method'], str) or 'result' in obj or 'error' in obj:
            raise Failure('protocol_error', 'Invalid method envelope')
    elif ('id' not in obj or isinstance(obj['id'], bool)
          or not isinstance(obj['id'], (int, str))
          or ('result' in obj) == ('error' in obj)):
        raise Failure('protocol_error', 'Invalid response envelope')
    if 'error' in obj:
        err = obj['error']
        if (not isinstance(err, dict) or type(err.get('code')) is not int
                or not isinstance(err.get('message'), str)):
            raise Failure('protocol_error', 'Invalid JSON-RPC error')
    return obj


def tool_success(result):
    require(isinstance(result, dict), 'Tool result must be an object')
    if result.get('isError', False):
        raise Failure('business_error', json.dumps(result, ensure_ascii=False))
    content = result.get('content')
    require(isinstance(content, list) and len(content) == 1, 'Expected one tool content block')
    require(content[0].get('type') == 'text', 'Expected tool text content')
    try:
        value = json.loads(content[0]['text'])
    except (ValueError, KeyError) as exc:
        raise Failure('semantic_error', 'Expected JSON DTO in text content') from exc
    require(isinstance(value, dict), 'Expected object DTO')
    if 'structuredContent' in result:
        require(result['structuredContent'] == value, 'Structured/text DTO mismatch')
    return value


def echo_assert(result, message, before, after):
    value = tool_success(result)
    # The SDK serializes DTO properties using camelCase, independent of C# names.
    require(set(value) == {'message', 'timestamp'}, 'Unexpected EchoResponse fields: ' + repr(value))
    require(value['message'] == message, 'Echo message differs')
    try:
        stamp = dt.datetime.fromisoformat(value['timestamp'].replace('Z', '+00:00'))
    except (ValueError, TypeError, AttributeError) as exc:
        raise Failure('semantic_error', 'Invalid echo timestamp') from exc
    require(stamp.tzinfo is not None and stamp.utcoffset() == dt.timedelta(0), 'Echo timestamp is not UTC')
    require(before <= stamp <= after, 'Echo timestamp outside invocation interval')


def divide_assert(result):
    value = tool_success(result)
    require(set(value) == {'result'} and type(value['result']) in (int, float)
            and value['result'] == 3.5, 'Expected CalcResponse result exactly 3.5')


def zero_assert(result):
    require(isinstance(result, dict), 'Expected tool result object')
    require(result.get('isError') is True, 'Division by zero must be a tool business error')
    content = result.get('content')
    require(isinstance(content, list) and len(content) == 1
            and content[0].get('type') == 'text'
            and content[0].get('text') == 'Division by zero is not allowed',
            'Expected exact division-by-zero business message, not an unrelated activation failure')


def capabilities_assert(caps, sample_tools):
    require(isinstance(caps, dict), 'Capabilities must be an object')
    required = ('tools', 'prompts', 'resources') if sample_tools == 'present' else ('prompts', 'resources')
    require(all(isinstance(caps.get(key), dict) for key in required), 'Missing required capabilities')
    if 'tools' in caps:
        require(isinstance(caps['tools'], dict), 'Tools capability must be an object')
    return caps


class Validator:
    def __init__(self, args):
        self.args = args
        self.output = Path(args.output).resolve()
        self.output.mkdir(parents=True, exist_ok=False)
        self.checks = []
        self.events = []
        self.q = queue.Queue()
        self.next_id = 0
        self.sent = set()
        self.received = set()
        self.capabilities = None
        self.transport_failures = []
        self.proc = None
        self.threads = []
        self.client = (self.output / 'client.jsonl').open('w', encoding='utf-8')

    def check(self, name, action):
        try:
            action()
            self.checks.append({'name': name, 'status': 'passed'})
        except Failure as exc:
            self.checks.append({'name': name, 'status': 'failed', 'category': exc.category, 'detail': str(exc)})
        except Exception as exc:
            self.checks.append({'name': name, 'status': 'failed', 'category': 'harness_error',
                                'detail': type(exc).__name__ + ': ' + str(exc)})

    def read_stream(self, stream, name):
        try:
            with (self.output / (name + '.log')).open('wb') as log:
                for raw in iter(stream.readline, b''):
                    log.write(raw)
                    log.flush()
                    if name == 'stdout':
                        self.q.put(('line', raw))
        finally:
            stream.close()
            if name == 'stdout':
                self.q.put(('eof', None))

    def send(self, method, params=None, request=False):
        assert self.proc is not None and self.proc.stdin is not None
        obj = {'jsonrpc': '2.0', 'method': method}
        if params is not None:
            obj['params'] = params
        if request:
            self.next_id += 1
            obj['id'] = self.next_id
            self.sent.add(self.next_id)
        raw = json.dumps(obj, ensure_ascii=False)
        self.client.write(raw + '\n')
        self.client.flush()
        try:
            self.proc.stdin.write((raw + '\n').encode())
            self.proc.stdin.flush()
        except (OSError, ValueError) as exc:
            raise Failure('abrupt_exit', 'Could not write to host stdin: ' + str(exc)) from exc
        return obj.get('id')

    def event(self, item):
        kind, raw = item
        if kind == 'eof':
            return None
        try:
            obj = decode_line(raw)
            if 'method' not in obj:
                if obj['id'] not in self.sent or obj['id'] in self.received:
                    raise Failure('protocol_error', 'Unsolicited or duplicate response ID: ' + repr(obj['id']))
                self.received.add(obj['id'])
            elif 'id' in obj:
                raise Failure('protocol_error', 'Unexpected server request (no client capabilities advertised)')
            self.events.append({'direction': 'server', 'message': obj})
            return obj
        except Failure as exc:
            detail = {'category': exc.category, 'detail': str(exc), 'raw': raw.decode('utf-8', errors='replace')}
            self.transport_failures.append(detail)
            self.events.append(detail)
            return None

    def request(self, method, params=None):
        assert self.proc is not None
        ident = self.send(method, params, True)
        deadline = time.monotonic() + self.args.timeout
        while True:
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                raise Failure('timeout', method + ' response deadline exceeded')
            try:
                item = self.q.get(timeout=remaining)
            except queue.Empty as exc:
                category = 'abrupt_exit' if self.proc.poll() is not None else 'timeout'
                raise Failure(category, method + ' returned no response') from exc
            if item[0] == 'eof':
                raise Failure('abrupt_exit', method + ': host stdout closed before response')
            obj = self.event(item)
            if obj and obj.get('id') == ident:
                if 'error' in obj:
                    raise Failure('jsonrpc_error', json.dumps(obj['error']))
                return obj['result']

    def initialize(self):
        result = self.request('initialize', {'protocolVersion': self.args.protocol_version,
                              'capabilities': {}, 'clientInfo': {'name': 'strict-stdio-validator', 'version': '1.0'}})
        require(result.get('protocolVersion') == self.args.protocol_version, 'Negotiated protocol differs')
        require(isinstance(result.get('serverInfo', {}).get('name'), str), 'Missing serverInfo.name')
        require(isinstance(result.get('serverInfo', {}).get('version'), str), 'Missing serverInfo.version')
        self.capabilities = capabilities_assert(result.get('capabilities', {}), self.args.sample_tools)

    def tools_list(self):
        if self.capabilities is None:
            raise Failure('semantic_error', 'Tool discovery requires successful capability negotiation')
        if self.args.sample_tools == 'absent' and 'tools' not in self.capabilities:
            # No tools were advertised: a conforming client does not request this optional feature.
            return
        result = self.request('tools/list')
        tools = result['tools']
        names = [t['name'] for t in tools]
        require(len(names) == len(set(names)), 'Duplicate tool names')
        samples = {'echo', 'calc_divide'}
        if self.args.sample_tools == 'absent':
            require(not samples.intersection(names), 'Sample tools unexpectedly present')
            return
        require(samples.issubset(names), 'Missing sample tools: ' + repr(names))
        for name, fields in [('echo', {'message': 'string'}), ('calc_divide', {'a': 'number', 'b': 'number'})]:
            schema = next(t for t in tools if t['name'] == name)['inputSchema']
            require(schema['type'] == 'object', 'Input schema must be an object')
            require(set(schema.get('required', [])) == set(fields), 'Unexpected required parameters')
            require(all(schema['properties'][k]['type'] == v for k, v in fields.items()), 'Unexpected parameter types')

    def echo(self):
        message = 'strict stdio E2E: quote " slash \\ Unicode café ☃\nsecond line'
        before = dt.datetime.now(dt.timezone.utc)
        result = self.request('tools/call', {'name': 'echo', 'arguments': {'message': message}})
        echo_assert(result, message, before, dt.datetime.now(dt.timezone.utc))

    def prompts_list(self):
        prompts = self.request('prompts/list')['prompts']
        names = [p['name'] for p in prompts]
        require(len(names) == len(set(names)) and 'greeting' in names, 'Missing/duplicate greeting prompt')
        greeting = next(p for p in prompts if p['name'] == 'greeting')
        require(any(a['name'] == 'language' and not a.get('required', False)
                    for a in greeting.get('arguments', [])), 'Expected optional language argument')

    def prompt_get(self):
        result = self.request('prompts/get', {'name': 'greeting', 'arguments': {'language': 'fr'}})
        expected = 'Bonjour ! Bienvenue sur le modèle de serveur MCP.\n\nYou can use the available tools to interact with the server.'
        require(result.get('messages') == [{'role': 'user', 'content': {'type': 'text', 'text': expected}}],
                'Greeting prompt differs: ' + repr(result))

    def resources_list(self):
        resources = self.request('resources/list')['resources']
        uris = [r['uri'] for r in resources]
        require(len(uris) == len(set(uris)), 'Duplicate resource URIs')
        for uri, mime in [('resource://welcome', 'text/plain'), ('resource://status', 'application/json')]:
            require(any(r['uri'] == uri and r.get('mimeType') == mime for r in resources), 'Missing resource ' + uri)

    def resource(self, uri, mime):
        contents = self.request('resources/read', {'uri': uri})['contents']
        require(len(contents) == 1 and contents[0].get('uri') == uri
                and contents[0].get('mimeType') == mime, 'Unexpected resource contents metadata')
        text = contents[0]['text']
        if uri == 'resource://welcome':
            # Generated template names vary; all other welcome text must match.
            normalized = re.sub(r'^Welcome to [^\n]+!$', 'Welcome to McpServer.Template!', text, count=1, flags=re.M)
            require(normalized.splitlines() == WELCOME.splitlines(), 'Welcome resource text differs')
        else:
            value = json.loads(text)
            require(set(value) == {'Timestamp', 'Version', 'Uptime', 'Status'}, 'Unexpected status fields')
            require(value['Status'] == 'Running', 'Server status is not Running')
            stamp = dt.datetime.strptime(value['Timestamp'], '%Y-%m-%d %H:%M:%S UTC').replace(tzinfo=dt.timezone.utc)
            require(abs((dt.datetime.now(dt.timezone.utc) - stamp).total_seconds()) <= self.args.timeout + 2, 'Stale status timestamp')
            require(re.fullmatch(r'\d+\.\d+\.\d+\.\d+', value['Version']) is not None, 'Invalid assembly version')
            require(re.fullmatch(r'(?:\d+d \d+h \d+m|\d+h \d+m \d+s|\d+m \d+s|\d+s)', value['Uptime']) is not None,
                    'Invalid uptime format')

    def stdout_purity(self):
        if self.transport_failures:
            categories = sorted({f['category'] for f in self.transport_failures})
            raise Failure('protocol_error' if 'protocol_error' in categories else 'stdout_noise',
                          str(len(self.transport_failures)) + ' invalid stdout frames; see events.json and stdout.log')

    def shutdown(self):
        assert self.proc is not None and self.proc.stdin is not None
        try:
            if self.proc.poll() is not None:
                raise Failure('abrupt_exit', 'Host exited before stdin EOF')
            try:
                self.proc.stdin.close()
            except OSError as exc:
                raise Failure('abrupt_exit', 'Host closed stdin before EOF shutdown') from exc
            try:
                code = self.proc.wait(timeout=self.args.shutdown_timeout)
            except subprocess.TimeoutExpired as exc:
                raise Failure('shutdown_timeout', 'Host did not exit on stdin EOF') from exc
            if code != 0:
                raise Failure('abrupt_exit', 'Nonzero host exit code: ' + str(code))
        finally:
            if self.proc.poll() is None:
                os.killpg(self.proc.pid, signal.SIGTERM)
                try:
                    self.proc.wait(timeout=2)
                except subprocess.TimeoutExpired:
                    os.killpg(self.proc.pid, signal.SIGKILL)
                    self.proc.wait(timeout=2)
            if not self.proc.stdin.closed:
                try:
                    self.proc.stdin.close()
                except OSError:
                    pass  # Preserve the original abrupt-exit failure during cleanup.
            for thread in self.threads:
                thread.join(timeout=2)
            while not self.q.empty():
                self.event(self.q.get_nowait())

    def run(self):
        started = dt.datetime.now(dt.timezone.utc).isoformat()
        command = [self.args.dotnet, str(Path(self.args.dll).resolve())]
        try:
            self.proc = subprocess.Popen(command, cwd=self.args.cwd, stdin=subprocess.PIPE,
                                         stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True)
            for name in ('stdout', 'stderr'):
                thread = threading.Thread(target=self.read_stream, args=(getattr(self.proc, name), name), daemon=True)
                thread.start()
                self.threads.append(thread)
            self.check('initialize', self.initialize)
            self.check('notifications/initialized', lambda: self.send('notifications/initialized'))
            self.check('tools capability/discovery', self.tools_list)
            if self.args.sample_tools == 'present':
                self.check('tools/call echo', self.echo)
                self.check('tools/call calc_divide', lambda: divide_assert(self.request('tools/call', {'name': 'calc_divide', 'arguments': {'a': 7, 'b': 2}})))
                self.check('tools/call division by zero', lambda: zero_assert(self.request('tools/call', {'name': 'calc_divide', 'arguments': {'a': 7, 'b': 0}})))
            self.check('prompts/list', self.prompts_list)
            self.check('prompts/get greeting fr', self.prompt_get)
            self.check('resources/list', self.resources_list)
            self.check('resources/read welcome', lambda: self.resource('resource://welcome', 'text/plain'))
            self.check('resources/read status', lambda: self.resource('resource://status', 'application/json'))
        except Exception as exc:
            self.checks.append({'name': 'host startup', 'status': 'failed', 'category': 'launch_error', 'detail': str(exc)})
        finally:
            if self.proc:
                self.check('clean shutdown on stdin EOF', self.shutdown)
            self.check('stdout protocol purity', self.stdout_purity)
            self.client.close()
        counts = {status: sum(c['status'] == status for c in self.checks) for status in ('passed', 'failed')}
        summary = {'started_utc': started, 'command': command, 'cwd': str(Path(self.args.cwd).resolve()),
                   'dll_sha256': hashlib.sha256(Path(self.args.dll).read_bytes()).hexdigest() if Path(self.args.dll).is_file() else None,
                   'protocol_version': self.args.protocol_version, 'expected_sample_tools': self.args.sample_tools,
                   'counts': counts, 'host_exit_code': self.proc.returncode if self.proc else None,
                   'transport_failures': self.transport_failures, 'checks': self.checks,
                   'limitations': ['No pagination traversal: baseline discovery is expected to fit one page.',
                                   'notifications/initialized is unacknowledged by protocol; later calls validate continued operation.',
                                   'Inherited host environment is not changed or recorded; do not include secrets in test payloads.']}
        (self.output / 'events.json').write_text(json.dumps(self.events, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
        (self.output / 'summary.json').write_text(json.dumps(summary, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
        print(json.dumps(summary, indent=2, ensure_ascii=False))
        return int(counts['failed'] != 0)


class HarnessTests(unittest.TestCase):
    def test_tools_capability_is_optional_when_samples_are_absent(self):
        caps = {'prompts': {}, 'resources': {}}
        self.assertEqual(capabilities_assert(caps, 'absent'), caps)
        with self.assertRaises(Failure):
            capabilities_assert(caps, 'present')
        with self.assertRaises(Failure):
            capabilities_assert({**caps, 'tools': False}, 'absent')

    def test_noise_rejected(self):
        with self.assertRaises(Failure) as context:
            decode_line(b'info: host started\n')
        self.assertEqual(context.exception.category, 'stdout_noise')

    def test_invalid_envelope_rejected(self):
        for raw in [b'{}\n', b'{"jsonrpc":"2.0","id":1,"result":{},"error":{}}\n',
                    b'{"jsonrpc":"2.0","id":1,"result":{}}']:
            with self.assertRaises(Failure):
                decode_line(raw)

    def test_error_categories(self):
        obj = decode_line(b'{"jsonrpc":"2.0","id":1,"error":{"code":-32603,"message":"failure"}}\n')
        self.assertEqual(obj['error']['code'], -32603)
        with self.assertRaises(Failure) as context:
            tool_success({'isError': True, 'content': [{'type': 'text', 'text': 'activation failure'}]})
        self.assertEqual(context.exception.category, 'business_error')

    def test_exact_division_and_negative_control(self):
        divide_assert({'content': [{'type': 'text', 'text': '{"result":3.5}'}]})
        with self.assertRaises(Failure):
            divide_assert({'content': [{'type': 'text', 'text': '{"result":3}'}]})

    def test_echo_exact_semantics_and_negative_control(self):
        now = dt.datetime.now(dt.timezone.utc)
        result = {'content': [{'type': 'text', 'text': json.dumps({'message': 'expected', 'timestamp': now.isoformat()})}]}
        echo_assert(result, 'expected', now - dt.timedelta(seconds=1), now + dt.timedelta(seconds=1))
        with self.assertRaises(Failure):
            echo_assert(result, 'wrong', now - dt.timedelta(seconds=1), now + dt.timedelta(seconds=1))

    def test_failed_check_is_recorded(self):
        validator = object.__new__(Validator)
        validator.checks = []
        validator.check('negative control', lambda: require(False, 'intentional test failure'))
        self.assertEqual(validator.checks[0]['status'], 'failed')
        self.assertEqual(validator.checks[0]['category'], 'semantic_error')

    def test_unrelated_error_not_zero_success(self):
        with self.assertRaises(Failure):
            zero_assert({'isError': True, 'content': [{'type': 'text', 'text': 'Unable to activate tool'}]})
        zero_assert({'isError': True, 'content': [{'type': 'text', 'text': 'Division by zero is not allowed'}]})


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--dll', help='Path to the prebuilt stdio host DLL')
    parser.add_argument('--cwd', help='Host working directory (configuration/content root)')
    parser.add_argument('--output', help='New evidence directory; must not already exist')
    parser.add_argument('--dotnet', default='dotnet', help='dotnet executable (default: PATH)')
    parser.add_argument('--sample-tools', choices=('present', 'absent'), default='present')
    parser.add_argument('--protocol-version', default=PROTOCOL)
    parser.add_argument('--timeout', type=float, default=10)
    parser.add_argument('--shutdown-timeout', type=float, default=5)
    parser.add_argument('--self-test', action='store_true')
    args = parser.parse_args()
    if args.self_test:
        suite = unittest.defaultTestLoader.loadTestsFromTestCase(HarnessTests)
        return int(not unittest.TextTestRunner(verbosity=2).run(suite).wasSuccessful())
    if os.name != 'posix':
        parser.error('Host validation currently requires Linux/macOS process groups')
    if not all((args.dll, args.cwd, args.output)):
        parser.error('--dll, --cwd and --output are required unless --self-test is used')
    if args.timeout <= 0 or args.shutdown_timeout <= 0:
        parser.error('Timeouts must be positive')
    repository = Path(__file__).resolve().parents[1]
    output = Path(args.output).resolve()
    if output == repository or output.is_relative_to(repository):
        parser.error('--output must be outside the repository')
    if output.exists():
        parser.error('--output must be a new directory; existing evidence is never overwritten')
    return Validator(args).run()


if __name__ == '__main__':
    sys.exit(main())
