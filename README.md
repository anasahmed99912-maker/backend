# Secure Messaging API

ASP.NET Core backend for Cipherline, an end-to-end encrypted direct messaging
application.

## Features

- JWT authentication
- Username/password registration with BCrypt
- Google OAuth ID-token authentication
- SignalR real-time message delivery
- MongoDB persistence
- Conversation membership authorization
- Ciphertext-only text and image storage
- Replay protection and authentication rate limiting

Message encryption happens in the browser with ECDH P-256 and AES-256-GCM.
The API never receives plaintext messages or private encryption keys.

## Requirements

- .NET 10 SDK
- MongoDB or MongoDB Atlas

## Configuration

Configure secrets with environment variables or .NET user secrets:

```powershell
dotnet user-secrets set "MongoDb:ConnectionString" "your-mongodb-connection-string"
dotnet user-secrets set "Jwt:SecretKey" "a-long-random-secret-at-least-32-characters"
dotnet user-secrets set "GoogleOAuth:ClientId" "your-client-id.apps.googleusercontent.com"
```

For hosted environments, use double underscores:

```text
MongoDb__ConnectionString
MongoDb__DatabaseName
Jwt__SecretKey
GoogleOAuth__ClientId
AllowedOrigins__0
```

## Run

```powershell
dotnet run
```

Local endpoints:

- `http://localhost:5199`
- `https://localhost:7199`
- `GET /health`

## Build

```powershell
dotnet build
```
