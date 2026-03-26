import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { firstValueFrom, Subscription } from 'rxjs';
import { MeetingService } from '../../../services/meeting.service';
import { SignalrService } from '../../../services/signalr.service';
import { WebrtcService } from '../../../services/webrtc.service';
import { ChatService } from '../../../services/chat.service';
import { AuthService } from '../../../services/auth.service';
import { HandRaiseEvent, MeetingReaction, MeetingResponse, Participant, WaitingParticipant } from '../../../models/meeting.models';
import { ChatMessage } from '../../../models/chat.models';
import { SrcObjectDirective } from '../../../directives/src-object.directive';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EndMeetingDialogComponent } from '../end-meeting-dialog/end-meeting-dialog.component';
import { InvitePeopleDialogComponent } from '../invite-people-dialog/invite-people-dialog.component';

interface RemoteStream {
  userId: string;
  userName: string;
  stream: MediaStream;
  isCameraOn: boolean;
  isMicrophoneOn: boolean;
  isScreenSharing: boolean;
}

interface ParticipantTile {
  userId: string;
  userName: string;
  profilePictureUrl?: string | null;
  stream?: MediaStream;
  isCameraOn: boolean;
  isMicrophoneOn: boolean;
  isScreenSharing: boolean;
  isHandRaised: boolean;
  isHost: boolean;
  isLocal: boolean;
}

interface RaisedHandEntry {
  userId: string;
  userName: string;
  profilePictureUrl?: string | null;
  isLocal: boolean;
  isHost: boolean;
  handRaisedAt?: Date | string | null;
}

interface ContributorEntry {
  userId: string;
  userName: string;
  profilePictureUrl?: string | null;
  isLocal: boolean;
  isHost: boolean;
  isCameraOn: boolean;
  isMicrophoneOn: boolean;
  isScreenSharing: boolean;
}

interface WaitingParticipantEntry {
  userId: string;
  userName: string;
  profilePictureUrl?: string | null;
  requestedAt?: Date | string | null;
}

@Component({
  selector: 'app-meeting-room',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSidenavModule,
    MatListModule,
    MatFormFieldModule,
    MatInputModule,
    MatTooltipModule,
    MatBadgeModule,
    MatMenuModule,
    MatDialogModule,
    SrcObjectDirective
  ],
  templateUrl: './meeting-room.component.html',
  styleUrls: ['./meeting-room.component.css']
})
export class MeetingRoomComponent implements OnInit, OnDestroy {
  meeting?: MeetingResponse;
  participants: Participant[] = [];
  remoteStreams: RemoteStream[] = [];
  chatMessages: ChatMessage[] = [];
  
  localStream?: MediaStream;
  localDisplayStream?: MediaStream;
  isCameraOn = true;
  isMicrophoneOn = true;
  isScreenSharing = false;
  currentScreenSharerId: string | null = null;
  
  showParticipants = false;
  showChat = false;
  showReactionPicker = false;
  unreadMessages = 0;
  participantSearchTerm = '';
  currentTimeLabel = '';
  isWaitingForAdmission = false;
  isJoinDenied = false;
  hasJoinedMeeting = false;
  showRaisedHandsSection = true;
  showContributorsSection = true;
  showWaitingToJoinSection = true;
  isHandRaised = false;
  localHandRaisedAt?: Date | string | null;
  activeReactions: MeetingReaction[] = [];
  waitingParticipants: WaitingParticipant[] = [];
  readonly availableReactions = ['❤️', '👍', '🎉', '👏', '😂', '😮', '😢', '🤔', '👎'];
  
  chatForm: FormGroup;
  meetingId = 0;
  currentUserId = '';
  isHost = false;
  loading = true;
  errorMessage = '';
  denyRedirectSeconds = 60;
  openWaitingParticipantMenuUserId: string | null = null;
  
