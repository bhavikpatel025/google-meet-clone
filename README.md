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

**Video Meet Clone** is a full-featured video conferencing application inspired by Google Meet. Built with modern technologies, it provides a seamless experience for creating and joining virtual meetings with real-time video, audio, chat, screen sharing, reactions, and advanced meeting controls.

### Key Highlights

- 🎥 **Real-time Video & Audio** - WebRTC-powered peer-to-peer connections
- 💬 **Live Chat** - In-meeting messaging with SignalR
- 🖥️ **Screen Sharing** - Share your screen with participants
- 😊 **Reactions** - Express yourself with emoji reactions during calls
- ✋ **Hand Raise** - Request to speak with a visual indicator
- 🚪 **Waiting Room** - Host approval system before joining meetings
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
- **Waiting Room** - Host can review and approve participants before they join
- **Host Controls** - Advanced meeting management capabilities

### 🎥 Video Conferencing
- **Real-time Video/Audio** - High-quality WebRTC connections
- **Camera Controls** - Toggle camera on/off
- **Microphone Controls** - Mute/unmute audio
- **Dynamic Video Grid** - Automatically adjusts based on participant count
- **Profile Picture Fallback** - Display user avatar when camera is off
- **Camera/Microphone Status Indicators** - Visual feedback for all participants

### 🖥️ Screen Sharing
- **Desktop Sharing** - Share entire screen or specific windows
- **Application Sharing** - Share individual applications
- **Real-time Sync** - All participants see shared content instantly
- **Screen Share Indicators** - Visual cues when someone is presenting

### 💬 Chat System
- **Real-time Messaging** - Instant in-meeting chat
- **Message History** - View all messages sent during the meeting
- **System Notifications** - Alerts when users join/leave
- **Typing Indicators** - See when participants are typing

### 😊 Reactions & Interactions
- **Emoji Reactions** - Express emotions during the meeting
  - 👍 Thumbs Up
  - ❤️ Heart
  - 😂 Laugh
  - 👏 Clap
  - 🎉 Celebrate
  - And more!
- **Real-time Display** - Reactions appear as floating animations
- **Quick Access** - Easy-to-use reaction toolbar

### ✋ Hand Raise System
- **Request to Speak** - Raise your hand to get host's attention
- **Visual Indicator** - Hand icon appears next to participant's name
- **Priority Queue** - Host can see who raised hand first
- **Lower Hand** - Manually lower or auto-lower after speaking
- **Host Notifications** - Alert when participants raise hands

### 🚪 Waiting Room
- **Pre-Meeting Lobby** - Participants wait for host approval
- **Host Dashboard** - View all waiting participants
- **Admit/Deny Controls** - Accept or reject join requests
- **Bulk Admit** - Admit all waiting participants at once
- **Custom Waiting Messages** - Display information to waiting users
- **Auto-Admit Settings** - Option to disable waiting room for trusted users

### 👥 Participant Management
- **Participant List** - See all active participants with status indicators
- **Email Invitations** - Invite users via email
- **Join via Link** - Direct meeting access from invitation emails
- **Host Controls** - Manage participants, remove users if needed
- **Participant Status** - See who has camera/mic on, who raised hand
- **Kick/Remove** - Host can remove disruptive participants

### 👤 User Profile Features
- **Profile Picture Upload** - Store images in `backend/wwwroot/profile/`
- **Default Avatars** - First letter of name shown if no picture uploaded
- **Profile Display** - Show avatar instead of video when camera is off
- **Status Indicators** - Online/offline, in-meeting status

### 📧 Email Integration
- **SMTP Integration** - Send meeting invitations via email
- **Meeting Links** - Include direct join links in emails
- **Invitation Templates** - Professional email templates
- **Reminder Emails** - Automated meeting reminders (optional)

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
│  • Waiting Room (Lobby)          │
└────────────┬─────────────────────┘
             │
┌────────────▼─────────────────────┐
│       Services (Logic)           │
│  • Auth Service                  │
│  • Meeting Service               │
│  • SignalR Service               │
│  • WebRTC Service                │
│  • Reaction Service              │
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
3. **Create Meeting** - Get a meeting code and enable waiting room
4. **Join Meeting** - Open incognito window, join with code (will enter waiting room)
5. **Admit Participant** - Host admits from waiting room
6. Enable camera/microphone when prompted
7. Test new features:
   - 😊 Click reaction button and select emoji
   - ✋ Raise hand to request to speak
   - 💬 Send chat messages
   - 🖥️ Share your screen
