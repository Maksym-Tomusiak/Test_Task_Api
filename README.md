"# Test Task API

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server (LocalDB or full instance)

## Setup Instructions

### 1. Configure User Secrets

Navigate to the API project directory:

```bash
cd TestTaskApi/src/Api
```

Initialize user secrets:

```bash
dotnet user-secrets init
```

Set the required secrets:

```bash
dotnet user-secrets set "EmailSettings:SmtpHost" "smtp.gmail.com"
dotnet user-secrets set "EmailSettings:SmtpPort" "465"
dotnet user-secrets set "EmailSettings:SenderEmail" "mrmarkizik@gmail.com"
dotnet user-secrets set "EmailSettings:SenderPassword" "kbds lkte jets jijw"
dotnet user-secrets set "EmailSettings:SenderName" "test_task"
dotnet user-secrets set "Encryption:Key" "u7k1+sIp3/Xy7f9Q2qL5vP8xR9kL4mZ1n3oP6qR8tYk="
dotnet user-secrets set "FrontendUrl" "http://localhost:5173/"
```

### 2. Run the Application

From the `TestTaskApi/src/Api` directory:

```bash
dotnet run
```

The API will start on `http://localhost:5204` by default."
