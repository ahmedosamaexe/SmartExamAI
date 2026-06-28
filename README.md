# SmartExamAI

A modern, full-featured online examination platform built with ASP.NET Core. SmartExamAI enables teachers to create courses, design exams, and grade student submissions — while students can enroll, take proctored exams, and view their results — all within a clean, role-based interface.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-0A7C6E)
![SQL Server](https://img.shields.io/badge/SQL%20Server-EF%20Core-CC2927?logo=microsoftsqlserver&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-blue)

---

## ✨ Features

### 👩‍🏫 Teacher Module
- **Course Management** — Create, edit, and delete courses with auto-generated enrollment codes and optional CSV bulk enrollment.
- **Exam Builder** — Multi-step exam creation with MCQ and short-answer questions, configurable duration, scheduling, and randomization.
- **Grading Dashboard** — Individual and bulk grading workflows for short-answer questions, with teacher feedback per answer.
- **Results & Analytics** — Aggregate performance metrics (average scores, pass rates), results publishing, and Excel export via EPPlus.
- **Violation Monitor** — Real-time proctoring violation tracking per student per exam.

### 🎓 Student Module
- **Course Enrollment** — Join courses using teacher-provided enrollment codes.
- **Proctored Exams** — One-question-per-page exam interface with tab-switch detection, fullscreen enforcement, and automatic termination on violation threshold.
- **Live Timer** — Countdown timer with color-coded urgency transitions (teal → amber → coral).
- **Auto-Save** — Answers are saved server-side on every navigation between questions.
- **Results View** — Detailed submission results once published by the teacher.

### 🔐 Authentication & Security
- **ASP.NET Core Identity** — Role-based access (Teacher / Student) with cookie authentication.
- **First-Login Password Change** — Enforced via a global action filter.
- **Anti-Forgery Tokens** — All POST actions are CSRF-protected.
- **Global Exception Middleware** — Centralized error handling.

### 🎨 UI/UX
- **Teal & Orange Design System** — Custom CSS variable-driven theme with semantic color tokens.
- **Responsive Sidebar Layout** — Collapsible sidebar with role-aware navigation.
- **Toast Notifications** — Global success/error feedback system powered by `TempData`.
- **Loading States** — Spinner overlays on async buttons to prevent double submissions.
- **Password Strength Meter** — Real-time visual strength indicator on registration and password change forms.
- **Breadcrumb Navigation** — Consistent breadcrumbs across all Teacher and Student pages.
- **Avatar Dropdown** — Profile menu with initials avatar in the topbar.

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-----------|
| **Framework** | ASP.NET Core 10.0 (MVC) |
| **Language** | C# 13 |
| **Database** | SQL Server + Entity Framework Core 10 |
| **Identity** | ASP.NET Core Identity |
| **Frontend** | Razor Views, Bootstrap 5, Vanilla CSS & JS |
| **Export** | EPPlus 8.x (Excel generation) |

---

## 📁 Project Structure

```
SmartExamAI/
├── Areas/
│   ├── Student/          # Student controllers & views
│   │   ├── Controllers/  # Dashboard, Courses, Exam
│   │   └── Views/
│   └── Teacher/          # Teacher controllers & views
│       ├── Controllers/  # Dashboard, Courses, Exams, Results
│       └── Views/
├── Controllers/          # Account (Login, Register, Profile)
├── Data/                 # AppDbContext, seed logic
├── Filters/              # ForcePasswordChangeFilter
├── Helpers/              # ExamStatusHelper
├── Infrastructure/       # CustomClaimsPrincipal, GlobalExceptionMiddleware
├── Models/               # EF Core entities (Course, Exam, Question, etc.)
├── ViewModels/           # Teacher & Student view models
├── Views/                # Shared layouts, Account views
└── wwwroot/
    ├── css/site.css      # Design system & utility classes
    └── js/proctoring.js  # Client-side proctoring engine
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (LocalDB, Express, or full)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/ahmedosamaexe/SmartExamAI.git
   cd SmartExamAI/SmartExamAI
   ```

2. **Configure the database connection**

   Edit `appsettings.json` and set your connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.;Database=SmartExamAI;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

3. **Apply migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Open in browser**

   Navigate to `https://localhost:5097` (or the port shown in terminal).

---

## 🎨 Design System

The UI uses a custom Teal & Orange color palette defined as CSS variables:

| Token | Value | Purpose |
|-------|-------|---------|
| `--c-primary` | `#0A7C6E` | Primary actions, links, sidebar |
| `--c-primary-hover` | `#085F55` | Hover states |
| `--c-danger` | `#FF6B35` | Destructive actions, alerts |
| `--c-warning` | `#F59E0B` | Cautions, pending states |
| `--c-success` | `#0A7C6E` | Confirmations, positive states |
| `--c-text` | `#0D2B27` | Primary text |
| `--c-muted` | `#6B7280` | Secondary text, labels |

---

## 📄 License

This project is licensed under the MIT License.