  private subscriptions: Subscription[] = [];
  private timeRefreshHandle?: ReturnType<typeof setInterval>;
  private reactionTimeoutHandles = new Map<string, ReturnType<typeof setTimeout>>();
  private chatHistoryLoaded = false;
  private denyRedirectHandle?: ReturnType<typeof setInterval>;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private meetingService: MeetingService,
    private signalrService: SignalrService,
    private webrtcService: WebrtcService,
    private chatService: ChatService,
    private authService: AuthService,
    private dialog: MatDialog
  ) {
    this.currentUserId = this.authService.userId;
    console.log('Meeting room current user id:', this.currentUserId || '(missing)');
    this.chatForm = this.fb.group({
      message: ['']
    });
  }

  async ngOnInit(): Promise<void> {
    this.updateCurrentTimeLabel();
    this.timeRefreshHandle = setInterval(() => this.updateCurrentTimeLabel(), 60_000);

    // Get meeting ID from route
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.meetingId = +id;
      await this.initializeMeeting();
    } else {
      this.errorMessage = 'Invalid meeting ID';
      this.loading = false;
    }
  }

  async initializeMeeting(): Promise<void> {
    try {
      // Step 1: Load meeting details
      await this.loadMeetingDetails();
      
      // Step 2: Connect to SignalR
      const token = this.authService.token;
      if (!token) {
        throw new Error('No authentication token found');
      }
      
      await this.signalrService.startConnection(token);
      
      // Step 3: Setup SignalR listeners
      this.setupSignalRListeners();
      
      // Step 4: Get local media stream before negotiation starts
      await this.initializeMedia();

      // Step 5: Join meeting via SignalR
      await this.signalrService.joinMeeting(
        this.meeting!.meetingCode,
        this.currentUserName,
        this.authService.currentUserValue?.profilePictureUrl ?? null
      );
    } catch (error) {
      console.error('Error initializing meeting:', error);
      this.errorMessage = 'Failed to join meeting';
      this.loading = false;
    }
  }

  async loadMeetingDetails(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.meetingService.getMeetingById(this.meetingId).subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.meeting = response.data;
            this.isHost = this.meeting.hostId === this.currentUserId;
            resolve();
          } else {
            reject(new Error('Meeting not found'));
          }
        },
        error: (error) => reject(error)
      });
    });
  }

  async initializeMedia(): Promise<void> {
    try {
      this.localStream = await this.webrtcService.getLocalStream(this.isCameraOn, this.isMicrophoneOn);
      this.localDisplayStream = this.localStream;
    } catch (error) {
      console.error('Error accessing media devices:', error);
      this.errorMessage = 'Failed to access camera/microphone';
    }
  }

  setupSignalRListeners(): void {
    const resolveCurrentUserId = (): string => {
      if (!this.currentUserId) {
        this.currentUserId = this.authService.userId;
      }

      return this.currentUserId;
    };

    // User joined
    const userJoinedSub = this.signalrService.userJoined$.subscribe(participant => {
      const currentUserId = resolveCurrentUserId();
      if (participant.userId !== currentUserId) {
        console.log('User joined:', participant);
        this.upsertParticipant(participant);
        
        // Create peer connection and send offer
        this.createPeerConnectionForUser(participant.userId);
      }
    });
    this.subscriptions.push(userJoinedSub);

    // User left
    const userLeftSub = this.signalrService.userLeft$.subscribe(data => {
      console.log('User left:', data.userId);
      this.participants = this.participants.filter(p => p.userId !== data.userId);
      this.remoteStreams = this.remoteStreams.filter(s => s.userId !== data.userId);
      this.webrtcService.closePeerConnection(data.userId);
    });
    this.subscriptions.push(userLeftSub);

    // Current participants
    const currentParticipantsSub = this.signalrService.currentParticipants$.subscribe(participants => {
      if (!this.hasJoinedMeeting) {
        return;
      }

      const currentUserId = resolveCurrentUserId();
      console.log('Current participants:', participants);
      const localParticipant = participants.find(participant => participant.userId === currentUserId);
      this.isHandRaised = localParticipant?.isHandRaised ?? false;
      this.localHandRaisedAt = localParticipant?.handRaisedAt ?? null;
      this.participants = participants.filter(p => p.userId !== currentUserId);
      this.syncRemoteStreamMetadata();
      this.updateCurrentScreenSharer();

      // Create peer connections for all existing participants
      this.participants.forEach(participant => {
        this.createPeerConnectionForUser(participant.userId);
      });
    });
    this.subscriptions.push(currentParticipantsSub);

    // WebRTC Offer received
    const offerSub = this.signalrService.offerReceived$.subscribe(async data => {
      const currentUserId = resolveCurrentUserId();
      if (data.fromUserId !== currentUserId) {
        console.log('Offer received from:', data.fromUserId);
        this.ensurePeerConnectionHandler(data.fromUserId);
        await this.webrtcService.handleOffer(data.fromUserId, data.offer, this.meetingId);
      }
    });
    this.subscriptions.push(offerSub);

    // WebRTC Answer received
    const answerSub = this.signalrService.answerReceived$.subscribe(async data => {
      const currentUserId = resolveCurrentUserId();
      if (data.fromUserId !== currentUserId) {
        console.log('Answer received from:', data.fromUserId);
        await this.webrtcService.handleAnswer(data.fromUserId, data.answer);
      }
    });
    this.subscriptions.push(answerSub);

    // ICE Candidate received
    const iceSub = this.signalrService.iceCandidateReceived$.subscribe(async data => {
      const currentUserId = resolveCurrentUserId();
      if (data.fromUserId !== currentUserId) {
        console.log('ICE candidate received from:', data.fromUserId);
        await this.webrtcService.handleIceCandidate(data.fromUserId, data.candidate);
      }
    });
    this.subscriptions.push(iceSub);

    // Camera toggled
    const cameraSub = this.signalrService.cameraToggled$.subscribe(data => {
      if (data.userId === this.currentUserId) {
        this.isCameraOn = data.isOn;
      }

      const stream = this.remoteStreams.find(s => s.userId === data.userId);
      if (stream) {
        stream.isCameraOn = data.isOn;
      }

      this.patchParticipantState(data.userId, { isCameraOn: data.isOn });
    });
    this.subscriptions.push(cameraSub);

    // Microphone toggled
    const micSub = this.signalrService.microphoneToggled$.subscribe(data => {
      if (data.userId === this.currentUserId) {
        this.isMicrophoneOn = data.isOn;
      }

      const stream = this.remoteStreams.find(s => s.userId === data.userId);
      if (stream) {
        stream.isMicrophoneOn = data.isOn;
      }

      this.patchParticipantState(data.userId, { isMicrophoneOn: data.isOn });
    });
    this.subscriptions.push(micSub);

    // Screen share state
    const screenSub = this.signalrService.screenShareState$.subscribe(data => {
      if (!data) {
        return;
      }

      if (data.userId === this.currentUserId || (!data.isSharing && this.isScreenSharing)) {
        this.isScreenSharing = data.isSharing && data.userId === this.currentUserId;
        this.localDisplayStream = this.isScreenSharing
          ? this.webrtcService.getScreenMediaStream() ?? this.localStream
          : this.localStream;
      }

      this.remoteStreams = this.remoteStreams.map(stream => ({
        ...stream,
        isScreenSharing: data.isSharing ? stream.userId === data.userId : false
      }));

      this.patchScreenShareState(data.userId ?? null, data.isSharing);
      this.updateCurrentScreenSharer();
    });
    this.subscriptions.push(screenSub);

    const screenSharerSub = this.signalrService.currentScreenSharerId$.subscribe(userId => {
      this.currentScreenSharerId = userId === this.currentUserId && this.isScreenSharing ? this.currentUserId : userId;
    });
    this.subscriptions.push(screenSharerSub);

    const reconnectedSub = this.signalrService.reconnected$.subscribe(() => {
      if (this.isWaitingForAdmission || !this.hasJoinedMeeting) {
        return;
      }

      console.log('SignalR reconnected, rebuilding peer connections');
      const participantIds = this.participants.map(participant => participant.userId);
      this.remoteStreams = [];
      this.webrtcService.closeAllConnections();

      participantIds.forEach(userId => {
        void this.createPeerConnectionForUser(userId);
      });

      if (this.isScreenSharing) {
        void this.signalrService.toggleScreenShare(this.meetingId, true);
      }
    });
    this.subscriptions.push(reconnectedSub);

    const waitingRoomEnteredSub = this.signalrService.waitingRoomEntered$.subscribe(state => {
      if (state.meetingId !== this.meetingId) {
        return;
      }

      this.isWaitingForAdmission = state.isWaiting;
      this.hasJoinedMeeting = !state.isWaiting;
      this.loading = false;
    });
    this.subscriptions.push(waitingRoomEnteredSub);

    const joinApprovedSub = this.signalrService.joinApproved$.subscribe(state => {
      if (state.meetingId !== this.meetingId) {
        return;
      }

      this.isJoinDenied = false;
      this.isWaitingForAdmission = false;
      this.hasJoinedMeeting = true;
      this.loading = false;

      if (!this.chatHistoryLoaded) {
        this.loadChatHistory();
      }

      void this.syncAdmittedMediaState();
    });
    this.subscriptions.push(joinApprovedSub);

    const joinDeniedSub = this.signalrService.joinDenied$.subscribe(state => {
      if (state.meetingId !== this.meetingId) {
        return;
      }

      void this.handleJoinDenied();
    });
    this.subscriptions.push(joinDeniedSub);

    const waitingParticipantsUpdatedSub = this.signalrService.waitingParticipantsUpdated$.subscribe(participants => {
      this.waitingParticipants = participants;
    });
    this.subscriptions.push(waitingParticipantsUpdatedSub);

    // New message
    const messageSub = this.signalrService.newMessage$.subscribe(message => {
      this.chatMessages.push(message);
      if (!this.showChat) {
        this.unreadMessages++;
      }
    });
    this.subscriptions.push(messageSub);

    const handRaisedSub = this.signalrService.handRaised$.subscribe(data => {
      this.applyLocalHandRaiseState(data);
    });
    this.subscriptions.push(handRaisedSub);

    const handLoweredSub = this.signalrService.handLowered$.subscribe(data => {
      if (data.userId === this.currentUserId) {
        this.isHandRaised = false;
        this.localHandRaisedAt = null;
      }
    });
    this.subscriptions.push(handLoweredSub);

    const allHandsLoweredSub = this.signalrService.allHandsLowered$.subscribe(() => {
      this.isHandRaised = false;
      this.localHandRaisedAt = null;
    });
    this.subscriptions.push(allHandsLoweredSub);

    const reactionSub = this.signalrService.reactionReceived$.subscribe(reaction => {
      this.activeReactions = [...this.activeReactions, reaction];
      this.scheduleReactionRemoval(reaction.id);
    });
    this.subscriptions.push(reactionSub);

    // Meeting ended
    const meetingEndedSub = this.signalrService.meetingEnded$.subscribe(ended => {
      if (!ended) {
        return;
      }

      alert('Meeting has been ended by the host');
      this.leaveMeeting();
    });
    this.subscriptions.push(meetingEndedSub);
  }

  async createPeerConnectionForUser(userId: string): Promise<void> {
    const currentUserId = this.currentUserId || this.authService.userId;
    if (!userId || !currentUserId || userId === currentUserId) {
      return;
    }

    const hasExistingConnection = !!this.webrtcService.getPeerConnection(userId);
    this.ensurePeerConnectionHandler(userId);

    if (!hasExistingConnection && this.shouldInitiateConnection(userId)) {
      await this.webrtcService.createOffer(userId, this.meetingId);
    }
  }

  private ensurePeerConnectionHandler(userId: string): void {
    const peerConnection = this.webrtcService.createPeerConnection(userId, this.meetingId);

    peerConnection.ontrack = (event) => {
      console.log('Remote track received from:', userId);
      const participant = this.participants.find(p => p.userId === userId);
      const incomingStream = event.streams[0] ?? new MediaStream([event.track]);

      const remoteStream: RemoteStream = {
        userId,
        userName: participant?.userName || 'Unknown',
        stream: incomingStream,
        isCameraOn: participant?.isCameraOn ?? true,
        isMicrophoneOn: participant?.isMicrophoneOn ?? true,
        isScreenSharing: participant?.isScreenSharing ?? false
      };

      const existingStreamIndex = this.remoteStreams.findIndex(stream => stream.userId === userId);
      if (existingStreamIndex >= 0) {
        const existingStream = this.remoteStreams[existingStreamIndex];

        if (existingStream.stream.id === incomingStream.id) {
          const existingTracks = existingStream.stream.getTracks();
          const incomingTrackIds = new Set(incomingStream.getTracks().map(track => track.id));

          existingTracks
            .filter(track => !incomingTrackIds.has(track.id))
            .forEach(track => existingStream.stream.removeTrack(track));

          incomingStream.getTracks().forEach(track => {
            const hasTrack = existingStream.stream.getTracks().some(existingTrack => existingTrack.id === track.id);
            if (!hasTrack) {
              existingStream.stream.addTrack(track);
            }
          });

          this.remoteStreams[existingStreamIndex] = {
            ...existingStream,
            userName: remoteStream.userName,
            isCameraOn: remoteStream.isCameraOn,
            isMicrophoneOn: remoteStream.isMicrophoneOn,
            isScreenSharing: remoteStream.isScreenSharing
          };
        } else {
          this.remoteStreams[existingStreamIndex] = remoteStream;
        }
      } else {
        this.remoteStreams = [...this.remoteStreams, remoteStream];
      }
    };
  }

  private shouldInitiateConnection(userId: string): boolean {
    const currentUserId = this.currentUserId || this.authService.userId;
    if (!currentUserId || currentUserId === userId) {
      return false;
    }

    return currentUserId.localeCompare(userId) < 0;
  }

  async toggleCamera(): Promise<void> {
    this.isCameraOn = !this.isCameraOn;
    this.webrtcService.toggleVideo(this.isCameraOn);

    if (!this.hasJoinedMeeting) {
      return;
    }

    await this.signalrService.toggleCamera(this.meetingId, this.isCameraOn);
    this.meetingService.updateMediaState(this.meetingId, { isCameraOn: this.isCameraOn }).subscribe();
  }

  async toggleMicrophone(): Promise<void> {
    this.isMicrophoneOn = !this.isMicrophoneOn;
    this.webrtcService.toggleAudio(this.isMicrophoneOn);

    if (!this.hasJoinedMeeting) {
      return;
    }

    await this.signalrService.toggleMicrophone(this.meetingId, this.isMicrophoneOn);
    this.meetingService.updateMediaState(this.meetingId, { isMicrophoneOn: this.isMicrophoneOn }).subscribe();
  }

  async toggleScreenShare(): Promise<void> {
    if (!this.isScreenSharing) {
      try {
        const screenStream = await this.webrtcService.getScreenStream();
        const screenTrack = screenStream.getVideoTracks()[0];

        if (screenTrack) {
          screenTrack.onended = () => {
            if (this.isScreenSharing) {
              void this.toggleScreenShare();
            }
          };
        }

        await this.webrtcService.replaceVideoTrack(true, this.meetingId);
        this.localDisplayStream = screenStream;
        this.isScreenSharing = true;
        await this.signalrService.toggleScreenShare(this.meetingId, true);
        this.updateCurrentScreenSharer();
        
        // Update backend
        this.meetingService.updateMediaState(this.meetingId, { isScreenSharing: true }).subscribe();
      } catch (error) {
        console.error('Error sharing screen:', error);
      }
    } else {
      this.isScreenSharing = false;
      this.localDisplayStream = this.localStream;
      await this.webrtcService.replaceVideoTrack(false, this.meetingId);
      this.webrtcService.stopScreenShare();
      await this.signalrService.toggleScreenShare(this.meetingId, false);
      this.updateCurrentScreenSharer();
      
      // Update backend
      this.meetingService.updateMediaState(this.meetingId, { isScreenSharing: false }).subscribe();
    }
  }

  toggleParticipants(): void {
    const nextState = !this.showParticipants;
    this.resetPanels();
    this.showParticipants = nextState;
  }

  toggleChat(): void {
    const nextState = !this.showChat;
    this.resetPanels();
    this.showChat = nextState;
    this.showReactionPicker = false;
    if (this.showChat) {
      this.unreadMessages = 0;
    }
  }

  async toggleRaiseHand(): Promise<void> {
    if (this.isHandRaised) {
      await this.signalrService.lowerHand(this.meetingId, this.currentUserId);
      return;
    }

    await this.signalrService.raiseHand(this.meetingId);
  }

  async lowerHand(userId: string): Promise<void> {
    await this.signalrService.lowerHand(this.meetingId, userId);
  }

  async lowerAllHands(): Promise<void> {
    await this.signalrService.lowerAllHands(this.meetingId);
  }

  toggleReactionPicker(): void {
    this.showReactionPicker = !this.showReactionPicker;
  }

  async sendReaction(reaction: string): Promise<void> {
    await this.signalrService.sendReaction(this.meetingId, reaction);
  }

  loadChatHistory(): void {
    this.chatService.getMessages(this.meetingId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.chatMessages = response.data;
          this.chatHistoryLoaded = true;
        }
      },
      error: (error) => {
        console.error('Error loading chat history:', error);
      }
    });
  }

  sendMessage(): void {
    const message = this.chatForm.value.message?.trim();
    if (message) {
      this.signalrService.sendMessage(this.meetingId, message);
      this.chatForm.reset();
    }
  }

  async leaveMeeting(): Promise<void> {
    try {
      await this.signalrService.leaveMeeting(this.meetingId);

      if (this.hasJoinedMeeting) {
        this.meetingService.leaveMeeting(this.meetingId).subscribe();
      }

      this.cleanup();
      this.router.navigate(['/my-meetings']);
    } catch (error) {
      console.error('Error leaving meeting:', error);
      this.router.navigate(['/my-meetings']);
    }
  }

  async endMeeting(): Promise<void> {
    const dialogRef = this.dialog.open(EndMeetingDialogComponent, {
      width: '420px',
      maxWidth: '92vw',
      autoFocus: false,
      restoreFocus: false
    });

    const confirmed = await firstValueFrom(dialogRef.afterClosed());
    if (!confirmed) {
      return;
    }

    try {
      await this.signalrService.endMeeting(this.meetingId);
      this.meetingService.endMeeting(this.meetingId).subscribe({
        next: () => {
          this.cleanup();
          this.router.navigate(['/my-meetings']);
        },
        error: (error) => {
          console.error('Error ending meeting:', error);
          alert('Failed to end meeting');
        }
      });
    } catch (error) {
      console.error('Error ending meeting:', error);
    }
  }

  private cleanup(): void {
    if (this.timeRefreshHandle) {
      clearInterval(this.timeRefreshHandle);
      this.timeRefreshHandle = undefined;
    }

    if (this.denyRedirectHandle) {
      clearInterval(this.denyRedirectHandle);
      this.denyRedirectHandle = undefined;
    }

    // Unsubscribe from all observables
    this.subscriptions.forEach(sub => sub.unsubscribe());
    
    // Stop SignalR connection
    this.signalrService.stopConnection();
    
    // Cleanup WebRTC
    this.webrtcService.cleanup();

    this.reactionTimeoutHandles.forEach(handle => clearTimeout(handle));
    this.reactionTimeoutHandles.clear();
  }

  ngOnDestroy(): void {
    this.cleanup();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;
    const clickedInsideReactionUi = !!target?.closest('.reaction-picker, .reaction-toggle-button');
    if (!clickedInsideReactionUi) {
      this.showReactionPicker = false;
    }

    this.closeWaitingParticipantMenu();
  }

  get allParticipantTiles(): ParticipantTile[] {
    const localTile: ParticipantTile = {
      userId: this.currentUserId,
      userName: `You${this.isHost ? ' (Host)' : ''}`,
      profilePictureUrl: this.authService.currentUserValue?.profilePictureUrl ?? null,
      stream: this.localDisplayStream,
      isCameraOn: this.isCameraOn,
      isMicrophoneOn: this.isMicrophoneOn,
      isScreenSharing: this.isScreenSharing,
      isHandRaised: this.isHandRaised,
      isHost: this.isHost,
      isLocal: true
    };

    const remoteTiles: ParticipantTile[] = this.participants.map(participant => {
      const remoteStream = this.remoteStreams.find(stream => stream.userId === participant.userId);

      return {
        userId: participant.userId,
        userName: `${participant.userName}${participant.role === 'Host' ? ' (Host)' : ''}`,
        profilePictureUrl: participant.profilePictureUrl ?? null,
        stream: remoteStream?.stream,
        isCameraOn: remoteStream?.isCameraOn ?? participant.isCameraOn,
        isMicrophoneOn: remoteStream?.isMicrophoneOn ?? participant.isMicrophoneOn,
        isScreenSharing: remoteStream?.isScreenSharing ?? participant.isScreenSharing,
        isHandRaised: participant.isHandRaised ?? false,
        isHost: participant.role === 'Host',
        isLocal: false
      };
    });

    return [localTile, ...remoteTiles];
  }

  get screenShareTile(): ParticipantTile | null {
    return this.allParticipantTiles.find(tile => tile.isScreenSharing) ?? null;
  }

  get localPreviewTile(): ParticipantTile {
    return this.allParticipantTiles[0];
  }

  get filteredParticipants(): Participant[] {
    const searchTerm = this.participantSearchTerm.trim().toLowerCase();
    if (!searchTerm) {
      return this.participants;
    }

    return this.participants.filter(participant =>
      participant.userName.toLowerCase().includes(searchTerm)
    );
  }

  get secondaryTiles(): ParticipantTile[] {
    if (!this.screenShareTile) {
      return [];
    }

    return this.allParticipantTiles.filter(tile => tile.userId !== this.screenShareTile?.userId);
  }

  get raisedHands(): RaisedHandEntry[] {
    const entries: RaisedHandEntry[] = [];

    if (this.isHandRaised) {
      entries.push({
        userId: this.currentUserId,
        userName: 'You',
        profilePictureUrl: this.authService.currentUserValue?.profilePictureUrl ?? null,
        isLocal: true,
        isHost: this.isHost,
        handRaisedAt: this.localHandRaisedAt
      });
    }

    this.participants
      .filter(participant => participant.isHandRaised)
      .forEach(participant => {
        entries.push({
          userId: participant.userId,
          userName: participant.userName,
          profilePictureUrl: participant.profilePictureUrl ?? null,
          isLocal: false,
          isHost: participant.role === 'Host',
          handRaisedAt: participant.handRaisedAt
        });
      });

    return entries.sort((left, right) => this.getHandRaisedTimestamp(left.handRaisedAt) - this.getHandRaisedTimestamp(right.handRaisedAt));
  }

  get contributors(): ContributorEntry[] {
    const entries: ContributorEntry[] = [];

    entries.push({
      userId: this.currentUserId,
      userName: 'You',
      profilePictureUrl: this.authService.currentUserValue?.profilePictureUrl ?? null,
      isLocal: true,
      isHost: this.isHost,
      isCameraOn: this.isCameraOn,
      isMicrophoneOn: this.isMicrophoneOn,
      isScreenSharing: this.isScreenSharing
    });

    this.participants.forEach(participant => {
      entries.push({
        userId: participant.userId,
        userName: participant.userName,
        profilePictureUrl: participant.profilePictureUrl ?? null,
        isLocal: false,
        isHost: participant.role === 'Host',
        isCameraOn: participant.isCameraOn,
        isMicrophoneOn: participant.isMicrophoneOn,
        isScreenSharing: participant.isScreenSharing
      });
    });

    return entries;
  }

  get filteredContributors(): ContributorEntry[] {
    const searchTerm = this.participantSearchTerm.trim().toLowerCase();
    if (!searchTerm) {
      return this.contributors;
    }

    return this.contributors.filter(contributor =>
      contributor.userName.toLowerCase().includes(searchTerm)
    );
  }

  get participantGridClass(): string {
    const count = this.allParticipantTiles.length;

    if (count <= 1) {
      return 'layout-single';
    }

    if (count === 2) {
      return 'layout-dual';
    }

    if (count <= 4) {
      return 'layout-quad';
    }

    if (count <= 6) {
      return 'layout-hex';
    }

    if (count <= 9) {
      return 'layout-nine';
    }

    return 'layout-auto';
  }

  get videoGridLayoutClass(): string {
    const count = this.allParticipantTiles.length;

    if (count === 3) {
      return 'layout-quad has-three';
    }

    if (count === 5) {
      return 'layout-hex has-five';
    }

    return this.participantGridClass;
  }

  getVideoTileColumnClasses(): string {
    const count = this.allParticipantTiles.length;

    if (count <= 1) {
      return 'col-12 col-xl-8 col-xxl-7';
    }

    if (count === 2) {
      return 'col-12 col-md-6';
    }

    if (count <= 4) {
      return 'col-12 col-md-6';
    }

    if (count <= 9) {
      return 'col-12 col-md-6 col-lg-4';
    }

    return 'col-12 col-sm-6 col-lg-4 col-xl-3';
  }

  getParticipantCount(): number {
    return this.participants.length + 1; // +1 for self
  }

  get waitingToJoin(): WaitingParticipantEntry[] {
    return this.waitingParticipants
      .map(participant => ({
        userId: participant.userId,
        userName: participant.userName,
        profilePictureUrl: participant.profilePictureUrl ?? null,
        requestedAt: participant.requestedAt ?? null
      }))
      .sort((left, right) => this.getHandRaisedTimestamp(left.requestedAt) - this.getHandRaisedTimestamp(right.requestedAt));
  }

  get filteredWaitingToJoin(): WaitingParticipantEntry[] {
    const searchTerm = this.participantSearchTerm.trim().toLowerCase();
    if (!searchTerm) {
      return this.waitingToJoin;
    }

    return this.waitingToJoin.filter(participant =>
      participant.userName.toLowerCase().includes(searchTerm)
    );
  }

  get denyCountdownProgress(): number {
    return (this.denyRedirectSeconds / 60) * 100;
  }

  trackTile(_: number, tile: ParticipantTile): string {
    return tile.userId;
  }

  formatTime(date: Date): string {
    return new Date(date).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  getReactionsForTile(userId: string): MeetingReaction[] {
    return this.activeReactions.filter(reaction => reaction.userId === userId);
  }

  trackReaction(_: number, reaction: MeetingReaction): string {
    return reaction.id;
  }

  trackContributor(_: number, contributor: ContributorEntry): string {
    return contributor.userId;
  }

  toggleRaisedHandsSection(): void {
    this.showRaisedHandsSection = !this.showRaisedHandsSection;
  }

  toggleWaitingToJoinSection(): void {
    this.showWaitingToJoinSection = !this.showWaitingToJoinSection;
  }

  toggleContributorsSection(): void {
    this.showContributorsSection = !this.showContributorsSection;
  }

  async admitParticipant(userId: string): Promise<void> {
    await this.signalrService.admitParticipant(this.meetingId, userId);
  }

  async admitAllParticipants(): Promise<void> {
    await this.signalrService.admitAllParticipants(this.meetingId);
  }

  async denyParticipant(userId: string | null): Promise<void> {
    if (!userId) {
      return;
    }

    this.openWaitingParticipantMenuUserId = null;
    await this.signalrService.denyParticipant(this.meetingId, userId);
  }

  toggleWaitingParticipantMenu(event: MouseEvent, userId: string): void {
    event.stopPropagation();
    this.openWaitingParticipantMenuUserId =
      this.openWaitingParticipantMenuUserId === userId ? null : userId;
  }

  closeWaitingParticipantMenu(): void {
    this.openWaitingParticipantMenuUserId = null;
  }

  returnToHomeScreen(): void {
    if (this.denyRedirectHandle) {
      clearInterval(this.denyRedirectHandle);
      this.denyRedirectHandle = undefined;
    }

    this.router.navigate(['/my-meetings']);
  }

  openInviteDialog(): void {
    if (!this.meeting || !this.isHost) {
      return;
    }

    this.dialog.open(InvitePeopleDialogComponent, {
      width: '680px',
      maxWidth: '96vw',
      autoFocus: false,
      restoreFocus: false,
      data: {
        meetingId: this.meeting.id,
        meetingCode: this.meeting.meetingCode,
        meetingTitle: this.meeting.title,
        hostName: this.currentUserName
      }
    });
  }

  shouldShowVideo(tile: ParticipantTile): boolean {
    if (tile.isScreenSharing) {
      return !!tile.stream;
    }

    return !!tile.stream && tile.isCameraOn;
  }

  getAvatarImageUrl(profilePictureUrl?: string | null): string | null {
    return this.authService.resolveAssetUrl(profilePictureUrl ?? null);
  }

  getAvatarInitial(name: string): string {
    return name.trim().charAt(0).toUpperCase() || 'U';
  }

  getChatAvatarUrl(message: ChatMessage): string | null {
    if (message.senderId === this.currentUserId) {
      return this.currentUserProfileImageUrl;
    }

    const participant = this.participants.find(currentParticipant => currentParticipant.userId === message.senderId);
    return this.getAvatarImageUrl(participant?.profilePictureUrl ?? null);
  }

  getChatAvatarInitial(message: ChatMessage): string {
    return this.getAvatarInitial(message.senderName);
  }

  get currentUserName(): string {
    const currentUser = this.authService.currentUserValue;
    return `${currentUser?.firstName ?? ''} ${currentUser?.lastName ?? ''}`.trim() || 'You';
  }

  get currentUserProfileImageUrl(): string | null {
    return this.authService.resolveAssetUrl(this.authService.currentUserValue?.profilePictureUrl ?? null);
  }

  private upsertParticipant(participant: Participant): void {
    const participantIndex = this.participants.findIndex(existing => existing.userId === participant.userId);

    if (participantIndex >= 0) {
      this.participants[participantIndex] = { ...this.participants[participantIndex], ...participant };
    } else {
      this.participants = [...this.participants, participant];
    }

    this.syncRemoteStreamMetadata();
    this.updateCurrentScreenSharer();
  }

  private patchParticipantState(userId: string, updates: Partial<Participant>): void {
    this.participants = this.participants.map(participant =>
      participant.userId === userId ? { ...participant, ...updates } : participant
    );

    this.syncRemoteStreamMetadata();
  }

  private patchScreenShareState(userId: string | null, isSharing: boolean): void {
    this.participants = this.participants.map(participant => {
      if (participant.userId === userId) {
        return { ...participant, isScreenSharing: isSharing };
      }

      return { ...participant, isScreenSharing: false };
    });
  }

  private syncRemoteStreamMetadata(): void {
    this.remoteStreams = this.remoteStreams.map(stream => {
      const participant = this.participants.find(currentParticipant => currentParticipant.userId === stream.userId);
      if (!participant) {
        return stream;
      }

      return {
        ...stream,
        userName: participant.userName,
        isCameraOn: participant.isCameraOn,
        isMicrophoneOn: participant.isMicrophoneOn,
        isScreenSharing: participant.isScreenSharing
      };
    });
  }

  private updateCurrentScreenSharer(): void {
    if (this.isScreenSharing) {
      this.currentScreenSharerId = this.currentUserId;
      return;
    }

    this.currentScreenSharerId = this.participants.find(participant => participant.isScreenSharing)?.userId ?? null;
  }

  private updateCurrentTimeLabel(): void {
    this.currentTimeLabel = new Date().toLocaleTimeString([], {
      hour: 'numeric',
      minute: '2-digit'
    });
  }

  private resetPanels(): void {
    this.showParticipants = false;
    this.showChat = false;
    this.showReactionPicker = false;
  }

  private applyLocalHandRaiseState(data: HandRaiseEvent): void {
    if (data.userId !== this.currentUserId) {
      return;
    }

    this.isHandRaised = true;
    this.localHandRaisedAt = data.handRaisedAt;
  }

  private async handleJoinDenied(): Promise<void> {
    this.isJoinDenied = true;
    this.isWaitingForAdmission = false;
    this.hasJoinedMeeting = false;
    this.loading = false;
    this.showParticipants = false;
    this.showChat = false;
    this.showReactionPicker = false;
    this.waitingParticipants = [];
    this.openWaitingParticipantMenuUserId = null;
    this.denyRedirectSeconds = 60;

    if (this.denyRedirectHandle) {
      clearInterval(this.denyRedirectHandle);
    }

    await this.signalrService.stopConnection();
    this.webrtcService.cleanup();
    this.localStream = undefined;
    this.localDisplayStream = undefined;

    this.denyRedirectHandle = setInterval(() => {
      this.denyRedirectSeconds -= 1;

      if (this.denyRedirectSeconds <= 0) {
        this.returnToHomeScreen();
      }
    }, 1000);
  }

  private async syncAdmittedMediaState(): Promise<void> {
    await this.signalrService.toggleCamera(this.meetingId, this.isCameraOn);
    await this.signalrService.toggleMicrophone(this.meetingId, this.isMicrophoneOn);

    this.meetingService.updateMediaState(this.meetingId, {
      isCameraOn: this.isCameraOn,
      isMicrophoneOn: this.isMicrophoneOn
    }).subscribe();
  }

  private scheduleReactionRemoval(reactionId: string): void {
    const existingHandle = this.reactionTimeoutHandles.get(reactionId);
    if (existingHandle) {
      clearTimeout(existingHandle);
    }

    const timeoutHandle = setTimeout(() => {
      this.activeReactions = this.activeReactions.filter(reaction => reaction.id !== reactionId);
      this.reactionTimeoutHandles.delete(reactionId);
    }, 2600);

    this.reactionTimeoutHandles.set(reactionId, timeoutHandle);
  }

  private getHandRaisedTimestamp(value?: Date | string | null): number {
    if (!value) {
      return Number.MAX_SAFE_INTEGER;
    }

    const timestamp = new Date(value).getTime();
    return Number.isNaN(timestamp) ? Number.MAX_SAFE_INTEGER : timestamp;
  }
}
