# Change: Refresh onboarding docs and structure

## Why
The current README blends architecture deep dives, operational guides, and onboarding steps, which slows down new users trying to install and run the template.

## What Changes
- Create a concise, onboarding-focused root README with prerequisites, installation, configuration, and quick start flow for both stdio and HTTP hosts.
- Introduce a docs/ directory that holds deeper reference content (architecture, capabilities, operations) split into sectional markdown files.
- Add cross-links from the README to the appropriate docs/ sections to keep detailed material discoverable without crowding the landing page.

## Impact
- Affected specs: developer-docs
- Affected code: README.md, new docs/*.md