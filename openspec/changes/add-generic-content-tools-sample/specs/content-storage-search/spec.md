## ADDED Requirements

### Requirement: Text documents can be saved with stable identifiers
The system MUST allow clients to persist plain text content by supplying a stable `document_id` and text `content`, and the server MUST manage the physical storage location internally.

#### Scenario: Save a new text document
- **WHEN** a client invokes `save_text` with a valid `document_id` and non-empty `content`
- **THEN** the system stores the document successfully
- **AND** the result includes the `document_id`
- **AND** the result includes persisted size metadata

#### Scenario: Reject duplicate save when overwrite is disabled
- **WHEN** a client invokes `save_text` for an existing `document_id` without enabling overwrite
- **THEN** the system rejects the request with a conflict-style error
- **AND** the existing stored content remains unchanged

#### Scenario: Overwrite an existing text document
- **WHEN** a client invokes `save_text` for an existing `document_id` with overwrite enabled
- **THEN** the system replaces the stored content
- **AND** the result indicates the updated document metadata

### Requirement: Text document access is bounded by server-managed identifiers
The system MUST treat `document_id` as the only public locator for stored text and MUST NOT require or expose arbitrary filesystem paths in tool contracts.

#### Scenario: Reject invalid document identifier
- **WHEN** a client invokes `save_text` or `read_text` with a malformed or unsafe `document_id`
- **THEN** the system rejects the request with a validation error

#### Scenario: Read a stored text document
- **WHEN** a client invokes `read_text` with an existing `document_id`
- **THEN** the system returns the stored `content`
- **AND** the result includes size and last-updated metadata

#### Scenario: Reading an unknown document fails clearly
- **WHEN** a client invokes `read_text` with a `document_id` that does not exist
- **THEN** the system rejects the request with a not-found error

### Requirement: Stored text documents can be searched with simple full-text behavior
The system MUST allow clients to search across stored text documents using substring matching without requiring an external search engine.

#### Scenario: Search finds matching documents
- **WHEN** a client invokes `search_text` with a query that appears in one or more stored documents
- **THEN** the system returns matching `document_id` values
- **AND** each match includes a text snippet
- **AND** each match includes an occurrence count

#### Scenario: Search returns no matches when query is absent
- **WHEN** a client invokes `search_text` with a query that appears in no stored document
- **THEN** the system returns an empty match set

#### Scenario: Search result count is bounded
- **WHEN** a client invokes `search_text` with a limit smaller than the total number of matches
- **THEN** the system returns no more than the requested number of matches

### Requirement: Search behavior remains predictable for a sample implementation
The system MUST keep search semantics simple and explicit so the sample remains understandable and testable.

#### Scenario: Case-insensitive search by default
- **WHEN** a client invokes `search_text` without enabling case-sensitive matching
- **THEN** the system matches documents regardless of letter casing

#### Scenario: Search is scoped to persisted sample documents
- **WHEN** a client invokes `search_text`
- **THEN** the system searches only documents managed by the server's text storage capability
- **AND** the system does not scan arbitrary files outside the managed storage root