# SmartContractPlatform

An ASP.NET Core MVC (.NET 8) web platform for creating, managing, and digitally signing contracts. It follows a clean three-layer architecture (DAL / BLL / UI) and supports contract templates, multi-party signing, PDF generation, email notifications, and audit logging.

## Features

- User registration, login, and role-based access (Admin / regular users) via ASP.NET Core Identity
- Create contracts from predefined templates, categorized by type
- Add multiple parties to a contract with defined roles
- Digital signing workflow with different signature types
- Contract status tracking (e.g. draft, pending, signed, completed)
- Automatic PDF generation of finalized contracts (via QuestPDF)
- Email notifications for contract and signing events (via MailKit)
- Full audit log of actions taken on contracts
- Admin panel for managing contract templates (create, edit, delete)

## Technologies

- C# / ASP.NET Core MVC (.NET 8)
- Entity Framework Core with SQL Server
- ASP.NET Core Identity (authentication & authorization)
- AutoMapper (object mapping between entities and DTOs)
- FluentValidation (input validation)
- QuestPDF (PDF document generation)
- MailKit / MimeKit (email sending)
- JWT-based authentication support
- Bootstrap, jQuery (frontend)

## Architecture

The solution is split into three projects:

- **SmartContractPlatform.DAL** — Data Access Layer: entities, EF Core `ApplicationDbContext`, repositories, unit of work, and database migrations.
- **SmartContractPlatform.BLL** — Business Logic Layer: services (contracts, templates, documents, signing, auth, email, audit, PDF), DTOs, and AutoMapper profiles.
- **SmartContractPlatform.UI(MVC)** — Presentation Layer: MVC controllers, views, and view models.

## Requirements

- .NET SDK 8.0 or higher installed (https://dotnet.microsoft.com/download)
- SQL Server (LocalDB or full instance)
- SMTP credentials for email sending (configured in `appsettings.json`)

## Installation and Run

git clone https://github.com/RenaMehdiyeva88/SmartContractPlatform.git
cd SmartContractPlatform

Update the connection string and email/SMTP settings in `SmartContractPlatform.UI(MVC)/appsettings.json`, then apply the database migrations and run the app:

cd SmartContractPlatform.UI(MVC)
dotnet ef database update
dotnet run

Open the URL shown in the console (e.g. https://localhost:5001) in your browser.

## Usage Example

1. Register a new account or log in.
2. Browse available contract templates.
3. Create a new contract from a template and add the involved parties.
4. Send the contract for digital signing.
5. Once all parties sign, download the finalized contract as a PDF.
6. Admins can manage templates and review the audit log from the Admin panel.

## Project Structure

SmartContractPlatform/
├── SmartContractPlatform.DAL/
│   ├── Data/ApplicationDbContext.cs
│   ├── Entities/                # Contract, Document, Signature, Template, User, AuditLog...
│   ├── Enums/                   # ContractStatus, PartyRole, SignatureType, UserRole...
│   ├── Migrations/
│   └── Repositories/            # Generic + specific repositories, UnitOfWork
├── SmartContractPlatform.BLL/
│   ├── DTOs/                    # ContractDtos, AuthDtos, DocumentDtos, TemplateDtos...
│   ├── Mapping/SmartContractProfile.cs
│   └── Services/                # ContractService, AuthService, DocumentService, PdfService...
├── SmartContractPlatform.UI(MVC)/
│   ├── Controllers/              # Account, Admin, Contracts, Home, Templates
│   ├── Models/                   # View models
│   ├── Views/
│   └── Program.cs
└── README.md

## Author

RenaMehdiyeva88 (https://github.com/RenaMehdiyeva88)
