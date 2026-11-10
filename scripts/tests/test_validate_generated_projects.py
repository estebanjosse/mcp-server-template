"""Unit/negative-control tests for the generated-project validation harness.

These synthetic TRX fixtures test the validator only; they are never reported as
results from the real .NET applications. Run with Python's unittest discovery.
"""

import ctypes
import importlib.util
import json
import os
import pathlib
import signal
import time
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET

SCRIPT = pathlib.Path(__file__).resolve().parents[1] / "validate_generated_projects.py"
SPEC = importlib.util.spec_from_file_location("generated_validation", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
validation = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(validation)


class MatrixTests(unittest.TestCase):
    def test_matrix_covers_every_valid_host_test_sample_combination_once(self):
        cases = validation.matrix()
        self.assertEqual(12, len(cases))
        self.assertEqual(12, len({case["name"] for case in cases}))
        self.assertEqual(12, len({(c["http"], c["stdio"], c["tests"], c["samples"]) for c in cases}))
        self.assertTrue(all(c["http"] or c["stdio"] for c in cases))
        self.assertEqual(8, sum(c["http"] for c in cases))
        self.assertEqual(8, sum(c["stdio"] for c in cases))
        self.assertEqual(6, sum(c["tests"] for c in cases))


class SolutionInventoryTests(unittest.TestCase):
    def test_solution_requires_exact_unique_resolved_project_membership(self):
        with tempfile.TemporaryDirectory(prefix="mcp-validator-solution-") as directory:
            directory = pathlib.Path(directory)
            src = directory / "src"
            src.mkdir()
            projects = [src / "A.csproj", src / "B.csproj"]
            for project in projects:
                project.touch()
            (src / "Other.csproj").touch()
            solution = directory / "Example.slnx"
            cases = [
                (["src/A.csproj", "src/B.csproj"], True),
                (["src/../src/A.csproj", str(projects[1].resolve())], True),
                (["src/A.csproj", "src/A.csproj"], False),
                (["src/A.csproj", "src/../src/A.csproj"], False),
                (["src/A.csproj", "src/Other.csproj"], False),
                (["src/A.csproj", "src/Missing.csproj"], False),
                (["src/A.csproj"], False),
            ]
            for references, expected in cases:
                with self.subTest(references=references):
                    root = ET.Element("Solution")
                    folder = ET.SubElement(root, "Folder", {"Name": "/src/"})
                    for reference in references:
                        ET.SubElement(folder, "Project", {"Path": reference})
                    ET.ElementTree(root).write(solution, encoding="unicode")
                    self.assertEqual(expected, validation.solution_projects_match(solution, projects))


class TrxSummaryTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="mcp-validator-test-")
        self.addCleanup(self.temp.cleanup)
        self.directory = pathlib.Path(self.temp.name)

    def write_trx(self, filename="one.trx", storage="example.tests.dll", overrides=None):
        ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
        root = ET.Element(f"{{{ns}}}TestRun")
        summary = ET.SubElement(root, f"{{{ns}}}ResultSummary")
        counts = {"total": "1", "executed": "1", "passed": "1", "failed": "0",
                  "error": "0", "timeout": "0", "aborted": "0", "notExecuted": "0"}
        counts.update({key: str(value) for key, value in (overrides or {}).items()})
        ET.SubElement(summary, f"{{{ns}}}Counters", counts)
        definitions = ET.SubElement(root, f"{{{ns}}}TestDefinitions")
        ET.SubElement(definitions, f"{{{ns}}}UnitTest", {"storage": storage})
        ET.ElementTree(root).write(self.directory / filename, encoding="unicode")

    def test_complete_run_matches_adapter_lowercase_storage_name(self):
        self.write_trx()
        result = validation.trx_summary(self.directory, {"Example.Tests.dll"})
        self.assertTrue(result["complete"])
        self.assertEqual(1, result["totals"]["passed"])

    def test_empty_assembly_cannot_be_masked_by_another_assembly(self):
        self.write_trx()
        self.write_trx(filename="two.trx", storage="other.tests.dll",
                       overrides={"total": 0, "executed": 0, "passed": 0})
        result = validation.trx_summary(self.directory, {"Example.Tests.dll", "Other.Tests.dll"})
        self.assertEqual(1, result["totals"]["passed"])
        self.assertFalse(result["complete"], "a positive aggregate hid an empty expected assembly")

    def test_missing_results_cannot_pass(self):
        self.assertFalse(validation.trx_summary(self.directory, {"Example.Tests.dll"})["complete"])

    def test_missing_test_project_cannot_pass(self):
        self.write_trx()
        self.assertFalse(validation.trx_summary(self.directory, {"Example.Tests.dll", "Other.Tests.dll"})["complete"])

    def test_duplicate_assembly_reports_cannot_pass(self):
        self.write_trx()
        self.write_trx(filename="two.trx")
        self.assertFalse(validation.trx_summary(self.directory, {"Example.Tests.dll"})["complete"])

    def test_failing_skipped_aborted_or_inconsistent_counts_cannot_pass(self):
        for overrides in ({"passed": 0, "failed": 1}, {"executed": 0, "notExecuted": 1},
                          {"timeout": 1}, {"error": 1}, {"aborted": 1}, {"total": 0}, {"executed": 0}):
            with self.subTest(overrides=overrides):
                self.write_trx(overrides=overrides)
                self.assertFalse(validation.trx_summary(self.directory, {"Example.Tests.dll"})["complete"])

    def test_missing_counters_is_an_error_not_a_pass(self):
        (self.directory / "bad.trx").write_text('<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"/>')
        with self.assertRaises(ValueError):
            validation.trx_summary(self.directory, {"Example.Tests.dll"})


