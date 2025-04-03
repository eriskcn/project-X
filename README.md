# ProjectX

ProjectX is a job recruitment platform that allows businesses and freelance recruiters to manage job postings and applications. Candidates can search for jobs, apply, and track their application status.

## Technologies Used

- **Backend**: ASP.NET Core
- **Database**: Entity Framework Core
- **Authentication**: JWT Bearer Tokens
- **Authorization**: Role-based and policy-based authorization

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- SQL Server or any other compatible database
- Visual Studio or JetBrains Rider

### Installation

1. Clone the repository:
    ```sh
    git clone https://github.com/eriskcn/ProjectX.git
    cd ProjectX
    ```

2. Set up the database connection string in `appsettings.json`:
    ```json
    "ConnectionStrings": {
        "DefaultConnection": "YourDatabaseConnectionString"
    }
    ```

3. Apply database migrations:
    ```sh
    dotnet ef database update
    ```

4. Run the application:
    ```sh
    dotnet run
    ```
    
## Contributing

Contributions are welcome! Please fork the repository and submit a pull request.
