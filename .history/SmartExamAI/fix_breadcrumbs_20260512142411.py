import os

files = {
    "Areas/Teacher/Views/Courses/Index.cshtml": ("Courses", "My Courses"),
    "Areas/Teacher/Views/Courses/Details.cshtml": ("Courses", "Course Details"),
    "Areas/Teacher/Views/Courses/Create.cshtml": ("Courses", "Create Course"),
    "Areas/Teacher/Views/Courses/Edit.cshtml": ("Courses", "Edit Course"),
    "Areas/Teacher/Views/Exams/Create.cshtml": ("Exams", "Create Exam"),
    "Areas/Teacher/Views/Exams/Details.cshtml": ("Exams", "Exam Details"),
    "Areas/Teacher/Views/Exams/Edit.cshtml": ("Exams", "Edit Exam"),
    "Areas/Teacher/Views/Results/GradeExam.cshtml": ("Results", "Grade Exam"),
    "Areas/Teacher/Views/Results/GradeSubmission.cshtml": ("Results", "Grade Submission"),
    "Areas/Teacher/Views/Results/ViolationsMonitor.cshtml": ("Results", "Violations Monitor"),
    "Views/Account/Profile.cshtml": ("Account", "Profile"),
    "Areas/Student/Views/Courses/Index.cshtml": ("Courses", "My Courses"),
    "Areas/Student/Views/Courses/Details.cshtml": ("Courses", "Course Details"),
    "Areas/Student/Views/Exam/Result.cshtml": ("Exams", "Exam Result"),
    "Areas/Student/Views/Exam/Terminated.cshtml": ("Exams", "Exam Terminated")
}

for path, (section, current) in files.items():
    if not os.path.exists(path): continue
    
    is_student = "Student" in path or ("Account" in path)
    home_link = "/Student/Dashboard" if is_student else "/Teacher/Dashboard"
    
    breadcrumb = f'''@section Breadcrumb {{
<nav aria-label="breadcrumb" style="font-size:0.85rem;margin-bottom:1rem;">
  <ol class="breadcrumb" style="background:none;padding:0;margin:0;">
    <li class="breadcrumb-item"><a href="{home_link}" style="color:var(--c-primary);">Home</a></li>
    <li class="breadcrumb-item"><a href="#" style="color:var(--c-primary);">{section}</a></li>
    <li class="breadcrumb-item active" style="color:var(--c-muted);">{current}</li>
  </ol>
</nav>
}}
'''
    with open(path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    if '@section Breadcrumb' in content:
        print(f"Skipping {path}, already has Breadcrumb section.")
        continue

    # insert after } of ViewData["Title"]
    idx = content.find('}')
    if idx != -1:
        content = content[:idx+1] + "\n\n" + breadcrumb + content[idx+1:]
        with open(path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Updated {path}")
