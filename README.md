# CareerSync — Complete MVC Scope

ASP.NET Core 8 MVC + SQL Server + Repository Pattern + REST API.

## Core scope
- Graduate and Admin roles with separate dashboards.
- Graduate registration using any valid email address.
- ccount Recovery using Security Questions or OTP using Twilio
- Secure cookie login and login audit.
- CV upload: PDF, DOCX, TXT (5 MB limit).
- Job-description input and Analyse Match workflow.
- Data-driven matching algorithm for Computer Science and IT roles, including software development, cloud engineering, DevOps, cybersecurity, networking, databases, data/AI and related professional skills.
- Match score with weighted components:Technical skills 50%, qualifications 25%, experience 25%.
- Matched, missing and weak skill analysis plus learning recommendations.
- Graduate profile update and analysis history.
- Admin management of graduates, CV visibility, match analyses, notifications and account status.
- Printable graduate/admin reports.
- RepositoryWrapper, IRepositoryWrapper, RepositoryBase and IRepositoryBase architecture.

## Run in Visual Studio / VS Code
1. Install .NET 8 SDK and SQL Server LocalDB/SQL Server.
2. Open this folder.
3. Run `dotnet restore`.
4. Run `dotnet build`.
5. Run `dotnet run`.
6. Default seeded admin: `admin@careersync.local` / `Admin@123` — change this before production.

The app creates the database on first run with `EnsureCreated`. For a production submission, use EF Core migrations instead.

## Important security note
The sample admin password is intentionally documented for local development. Before deployment, replace the seed credentials and use a production-grade password hashing/authentication solution (ASP.NET Core Identity is recommended).
