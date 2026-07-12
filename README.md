# .NET Inventory Management API

[![.NET CI](https://github.com/anpieringer/dotnet-inventory-api/actions/workflows/ci.yml/badge.svg)](https://github.com/anpieringer/dotnet-inventory-api/actions/workflows/ci.yml)

RESTful inventory management API built with **ASP.NET Core**, **Entity Framework Core**, and **PostgreSQL**.

This backend portfolio project demonstrates relational database modeling, CRUD operations, authentication, role-based authorization, business rules, automated testing, interactive API documentation, continuous integration, and containerized execution.

## Features

- Complete CRUD operations for categories, suppliers, and products
- JWT Bearer authentication
- Role-based authorization with `User` and `Admin` roles
- Secure password hashing
- Unique user emails, category names, and product SKUs
- Product relationships with categories and suppliers
- Product search by name or SKU
- Filtering by category, supplier, active status, and low stock
- DTO-based request validation
- Business-rule validation
- PostgreSQL persistence with Entity Framework Core
- Code-first database migrations
- Automatic migrations in the Docker environment
- Interactive OpenAPI documentation with Scalar
- Bearer authentication directly from Scalar
- 24 automated tests with xUnit
- Continuous integration with GitHub Actions
- Docker and Docker Compose support
- Secure local configuration using .NET User Secrets and environment variables

## Technology Stack

- .NET 10
- ASP.NET Core Web API
- C#
- Entity Framework Core 10
- PostgreSQL
- Npgsql
- JWT Bearer Authentication
- Scalar
- OpenAPI
- xUnit
- Docker
- Docker Compose
- GitHub Actions
- Git and GitHub

## Project Structure

```text
dotnet-inventory-api/
├── .github/
│   └── workflows/
│       └── ci.yml
├── src/
│   └── DotnetInventoryApi/
│       ├── Controllers/
│       │   ├── AuthController.cs
│       │   ├── CategoriesController.cs
│       │   ├── ProductsController.cs
│       │   └── SuppliersController.cs
│       ├── Data/
│       │   ├── InventoryDbContext.cs
│       │   └── Migrations/
│       ├── Dtos/
│       │   ├── AuthDtos.cs
│       │   ├── CategoryDtos.cs
│       │   ├── ProductDtos.cs
│       │   └── SupplierDtos.cs
│       ├── Models/
│       │   ├── AppUser.cs
│       │   ├── Category.cs
│       │   ├── Product.cs
│       │   └── Supplier.cs
│       ├── Security/
│       │   ├── JwtOptions.cs
│       │   ├── JwtTokenService.cs
│       │   └── UserRoles.cs
│       ├── Program.cs
│       └── DotnetInventoryApi.csproj
├── tests/
│   └── DotnetInventoryApi.Tests/
│       ├── Controllers/
│       │   ├── AuthControllerTests.cs
│       │   ├── CategoriesControllerTests.cs
│       │   ├── ProductsControllerTests.cs
│       │   └── SuppliersControllerTests.cs
│       └── DotnetInventoryApi.Tests.csproj
├── .dockerignore
├── .env.example
├── .gitignore
├── compose.yml
├── Dockerfile
├── DotnetInventory.slnx
├── LICENSE
└── README.md
```

# Running with Docker

Docker Compose is the recommended way to run the complete project because it starts both the API and PostgreSQL.

## Requirements

- Docker Desktop
- Docker Compose
- WSL 2 on Windows

## 1. Clone the repository

```powershell
git clone https://github.com/anpieringer/dotnet-inventory-api.git
cd dotnet-inventory-api
```

## 2. Create the environment file

Copy the example file:

```powershell
Copy-Item '.env.example' '.env'
```

Open `.env` and replace the example values:

```env
POSTGRES_PASSWORD=replace_with_a_secure_password
JWT_KEY=replace_with_a_base64_key_of_at_least_32_bytes
```

A secure Base64 JWT key can be generated in PowerShell:

```powershell
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)
```

Copy the generated value into `JWT_KEY`.

The real `.env` file is ignored by Git and must never be committed.

## 3. Build and start the containers

```powershell
docker compose up --build -d
```

## 4. Check container status

```powershell
docker compose ps
```

The PostgreSQL container should report a healthy status, and the API container should be running.

## 5. Open the API documentation

```text
http://localhost:8080/scalar
```

The generated OpenAPI document is available at:

```text
http://localhost:8080/openapi/v1.json
```

## Docker Ports

| Service | Address |
|---|---|
| API | `http://localhost:8080` |
| Scalar | `http://localhost:8080/scalar` |
| PostgreSQL | `localhost:5433` |

PostgreSQL uses host port `5433` to avoid conflicts with a local PostgreSQL installation using port `5432`.

## Docker Commands

View container status:

```powershell
docker compose ps
```

View API logs:

```powershell
docker compose logs api --tail 100
```

Stop the containers without deleting database data:

```powershell
docker compose down
```

Start existing containers again:

```powershell
docker compose up -d
```

Rebuild after changing source code:

```powershell
docker compose up --build -d
```

Delete containers and the persisted Docker database:

```powershell
docker compose down -v
```

> Warning: the `-v` option permanently deletes the PostgreSQL volume and its stored data.

# Local Development without Docker

## Requirements

- .NET SDK 10
- PostgreSQL
- Entity Framework Core CLI tools

Verify the installations:

```powershell
dotnet --version
dotnet ef --version
```

Install the Entity Framework CLI if necessary:

```powershell
dotnet tool install --global dotnet-ef
```

## 1. Clone the repository

```powershell
git clone https://github.com/anpieringer/dotnet-inventory-api.git
cd dotnet-inventory-api
```

## 2. Restore dependencies

```powershell
dotnet restore '.\DotnetInventory.slnx'
```

## 3. Create the PostgreSQL role and database

Connect to PostgreSQL and execute:

```sql
CREATE ROLE inventory_app
WITH LOGIN
PASSWORD 'YOUR_SECURE_PASSWORD';

CREATE DATABASE inventory_db
WITH OWNER = inventory_app
ENCODING = 'UTF8';
```

## 4. Configure local secrets

Store the PostgreSQL connection string:

```powershell
dotnet user-secrets set `
    'ConnectionStrings:InventoryDatabase' `
    'Host=localhost;Port=5432;Database=inventory_db;Username=inventory_app;Password=YOUR_SECURE_PASSWORD' `
    --project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

Generate a JWT key:

```powershell
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)
```

Store it with User Secrets:

```powershell
dotnet user-secrets set `
    'Jwt:Key' `
    "$jwtKey" `
    --project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

Do not commit passwords, JWT keys, or production credentials.

## 5. Apply database migrations

```powershell
dotnet ef database update `
    --project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj' `
    --startup-project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

## 6. Build the solution

```powershell
dotnet build '.\DotnetInventory.slnx'
```

## 7. Run the automated tests

```powershell
dotnet test '.\DotnetInventory.slnx'
```

The solution currently contains 24 automated tests.

## 8. Run the API

```powershell
dotnet run --project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

The application displays its local address in the terminal, for example:

```text
http://localhost:5063
```

The local port can vary.

Open Scalar using the port displayed by the application:

```text
http://localhost:5063/scalar
```

# Authentication and Authorization

The API uses JWT Bearer authentication.

## Authentication Endpoints

| Method | Endpoint | Access | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Public | Register a new user |
| `POST` | `/api/auth/login` | Public | Log in and receive an access token |
| `GET` | `/api/auth/me` | Authenticated | Return the current user |

## Register

```http
POST /api/auth/register
```

Example request:

```json
{
  "fullName": "Inventory User",
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

Public registration always assigns the `User` role. A client cannot register itself as an administrator.

## Login

```http
POST /api/auth/login
```

Example request:

```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

Example response:

```json
{
  "accessToken": "JWT_ACCESS_TOKEN",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-01-01T12:00:00Z",
  "user": {
    "id": 1,
    "fullName": "Inventory User",
    "email": "user@example.com",
    "role": "User"
  }
}
```

Send the token in protected requests:

```http
Authorization: Bearer JWT_ACCESS_TOKEN
```

## Roles

### User

A user can:

- View categories
- View suppliers
- View products
- Search and filter products
- View their authenticated profile

A user cannot create, update, or delete inventory records.

### Admin

An administrator can:

- Perform every operation available to a user
- Create, update, and delete categories
- Create, update, and delete suppliers
- Create, update, and delete products

## Creating an Administrator for Local Testing

Register the account normally through `/api/auth/register`.

When using Docker, open PostgreSQL:

```powershell
docker compose exec db psql -U inventory_app -d inventory_db
```

Then assign the role:

```sql
UPDATE "Users"
SET "Role" = 'Admin'
WHERE "NormalizedEmail" = 'ADMIN@EXAMPLE.COM';
```

Exit PostgreSQL:

```text
\q
```

Log in again after changing the role. Existing JWTs retain the old role until a new token is generated.

# Scalar Authentication

Open Scalar:

```text
http://localhost:8080/scalar
```

Then:

1. Execute `POST /api/auth/login`.
2. Copy the returned `accessToken`.
3. Select **Authenticate**.
4. Paste only the token, without manually adding the `Bearer` prefix.
5. Execute protected endpoints directly from Scalar.

# API Endpoints

All inventory endpoints require authentication.

## Categories

| Method | Endpoint | Required role | Description |
|---|---|---|---|
| `GET` | `/api/categories` | User or Admin | List categories |
| `GET` | `/api/categories/{id}` | User or Admin | Get a category |
| `POST` | `/api/categories` | Admin | Create a category |
| `PUT` | `/api/categories/{id}` | Admin | Update a category |
| `DELETE` | `/api/categories/{id}` | Admin | Delete a category |

Example request:

```json
{
  "name": "Turbochargers",
  "description": "Automotive turbocharger products"
}
```

## Suppliers

| Method | Endpoint | Required role | Description |
|---|---|---|---|
| `GET` | `/api/suppliers` | User or Admin | List suppliers |
| `GET` | `/api/suppliers/{id}` | User or Admin | Get a supplier |
| `POST` | `/api/suppliers` | Admin | Create a supplier |
| `PUT` | `/api/suppliers/{id}` | Admin | Update a supplier |
| `DELETE` | `/api/suppliers/{id}` | Admin | Delete a supplier |

Example request:

```json
{
  "name": "Demo Supplier",
  "email": "supplier@example.com",
  "phone": "+56 9 1234 5678",
  "address": "Santiago, Chile"
}
```

## Products

| Method | Endpoint | Required role | Description |
|---|---|---|---|
| `GET` | `/api/products` | User or Admin | List products |
| `GET` | `/api/products/{id}` | User or Admin | Get a product |
| `POST` | `/api/products` | Admin | Create a product |
| `PUT` | `/api/products/{id}` | Admin | Update a product |
| `DELETE` | `/api/products/{id}` | Admin | Delete a product |

Example request:

```json
{
  "sku": "TURBO-001",
  "name": "Demo Turbocharger",
  "description": "Inventory API test product",
  "costPrice": 500000,
  "salePrice": 650000,
  "stock": 2,
  "minimumStock": 3,
  "categoryId": 1,
  "supplierId": 1
}
```

# Product Search and Filters

Search by product name or SKU:

```http
GET /api/products?search=Turbo
```

Filter by category:

```http
GET /api/products?categoryId=1
```

Filter by supplier:

```http
GET /api/products?supplierId=1
```

Filter by active status:

```http
GET /api/products?isActive=true
```

Filter by low stock:

```http
GET /api/products?lowStock=true
```

Filters can be combined:

```http
GET /api/products?search=Turbo&categoryId=1&isActive=true&lowStock=true
```

# Business Rules

- User emails must be unique.
- Category names must be unique.
- Product SKUs must be unique.
- Product SKUs are normalized to uppercase.
- Products must reference existing categories and suppliers.
- Products must reference active categories and suppliers.
- Prices cannot be negative.
- Stock values cannot be negative.
- Categories with associated products cannot be deleted.
- Suppliers with associated products cannot be deleted.
- A product is considered low stock when `stock <= minimumStock`.
- Public registration cannot assign the `Admin` role.
- Inactive users cannot log in.

# Automated Tests

The project contains 24 automated tests covering:

- Category ordering, creation, validation, duplicates, and deletion
- Supplier ordering, normalization, updates, duplicates, and relationships
- Product filters, low stock, normalization, relationships, updates, and deletion
- User registration
- Password hashing
- Duplicate-email rejection
- Valid and invalid login attempts
- JWT generation
- Authenticated-user retrieval

Run all tests:

```powershell
dotnet test '.\DotnetInventory.slnx'
```

# Continuous Integration

The repository includes a GitHub Actions workflow located at:

```text
.github/workflows/ci.yml
```

The workflow runs automatically on:

- Pushes to `main`
- Pull requests targeting `main`
- Manual execution from the GitHub Actions interface

The workflow:

1. Checks out the repository.
2. Installs .NET 10.
3. Restores dependencies.
4. Builds the solution in Release mode.
5. Runs the automated tests.
6. Uploads the test results as an artifact.

# Security

- Passwords are stored as hashes rather than plain text.
- JWTs validate their signature, issuer, audience, and expiration.
- Inventory modification endpoints require the `Admin` role.
- Database credentials are stored with User Secrets during local development.
- Docker credentials are supplied through an ignored `.env` file.
- `.env.example` documents required variables without containing real credentials.
- JWT keys and database passwords are not committed to the repository.

This project is intended as a portfolio and learning project. Additional security review and deployment hardening would be required before using it in a production environment.

# Roadmap

Planned improvements:

- PostgreSQL integration tests
- Pagination and sorting
- Centralized exception handling
- Structured logging
- API health checks
- Refresh-token support
- Audit logs
- Soft-delete support
- Cloud deployment
- Frontend client application

# Skills Demonstrated

- RESTful API design
- ASP.NET Core controllers
- JWT Bearer authentication
- Role-based authorization
- Secure password hashing
- Entity Framework Core
- PostgreSQL
- Relational database modeling
- DTOs and request validation
- LINQ queries and filtering
- Code-first migrations
- Dependency injection
- OpenAPI documentation
- Automated unit testing
- Continuous integration
- Docker containerization
- Secure configuration management
- Git and GitHub workflows

# Author

**Angelo Pieringer**

Junior .NET Full Stack Developer based in Santiago, Chile.

- GitHub: [anpieringer](https://github.com/anpieringer)
- LinkedIn: [angelo-pieringer-dev](https://www.linkedin.com/in/angelo-pieringer-dev/)

# License

This project is licensed under the [MIT License](LICENSE).