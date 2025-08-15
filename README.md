# TaskManager-Api

**TaskManager-Api** is a RESTful API built with **.NET 8** and **ASP.NET Core Web API**.  
It serves as the backend for the [TaskManager-Web](https://github.com/lucianohalbus/taskmanager-web) application, providing authentication, task management, and secure data access.

---

## 🚀 Technologies Used

- [.NET 8](https://dotnet.microsoft.com/) — Backend framework
- [ASP.NET Core Web API](https://learn.microsoft.com/aspnet/core) — API development
- [Entity Framework Core](https://learn.microsoft.com/ef/core/) — ORM for database access
- [SQL Server](https://www.microsoft.com/sql-server) — Relational database
- [JWT Authentication](https://jwt.io/) — Secure authentication

---

## 📂 Project Structure

```plaintext
TaskManagerApi/
 ├── Controllers/V2/          # API endpoints (v2 versioning)
 │    ├── AuthController.cs
 │    ├── TaskItemController.cs
 │    └── UserController.cs
 │
 ├── Data/                    # Database context
 │    └── TaskManagerContext.cs
 │
 ├── Dtos/                    # Data Transfer Objects
 │    └── TaskItemDtos.cs
 │
 ├── Models/                  # Database entity models
 │    ├── TaskItem.cs
 │    └── User.cs
 │
 ├── Properties/
 │    └── launchSettings.json # Environment launch settings
 │
 ├── Settings/                # API configuration helpers
 │    └── ConfigureSwaggerOptions.cs
 │
 ├── Utils/                   # Utility classes
 │    └── PasswordUtils.cs
 │
 ├── Program.cs               # API entry point
 ├── appsettings.json         # Default configuration
 ├── appsettings.Development.json
 └── TaskManagerApi.csproj    # Project file
