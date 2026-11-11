"""Real-host/package regression tests. Build Release before running; see docs.

These are intentionally separate from fast harness unit tests. No mocks, expected
failures, application overrides, or SDK upgrades are used.
"""
import importlib.util
import json
import os
from pathlib import Path
import sys
import unittest

REPO = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("generation_validator", REPO / "scripts/validate_generated_projects.py")
assert spec is not None and spec.loader is not None
validation = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validation)


def evidence_directory(name):
    value = os.environ.get("MCP_REGRESSION_OUTPUT")
    if not value:
        raise RuntimeError("Set MCP_REGRESSION_OUTPUT to a new evidence directory outside the repository")
    root = Path(value).resolve()
    if root == REPO or root.is_relative_to(REPO):
        raise RuntimeError("Evidence must be outside the repository")
    directory = root / name
    directory.mkdir(parents=True, exist_ok=False)
    return directory


class StdioRegressions(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.output = evidence_directory("stdio")
        host = REPO / "src/McpServer.Template.Host.Stdio"
        dll = host / "bin/Release/net10.0/McpServer.Template.Host.Stdio.dll"
        if not dll.is_file():
            raise RuntimeError("Build the Release solution before running stdio regressions")
        result = validation.invoke(
            [sys.executable, "-B", str(REPO / "scripts/validate_stdio.py"),
             "--dll", str(dll), "--cwd", str(host),
             "--output", str(cls.output / "protocol"), "--sample-tools", "present"],
            REPO, cls.output / "validator.log", 90)
        if result["exitCode"] not in (0, 1):
            raise RuntimeError(f"Validator infrastructure failed: {result}")
        cls.summary = json.loads((cls.output / "protocol/summary.json").read_text())
        cls.checks = {item["name"]: item for item in cls.summary["checks"]}

    def assert_check_passed(self, name):
        self.assertIn(name, self.checks)
        check = self.checks[name]
        self.assertEqual("passed", check["status"], json.dumps(check, ensure_ascii=False))

    def test_echo_returns_exact_dto(self):
        self.assert_check_passed("tools/call echo")

    def test_division_returns_exact_result(self):
        self.assert_check_passed("tools/call calc_divide")

    def test_division_by_zero_returns_business_error(self):
        self.assert_check_passed("tools/call division by zero")

    def test_stdout_contains_only_protocol_frames(self):
        self.assert_check_passed("stdout protocol purity")

    def test_remaining_protocol_checks_and_shutdown(self):
        names = {
            "initialize", "notifications/initialized", "tools capability/discovery",
            "prompts/list", "prompts/get greeting fr", "resources/list",
            "resources/read welcome", "resources/read status", "clean shutdown on stdin EOF",
        }
        self.assertEqual(len(self.summary["checks"]), len(self.checks), "Duplicate check names")
        self.assertEqual(set(self.checks), names | {
            "tools/call echo", "tools/call calc_divide", "tools/call division by zero", "stdout protocol purity"})
        for name in sorted(names):
            with self.subTest(check=name):
                self.assert_check_passed(name)
        self.assertEqual(0, self.summary["host_exit_code"])
        self.assertFalse(any(self.summary["readers_alive"]))
        self.assertEqual([], self.summary["reader_errors"])
        self.assertEqual([], self.summary["drain_failures"])


class TemplateRegressions(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.output = evidence_directory("template")
        cls.hive = cls.output / "hive"
        commands = [
            ("pack", ["dotnet", "pack", str(REPO / "McpServer.Template.csproj"),
                      "-c", "Release", "--output", str(cls.output / "packages"),
                      "--artifacts-path", str(cls.output / "pack-artifacts"),
                      "-p:Version=0.0.0-regressions", "--disable-build-servers"]),
        ]
        for name, command in commands:
            result = validation.invoke(command, REPO, cls.output / f"{name}.log", 240)
            if result["exitCode"] != 0:
                raise RuntimeError(f"Template setup failed: {result}")
        packages = list((cls.output / "packages").glob("*.nupkg"))
        if len(packages) != 1:
            raise RuntimeError("Expected exactly one real template package")
        result = validation.invoke(["dotnet", "new", "install", str(packages[0]),
                                    "--debug:custom-hive", str(cls.hive)],
                                   REPO, cls.output / "install.log", 120)
        if result["exitCode"] != 0:
            raise RuntimeError(f"Template install failed: {result}")

    def test_no_host_is_rejected_without_partial_solution(self):
        target = self.output / "generated-no-host"
        result = validation.invoke([
            "dotnet", "new", "mcp-server", "--name", "Regression.NoHost",
            "--output", str(target), "--debug:custom-hive", str(self.hive),
            "--http-host", "false", "--stdio-host", "false"],
            REPO, self.output / "no-host.log", 120)
        text = (self.output / "no-host.log").read_text(errors="replace").lower()
        evidence = {**result, "partialSolutions": [str(p) for p in target.rglob("*.slnx")]}
        (self.output / "result.json").write_text(json.dumps(evidence, indent=2) + "\n")
        self.assertNotEqual(124, result["exitCode"], "Generation timed out rather than rejecting options")
        self.assertNotEqual(0, result["exitCode"], "Template accepted both hosts disabled; see no-host.log")
        self.assertIn("host", text)
        self.assertTrue(any(word in text for word in ("required", "at least", "constraint")), text)
        self.assertEqual([], evidence["partialSolutions"], "Rejected generation left a partial solution")


if __name__ == "__main__":
    unittest.main(verbosity=2)
