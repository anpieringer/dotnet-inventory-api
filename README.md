# .NET Inventory Management API

A RESTful inventory management API built with **ASP.NET Core**, **Entity Framework Core**, and **PostgreSQL**.

The project demonstrates backend development with relational data, DTO validation, database migrations, product filtering, business rules, and interactive API documentation.

## Features

* Complete CRUD operations for categories, suppliers, and products
* Product relationships with categories and suppliers
* Unique category names
* Unique product SKUs
* Product search by name or SKU
* Filtering by category, supplier, active status, and low stock
* DTO-based request validation
* Business-rule validation
* PostgreSQL persistence with Entity Framework Core
* Code-first database migrations
* Secure local database configuration using .NET User Secrets
* OpenAPI document generation
* Interactive API documentation with Scalar

## Tech Stack

* .NET 10
* ASP.NET Core Web API
* C#
* Entity Framework Core 10
* PostgreSQL
* Npgsql
* OpenAPI
* Scalar
* Git
* GitHub

## Project Structure

```text
dotnet-inventory-api/
├── src/
│   └── DotnetInventoryApi/
│       ├── Controllers/
│       │   ├── CategoriesController.cs
│       │   ├── ProductsController.cs
│       │   └── SuppliersController.cs
│       ├── Data/
│       │   ├── InventoryDbContext.cs
│       │   └── Migrations/
│       ├── Dtos/
│       │   ├── CategoryDtos.cs
│       │   ├── ProductDtos.cs
│       │   └── SupplierDtos.cs
│       ├── Models/
│       │   ├── Category.cs
│       │   ├── Product.cs
│       │   └── Supplier.cs
│       ├── Program.cs
│       └── DotnetInventoryApi.csproj
├── .gitignore
├── LICENSE
└── README.md
```

## Requirements

Before running the project, install:

* .NET SDK 10
* PostgreSQL
* Entity Framework Core CLI tools

Verify the installations:

```powershell
dotnet --version
dotnet ef --version
```

If the Entity Framework Core CLI is not installed:

```powershell
dotnet tool install --global dotnet-ef
```

## Local Setup

### 1. Clone the repository

```powershell
git clone https://github.com/anpieringer/dotnet-inventory-api.git
cd dotnet-inventory-api
```

### 2. Restore dependencies

```powershell
dotnet restore '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

### 3. Create the PostgreSQL role and database

Connect to PostgreSQL and execute:

```sql
CREATE ROLE inventory_app
WITH LOGIN
PASSWORD 'YOUR_SECURE_PASSWORD';

CREATE DATABASE inventory_db
WITH OWNER = inventory_app
ENCODING = 'UTF8';
```

Replace `YOUR_SECURE_PASSWORD` with your own secure local password.

### 4. Configure the database connection

Initialize .NET User Secrets:

```powershell
dotnet user-secrets init --project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

Store the PostgreSQL connection string:

```powershell
dotnet user-secrets set 'ConnectionStrings:InventoryDatabase' 'Host=localhost;Port=5432;Database=inventory_db;Username=inventory_app;Password=YOUR_SECURE_PASSWORD' --project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

Passwords and production credentials should never be committed to GitHub.

### 5. Apply database migrations

```powershell
dotnet ef database update --project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj' --startup-project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

### 6. Build the project

```powershell
dotnet build '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

### 7. Run the API

```powershell
dotnet run --project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

The application will display its local URL in the terminal, for example:

```text
http://localhost:5063
```

The port can change depending on the local environment.

## Interactive API Documentation

While the application is running in the Development environment, open Scalar:

```text
http://localhost:5063/scalar
```

Scalar provides an interactive interface where each endpoint can be inspected and executed directly from the browser.

The generated OpenAPI document is available at:

```text
http://localhost:5063/openapi/v1.json
```

Replace `5063` with the port shown in the terminal when the application starts.

## API Endpoints

### Categories

| Method   | Endpoint               | Description          |
| -------- | ---------------------- | -------------------- |
| `GET`    | `/api/categories`      | List all categories  |
| `GET`    | `/api/categories/{id}` | Get a category by ID |
| `POST`   | `/api/categories`      | Create a category    |
| `PUT`    | `/api/categories/{id}` | Update a category    |
| `DELETE` | `/api/categories/{id}` | Delete a category    |

Example request:

```json
{
  "name": "Turbochargers",
  "description": "Automotive turbocharger products"
}
```

### Suppliers

| Method   | Endpoint              | Description          |
| -------- | --------------------- | -------------------- |
| `GET`    | `/api/suppliers`      | List all suppliers   |
| `GET`    | `/api/suppliers/{id}` | Get a supplier by ID |
| `POST`   | `/api/suppliers`      | Create a supplier    |
| `PUT`    | `/api/suppliers/{id}` | Update a supplier    |
| `DELETE` | `/api/suppliers/{id}` | Delete a supplier    |

Example request:

```json
{
  "name": "Demo Supplier",
  "email": "supplier@example.com",
  "phone": "+56 9 1234 5678",
  "address": "Santiago, Chile"
}
```

### Products

| Method   | Endpoint             | Description         |
| -------- | -------------------- | ------------------- |
| `GET`    | `/api/products`      | List all products   |
| `GET`    | `/api/products/{id}` | Get a product by ID |
| `POST`   | `/api/products`      | Create a product    |
| `PUT`    | `/api/products/{id}` | Update a product    |
| `DELETE` | `/api/products/{id}` | Delete a product    |

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

## Product Search and Filters

Products can be searched and filtered using query-string parameters.

### Search by product name or SKU

```text
GET /api/products?search=Turbo
```

### Filter by category

```text
GET /api/products?categoryId=1
```

### Filter by supplier

```text
GET /api/products?supplierId=1
```

### Filter by active status

```text
GET /api/products?isActive=true
```

### Filter by low stock

```text
GET /api/products?lowStock=true
```

Filters can also be combined:

```text
GET /api/products?search=Turbo&categoryId=1&isActive=true&lowStock=true
```

## Business Rules

* Category names must be unique.
* Product SKUs must be unique.
* Products must reference existing categories and suppliers.
* Products must reference active categories and suppliers.
* Prices cannot be negative.
* Stock values cannot be negative.
* Categories with associated products cannot be deleted.
* Suppliers with associated products cannot be deleted.
* A product is considered low stock when `stock <= minimumStock`.

## Security

The database connection string is stored locally using **.NET User Secrets**.

Sensitive credentials are not included in the repository and should never be committed to source control.

For production environments, credentials should be supplied through environment variables or a dedicated secrets-management service.

## Roadmap

Planned improvements:

* JWT authentication and role-based authorization
* Pagination and sorting
* Automated unit and integration tests
* Docker support
* GitHub Actions continuous integration
* Cloud deployment
* Centralized exception handling
* Audit logs
* Soft-delete support

## Skills Demonstrated

This project demonstrates experience with:

* RESTful API design
* ASP.NET Core controllers
* Entity Framework Core
* Relational database modeling
* PostgreSQL
* DTOs and input validation
* LINQ queries and filtering
* Database migrations
* Dependency injection
* Secure configuration management
* OpenAPI documentation
* Git and GitHub workflows

## Author

**Angelo Pieringer**

Junior .NET Full Stack Developer based in Santiago, Chile.

* GitHub: [anpieringer](https://github.com/anpieringer)
* LinkedIn: [angelo-pieringer-dev](https://www.linkedin.com/in/angelo-pieringer-dev/)

## License

This project is licensed under the [MIT License](LICENSE).
