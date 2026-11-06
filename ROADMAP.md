# Roadmap

This is the working plan for docx-template-studio. Stories are grouped by epic and tagged with the milestone they belong to.

## Scope

In scope:
- Uploading Word templates with fields marked by content controls.
- Filling templates from records of three demo modules.
- Generating Word or PDF output that keeps the template's layout.

Out of scope:
- Not a document editor. Templates are designed in Word (or any editor that saves `.docx`).
- Not a full HR or training management tool. The three business modules are simple demos that feed the template engine.
- No real user data. The demo runs on fictional accounts, records and templates.

## Known limit

Word output keeps the template exactly. PDF output is produced by LibreOffice: very close to Word, but complex layouts can differ slightly. This is documented, not hidden.

## Glossary

| Term | Meaning |
|------|---------|
| Template | A `.docx` file with fields marked by content controls |
| Field key | The tag of a content control, e.g. `employee.lastName` |
| Module | A business area: Contracts, Training agreements, Mission orders |
| Record | One item in a module, e.g. one contract |
| Field catalog | The list of keys a module can fill, with label, type and example |
| Generation | Filling a template with a record, as Word or PDF |

## Milestones

| Milestone | Goal |
|-----------|------|
| M0 Foundation | Runnable app: accounts, demo data, contracts CRUD, template upload with key detection |
| M1 Generation | Fill content controls, Word output, "Generate document" in the UI |
| M2 PDF and modules | PDF output through LibreOffice, training agreements and mission orders modules, repeating sections |
| M3 Template quality | Key checks against the catalog, versions, generation history, format rules |
| M4 Quality | CI, tests, accessibility, docs |

---

## EPIC 1 — Project setup

| ID | Story | Milestone |
|----|-------|-----------|
| SETUP-1 | Repository with README, LICENSE (MIT), .gitignore, .editorconfig | M0 |
| SETUP-2 | .NET solution: Api, Domain, Application, Infrastructure and Tests projects | M0 |
| SETUP-3 | SQLite + EF Core, first migration, database created on startup in dev | M0 |
| SETUP-4 | Angular workspace with the main app and shared libraries | M0 |
| SETUP-5 | GitHub Actions: build and test both API and front end on each push | M4 |
| SETUP-6 | The API finds LibreOffice on startup and reports clearly when it is missing | M2 |

## EPIC 2 — Accounts

| ID | Story | Milestone |
|----|-------|-----------|
| AUTH-1 | As a visitor, I create an account with name, email and password | M0 |
| AUTH-2 | As a user, I log in and get a token | M0 |
| AUTH-3 | As a user, I log out, and I am sent back to login when my session expires | M0 |
| AUTH-4 | As a user, I change my password with current password, new password and confirmation | M0 |
| AUTH-5 | Demo accounts are seeded with a documented demo password | M0 |

## EPIC 3 — Template library

| ID | Story | Milestone |
|----|-------|-----------|
| TPL-1 | As a user, I upload a `.docx` template and attach it to a module. Other files are rejected | M0 |
| TPL-2 | On upload, the app lists the field keys found in the template | M0 |
| TPL-3 | As a user, I list, download and delete templates per module | M0 |
| TPL-4 | Keys unknown to the module's catalog are flagged, with a suggestion when a close key exists | M3 |
| TPL-5 | As a user, I replace a template with a new version and keep the old one | M3 |
| TPL-6 | Sample branded templates for each module ship with the repo | M1 |

## EPIC 4 — Field catalog

| ID | Story | Milestone |
|----|-------|-----------|
| CAT-1 | Each module exposes its field catalog: key, label, type, example | M1 |
| CAT-2 | As a user, I browse a module's catalog and copy a key to tag my template | M1 |
| CAT-3 | Dates, amounts and numbers are formatted with a configurable culture | M3 |

## EPIC 5 — Contracts module

| ID | Story | Milestone |
|----|-------|-----------|
| CONTRACT-1 | As a user, I list contracts with search and paging | M0 |
| CONTRACT-2 | As a user, I create and edit a contract in a form (employee, position, type, start date, salary) | M0 |
| CONTRACT-3 | As a user, I delete a contract | M0 |

## EPIC 6 — Training agreements module

| ID | Story | Milestone |
|----|-------|-----------|
| TRAIN-1 | As a user, I list training agreements | M2 |
| TRAIN-2 | As a user, I create and edit an agreement (client company, course title, dates, hours, price, trainees) | M2 |
| TRAIN-3 | As a user, I delete an agreement | M2 |

## EPIC 7 — Mission orders module

| ID | Story | Milestone |
|----|-------|-----------|
| MISSION-1 | As a user, I list mission orders | M2 |
| MISSION-2 | As a user, I create and edit a mission order (employee, destination, purpose, dates, transport, expenses) | M2 |
| MISSION-3 | As a user, I delete a mission order | M2 |

## EPIC 8 — Generation engine

| ID | Story | Milestone |
|----|-------|-----------|
| GEN-1 | Text content controls are filled with the record's values | M1 |
| GEN-2 | Filled text keeps the font and style of the content control | M1 |
| GEN-3 | Repeating section controls are filled from lists (trainees, expenses) | M2 |
| GEN-4 | Missing values follow a setting: leave empty, keep the placeholder, or fail | M3 |
| GEN-5 | Fields in headers, footers and tables are filled too | M1 |

## EPIC 9 — Output and history

| ID | Story | Milestone |
|----|-------|-----------|
| OUT-1 | As a user, I download the generated document as Word | M1 |
| OUT-2 | As a user, I download the generated document as PDF | M2 |
| OUT-3 | PDF conversion goes through an interface. The first implementation runs local LibreOffice | M2 |
| OUT-4 | As a user, I see the documents generated for a record and download them again | M3 |

## EPIC 10 — Generate document flow

| ID | Story | Milestone |
|----|-------|-----------|
| UI-1 | Each list row has "Edit" and "Generate document" buttons | M1 |
| UI-2 | "Generate document" opens a dialog: pick a template of this module, pick Word or PDF, generate and download | M1 |

## EPIC 11 — Quality and docs

| ID | Story | Milestone |
|----|-------|-----------|
| QA-1 | Engine tests on sample templates: every key filled, styles kept, headers and tables covered | M4 |
| QA-2 | API integration tests for upload and generation | M4 |
| QA-3 | Unit tests for key Angular components and services | M4 |
| QA-4 | End-to-end test: create a contract, generate Word and PDF (Playwright) | M4 |
| QA-5 | Docs: how to tag a Word template with content controls, how to install LibreOffice, demo accounts | M4 |

---

## Later ideas

- Gotenberg converter for Docker-based deployments
- `.odt` templates for LibreOffice users
- Support for Word merge fields (`MERGEFIELD`)
- Conditional blocks (show a paragraph only if a value is set)
- Batch generation for several records
- Live preview with sample data