8. Enjoy your enhanced video meeting! 🎉

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
  },
  "MeetingSettings": {
    "WaitingRoomEnabled": true,
    "MaxParticipants": 50,
    "ReactionDisplayDuration": 3000
  }
}
```

### Frontend Configuration

**File:** `frontend/src/environments/environment.ts`

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7001/api',
  hubUrl: 'https://localhost:7001/hubs/videocall',
  features: {
    waitingRoom: true,
    reactions: true,
    handRaise: true
  }
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
   - Scheduled time (optional)
   - Enable/disable waiting room
4. Click **"Create"** to generate meeting code
5. **Share the code** with participants

### Joining a Meeting

**Method 1: Meeting Code**
1. Click **"Join Meeting"**
2. Enter the 8-character code
3. If waiting room is enabled, wait for host approval
4. Click **"Join"** once admitted

**Method 2: Email Invitation**
1. Click the **meeting link** in invitation email
2. Enter waiting room if enabled
3. Automatically joins once host admits you

**Method 3: My Meetings**
1. Go to **"My Meetings"**
2. Click **"Join"** on any active meeting
3. Bypass waiting room as meeting creator

### Waiting Room (Host)

**As Meeting Host:**
1. See **"Waiting Room"** badge with count
2. Click to view all waiting participants
3. Review participant details
4. **Admit** individual participants
5. **Admit All** to let everyone in at once
6. **Deny** to reject join requests
7. Receive notifications when new participants arrive

**As Participant:**
1. Enter meeting code and click join
2. See **"Waiting for host..."** message
3. Wait patiently for approval
4. Automatically enter meeting once admitted

### During a Meeting

**Video Controls:**
- 📹 Toggle camera on/off
- 🎤 Mute/unmute microphone
- 🖥️ Share screen
- 📱 View participants
- 💬 Open chat panel
- 😊 Send reactions
- ✋ Raise/lower hand

**Using Reactions:**
1. Click the **Reaction** button (😊 icon)
2. Select from emoji panel:
   - 👍 Thumbs Up
   - ❤️ Heart
   - 😂 Laugh
   - 👏 Clap
   - 🎉 Celebrate
   - 😮 Surprised
   - 🤔 Thinking
   - 👋 Wave
3. Your reaction appears as floating animation
4. All participants see your reaction in real-time
5. Reactions auto-disappear after 3 seconds

**Using Hand Raise:**
1. Click the **Hand Raise** button (✋ icon)
2. Your hand icon appears next to your name
3. All participants see you raised your hand
4. Host receives notification
5. Click again to lower your hand
6. Or hand auto-lowers when you start speaking

**Host Controls:**
- 📧 Invite participants via email
- 🚪 Manage waiting room
- ✋ See all raised hands with timestamps
- ❌ Remove participants if needed
- 🔇 Mute all participants
- 🎬 End meeting for all

**Participant Features:**
- 👋 See all participants with status indicators
- 💬 Send chat messages
- 😊 React with emojis
- ✋ Raise hand to speak
- 📤 Leave meeting
- 🖥️ View screen shares

### Uploading Profile Picture

1. Go to **Profile Settings**
2. Click **"Upload Picture"**
3. Select image file (JPG, PNG)
4. Image saves to `backend/wwwroot/profile/`
5. Picture displays when camera is off
6. Appears in waiting room and participant list

---

## 📁 Project Structure

```
video-meet-clone/
│
├── backend/
│   ├── VideoCallApp.API/              # API Layer
│   │   ├── Controllers/               # REST endpoints
│   │   │   ├── AuthController.cs
│   │   │   ├── MeetingController.cs
│   │   │   ├── ChatController.cs
│   │   │   ├── ProfileController.cs
│   │   │   └── WaitingRoomController.cs
│   │   ├── Hubs/                      # SignalR hubs
│   │   │   ├── VideoCallHub.cs
│   │   │   └── WaitingRoomHub.cs
│   │   ├── Middleware/                # Error handling
│   │   ├── wwwroot/                   # Static files
│   │   │   └── profile/               # Profile pictures
│   │   └── appsettings.json           # Configuration
│   │
│   ├── VideoCallApp.Application/      # Application Layer
│   │   ├── DTOs/                      # Data transfer objects
│   │   │   ├── MeetingDto.cs
│   │   │   ├── ParticipantDto.cs
│   │   │   ├── ReactionDto.cs
│   │   │   ├── HandRaiseDto.cs
│   │   │   └── WaitingRoomDto.cs
│   │   ├── Interfaces/                # Service contracts
│   │   │   ├── IMeetingService.cs
│   │   │   ├── IReactionService.cs
│   │   │   └── IWaitingRoomService.cs
│   │   ├── Validators/                # Input validation
│   │   └── Mappings/                  # AutoMapper profiles
│   │
│   ├── VideoCallApp.Infrastructure/   # Infrastructure Layer
│   │   ├── Services/                  # Service implementations
│   │   │   ├── AuthService.cs
│   │   │   ├── MeetingService.cs
│   │   │   ├── EmailService.cs
│   │   │   ├── StorageService.cs
│   │   │   ├── ReactionService.cs
│   │   │   └── WaitingRoomService.cs
│   │   └── Configuration/
│   │
│   ├── VideoCallApp.Persistence/      # Persistence Layer
│   │   ├── Data/                      # DbContext
│   │   │   └── ApplicationDbContext.cs
│   │   └── Migrations/                # EF migrations
│   │
│   └── VideoCallApp.Domain/           # Domain Layer
│       └── Entities/                  # Domain models
│           ├── User.cs
│           ├── Meeting.cs
│           ├── Participant.cs
│           ├── Message.cs
│           ├── Reaction.cs
│           ├── HandRaise.cs
│           └── WaitingRoomEntry.cs
│
├── frontend/
│   ├── src/
│   │   ├── app/
│   │   │   ├── components/
│   │   │   │   ├── auth/              # Login, Register
│   │   │   │   │   ├── login/
│   │   │   │   │   └── register/
│   │   │   │   ├── meeting/           # Meeting components
│   │   │   │   │   ├── create-meeting/
│   │   │   │   │   ├── join-meeting/
│   │   │   │   │   ├── meeting-room/
│   │   │   │   │   ├── waiting-room/
│   │   │   │   │   ├── participants-list/
│   │   │   │   │   ├── reactions-panel/
│   │   │   │   │   └── hand-raise-indicator/
│   │   │   │   ├── profile/           # Profile management
│   │   │   │   └── chat/              # Chat panel
│   │   │   ├── services/              # Angular services
│   │   │   │   ├── auth.service.ts
│   │   │   │   ├── meeting.service.ts
│   │   │   │   ├── signalr.service.ts
│   │   │   │   ├── webrtc.service.ts
│   │   │   │   ├── reaction.service.ts
│   │   │   │   ├── hand-raise.service.ts
│   │   │   │   └── waiting-room.service.ts
│   │   │   ├── models/                # TypeScript interfaces
│   │   │   │   ├── meeting.model.ts
│   │   │   │   ├── participant.model.ts
│   │   │   │   ├── reaction.model.ts
│   │   │   │   └── waiting-room.model.ts
│   │   │   ├── guards/                # Route guards
│   │   │   └── interceptors/          # HTTP interceptors
│   │   ├── environments/              # Environment configs
│   │   └── assets/                    # Images, icons
│   │       └── emojis/                # Reaction emojis
│   │
│   ├── package.json
│   └── angular.json
│
├── docs/                              # Documentation
│   ├── SETUP_GUIDE.md
│   ├── API_DOCUMENTATION.md
│   ├── ARCHITECTURE.md
│   ├── FEATURES.md
│   └── TROUBLESHOOTING.md
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

