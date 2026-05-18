# EduVibe LMS Backend

## Prerequisites
- .NET 8 SDK
- PostgreSQL

## Migration commands
1. `dotnet tool install --global dotnet-ef`
2. `dotnet ef migrations add InitialCreate -p LMS.Infrastructure -s LMS.WebApi`
3. `dotnet ef database update -p LMS.Infrastructure -s LMS.WebApi`

## Run API
1. Configure connection string and JWT key in `LMS.WebApi/appsettings.json` (JWT key must be at least 32 bytes for HS256)
2. Run: `dotnet run --project LMS.WebApi`

## Default admin login (seeded and usable)
- Email: `director@eduvibe.local`
- Password: `ChangeMe123!`

## Notes / TODOs (no extra tables created)
- Support teacher many-to-many class assignment is TODO; current checks rely on `Class.TeacherUserId`.
- Recording/whiteboard/notifications/QR/calendar integrations are TODO service integrations.
- AI assignment generation should be implemented behind a service interface without adding tables.
