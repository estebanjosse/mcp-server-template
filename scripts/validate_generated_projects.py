#!/usr/bin/env python3
"""Validate the actual packed template in an isolated template-engine hive.

Requires Python 3.11+ and the repository's .NET SDK. Output must be a new directory
outside the repository. No files are removed; JSON, logs and TRX results are kept
there. This is a characterization harness, not a migration or an application fix.
"""

from __future__ import annotations

import argparse
import hashlib
import itertools
import json
import pathlib
import subprocess
import time
import xml.etree.ElementTree as ET
import zipfile
from typing import Any

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def invoke(command: list[str], cwd: pathlib.Path, log: pathlib.Path, timeout: int) -> dict:
    started = time.monotonic()
    log.parent.mkdir(parents=True, exist_ok=True)
    with log.open("w") as stream:
        try:
            process = subprocess.run(command, cwd=cwd, stdout=stream,
                                     stderr=subprocess.STDOUT, timeout=timeout)
            code = process.returncode
        except subprocess.TimeoutExpired:
            code = 124
    return {"command": command, "exitCode": code,
            "seconds": round(time.monotonic() - started, 2), "log": str(log)}


def matrix() -> list[dict]:
    return [
        {"name": f"{host}-tests{int(tests)}-samples{int(samples)}",
         "http": host in ("http", "both"), "stdio": host in ("stdio", "both"),
         "tests": tests, "samples": samples,
         "projectName": f"Baseline.{host.title()}Tests{int(tests)}Samples{int(samples)}"}
        for host, tests, samples in itertools.product(
            ("http", "stdio", "both"), (False, True), (False, True))
    ]


