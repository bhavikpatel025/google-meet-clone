import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  HandLowerEvent,
  HandRaiseEvent,
  MeetingReaction,
  Participant,
  ScreenShareState,
  WebRTCAnswer,
  WebRTCIceCandidate,
  WebRTCOffer
} from '../models/meeting.models';
import { ChatMessage } from '../models/chat.models';

@Injectable({
  providedIn: 'root'
})
export class SignalrService {
  private hubConnection?: HubConnection;
  private currentMeetingCode?: string;
  private connectionState = new BehaviorSubject<HubConnectionState>(HubConnectionState.Disconnected);
  
  // Observables for real-time events
  public userJoined$ = new Subject<Participant>();
  public userLeft$ = new Subject<{ userId: string }>();
  public currentParticipants$ = new BehaviorSubject<Participant[]>([]);
  public currentScreenSharerId$ = new BehaviorSubject<string | null>(null);
  public newMessage$ = new Subject<ChatMessage>();
  public cameraToggled$ = new Subject<{ userId: string, isOn: boolean }>();
  public microphoneToggled$ = new Subject<{ userId: string, isOn: boolean }>();
  public screenShareState$ = new BehaviorSubject<ScreenShareState | null>(null);
  public meetingEnded$ = new Subject<boolean>();
  public reconnected$ = new Subject<void>();
  public handRaised$ = new Subject<HandRaiseEvent>();
  public handLowered$ = new Subject<HandLowerEvent>();
  public allHandsLowered$ = new Subject<{ meetingId: number }>();
  public reactionReceived$ = new Subject<MeetingReaction>();

  // WebRTC signaling observables
  public offerReceived$ = new Subject<WebRTCOffer>();
  public answerReceived$ = new Subject<WebRTCAnswer>();
  public iceCandidateReceived$ = new Subject<WebRTCIceCandidate>();

  constructor() {}

  private log(message: string, details?: unknown): void {
    if (details === undefined) {
      console.log(`[SignalR] ${message}`);
      return;
    }

    console.log(`[SignalR] ${message}`, details);
  }

