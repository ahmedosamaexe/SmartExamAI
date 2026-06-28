# SmartExamAI: Web-Based Proctored Examination System

## Overview
SmartExamAI is a modern, comprehensive web-based examination management platform built with **ASP.NET Core 10.0 (MVC)** and **Entity Framework Core 10**. The system implements clean multi-tier architecture utilizing Repository and Service layers, providing robust role-based administration for teachers and students. Designed to streamline academic assessment workflows, the platform automates course management, exam creation, secure client-side proctoring, evaluation grading, and analytical Excel reporting.

## Core Capabilities

### Teacher Module
- **Course Administration**: Teachers can create, edit, and organize academic courses with custom styling and categorization. Each course automatically generates a unique 8-character enrollment code for seamless student registration, alongside direct student enrollment capabilities.
- **Assessment & Exam Design**: Supports flexible exam configurations featuring multiple-choice and constructed-response question structures. Instructors can configure scheduling windows (`StartTime`), duration limits, and automated grading thresholds.
- **Evaluation & Grading Dashboard**: Features automated evaluation for objective multiple-choice questions and a dedicated grading interface (`GradeExam`, `GradeSubmission`) allowing teachers to review student submissions, award manual scores, and monitor performance.
- **Analytics & Excel Export**: Aggregates comprehensive examination metrics—including submission rates, scores, and proctoring warning frequencies—with direct export capabilities to formatted spreadsheets via the **EPPlus 8.x** engine.
- **Integrity Monitoring**: Tracks student exam sessions in real time, recording proctoring warnings (such as focus loss or browser tab switching) and logging session terminations when infraction limits are exceeded.

### Student Module
- **Course Enrollment**: Students can quickly join active courses using validated enrollment codes provided by instructors.
- **Proctored Examination Interface**: An interactive assessment environment equipped with client-side monitoring that detects window visibility changes or tab switching, issues real-time warnings, and terminates sessions upon severe policy infractions.
- **Session & Time Management**: Includes active countdown timers enforcing strict exam durations and automated submission upon time expiration.
- **Dashboard & Academic Tracking**: Visual dashboards tracking course completion percentages, active examination alerts, and detailed post-publication results and score breakdowns.

### Security and Access Control
- **Role-Based Access Control (RBAC)**: Powered by **ASP.NET Core Identity** with dedicated `Teacher` and `Student` role assignments injected via custom claims principal factories (`CustomClaimsPrincipalFactory`).
- **Secure Authentication & Session Handling**: Configured with strict password complexity standards, robust cookie-based authentication with sliding expiration, and CSRF anti-forgery token validation across state-changing endpoints.
- **Reliable Pipeline & Error Handling**: Centralized routing and exception middleware ensuring graceful recovery and secure execution.

## Software Architecture and Technologies

| Component | Technology |
|---|---|
| **Application Framework** | ASP.NET Core 10.0 (MVC) |
| **Programming Language** | C# 13 |
| **Object-Relational Mapping** | Entity Framework Core 10 |
| **Database Management System** | Microsoft SQL Server |
| **Identity & Authentication** | ASP.NET Core Identity |
| **Frontend Architecture** | Razor Views, Bootstrap 5, Custom CSS/JavaScript |
| **Document Export Engine** | EPPlus 8.5.4 |

## Repository Structure

```text
SmartExamAI/
├── Areas/
│   ├── Student/          # Student presentation layer (Controllers and Views)
│   └── Teacher/          # Teacher presentation layer (Controllers and Views)
├── Controllers/          # Authentication and core home page routing
├── Data/                 # Database context and identity role seeding
├── Helpers/              # Utility classes and helper extensions (e.g., DateHelper)
├── Migrations/           # Entity Framework Core relational database schema migrations
├── Models/               # Domain database entities (Course, Exam, Question, Submission, etc.)
├── Repositories/         # Repository pattern abstractions and EF Core implementations
├── Services/             # Domain business logic and application service layer
├── ViewModels/           # Strongly-typed data transfer objects for UI binding
├── Views/                # Shared layout templates and account authentication views
└── wwwroot/              # Static web assets (stylesheets, JavaScript scripts, vendor libraries)
```

## System Requirements and Installation

### Prerequisites
- **.NET 10.0 SDK** or later
- **Microsoft SQL Server** (LocalDB or Express/Enterprise instance)

### Installation Procedure

1. **Clone the Repository**:
```bash
git clone https://github.com/ahmedosamaexe/SmartExamAI.git
cd SmartExamAI/SmartExamAI
```

2. **Configure Database Connection**:
Update the database connection string within `appsettings.json` if necessary:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SmartExamAI;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

3. **Apply Schema Migrations**:
Run Entity Framework Core migrations to create and initialize the relational database schema:
```bash
dotnet ef database update
```

4. **Execute Application**:
Launch the local web application server:
```bash
dotnet run
```
Access the application by navigating to `https://localhost:5097` in your web browser.

## License
This project is licensed under the MIT License.
