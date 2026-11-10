"""Unit/negative-control tests for the generated-project validation harness.

These synthetic TRX fixtures test the validator only; they are never reported as
results from the real .NET applications. Run with Python's unittest discovery.
"""

import importlib.util
import pathlib
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

    def test_timeout_is_not_a_success(self):
        with tempfile.TemporaryDirectory(prefix="mcp-validator-timeout-") as directory:
            directory = pathlib.Path(directory)
            result = validation.invoke([sys.executable, "-c", "import time; time.sleep(30)"], directory, directory / "command.log", 1)
            self.assertEqual(124, result["exitCode"])


if __name__ == "__main__":
    unittest.main()
