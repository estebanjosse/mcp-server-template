## 1. Upgrade framework and build pipeline

- [ ] 1.1 Update target frameworks and SDK settings to .NET 10 across solution and project files, then run the full test suite successfully.
- [ ] 1.2 Update centralized package/tooling versions (where required for .NET 10 compatibility) and validate restore/build passes in CI-equivalent commands.

## 2. Update containerization and docs

- [ ] 2.1 Update `Dockerfile` to .NET 10 SDK/runtime images and verify container build plus HTTP host startup checks succeed.
- [ ] 2.2 Add a `.NET 10` badge in the main `README.md` and verify rendered markdown displays the new badge correctly.

## 3. Operational compatibility follow-up

- [ ] 3.1 Create and document a manual process to keep a dedicated `.NET 8` maintenance branch in sync for critical fixes (manual task, not automated in this change).
