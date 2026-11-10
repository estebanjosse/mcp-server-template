## ADDED Requirements

### Requirement: Non-destructive existing-project migration

The project SHALL publish English migration guidance distinguishing template-package updates, SDK-package updates and protocol negotiation. It SHALL NOT imply that installing a new template updates already generated source.

#### Scenario: Existing generated application
- **GIVEN** a user owns a customized application generated with SDK 1.3.0
- **WHEN** they follow the migration guide
- **THEN** they SHALL be instructed to checkpoint their application, inventory custom protocol features, align dependencies and choose a transport compatibility policy
- **AND** compare a fresh scaffold outside their working directory rather than overwrite their application.

### Requirement: Compatibility and rollback instructions

The guide SHALL describe modern, hybrid and down-level-only trade-offs, conditional breaking changes in customized applications, verification and rollback, and known validation limits.

#### Scenario: Session-dependent deployment
- **GIVEN** an existing deployment depends on legacy HTTP sessions
- **WHEN** its owner selects a transition path
- **THEN** the guide SHALL distinguish hybrid compatibility from stateful-only operation
- **AND** require testing the owner's actual client/proxy/authentication configuration before rollout.

### Requirement: Deferred feature showcases

New feature examples SHALL be separately scoped and approved, label protocol capabilities versus optional/experimental extensions, and preserve a usable minimal scaffold.

#### Scenario: Feature roadmap
- **GIVEN** the protocol migration is being reviewed
- **WHEN** Tasks, Apps, Skills or MRTR examples are proposed
- **THEN** they SHALL remain separate future work unless explicitly approved
- **AND** experimental APIs SHALL NOT be presented as required stable core protocol functionality.
