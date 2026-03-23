export interface CreateMeetingRequest {
  title?: string;
  description?: string;
}

export interface MeetingResponse {
  id: number;
  meetingCode: string;
  title: string;
  description?: string;
  hostId: string;
  hostName: string;
  actualStartTime?: Date;
  endTime?: Date;
  isActive: boolean;
  status: string;
  currentParticipants: number;
  createdAt: Date;
}

export interface Participant {
  id: number;
  userId: string;
  userName: string;
  email: string;
  profilePictureUrl?: string | null;
  isCameraOn: boolean;
  isMicrophoneOn: boolean;
  isScreenSharing: boolean;
  role: string;
  joinedAt: Date;
  connectionId?: string;
}

export interface JoinMeetingRequest {
  meetingCode: string;
}

export interface UpdateMediaStateRequest {
  isCameraOn?: boolean;
  isMicrophoneOn?: boolean;
  isScreenSharing?: boolean;
}

export interface WebRTCOffer {
  fromUserId: string;
  offer: string;
}

export interface WebRTCAnswer {
  fromUserId: string;
  answer: string;
}

export interface WebRTCIceCandidate {
  fromUserId: string;
  candidate: string;
}

export interface ScreenShareState {
  meetingId: number;
  userId?: string | null;
  isSharing: boolean;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}