  public async startConnection(token: string): Promise<void> {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, {
        accessTokenFactory: () => token,
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect()
      .build();

    this.registerSignalREvents();
    this.registerConnectionLifecycleEvents();

    try {
      await this.hubConnection.start();
      this.connectionState.next(HubConnectionState.Connected);
      this.log('Connected');
    } catch (err) {
      console.error('[SignalR] Connection error:', err);
      this.connectionState.next(HubConnectionState.Disconnected);
      throw err;
    }
  }

  public async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.connectionState.next(HubConnectionState.Disconnected);
      this.log('Disconnected');
    }
  }

  public getConnectionState(): Observable<HubConnectionState> {
    return this.connectionState.asObservable();
  }

  private registerSignalREvents(): void {
    if (!this.hubConnection) return;

    // User joined
    this.hubConnection.on('UserJoined', (participant: Participant) => {
      this.log('User joined', participant);
      this.upsertParticipant(participant);
      this.userJoined$.next(participant);
    });

    // User left
    this.hubConnection.on('UserLeft', (data: { userId: string }) => {
      this.log('User left', data);
      this.removeParticipant(data.userId);
      this.userLeft$.next(data);
    });

    // Current participants
    this.hubConnection.on('CurrentParticipants', (participants: Participant[]) => {
      this.log('Current participants', participants);
      this.setParticipants(participants);
    });

    // WebRTC Signaling - Receive Offer
    this.hubConnection.on('ReceiveOffer', (data: WebRTCOffer) => {
      this.log(`Received offer from ${data.fromUserId}`);
      this.offerReceived$.next(data);
    });

    // WebRTC Signaling - Receive Answer
    this.hubConnection.on('ReceiveAnswer', (data: WebRTCAnswer) => {
      this.log(`Received answer from ${data.fromUserId}`);
      this.answerReceived$.next(data);
    });

    // WebRTC Signaling - Receive ICE Candidate
    this.hubConnection.on('ReceiveIceCandidate', (data: WebRTCIceCandidate) => {
      this.log(`Received ICE candidate from ${data.fromUserId}`);
      this.iceCandidateReceived$.next(data);
    });

    // Camera toggled
    this.hubConnection.on('CameraToggled', (data: { userId: string, isOn: boolean }) => {
      this.log('Camera toggled', data);
      this.patchParticipant(data.userId, { isCameraOn: data.isOn });
      this.cameraToggled$.next(data);
    });

    // Microphone toggled
    this.hubConnection.on('MicrophoneToggled', (data: { userId: string, isOn: boolean }) => {
      this.log('Microphone toggled', data);
      this.patchParticipant(data.userId, { isMicrophoneOn: data.isOn });
      this.microphoneToggled$.next(data);
    });

    // Screen share started
    this.hubConnection.on('ScreenShareStarted', (data: { meetingId: number, userId: string }) => {
      this.log('Screen share started', data);
      this.currentScreenSharerId$.next(data.userId);
      this.patchScreenShareState(data.userId, true);
      this.screenShareState$.next({
        meetingId: data.meetingId,
        userId: data.userId,
        isSharing: true
      });
    });

    // Screen share stopped
    this.hubConnection.on('ScreenShareStopped', (data: { meetingId: number, userId: string }) => {
      this.log('Screen share stopped', data);
      this.currentScreenSharerId$.next(null);
      this.patchScreenShareState(data.userId, false);
      this.screenShareState$.next({
        meetingId: data.meetingId,
        userId: data.userId,
        isSharing: false
      });
    });

    // Current screen share state
    this.hubConnection.on('ScreenShareState', (data: ScreenShareState) => {
      this.log('Screen share state', data);
      this.currentScreenSharerId$.next(data.isSharing ? data.userId ?? null : null);
      this.patchScreenShareState(data.userId ?? null, data.isSharing);
      this.screenShareState$.next(data);
    });

    // New message
    this.hubConnection.on('NewMessage', (message: ChatMessage) => {
      this.log('New message', message);
      this.newMessage$.next(message);
    });

    this.hubConnection.on('HandRaised', (data: HandRaiseEvent) => {
      this.log('Hand raised', data);
      this.patchParticipant(data.userId, {
        isHandRaised: true,
        handRaisedAt: data.handRaisedAt
      });
      this.handRaised$.next(data);
    });

    this.hubConnection.on('HandLowered', (data: HandLowerEvent) => {
      this.log('Hand lowered', data);
      this.patchParticipant(data.userId, {
        isHandRaised: false,
        handRaisedAt: null
      });
      this.handLowered$.next(data);
    });

    this.hubConnection.on('AllHandsLowered', (data: { meetingId: number }) => {
      this.log('All hands lowered', data);
      const participants = this.currentParticipants$.value.map(participant => ({
        ...participant,
        isHandRaised: false,
        handRaisedAt: null
      }));
      this.setParticipants(participants);
      this.allHandsLowered$.next(data);
    });

    this.hubConnection.on('ReactionSent', (reaction: MeetingReaction) => {
      this.log('Reaction sent', reaction);
      this.reactionReceived$.next(reaction);
    });

    // Meeting ended
    this.hubConnection.on('MeetingEnded', () => {
      this.log('Meeting ended');
      this.meetingEnded$.next(true);
    });
  }

  private registerConnectionLifecycleEvents(): void {
    if (!this.hubConnection) return;

    this.hubConnection.onreconnecting(() => {
      this.log('Reconnecting');
      this.connectionState.next(HubConnectionState.Reconnecting);
    });

    this.hubConnection.onreconnected(async () => {
      this.log('Reconnected');
      this.connectionState.next(HubConnectionState.Connected);

      if (this.currentMeetingCode) {
        await this.joinMeeting(this.currentMeetingCode);
      }

      this.reconnected$.next();
    });

    this.hubConnection.onclose(() => {
      this.log('Closed');
      this.connectionState.next(HubConnectionState.Disconnected);
    });
  }

  private setParticipants(participants: Participant[]): void {
    this.currentParticipants$.next(participants);
    this.currentScreenSharerId$.next(this.findScreenSharerId(participants));
  }

  private upsertParticipant(participant: Participant): void {
    const participants = [...this.currentParticipants$.value];
    const index = participants.findIndex(existing => existing.userId === participant.userId);

    if (index >= 0) {
      participants[index] = { ...participants[index], ...participant };
    } else {
      participants.push(participant);
    }

    this.setParticipants(participants);
  }

  private removeParticipant(userId: string): void {
    const participants = this.currentParticipants$.value.filter(participant => participant.userId !== userId);
    this.setParticipants(participants);
  }

  private patchParticipant(userId: string, updates: Partial<Participant>): void {
    const participants = this.currentParticipants$.value.map(participant =>
      participant.userId === userId ? { ...participant, ...updates } : participant
    );

    this.setParticipants(participants);
  }

  private patchScreenShareState(userId: string | null, isSharing: boolean): void {
    const participants = this.currentParticipants$.value.map(participant => {
      if (participant.userId === userId) {
        return { ...participant, isScreenSharing: isSharing };
      }

      return { ...participant, isScreenSharing: false };
    });

    this.setParticipants(participants);
  }

  private findScreenSharerId(participants: Participant[]): string | null {
    return participants.find(participant => participant.isScreenSharing)?.userId ?? null;
  }

  // Hub method calls
  public async joinMeeting(meetingCode: string): Promise<void> {
    this.currentMeetingCode = meetingCode;
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.invokeHub('JoinMeeting', meetingCode);
    }
  }

  public async leaveMeeting(meetingId: number): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.invokeHub('LeaveMeeting', meetingId);
    }
  }

  public async sendOffer(meetingId: number, targetUserId: string, offer: string): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      this.log('Sending offer', { meetingId, targetUserId });
      await this.invokeHub('SendOffer', meetingId, targetUserId, offer);
    }
  }

  public async sendAnswer(meetingId: number, targetUserId: string, answer: string): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      this.log('Sending answer', { meetingId, targetUserId });
      await this.invokeHub('SendAnswer', meetingId, targetUserId, answer);
    }
  }

  public async sendIceCandidate(meetingId: number, targetUserId: string, candidate: string): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.invokeHub('SendIceCandidate', meetingId, targetUserId, candidate);
    }
  }

  public async toggleCamera(meetingId: number, isOn: boolean): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.invokeHub('ToggleCamera', meetingId, isOn);
    }
  }

  public async toggleMicrophone(meetingId: number, isOn: boolean): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.invokeHub('ToggleMicrophone', meetingId, isOn);
    }
  }

  public async toggleScreenShare(meetingId: number, isSharing: boolean): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      this.log('Toggling screen share', { meetingId, isSharing });
      await this.invokeHub('ToggleScreenShare', meetingId, isSharing);
    }
  }

  public async sendMessage(meetingId: number, message: string): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.invokeHub('SendMessage', meetingId, message);
    }
  }

  public async endMeeting(meetingId: number): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.invokeHub('EndMeeting', meetingId);
    }
  }

  public async raiseHand(meetingId: number): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.invokeHub('RaiseHand', meetingId);
    }
  }

  public async lowerHand(meetingId: number, userId: string): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.invokeHub('LowerHand', meetingId, userId);
    }
  }

  public async lowerAllHands(meetingId: number): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.invokeHub('LowerAllHands', meetingId);
    }
  }

  public async sendReaction(meetingId: number, reaction: string): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.invokeHub('SendReaction', meetingId, reaction);
    }
  }

  private async invokeHub(methodName: string, ...args: unknown[]): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) {
      this.log(`Skipped ${methodName}; connection is not ready`, this.hubConnection?.state);
      return;
    }

    try {
      await this.hubConnection.invoke(methodName, ...args);
    } catch (error) {
      console.error(`[SignalR] Failed to invoke ${methodName}:`, error);
      throw error;
    }
  }
}
