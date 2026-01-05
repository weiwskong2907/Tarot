# Tarot Platform

Tarot is a comprehensive booking and content management platform built with .NET 9.0, following a modular MVC architecture. It provides functionalities for appointment scheduling, payment processing, blog management, and plugin extensibility.

## Features

### Phase 1: Foundation & Core
- **Modular Architecture**: Separated into API, Core, Infrastructure, and Worker projects.
- **Authentication**: Secure JWT-based authentication with ASP.NET Core Identity.
- **Database**: PostgreSQL integration using Entity Framework Core (Code-First).
- **Automation**: Background worker for appointment status management (Auto-complete/Cancel).

### Phase 2: Business Logic
- **Appointment System**: Book, cancel, and view appointments.
- **Service Management**: Manage offered services with pricing and duration.
- **Consultation Records**: Track consultation notes and history.

### Phase 3: Extensions & Integrations
- **Blog System**: Full CRUD capabilities for blog posts with SEO metadata.
- **Payment Integration**: Mock payment service architecture ready for provider integration (e.g., Stripe/PayPal).
- **Plugin System**: Extensible architecture to load external plugins dynamically.

## Tech Stack

- **Framework**: .NET 9.0
- **Database**: PostgreSQL (Production/Dev), InMemory (Testing)
- **ORM**: Entity Framework Core
- **Caching**: Redis (Planned/Ready)
- **Testing**: xUnit, Moq, Microsoft.AspNetCore.Mvc.Testing
- **CI/CD**: GitHub Actions

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL (or use InMemory mode for testing)
- Docker (optional, for containerized deployment)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/tarot.git
   cd tarot
   ```

2. Configure Database:
   Update `src/Tarot.Api/appsettings.json` with your PostgreSQL connection string.
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=TarotDb;Username=postgres;Password=yourpassword"
   }
   ```

3. Run Migrations:
   ```bash
   dotnet ef database update --project src/Tarot.Infrastructure --startup-project src/Tarot.Api
   ```

4. Run the Application:
   ```bash
   dotnet run --project src/Tarot.Api
   ```

### Running Tests

The project includes comprehensive Unit and Integration tests.

To run all tests:
```bash
dotnet test
```

### CI/CD

The project uses GitHub Actions for Continuous Integration. The workflow is defined in `.github/workflows/dotnet.yml` and performs the following on every push to `main`:
- Restores dependencies
- Builds the project
- Runs all tests
- Publishes the application

## Documentation

- **API Documentation**: Available via Swagger UI at `/swagger` when running in Development mode.
- **Development Plan**: See `docs/Project_Development_Plan.md`.
