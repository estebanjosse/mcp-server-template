## 1. Study checkpoint — implementation not approved

- [x] 1.1 Verify dated stable protocol/SDK targets against official sources and published package metadata.
- [x] 1.2 Inspect current source and map behavioral, dependency and generated-project impact.
- [x] 1.3 Draft the compatibility decisions, user migration guide and deferred showcase plan in English.
- [ ] 1.4 Obtain Esteban's decision on HTTP compatibility policy and implementation scope.

## 2. Authorized implementation only — blocked on 1.4

- [ ] 2.1 Establish a runnable baseline and reproduce any stdio startup/tool-registration blocker before changing dependencies.
- [ ] 2.2 Reconcile affected existing specs and implement approved regression tests for modern/down-level behavior.
- [ ] 2.3 Upgrade aligned MCP packages and reconcile direct/transitive dependency floors without unrelated updates.
- [ ] 2.4 Explicitly configure the approved HTTP mode; preserve business services and fix only demonstrated migration blockers.
- [ ] 2.5 Adapt tests and any custom result/cache/error handling; qualify both transports and authentication modes.

## 3. Consumer and release qualification — no publication authorized

- [ ] 3.1 Pack and install the candidate template in an isolated environment and validate representative flag combinations.
- [ ] 3.2 Rehearse the documented migration on an application generated from published template 1.4.0.
- [ ] 3.3 Verify actual client/proxy scenarios, rollback and timeout/cancellation behavior.
- [ ] 3.4 Finalize migration instructions, release notes, public compatibility matrix and required existing-spec deltas.
- [ ] 3.5 Review the exact implementation head and CI; obtain separate merge/release authorization before archival or publication.

## 4. Separate future work

- [ ] 4.1 Obtain scope approval for accessible opt-in examples of new protocol functionality and separate extensions.