### Create Meeting (with Waiting Room Option)
*Coming soon - Add screenshot of meeting creation form with waiting room toggle*

### Waiting Room - Host View
*Coming soon - Add screenshot of host waiting room dashboard with pending participants*

### Waiting Room - Participant View
*Coming soon - Add screenshot of waiting room from participant perspective*

### Meeting Room - Video Grid with Reactions
*Coming soon - Add screenshot of active meeting with floating emoji reactions*

### Hand Raise Indicator
*Coming soon - Add screenshot showing raised hand icon next to participant name*

### Reaction Panel
*Coming soon - Add screenshot of emoji reaction selector*

### Chat Panel
*Coming soon - Add screenshot of in-meeting chat*

### Participant List with Status Indicators
*Coming soon - Add screenshot of participants panel showing camera/mic/hand status*

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
| PUT | `/api/Meeting/{id}/waiting-room` | Toggle waiting room |

### Waiting Room Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/WaitingRoom/{meetingId}` | Get waiting participants |
| POST | `/api/WaitingRoom/admit` | Admit participant |
| POST | `/api/WaitingRoom/admit-all` | Admit all waiting |
| POST | `/api/WaitingRoom/deny` | Deny participant |

### Reaction Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/Reaction` | Send reaction |
| GET | `/api/Reaction/meeting/{id}` | Get recent reactions |

### Hand Raise Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/HandRaise/raise` | Raise hand |
| POST | `/api/HandRaise/lower` | Lower hand |
| GET | `/api/HandRaise/meeting/{id}` | Get raised hands |

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
- `ReactionReceived` - Emoji reaction sent
- `HandRaised` - Participant raised hand
- `HandLowered` - Participant lowered hand
- `ParticipantWaiting` - New participant in waiting room
- `ParticipantAdmitted` - Participant admitted from waiting room
- `ParticipantDenied` - Participant denied entry

