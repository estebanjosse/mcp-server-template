## ADDED Requirements

### Requirement: Optional migration with a simple main path

Migration of existing generated projects SHALL be optional. The deliverable SHALL be a simple English step-by-step guide, not an automatic updater or a commitment to maintain existing deployments. The main path SHALL cover files, commands, validation and rollback in plain language; advanced cases SHALL be separate.

#### Scenario: Owner does not choose to migrate
- **GIVEN** an owner has an application generated from an older template
- **WHEN** a newer template is released or installed
- **THEN** their application SHALL NOT be modified by the template update
- **AND** the guide SHALL state that migration is optional without promising maintenance or security coverage for the older application.

#### Scenario: Owner chooses to migrate
- **GIVEN** an owner decides to upgrade their generated application
- **WHEN** they open the migration guide
- **THEN** they SHALL find a short ordered path covering backup, dependency changes, host changes, validation and rollback
- **AND** advanced session/client compatibility cases SHALL be linked separately rather than imposed on every reader.

### Requirement: Non-destructive existing-project migration

The project SHALL publish English migration guidance distinguishing template-package updates, SDK-package updates and protocol negotiation. It SHALL NOT imply that installing a new template updates already generated source.

#### Scenario: Existing generated application
- **GIVEN** a user owns a customized application generated with SDK 1.3.0
- **WHEN** they follow the migration guide
- **THEN** they SHALL be instructed to checkpoint their application, inventory custom protocol features, align dependencies and choose a transport compatibility policy
- **AND** preserve their own application code, with any fresh-scaffold comparison performed optionally in a separate directory rather than overwriting the application.

### Requirement: Compatibility and rollback instructions

The simple guide SHALL cover validation, rollback and known validation limits. A linked advanced reference SHALL describe modern, hybrid and down-level-only trade-offs and conditional breaking changes for owners who need them. Hybrid mode SHALL NOT be presented as mandatory for all migrated applications.

#### Scenario: Session-dependent deployment
- **GIVEN** an owner voluntarily migrates a deployment and chooses to retain clients depending on legacy HTTP sessions
- **WHEN** they consult the optional advanced compatibility guidance
- **THEN** the guide SHALL distinguish hybrid compatibility from stateful-only operation
- **AND** require testing the owner's actual client/proxy/authentication configuration before rollout.

### Requirement: Deferred feature showcases

New feature examples SHALL be separately scoped and approved, label protocol capabilities versus optional/experimental extensions, and preserve a usable minimal scaffold.

#### Scenario: Feature roadmap
- **GIVEN** the protocol migration is being reviewed
- **WHEN** Tasks, Apps, Skills or MRTR examples are proposed
- **THEN** they SHALL remain separate future work unless explicitly approved
- **AND** experimental APIs SHALL NOT be presented as required stable core protocol functionality.