def trx_summary(directory: pathlib.Path, expected: set[str]) -> dict:
    files = sorted(directory.glob("*.trx"))
    assemblies = []
    totals = {key: 0 for key in ("total", "executed", "passed", "failed", "error", "timeout", "aborted", "notExecuted")}
    for file in files:
        tree = ET.parse(file)
        counters = tree.find(".//t:Counters", NS)
        definitions = tree.findall(".//t:TestDefinitions/t:UnitTest", NS)
        # The TRX adapter lowercases storage paths; do not change real paths.
        names = {pathlib.PurePosixPath(test.attrib["storage"].replace("\\", "/")).name
                 for test in definitions}
        if counters is None or len(names) != 1:
            raise ValueError(f"Missing counters or ambiguous test assembly in {file}")
        assembly = next(iter(names))
        numbers = {key: int(counters.attrib[key]) for key in totals}
        assemblies.append({"storageName": assembly, "file": str(file), **numbers})
        for key in totals:
            totals[key] += numbers[key]
    actual = [item["storageName"].casefold() for item in assemblies]
    complete = (set(actual) == {name.casefold() for name in expected}
                and len(actual) == len(expected) and len(set(actual)) == len(actual)
                and totals["total"] > 0
                and totals["executed"] == totals["passed"] == totals["total"]
                and all(totals[key] == 0 for key in ("failed", "error", "timeout", "aborted", "notExecuted")))
    return {"assemblies": assemblies, "totals": totals, "complete": complete}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=pathlib.Path, default=pathlib.Path(__file__).resolve().parents[1])
    parser.add_argument("--output", type=pathlib.Path, required=True)
    parser.add_argument("--timeout", type=int, default=240, help="Per-command timeout in seconds")
    args = parser.parse_args()
    repo, output = args.repo.resolve(), args.output.resolve()
    if args.timeout <= 0:
        parser.error("--timeout must be positive")
    if output == repo or output.is_relative_to(repo):
        parser.error("--output must be outside the repository")
    if output.exists():
        parser.error("--output must be a new directory; existing output is never deleted")
    if not (repo / "McpServer.Template.csproj").is_file():
        parser.error("--repo does not contain the template packaging project")
    output.mkdir(parents=True)
    report: dict[str, Any] = {"scope": "pre-migration packed-template characterization", "steps": [],
              "cases": [], "expectedValidCases": 12, "noHost": None}

    def save() -> None:
        (output / "results.json").write_text(json.dumps(report, indent=2) + "\n")

    report["commit"] = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=repo, text=True).strip()
    report["sdk"] = subprocess.check_output(["dotnet", "--version"], cwd=repo, text=True).strip()
    report["harnessSha256"] = hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest()
    save()
    pack = invoke(["dotnet", "pack", str(repo / "McpServer.Template.csproj"),
                   "--configuration", "Release", "--output", str(output / "packages"),
                   "--artifacts-path", str(output / "pack-artifacts"),
                   "-p:Version=0.0.0-baseline", "--disable-build-servers"],
                  repo, output / "pack.log", args.timeout)
    report["steps"].append({"name": "pack", **pack})
    save()
    if pack["exitCode"]:
        print(json.dumps(report, indent=2))
        return 1
    packages = list((output / "packages").glob("*.nupkg"))
    if len(packages) != 1:
        report["error"] = "Expected exactly one packed template"
        save()
        return 1
    package = packages[0]
    report["package"] = {"path": str(package), "sha256": hashlib.sha256(package.read_bytes()).hexdigest()}
    with zipfile.ZipFile(package) as archive:
        inventory = sorted(archive.namelist())
    (output / "package-inventory.json").write_text(json.dumps(inventory, indent=2) + "\n")
    report["package"]["containsGeneratedCiWorkflow"] = "content/.github/workflows/ci.yml" in inventory
    hive = output / "template-hive"
    install = invoke(["dotnet", "new", "install", str(package), "--debug:custom-hive", str(hive)],
                     repo, output / "install.log", args.timeout)
    report["steps"].append({"name": "install", **install})
    save()
    if install["exitCode"]:
        print(json.dumps(report, indent=2))
        return 1
    for case in matrix():
        directory = output / "generated" / case["name"]
        logs = output / "logs" / case["name"]
        item = {**case, "directory": str(directory), "steps": [], "checks": [], "passed": False}
        report["cases"].append(item)
        flags = ["--http-host", str(case["http"]).lower(), "--stdio-host", str(case["stdio"]).lower(),
                 "--include-tests", str(case["tests"]).lower(),
                 "--include-sample-tools", str(case["samples"]).lower()]
        generation = invoke(["dotnet", "new", "mcp-server", "--name", case["projectName"],
                             "--output", str(directory), "--debug:custom-hive", str(hive), *flags],
                            repo, logs / "generate.log", args.timeout)
        item["steps"].append({"name": "generate", **generation})
        if generation["exitCode"]:
            save()
            print(case["name"], "FAIL generation", flush=True)
            continue
        projects = sorted(directory.glob("**/*.csproj"))
        names = {project.stem for project in projects}
        name = case["projectName"]
        for suffix, expected in ((".Host.Http", case["http"]), (".Host.Stdio", case["stdio"]),
                                 (".Application.Tests", case["tests"]),
                                 (".Host.Http.Tests", case["tests"] and case["http"]),
                                 (".Infrastructure.Tests", case["tests"]), (".Mcp.Tests", case["tests"])):
            item["checks"].append({"name": f"inventory:{suffix}", "passed": ((name + suffix) in names) == expected})
        for tool in ("EchoTool.cs", "CalcDivideTool.cs"):
            exists = (directory / "src" / f"{name}.Mcp" / "Tools" / tool).is_file()
            item["checks"].append({"name": f"inventory:{tool}", "passed": exists == case["samples"]})
        solutions = list(directory.glob("*.slnx"))
        valid_solution = len(solutions) == 1
        if valid_solution:
            tree = ET.parse(solutions[0])
            references = tree.findall(".//Project")
            valid_solution = (len(references) == len(projects)
                              and all((directory / ref.attrib["Path"]).is_file() for ref in references))
        item["checks"].append({"name": "solution-project-references", "passed": valid_solution})
        item["observations"] = {
            "generatedCiWorkflowPresent": (directory / ".github/workflows/ci.yml").is_file(),
            "dockerfilePresent": (directory / "Dockerfile").is_file(),
            "dockerfileReferencesAbsentHttpHost": (directory / "Dockerfile").is_file() and not case["http"],
        }
        if not valid_solution:
            save()
            print(case["name"], "FAIL solution inventory", flush=True)
            continue
        build = invoke(["dotnet", "build", str(solutions[0]), "--configuration", "Release",
                        "--disable-build-servers", "--maxcpucount:1", "-p:UseSharedCompilation=false"],
                       directory, logs / "build.log", args.timeout)
        item["steps"].append({"name": "build", **build})
        if not build["exitCode"] and case["tests"]:
            tests_dir = output / "test-results" / case["name"]
            test = invoke(["dotnet", "test", str(solutions[0]), "--configuration", "Release",
                           "--no-build", "--no-restore", "--maxcpucount:1", "--logger", "trx",
                           "--results-directory", str(tests_dir)], directory, logs / "test.log", args.timeout)
            item["steps"].append({"name": "test", **test})
            try:
                summary = trx_summary(tests_dir, {project.stem + ".dll" for project in projects if project.stem.endswith(".Tests")})
                item["testResults"] = summary
                item["checks"].append({"name": "complete-test-execution", "passed": summary["complete"]})
            except (ET.ParseError, ValueError, KeyError) as error:
                item["checks"].append({"name": "complete-test-execution", "passed": False, "error": str(error)})
        item["passed"] = (all(step["exitCode"] == 0 for step in item["steps"])
                          and all(check["passed"] for check in item["checks"]))
        save()
        print(case["name"], "PASS" if item["passed"] else "FAIL", flush=True)
    # Documentation says at least one host is required. Probe actual enforcement.
    no_host = invoke(["dotnet", "new", "mcp-server", "--name", "Baseline.NoHost",
                      "--output", str(output / "generated/no-host"), "--debug:custom-hive", str(hive),
                      "--http-host", "false", "--stdio-host", "false"],
                     repo, output / "logs/no-host.log", args.timeout)
    text = (output / "logs/no-host.log").read_text(errors="replace").lower()
    rejected = (no_host["exitCode"] not in (0, 124) and "host" in text
                and any(word in text for word in ("required", "at least", "constraint")))
    report["noHost"] = {**no_host, "expectedRejection": True, "passed": rejected}
    report["summary"] = {"validCases": len(report["cases"]),
                         "validPassed": sum(case["passed"] for case in report["cases"]),
                         "validFailed": sum(not case["passed"] for case in report["cases"]),
                         "testsPassed": sum(case.get("testResults", {}).get("totals", {}).get("passed", 0) for case in report["cases"]),
                         "noHostRejected": rejected}
    report["passed"] = (len(report["cases"]) == report["expectedValidCases"]
                        and all(case["passed"] for case in report["cases"]) and rejected)
    save()
    print(json.dumps(report["summary"], indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