**Client → Server:**
- `JoinMeeting` - Join meeting room
- `LeaveMeeting` - Leave meeting room
- `SendOffer` - Send WebRTC offer
- `SendAnswer` - Send WebRTC answer
- `SendIceCandidate` - Send ICE candidate
- `SendMessage` - Send chat message
- `SendReaction` - Send emoji reaction
- `RaiseHand` - Raise hand
- `LowerHand` - Lower hand
- `RequestJoinMeeting` - Request to join (waiting room)

For detailed API documentation, visit `/swagger` when backend is running.

---

## 🔮 Future Improvements

### Planned Features
- [ ] **Meeting Recording** - Record and save meetings
- [ ] **Breakout Rooms** - Split participants into smaller groups
- [ ] **Live Captions** - Real-time speech-to-text
- [ ] **Virtual Backgrounds** - Custom/blurred backgrounds
- [ ] **Whiteboard** - Collaborative drawing tool
- [ ] **File Sharing** - Share documents during meetings
- [ ] **Meeting Analytics** - Duration, participants stats
- [ ] **Polls & Surveys** - Interactive voting during meetings
- [ ] **Q&A Session** - Structured question and answer mode
- [ ] **Language Translation** - Real-time message translation

### Technical Improvements
- [ ] **Mobile Apps** - iOS and Android native apps
- [ ] **Docker Support** - Containerization
- [ ] **Kubernetes** - Orchestration support
- [ ] **Redis Cache** - Performance optimization
- [ ] **CDN Integration** - Faster asset delivery
- [ ] **Load Balancing** - Handle more concurrent users
- [ ] **End-to-End Encryption** - Enhanced security
- [ ] **OAuth Integration** - Google/Microsoft login
- [ ] **WebSocket Optimization** - Better real-time performance
- [ ] **Media Server** - Dedicated SFU for large meetings

### UI/UX Enhancements
- [ ] **Dark Mode** - Theme switching
- [ ] **Accessibility** - WCAG compliance
- [ ] **Internationalization** - Multi-language support
- [ ] **Custom Themes** - Branded meeting rooms
- [ ] **Mobile Responsive** - Better mobile experience
- [ ] **Keyboard Shortcuts** - Quick actions
- [ ] **Picture-in-Picture** - Floating video window
- [ ] **Grid View Options** - Multiple layout modes

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

- Follow existing code style and architecture
- Write meaningful commit messages
- Add tests for new features
- Update documentation
- Ensure all tests pass
- Test on multiple browsers/devices

### Development Setup

```bash
# Install backend dependencies
cd backend/VideoCallApp.API
dotnet restore

# Install frontend dependencies
cd frontend/video-call-frontend
npm install

# Run database migrations
dotnet ef database update

# Start development
# Terminal 1: Backend
dotnet watch run

# Terminal 2: Frontend
ng serve --ssl
```

---

## 🐛 Bug Reports

Found a bug? Please open an issue with:
- Description of the bug
- Steps to reproduce
- Expected behavior
- Screenshots (if applicable)
- Environment details (OS, browser, versions)
- Error messages or console logs

---

## 💬 Support

Need help? You can:

- 📧 Email: patelbhavik.0017@gmail.com
- 🐛 Issues: [GitHub Issues](https://github.com/bhavikpatel025/video-meet-clone/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/bhavikpatel025/video-meet-clone/discussions)

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🌟 Acknowledgments

- **Google Meet** - Inspiration for UI/UX
- **WebRTC** - Real-time communication technology
- **.NET Team** - Amazing framework
- **Angular Team** - Powerful frontend framework
- **SignalR Team** - Real-time messaging solution
- **Open Source Community** - Libraries and tools

---

## 📊 Project Statistics

- **Lines of Code:** ~15,000+
- **Components:** 25+
- **Services:** 12+
- **API Endpoints:** 30+
- **Database Tables:** 10+
- **SignalR Events:** 20+

---

## 🎉 Recent Updates

### Version 2.0.0 - New Interactive Features
- ✨ Added emoji reactions during meetings
- ✨ Implemented hand raise system for speaker requests
- ✨ Built waiting room with host approval system
- 🐛 Fixed camera toggle issues
- 🐛 Improved WebRTC connection stability
- 📚 Enhanced documentation

---

<div align="center">

### Made with ❤️ by [Bhavik Patel](https://github.com/bhavikpatel025)

**⭐ Star this repo if you find it helpful!**

</div>