# SmartExamAI: Web-Based Proctored Examination System

## Overview
SmartExamAI is a web-based examination management platform developed using ASP.NET Core MVC and Entity Framework Core. The system provides role-based administration for instructors and students, facilitating automated course management, assessment creation, asynchronous grading, and real-time integrity monitoring. Designed to support academic assessment workflows, the platform enforces browser proctoring protocols and secure authentication mechanisms.

## Core Capabilities

### Instructor Module
- Course Administration: Instructors can create, modify, and archive courses. The module supports batch student enrollment via CSV file processing and automatic generation of unique access codes.
- Assessment Design: Supports multi-stage exam configuration with multiple-choice and constructed-response question types. Instructors can define duration constraints, scheduling windows, and item randomization parameters.
- Evaluation and Grading: Facilitates automated evaluation for objective items and provides a dedicated evaluation dashboard for grading constructed responses with personalized feedback.
- Analytics and Reporting: Aggregates assessment performance data to calculate statistical metrics, including score distributions and pass rates. Results can be exported to Excel spreadsheets via the EPPlus library.
- Integrity Monitoring: Records proctoring infractions in real time during active examination sessions, logging specific browser behavioral violations per candidate.

### Student Module
- Course Enrollment: Allows participants to register for active courses using validated enrollment codes provided by instructors.
- Secure Examination Interface: Presents assessment items sequentially to maintain focus. Built-in client-side proctoring detects browser tab switching, enforces full-screen execution, and terminates the assessment if infraction thresholds are exceeded.
- Session Management: Features an active session countdown timer and enforces automatic submission upon time expiration.
- State Persistence: Synchronizes and records candidate responses asynchronously on the server during session navigation to prevent data loss.
- Academic Records: Grants access to finalized evaluations, scores, and instructor feedback upon formal publication.

### Security and Access Control
- Role-Based Access Control (RBAC): Implements ASP.NET Core Identity to establish distinct privileges for Instructor and Student accounts.
- Policy Enforcement: Enforces mandatory password modification upon initial authentication using global authorization filters.
- Request Verification: Secures all state-changing HTTP endpoints against Cross-Site Request Forgery (CSRF) via anti-forgery token validation.
- Centralized Exception Handling: Utilizes global middleware to intercept runtime errors, maintain execution logs, and ensure controlled failure recovery.

## Software Architecture and Technologies

| Component | Technology |
|---|---|
| Application Framework | ASP.NET Core 10.0 (MVC) |
| Programming Language | C# 13 |
| Object-Relational Mapping | Entity Framework Core 10 |
| Database Management System | Microsoft SQL Server |
| Identity & Authentication | ASP.NET Core Identity |
| Frontend Architecture | Razor Views, Bootstrap 5, Custom CSS/JavaScript |
| Document Export Engine | EPPlus 8.x |

## Repository Structure

```
SmartExamAI/
├── Areas/
│   ├── Student/          # Student presentation layer (Controllers and Views)
│   └── Teacher/          # Instructor presentation layer (Controllers and Views)
├── Controllers/          # Authentication and user account management
├── Data/                 # Database context definitions and initial data seeding
├── Filters/              # Action filters including password compliance enforcement
├── Helpers/              # Utility classes for evaluation and status tracking
├── Infrastructure/       # Custom middleware and security extensions
├── Models/               # Domain entities and relational database schema
├── ViewModels/           # Data transfer objects for presentation layer binding
├── Views/                # Shared layout definitions and core account views
└── wwwroot/              # Static web assets (stylesheets, scripts, libraries)
```

## System Requirements and Installation

### Prerequisites
- .NET 10.0 SDK or later
- Microsoft SQL Server (LocalDB or Enterprise instance)

### Installation Procedure

1. Repository Cloning:
```bash
git clone https://github.com/ahmedosamaexe/SmartExamAI.git
cd SmartExamAI/SmartExamAI
```

2. Database Configuration:
Configure the database connection string within `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SmartExamAI;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

3. Schema Migration:
Apply Entity Framework Core migrations to initialize the relational database schema:
```bash
dotnet ef database update
```

4. Application Execution:
Launch the local web server:
```bash
dotnet run
```
Access the application interface by navigating to `https://localhost:5097` in a web browser.

## License
This project is distributed under the MIT License.
