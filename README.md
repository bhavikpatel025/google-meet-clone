# 🎥 Video Meet Clone - Google Meet Alternative

<div align="center">

![Video Meeting App](https://img.shields.io/badge/Video-Meeting-blue?style=for-the-badge&logo=googlemeet)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![Angular 19](https://img.shields.io/badge/Angular-19-DD0031?style=for-the-badge&logo=angular)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**A professional video conferencing application built with .NET 8 and Angular 19**

[Features](#-features) • [Tech Stack](#-tech-stack) • [Installation](#-installation) • [Usage](#-usage) • [Screenshots](#-screenshots)

</div>

---

## 📋 Table of Contents

- [About](#-about)
- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Architecture](#-architecture)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Usage](#-usage)
- [Project Structure](#-project-structure)
- [Screenshots](#-screenshots)
- [API Documentation](#-api-documentation)
- [Future Improvements](#-future-improvements)
- [Contributing](#-contributing)

---

## 🎯 About

**Video Meet Clone** is a full-featured video conferencing application inspired by Google Meet. Built with modern technologies, it provides a seamless experience for creating and joining virtual meetings with real-time video, audio, chat, and screen sharing capabilities.

### Key Highlights

- 🎥 **Real-time Video & Audio** - WebRTC-powered peer-to-peer connections
- 💬 **Live Chat** - In-meeting messaging with SignalR
- 🖥️ **Screen Sharing** - Share your screen with participants
- 📧 **Email Invitations** - Send meeting links via email
- 👤 **Profile Pictures** - Upload and display user avatars
- 🎨 **Responsive Design** - Works seamlessly on desktop and mobile
- 🔒 **Secure Authentication** - JWT-based authentication system

---

## ✨ Features

### 🔐 Authentication
- **User Registration** - Sign up with email and password
- **User Login** - Secure JWT-based authentication
- **Profile Management** - Update user information and profile pictures

### 📅 Meeting Management
- **Instant Meetings** - Create and join meetings immediately
- **Scheduled Meetings** - Plan meetings for future dates
- **Meeting Codes** - Unique 8-character codes for easy joining
- **Meeting History** - View all past and upcoming meetings

### 🎥 Video Conferencing
- **Real-time Video/Audio** - High-quality WebRTC connections
- **Camera Controls** - Toggle camera on/off
- **Microphone Controls** - Mute/unmute audio
- **Dynamic Video Grid** - Automatically adjusts based on participant count
- **Profile Picture Fallback** - Display user avatar when camera is off

### 🖥️ Screen Sharing
- **Desktop Sharing** - Share entire screen or specific windows
- **Application Sharing** - Share individual applications
- **Real-time Sync** - All participants see shared content instantly

### 💬 Chat System
- **Real-time Messaging** - Instant in-meeting chat
- **Message History** - View all messages sent during the meeting
- **System Notifications** - Alerts when users join/leave

### 👥 Participant Management
- **Participant List** - See all active participants
- **Email Invitations** - Invite users via email
- **Join via Link** - Direct meeting access from invitation emails
- **Host Controls** - Meeting host can manage participants

### 👤 User Profile Features
- **Profile Picture Upload** - Store images in `backend/wwwroot/profile/`
- **Default Avatars** - First letter of name shown if no picture uploaded
- **Profile Display** - Show avatar instead of video when camera is off

### 📧 Email Integration
- **SMTP Integration** - Send meeting invitations via email
- **Meeting Links** - Include direct join links in emails
- **Invitation Templates** - Professional email templates

---

## 🛠️ Tech Stack

### Frontend
| Technology | Description |
|-----------|-------------|
| ![Angular](https://img.shields.io/badge/Angular-19-DD0031?logo=angular) | Progressive web framework |
| ![TypeScript](https://img.shields.io/badge/TypeScript-5.5-3178C6?logo=typescript) | Type-safe JavaScript |
| ![Angular Material](https://img.shields.io/badge/Material-UI-0081CB?logo=material-ui) | Material Design components |
| ![Bootstrap](https://img.shields.io/badge/Bootstrap-5-7952B3?logo=bootstrap) | Responsive layout framework |
| ![WebRTC](https://img.shields.io/badge/WebRTC-API-333333?logo=webrtc) | Real-time communication |
| ![SignalR](https://img.shields.io/badge/SignalR-Client-512BD4?logo=dotnet) | Real-time messaging |
| ![RxJS](https://img.shields.io/badge/RxJS-7.8-B7178C?logo=reactivex) | Reactive programming |

### Backend
| Technology | Description |
|-----------|-------------|
| ![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet) | Cross-platform framework |
| ![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp) | Programming language |
| ![SignalR](https://img.shields.io/badge/SignalR-Server-512BD4?logo=dotnet) | Real-time communication |
| ![Entity Framework](https://img.shields.io/badge/EF_Core-8.0-512BD4?logo=dotnet) | ORM for database |
| ![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?logo=microsoftsqlserver) | Database system |
| ![JWT](https://img.shields.io/badge/JWT-Auth-000000?logo=jsonwebtokens) | Authentication |
| ![AutoMapper](https://img.shields.io/badge/AutoMapper-13.0-BE2D24) | Object mapping |
| ![Serilog](https://img.shields.io/badge/Serilog-Logging-1E88E5) | Structured logging |

### Architecture
- **Clean Architecture** - Separation of concerns with 5 layers
- **Repository Pattern** - Data access abstraction
- **Dependency Injection** - Loosely coupled components
- **SOLID Principles** - Maintainable and scalable code

---

## 🏗️ Architecture

### Backend - Clean Architecture Layers

```
┌─────────────────────────────────────────┐
│         API Layer (Presentation)        │
│  • Controllers (REST endpoints)         │
│  • SignalR Hubs (Real-time)             │
│  • Middleware (Error handling)          │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│      Application Layer (Contracts)      │
│  • DTOs (Data Transfer Objects)         │
│  • Interfaces (Service contracts)       │
│  • Validators (Input validation)        │
│  • Mappings (AutoMapper profiles)       │
└────────────────┬────────────────────────┘
                 │
        ┌────────┴────────┐
        │                 │
┌───────▼──────┐  ┌───────▼────────┐
│Infrastructure│  │  Persistence   │
│  • Services  │  │  • DbContext   │
│  • Email     │  │  • Migrations  │
│  • Storage   │  │  • Config      │
└──────────────┘  └────────────────┘
        │                 │
        └────────┬────────┘
                 │
        ┌────────▼────────┐
        │  Domain Layer   │
        │  • Entities     │
        │  • Enums        │
        └─────────────────┘
```

### Frontend - Component Architecture

```
┌──────────────────────────────────┐
│       Components (UI)            │
│  • Auth (Login, Register)        │
│  • Meeting (Create, Join, Room)  │
│  • Profile (Settings, Upload)    │
└────────────┬─────────────────────┘
             │
┌────────────▼─────────────────────┐
│       Services (Logic)           │
│  • Auth Service                  │
│  • Meeting Service               │
│  • SignalR Service               │
│  • WebRTC Service                │
└────────────┬─────────────────────┘
             │
┌────────────▼─────────────────────┐
│    HTTP/WebSocket/WebRTC         │
│  • REST API calls                │
│  • SignalR real-time             │
│  • Peer connections              │
└──────────────────────────────────┘
```

---

## 📥 Installation

### Prerequisites

Before you begin, ensure you have the following installed:

- ✅ [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- ✅ [Node.js](https://nodejs.org/) (v18 or higher)
- ✅ [Angular CLI](https://angular.io/cli) v19
- ✅ [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (Express or LocalDB)
- ✅ [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio 2022](https://visualstudio.microsoft.com/)

### Verify Installation

```bash
# Check .NET version
dotnet --version
# Should show: 8.0.x

# Check Node.js version
node --version
# Should show: v18.x.x or higher

# Check Angular CLI
ng version
# Should show: Angular CLI: 19.x.x
```

---

## 🚀 Quick Start

### 1️⃣ Clone Repository

```bash
git clone https://github.com/yourusername/video-meet-clone.git
cd video-meet-clone
```

### 2️⃣ Backend Setup

```bash
# Navigate to backend folder
cd backend/VideoCallApp.API

# Restore NuGet packages
dotnet restore

# Update database connection string in appsettings.json
# (See Configuration section below)

# Run database migrations (auto-runs on first start)
dotnet run

# Backend will start on https://localhost:7001
```

**Swagger UI:** Open https://localhost:7001/swagger to test APIs

### 3️⃣ Frontend Setup

```bash
# Navigate to frontend folder (from root)
cd frontend/video-call-frontend

# Install npm packages
npm install

# Start development server
ng serve --ssl

# Frontend will start on https://localhost:4200
```

**Application URL:** Open https://localhost:4200 in your browser

### 4️⃣ Test the Application

1. **Register** a new user at `/register`
2. **Login** with your credentials
3. **Create Meeting** - Get a meeting code
4. **Join Meeting** - Use the code to join
5. Open **incognito window** to test with second user
6. Enable camera/microphone when prompted
7. Test video call features! 🎉

---

## ⚙️ Configuration

### Backend Configuration

**File:** `backend/VideoCallApp.API/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=VideoCallDB;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "Secret": "Your-Super-Secret-Key-Must-Be-At-Least-32-Characters-Long",
    "Issuer": "VideoCallApp",
    "Audience": "VideoCallApp",
    "ExpiryInHours": 24
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderName": "Video Meet Clone",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

### Frontend Configuration

**File:** `frontend/src/environments/environment.ts`

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7001/api',
  hubUrl: 'https://localhost:7001/hubs/videocall'
};
```

### Email Configuration (Gmail Example)

1. **Enable 2-Factor Authentication** in your Gmail account
2. **Generate App Password:**
   - Go to Google Account → Security
   - Select "2-Step Verification"
   - Scroll to "App passwords"
   - Generate password for "Mail"
3. **Update `appsettings.json`** with the app password

### Database Configuration

**SQL Server Express:**
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=VideoCallDB;Trusted_Connection=True;TrustServerCertificate=True"
```

**LocalDB:**
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=VideoCallDB;Trusted_Connection=True;TrustServerCertificate=True"
```

**SQL Server with Authentication:**
```json
"DefaultConnection": "Server=localhost;Database=VideoCallDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
```

---

## 📖 Usage

### Creating a Meeting

1. **Login** to your account
2. Click **"Create Meeting"**
3. Fill in meeting details:
   - Title
   
4. Click **"Create"** to generate meeting code
5. **Share the code** with participants

### Joining a Meeting

**Method 1: Meeting Code**
1. Click **"Join Meeting"**
2. Enter the 8-character code
3. Click **"Join"**

**Method 2: Email Invitation**
1. Click the **meeting link** in invitation email
2. Automatically joins the meeting

**Method 3: My Meetings**
1. Go to **"My Meetings"**
2. Click **"Join"** on any active meeting

### During a Meeting

**Video Controls:**
- 📹 Toggle camera on/off
- 🎤 Mute/unmute microphone
- 🖥️ Share screen
- 📱 View participants
- 💬 Open chat panel

**Host Controls:**
- 📧 Invite participants via email
- ❌ End meeting for all

**Participant Features:**
- 👋 See all participants
- 💬 Send chat messages
- 📤 Leave meeting

### Uploading Profile Picture

1. Go to **Profile Settings**
2. Click **"Upload Picture"**
3. Select image file (JPG, PNG)
4. Image saves to `backend/wwwroot/profile/`
5. Picture displays when camera is off

---

## 📁 Project Structure

```
video-meet-clone/
│
├── backend/
│   ├── VideoCallApp.API/              # API Layer
│   │   ├── Controllers/               # REST endpoints
│   │   ├── Hubs/                      # SignalR hubs
│   │   ├── Middleware/                # Error handling
│   │   ├── wwwroot/                   # Static files
│   │   │   └── profile/               # Profile pictures
│   │   └── appsettings.json           # Configuration
│   │
│   ├── VideoCallApp.Application/      # Application Layer
│   │   ├── DTOs/                      # Data transfer objects
│   │   ├── Interfaces/                # Service contracts
│   │   ├── Validators/                # Input validation
│   │   └── Mappings/                  # AutoMapper profiles
│   │
│   ├── VideoCallApp.Infrastructure/   # Infrastructure Layer
│   │   ├── Services/                  # Service implementations
│   │   │   ├── AuthService.cs
│   │   │   ├── MeetingService.cs
│   │   │   ├── EmailService.cs
│   │   │   └── StorageService.cs
│   │   └── Configuration/
│   │
│   ├── VideoCallApp.Persistence/      # Persistence Layer
│   │   ├── Data/                      # DbContext
│   │   └── Migrations/                # EF migrations
│   │
│   └── VideoCallApp.Domain/           # Domain Layer
│       └── Entities/                  # Domain models
│
├── frontend/
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/
│   │   │   │   ├── auth/              # Login, Register
│   │   │   │   ├── meeting/           # Meeting components
│   │   │   │   └── profile/           # Profile management
│   │   │   ├── services/              # Angular services
│   │   │   │   ├── auth.service.ts
│   │   │   │   ├── meeting.service.ts
│   │   │   │   ├── signalr.service.ts
│   │   │   │   └── webrtc.service.ts
│   │   │   ├── models/                # TypeScript interfaces
│   │   │   ├── guards/                # Route guards
│   │   │   └── interceptors/          # HTTP interceptors
│   │   ├── environments/              # Environment configs
│   │   └── assets/                    # Images, icons
│   │
│   ├── package.json
│   └── angular.json
│
├── docs/                              # Documentation
│   ├── SETUP_GUIDE.md
│   ├── API_DOCUMENTATION.md
│   └── ARCHITECTURE.md
│
├── .gitignore
├── LICENSE
└── README.md
```

---

## 📸 Screenshots

### Login Page
*Coming soon - Add screenshot of login page*

### Dashboard (My Meetings)
*Coming soon - Add screenshot of meeting list*

### Create Meeting
*Coming soon - Add screenshot of meeting creation form*

### Meeting Room - Video Grid
*Coming soon - Add screenshot of active meeting with video grid*

### Chat Panel
*Coming soon - Add screenshot of in-meeting chat*

### Participant List
*Coming soon - Add screenshot of participants panel*

### Profile Picture Upload
*Coming soon - Add screenshot of profile settings*

### Email Invitation
*Coming soon - Add screenshot of email invitation template*

---

## 📚 API Documentation

### Authentication Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Auth/register` | Register new user |
| POST | `/api/Auth/login` | Login user |

### Meeting Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Meeting` | Create new meeting |
| GET | `/api/Meeting/code/{code}` | Get meeting by code |
| GET | `/api/Meeting/my-meetings` | Get user's meetings |
| POST | `/api/Meeting/join` | Join meeting |
| POST | `/api/Meeting/{id}/leave` | Leave meeting |
| POST | `/api/Meeting/{id}/end` | End meeting (host) |
| GET | `/api/Meeting/{id}/participants` | Get participants |

### Chat Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Chat/meeting/{id}` | Send message |
| GET | `/api/Chat/meeting/{id}` | Get messages |

### Profile Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Profile/upload` | Upload profile picture |
| GET | `/api/Profile/picture/{userId}` | Get profile picture |

### SignalR Hub Events

**Server → Client:**
- `UserJoined` - New participant joined
- `UserLeft` - Participant left
- `NewMessage` - Chat message received
- `CameraToggled` - Camera state changed
- `MicrophoneToggled` - Microphone state changed
- `ScreenShareToggled` - Screen share state changed

**Client → Server:**
- `JoinMeeting` - Join meeting room
- `LeaveMeeting` - Leave meeting room
- `SendOffer` - Send WebRTC offer
- `SendAnswer` - Send WebRTC answer
- `SendIceCandidate` - Send ICE candidate
- `SendMessage` - Send chat message

For detailed API documentation, visit `/swagger` when backend is running.

---

## 🔮 Future Improvements

### Planned Features
- [ ] **Meeting Recording** - Record and save meetings
- [ ] **Breakout Rooms** - Split participants into smaller groups
- [ ] **Live Captions** - Real-time speech-to-text
- [ ] **Virtual Backgrounds** - Custom/blurred backgrounds
- [ ] **Reactions** - Emoji reactions during calls
- [ ] **Hand Raise** - Request to speak indicator
- [ ] **Whiteboard** - Collaborative drawing tool
- [ ] **File Sharing** - Share documents during meetings
- [ ] **Waiting Room** - Host approval before joining
- [ ] **Meeting Analytics** - Duration, participants stats

### Technical Improvements
- [ ] **Mobile Apps** - iOS and Android native apps
- [ ] **Docker Support** - Containerization
- [ ] **Kubernetes** - Orchestration support
- [ ] **Redis Cache** - Performance optimization
- [ ] **CDN Integration** - Faster asset delivery
- [ ] **Load Balancing** - Handle more concurrent users
- [ ] **End-to-End Encryption** - Enhanced security
- [ ] **OAuth Integration** - Google/Microsoft login

### UI/UX Enhancements
- [ ] **Dark Mode** - Theme switching
- [ ] **Accessibility** - WCAG compliance
- [ ] **Internationalization** - Multi-language support
- [ ] **Custom Themes** - Branded meeting rooms
- [ ] **Mobile Responsive** - Better mobile experience

---

## 🤝 Contributing

We welcome contributions! Please follow these steps:

1. **Fork the repository**
2. **Create a feature branch**
   ```bash
   git checkout -b feature/AmazingFeature
   ```
3. **Commit your changes**
   ```bash
   git commit -m 'Add some AmazingFeature'
   ```
4. **Push to the branch**
   ```bash
   git push origin feature/AmazingFeature
   ```
5. **Open a Pull Request**

### Contribution Guidelines

- Follow existing code style
- Write meaningful commit messages
- Add tests for new features
- Update documentation
- Ensure all tests pass

---

## 🐛 Bug Reports

Found a bug? Please open an issue with:
- Description of the bug
- Steps to reproduce
- Expected behavior
- Screenshots (if applicable)
- Environment details

---

## 💬 Support

Need help? You can:

- 📧 Email: patelbhavik.0017@gmail.com

---

## 🌟 Acknowledgments

- **Google Meet** - Inspiration for UI/UX
- **WebRTC** - Real-time communication technology
- **.NET Team** - Amazing framework
- **Angular Team** - Powerful frontend framework
- **Open Source Community** - Libraries and tools

---

Made with ❤️ by Bhavik patel(https://github.com/bhavikpatel025)