# ProjectX

**ProjectX** is a comprehensive job recruitment platform that connects businesses and freelance recruiters with candidates. The platform allows recruiters to manage job postings and applications, while candidates can search for jobs, apply, and track their application status.

---

## Features

- **User Authentication & Authorization**: Secure JWT-based authentication with role-based access control  
- **Job Management**: Create, search, and apply for job listings with detailed filtering options  
- **Campaign System**: Organize multiple job listings into recruitment campaigns  
- **Application Tracking**: Track application status through the entire recruitment process  
- **Company Profiles**: Detailed company information and ratings  
- **Recruiter Verification**: Business and freelance recruiter verification system  
- **Messaging System**: In-app communication between recruiters and candidates  
- **Social Features**: Post sharing and interaction capabilities  
- **Notification System**: Real-time notifications for important events  
- **Token-based Economy**: Access premium features using platform tokens  

---

## Technologies Used

- **Backend**: ASP.NET Core 8.0  
- **Database**: Entity Framework Core with SQL Server  
- **Authentication**: JWT Bearer Tokens  
- **Authorization**: Role-based and policy-based authorization  
- **Real-time Communication**: SignalR  
- **API Documentation**: Swagger  
- **Background Services**: Automated database backups, appointment reminders, and package management  

---

## Architecture

The application follows a RESTful API architecture with the following components:

- **Controllers**: Handle HTTP requests and responses  
- **Services**: Implement business logic  
- **Models**: Define database entities  
- **DTOs**: Data transfer objects for API responses  
- **Helpers**: Utility functions for common tasks  

---

## API Structure

The API follows RESTful principles with resource-based URLs and standard HTTP methods. All routes follow the base path pattern:

```

/capablanca/api/v0/

````

### Major API Sections

- **Authentication API**  
- **Job API**  
- **Campaign API**  
- **Application API**  
- **Company API**  
- **Business/Freelance Recruiter APIs**  
- **Social APIs**  
- **Messaging API**  
- **Reference Data APIs**  

---

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later  
- SQL Server or any other compatible database  
- Visual Studio or JetBrains Rider  

### Installation

Clone the repository:

```bash
git clone https://github.com/eriskcn/project-X.git  
cd project-X
````

Set up the database connection string in `appsettings.json`:

```json
"ConnectionStrings": {  
    "DefaultConnection": "YourDatabaseConnectionString"  
}
```

Apply database migrations:

```bash
dotnet ef database update
```

Run the application:

```bash
dotnet run
```

---

## Configuration

The application uses environment variables for configuration. Create a `.env` file in the root directory with the following variables:

```env
SA_PASSWORD=YourDatabasePassword  
JWT_SECRET=YourJwtSecretKey  
GOOGLE_CLIENT_ID=YourGoogleClientId  
GOOGLE_CLIENT_SECRET=YourGoogleClientSecret  
```

---

## Development

### CORS Configuration

The application is configured to work with a Next.js frontend running on:

```
http://localhost:3000
```

You can modify the CORS settings in `Program.cs` if needed.

### Background Services

The application includes several background services:

* **Database backup service** (runs daily at 2 AM)
* **Appointment reminder service**
* **Business package expiration service**

---

## Rate Limiting

API rate limiting is implemented to prevent abuse:

* **General endpoints**: 3 requests per 10 seconds
* **Login endpoints**: 5 requests per window

---

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request.

---

## License

This project is licensed under the **MIT License** – see the `LICENSE` file for details.

---