class CommandTests(unittest.TestCase):
    def test_nonzero_exit_and_output_are_preserved(self):
        with tempfile.TemporaryDirectory(prefix="mcp-validator-command-") as directory:
            directory = pathlib.Path(directory)
            log = directory / "command.log"
            result = validation.invoke([sys.executable, "-c", "print('negative control'); raise SystemExit(7)"], directory, log, 5)
            self.assertEqual(7, result["exitCode"])
            self.assertIn("negative control", log.read_text())

    @unittest.skipUnless(sys.platform == "linux", "Linux subreaper makes RED cleanup deterministic")
    def test_timeout_cleans_owned_parent_and_grandchild(self):
        # Adopt and reap the controlled orphan even when the old invoke leaks it.
        libc = ctypes.CDLL(None, use_errno=True)
        previous = ctypes.c_int()
        self.assertEqual(0, libc.prctl(37, ctypes.byref(previous), 0, 0, 0))
        self.assertEqual(0, libc.prctl(36, 1, 0, 0, 0))
        pids = []
        try:
            with tempfile.TemporaryDirectory(prefix="mcp-validator-tree-") as directory:
                directory = pathlib.Path(directory)
                pid_file = directory / "pids.json"
                child_code = "import signal,time; signal.signal(signal.SIGTERM, signal.SIG_IGN); print('grandchild ready', flush=True); time.sleep(60)"
                parent_code = (
                    "import json,os,pathlib,signal,subprocess,sys,time; "
                    "signal.signal(signal.SIGTERM, signal.SIG_IGN); "
                    "child=subprocess.Popen([sys.executable,'-B','-c',sys.argv[2]]); "
                    "pathlib.Path(sys.argv[1]).write_text(json.dumps([os.getpid(),child.pid])); "
                    "print('parent ready',flush=True); time.sleep(60)"
                )
                started = time.monotonic()
                try:
                    result = validation.invoke(
                        [sys.executable, "-B", "-c", parent_code, str(pid_file), child_code],
                        directory, directory / "command.log", 1)
                    pids = json.loads(pid_file.read_text())
                    deadline = time.monotonic() + 2
                    while True:
                        alive = []
                        for pid in pids:
                            try:
                                os.waitpid(pid, os.WNOHANG)
                            except ChildProcessError:
                                pass
                            try:
                                os.kill(pid, 0)
                                alive.append(pid)
                            except ProcessLookupError:
                                pass
                        if not alive or time.monotonic() >= deadline:
                            break
                        time.sleep(0.01)
                    print(f"timeout proof: owned={pids}, survivors={alive}, exit={result['exitCode']}", file=sys.stderr)
                    self.assertEqual(124, result["exitCode"])
                    self.assertLess(time.monotonic() - started, 8)
                    output = (directory / "command.log").read_text()
                    print(f"preserved child log:\n{output}", end="", file=sys.stderr)
                    self.assertIn("parent ready", output)
                    self.assertIn("grandchild ready", output)
                    self.assertEqual([], alive, "invoke leaked an owned descendant")
                finally:
                    if pid_file.exists():
                        pids = json.loads(pid_file.read_text())
                    for pid in pids:
                        try:
                            os.kill(pid, signal.SIGKILL)
                        except ProcessLookupError:
                            pass
                    for pid in pids:
                        try:
                            os.waitpid(pid, 0)
                        except ChildProcessError:
                            pass
                    for pid in pids:
                        with self.assertRaises(ProcessLookupError):
                            os.kill(pid, 0)
                    print(f"test-finally cleanup: owned={pids}, survivors=[]", file=sys.stderr)
        finally:
            self.assertEqual(0, libc.prctl(36, previous.value, 0, 0, 0))

    def test_timeout_is_not_a_success(self):
        with tempfile.TemporaryDirectory(prefix="mcp-validator-timeout-") as directory:
            directory = pathlib.Path(directory)
            result = validation.invoke([sys.executable, "-c", "import time; time.sleep(30)"], directory, directory / "command.log", 1)
            self.assertEqual(124, result["exitCode"])


if __name__ == "__main__":
    unittest.main()
