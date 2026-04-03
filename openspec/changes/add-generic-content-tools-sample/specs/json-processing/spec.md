## ADDED Requirements

### Requirement: JSON payloads can be validated against a client-supplied schema
The system MUST provide a `validate_json_schema` tool that validates a JSON payload against a supplied JSON Schema and returns an explicit validity result.

#### Scenario: JSON payload satisfies schema
- **WHEN** a client invokes `validate_json_schema` with a JSON payload that conforms to the supplied schema
- **THEN** the system returns `valid = true`
- **AND** the system returns no validation errors

#### Scenario: JSON payload violates schema
- **WHEN** a client invokes `validate_json_schema` with a JSON payload that does not conform to the supplied schema
- **THEN** the system returns `valid = false`
- **AND** the system returns one or more validation errors with machine-usable location information

#### Scenario: Invalid schema input is rejected
- **WHEN** a client invokes `validate_json_schema` with a malformed or unsupported schema
- **THEN** the system rejects the request with a validation error

### Requirement: JSON transformation remains intentionally bounded
The system MUST provide a `transform_json` tool that performs deterministic JSON reshaping without becoming a general-purpose transformation language.

#### Scenario: Project selected fields into a new object
- **WHEN** a client invokes `transform_json` with operation `project` and a field map referencing source JSON locations
- **THEN** the system returns a new JSON object containing the requested output fields

#### Scenario: Reject unsupported transformation operation
- **WHEN** a client invokes `transform_json` with an operation other than the supported set
- **THEN** the system rejects the request with a validation error

#### Scenario: Reject invalid projection mapping
- **WHEN** a client invokes `transform_json` with a projection map that cannot be resolved against the input JSON
- **THEN** the system rejects the request with a clear transformation error

### Requirement: Validation and transformation are separate capabilities
The system MUST keep schema validation and JSON transformation as separate tools with separate contracts and outcomes.

#### Scenario: Validation does not alter input content
- **WHEN** a client invokes `validate_json_schema`
- **THEN** the system returns only validation results
- **AND** the system does not return a transformed payload

#### Scenario: Transformation does not imply schema validation
- **WHEN** a client invokes `transform_json`
- **THEN** the system applies only the requested transformation behavior
- **AND** the system does not require a JSON Schema parameter

### Requirement: JSON tool errors are explicit and testable
The system MUST report invalid JSON, invalid schema, and unsupported transformation requests as distinct failure cases.

#### Scenario: Invalid JSON payload is rejected during validation
- **WHEN** a client invokes `validate_json_schema` with malformed JSON
- **THEN** the system rejects the request with a parse or validation error

#### Scenario: Invalid JSON payload is rejected during transformation
- **WHEN** a client invokes `transform_json` with malformed JSON
- **THEN** the system rejects the request with a parse or transformation error