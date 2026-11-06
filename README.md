# docx-template-studio

A web app that fills your own Word templates with data from the app.

Upload `.docx` templates where fields are marked with Word content controls. From a record (a contract, a training agreement, a mission order), pick a template and get the filled document as Word or PDF, with your layout and branding kept.

## Who it is for

Small businesses that keep retyping the same information into their branded documents.

## Stack

- Front end: Angular workspace (app + libraries)
- Back end: ASP.NET Core Web API (C#), Open XML SDK
- Database: SQLite with EF Core
- PDF conversion: LibreOffice

## Status

Early development (milestone M0). What works today:

- Create an account, log in, change your password. Sessions expire after 60 minutes.
- Search, create, edit and delete employment contracts, 20 per page.
- Upload `.docx` templates for a module (10 MB max). The app lists the field keys it finds: the tags of the Word content controls in the body, headers and footers. Templates can be filtered by module, downloaded and deleted.

Filling a template from a record and exporting to Word or PDF are planned (see [ROADMAP.md](ROADMAP.md)).

## Getting started

### Prerequisites

- [.NET SDK 10.0.403](https://dotnet.microsoft.com/download) or a later 10.0 feature band
- [Node.js 24](https://nodejs.org/) with npm 11

### Run the API

```bash
cd src/backend
dotnet run --project src/DocxTemplateStudio.Api --launch-profile http
```

The API listens on http://localhost:5082. In Development it creates the SQLite database in `src/backend/src/DocxTemplateStudio.Api/App_Data/`, applies the migrations, and adds the demo data below when the database has no users. Uploaded templates are stored in `App_Data/files`.

The access tokens are signed with a key that is never stored in the repository. In Development, a random key is generated at startup, so you need to log in again after each restart. In any other environment, set a base64 key of at least 32 bytes:

```bash
export Jwt__SigningKey="$(openssl rand -base64 32)"
```

### Run the web app

```bash
cd src/frontend
npm install
npm start
```

Open http://localhost:4200. Calls to `/api` are sent to the API on port 5082.

### Demo data

All demo accounts use the password `TestDemo123!`. They are fictional and only exist in Development.

| Name | Email |
|------|-------|
| Julie Lambert | julie.lambert@example.com |
| Thomas Girard | thomas.girard@example.com |
| Sarah Fontaine | sarah.fontaine@example.com |
| Hugo Lefevre | hugo.lefevre@example.com |
| Ines Garnier | ines.garnier@example.com |

The demo also holds eight fictional employment contracts (permanent, fixed-term, internship and apprenticeship). No templates are included: create one in Word, add content controls (Developer tab), give each one a tag such as `employee.firstName`, and upload it.

To start again from an empty demo, stop the API and delete `src/backend/src/DocxTemplateStudio.Api/App_Data/`.

### Run the tests

Back end (with coverage, migrations and generated code excluded):

```bash
cd src/backend
dotnet test --settings coverage.runsettings --collect "XPlat Code Coverage"
```

Front end (Vitest, the run fails below 100% coverage):

```bash
cd src/frontend
npm run test:ci
```

## Project layout

```
src/backend/    ASP.NET Core solution: Domain, Application.Contracts, Application, Infrastructure, Api, and their test projects
src/frontend/   Angular workspace: projects/apps/docx-template-studio-app and the shared libraries in projects/libs/shared
```

## License

[MIT](LICENSE)
