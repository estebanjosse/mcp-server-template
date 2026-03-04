## 1. Upgrade framework and build pipeline

- [x] 1.1 Update target frameworks and SDK settings to .NET 10 across solution and project files, then run the full test suite successfully.
- [x] 1.2 Update centralized package/tooling versions (where required for .NET 10 compatibility) and validate restore/build passes in CI-equivalent commands.

## 2. Update containerization and docs

- [x] 2.1 Update `Dockerfile` to .NET 10 SDK/runtime images and verify container build plus HTTP host startup checks succeed.
- [x] 2.2 Add a `.NET 10` badge in the main `README.md` and verify rendered markdown displays the new badge correctly.

