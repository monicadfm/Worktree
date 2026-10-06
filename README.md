# Worktree

Kanban-style project and task manager built with ASP.NET Core MVC, EF Core and SQL Server.
Final project for UFCD 5417 (CET 105 – CINEL).

## Features
- [x] Authentication: register, login/logout, profile, change password, password recovery by email
- [ ] Projects and members (roles)
- [ ] Tasks, labels and comments
- [ ] Kanban board
- [ ] Dashboard

## Tech stack
- ASP.NET Core MVC (.NET 8)
- Entity Framework Core + SQL Server (LocalDB)
- ASP.NET Core Identity
- MailKit (SMTP)
- Bootstrap 5

## Getting started
1. Clone the repository
2. Update the connection string in `appsettings.json` if needed
3. Set the mail password with User Secrets:
   `dotnet user-secrets set "Mail:Password" "<your app password>"`
4. Run the project. The database is created and seeded automatically.

## Demo accounts
teste1@worktree.com | 123456
teste2@worktree.com | 123456

## Screenshots
