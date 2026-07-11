# .NET Inventory API

A RESTful inventory management API built with ASP.NET Core and PostgreSQL.

## Features

- CRUD for categories, suppliers and products
- Unique category names and product SKUs
- Product relationships with categories and suppliers
- Search by product name or SKU
- Filters by category, supplier, active status and low stock
- DTO-based validation
- PostgreSQL with Entity Framework Core migrations
- Secure local connection configuration with .NET User Secrets
- OpenAPI document in Development

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- C#
- Entity Framework Core 10
- PostgreSQL
- Npgsql
- OpenAPI
- Git and GitHub

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

- .NET SDK 10
- PostgreSQL
- Entity Framework Core CLI tools

```powershell
dotnet --version
dotnet ef --version
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

```sql
CREATE ROLE inventory_app
WITH LOGIN
PASSWORD 'YOUR_SECURE_PASSWORD';

CREATE DATABASE inventory_db
WITH OWNER = inventory_app
ENCODING = 'UTF8';
```

### 4. Configure the connection string securely

```powershell
dotnet user-secrets init --project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

```powershell
dotnet user-secrets set 'ConnectionStrings:InventoryDatabase' 'Host=localhost;Port=5432;Database=inventory_db;Username=inventory_app;Password=YOUR_SECURE_PASSWORD' --project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

Do not commit passwords or production credentials to GitHub.

### 5. Apply migrations

```powershell
dotnet ef database update --project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj' --startup-project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

### 6. Build and run

```powershell
dotnet build '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
dotnet run --project '.\src\DotnetInventoryApi\DotnetInventoryApi.csproj'
```

The API will show its local URL in the terminal, for example:

```text
http://localhost:5063
```

OpenAPI document:

```text
http://localhost:5063/openapi/v1.json
```

## Endpoints

### Categories

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/categories` | List categories |
| GET | `/api/categories/{id}` | Get a category |
| POST | `/api/categories` | Create a category |
| PUT | `/api/categories/{id}` | Update a category |
| DELETE | `/api/categories/{id}` | Delete a category |

```json
{
  "name": "Turbochargers",
  "description": "Automotive turbocharger products"
}
```

### Suppliers

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/suppliers` | List suppliers |
| GET | `/api/suppliers/{id}` | Get a supplier |
| POST | `/api/suppliers` | Create a supplier |
| PUT | `/api/suppliers/{id}` | Update a supplier |
| DELETE | `/api/suppliers/{id}` | Delete a supplier |

```json
{
  "name": "Demo Supplier",
  "email": "supplier@example.com",
  "phone": "+56 9 1234 5678",
  "address": "Santiago, Chile"
}
```

### Products

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/products` | List products |
| GET | `/api/products/{id}` | Get a product |
| POST | `/api/products` | Create a product |
| PUT | `/api/products/{id}` | Update a product |
| DELETE | `/api/products/{id}` | Delete a product |

Filters:

```text
/api/products?search=Turbo
/api/products?categoryId=1
/api/products?supplierId=1
/api/products?isActive=true
/api/products?lowStock=true
```

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

## Business Rules

- Category names must be unique.
- Product SKUs must be unique.
- Products must reference active categories and suppliers.
- Prices and stock values cannot be negative.
- Categories and suppliers with associated products cannot be deleted.
- Low-stock products satisfy `stock <= minimumStock`.

## Roadmap

- JWT authentication and authorization
- Pagination and sorting
- Automated tests
- Swagger UI
- Docker support
- GitHub Actions
- Cloud deployment
- Audit logs
- Soft delete

## Author

**Angelo Pieringer**

Junior .NET Full Stack Developer based in Santiago, Chile.

- GitHub: [anpieringer](https://github.com/anpieringer)
- LinkedIn: [angelo-pieringer-dev](https://www.linkedin.com/in/angelo-pieringer-dev/)

## License

MIT
