import { Injectable } from '@angular/core';
import { SignalrService } from './signalr.service';

export interface PeerConnectionInfo {
  userId: string;
  peerConnection: RTCPeerConnection;
  stream?: MediaStream;
}

@Injectable({
  providedIn: 'root'
})
export class WebrtcService {
  private localStream?: MediaStream;
  private screenStream?: MediaStream;
  private peerConnections = new Map<string, RTCPeerConnection>();
  private pendingIceCandidates = new Map<string, RTCIceCandidateInit[]>();
  private peerMeetingIds = new Map<string, number>();
  private pendingNegotiations = new Set<string>();
  private activeOfferCreations = new Set<string>();
  
  private configuration: RTCConfiguration = {
    iceServers: [
      { urls: 'stun:stun.l.google.com:19302' },
      { urls: 'stun:stun1.l.google.com:19302' }
    ]
  };

  constructor(private signalrService: SignalrService) {}

  private log(message: string, details?: unknown): void {
    if (details === undefined) {
      console.log(`[WebRTC] ${message}`);
      return;
    }

    console.log(`[WebRTC] ${message}`, details);
  }

  // Get local media stream
  async getLocalStream(video: boolean = true, audio: boolean = true): Promise<MediaStream> {
    if (this.localStream && this.localStream.active) {
      return this.localStream;
    }

    try {
      this.localStream = await navigator.mediaDevices.getUserMedia({
        video: video ? { width: 1280, height: 720 } : false,
        audio: audio
      });
      this.log('Local media stream acquired', {
        audioTracks: this.localStream.getAudioTracks().length,
        videoTracks: this.localStream.getVideoTracks().length
      });
      return this.localStream;
    } catch (error) {
      console.error('[WebRTC] Error accessing media devices:', error);
      throw error;
    }
  }

  // Get screen sharing stream
  async getScreenStream(): Promise<MediaStream> {
    if (this.screenStream && this.screenStream.active) {
      return this.screenStream;
    }

    try {
      this.screenStream = await navigator.mediaDevices.getDisplayMedia({
        video: {
          frameRate: { ideal: 15, max: 30 }
        },
        audio: false
      });
      this.log('Screen share stream acquired', {
        videoTracks: this.screenStream.getVideoTracks().length
      });
      return this.screenStream;
    } catch (error) {
      console.error('[WebRTC] Error accessing screen share:', error);
      throw error;
    }
  }

  // Stop screen sharing
  stopScreenShare(): void {
    if (this.screenStream) {
      this.log('Stopping screen share stream');
      this.screenStream.getTracks().forEach(track => track.stop());
      this.screenStream = undefined;
    }
  }

  // Create peer connection
  createPeerConnection(userId: string, meetingId: number): RTCPeerConnection {
    const existingPeerConnection = this.peerConnections.get(userId);
    this.peerMeetingIds.set(userId, meetingId);
    if (existingPeerConnection) {
      this.syncOutgoingTracks(existingPeerConnection);
      return existingPeerConnection;
    }

    const peerConnection = new RTCPeerConnection(this.configuration);
    this.log(`Creating peer connection for ${userId}`);

    this.syncOutgoingTracks(peerConnection);

    // Handle ICE candidates
    peerConnection.onicecandidate = (event) => {
      if (event.candidate) {
        this.log(`Sending ICE candidate to ${userId}`, {
          candidate: event.candidate.candidate,
          sdpMid: event.candidate.sdpMid,
          sdpMLineIndex: event.candidate.sdpMLineIndex
        });
        this.signalrService.sendIceCandidate(
          meetingId,
          userId,
          JSON.stringify(event.candidate)
        );
      }
    };

    // Handle connection state changes
    peerConnection.onconnectionstatechange = () => {
      this.log(`Connection state with ${userId}`, peerConnection.connectionState);
    };

    // Handle ICE connection state
    peerConnection.oniceconnectionstatechange = () => {
      this.log(`ICE connection state with ${userId}`, peerConnection.iceConnectionState);
    };

    peerConnection.onsignalingstatechange = () => {
      this.log(`Signaling state with ${userId}`, peerConnection.signalingState);
      if (peerConnection.signalingState === 'stable') {
        void this.flushQueuedNegotiation(userId, meetingId, peerConnection);
      }
    };

    this.peerConnections.set(userId, peerConnection);
    return peerConnection;
  }

  // Create and send offer
  async createOffer(userId: string, meetingId: number, reason: string = 'manual'): Promise<void> {
    const peerConnection = this.peerConnections.get(userId) || this.createPeerConnection(userId, meetingId);

    try {
      if (this.activeOfferCreations.has(userId)) {
        this.pendingNegotiations.add(userId);
        this.log(`Offer creation already in progress for ${userId}; queued`, { reason });
        return;
      }

      if (peerConnection.signalingState !== 'stable') {
        this.pendingNegotiations.add(userId);
        this.log(`Peer ${userId} is not stable; queued renegotiation`, {
          reason,
          signalingState: peerConnection.signalingState
        });
        return;
      }

      this.activeOfferCreations.add(userId);
      this.pendingNegotiations.delete(userId);
      this.syncOutgoingTracks(peerConnection);

      this.log(`Creating offer for ${userId}`, { reason });
      const offer = await peerConnection.createOffer();
      await peerConnection.setLocalDescription(offer);
      
      await this.signalrService.sendOffer(
        meetingId,
        userId,
        JSON.stringify(offer)
      );
    } catch (error) {
      this.pendingNegotiations.add(userId);
      console.error(`[WebRTC] Error creating offer for ${userId}:`, error);
    } finally {
      this.activeOfferCreations.delete(userId);

      if (peerConnection.signalingState === 'stable') {
        void this.flushQueuedNegotiation(userId, meetingId, peerConnection);
      }
    }
  }

  // Handle received offer
  async handleOffer(fromUserId: string, offerString: string, meetingId: number): Promise<void> {
    const peerConnection = this.peerConnections.get(fromUserId) || this.createPeerConnection(fromUserId, meetingId);

    try {
      const offer = JSON.parse(offerString);
      this.log(`Handling offer from ${fromUserId}`, {
        signalingState: peerConnection.signalingState
      });

      if (peerConnection.signalingState === 'have-local-offer') {
        this.log(`Rolling back local offer before applying remote offer from ${fromUserId}`);
        await peerConnection.setLocalDescription({ type: 'rollback' });
      }

      if (peerConnection.signalingState !== 'stable') {
        this.log(`Ignoring offer from ${fromUserId} because signaling state is not stable`, peerConnection.signalingState);
        return;
      }

      await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
      await this.flushPendingIceCandidates(fromUserId, peerConnection);

      const answer = await peerConnection.createAnswer();
      await peerConnection.setLocalDescription(answer);

      await this.signalrService.sendAnswer(
        meetingId,
        fromUserId,
        JSON.stringify(answer)
      );
    } catch (error) {
      console.error(`[WebRTC] Error handling offer from ${fromUserId}:`, error);
    } finally {
      if (peerConnection.signalingState === 'stable') {
        void this.flushQueuedNegotiation(fromUserId, meetingId, peerConnection);
      }
    }
  }

  // Handle received answer
  async handleAnswer(fromUserId: string, answerString: string): Promise<void> {
    const peerConnection = this.peerConnections.get(fromUserId);
    if (!peerConnection) return;

    try {
      if (peerConnection.signalingState !== 'have-local-offer') {
        this.log(`Ignoring answer from ${fromUserId}; unexpected signaling state`, peerConnection.signalingState);
        return;
      }

      const answer = JSON.parse(answerString);
      await peerConnection.setRemoteDescription(new RTCSessionDescription(answer));
      await this.flushPendingIceCandidates(fromUserId, peerConnection);
    } catch (error) {
      console.error(`[WebRTC] Error handling answer from ${fromUserId}:`, error);
    } finally {
      if (peerConnection.signalingState === 'stable') {
        void this.flushQueuedNegotiation(fromUserId, this.peerMeetingIds.get(fromUserId) ?? 0, peerConnection);
      }
    }
  }

  // Handle received ICE candidate
  async handleIceCandidate(fromUserId: string, candidateString: string): Promise<void> {
    const peerConnection = this.peerConnections.get(fromUserId);
    const candidate = JSON.parse(candidateString) as RTCIceCandidateInit;

    if (!peerConnection || !peerConnection.remoteDescription) {
      this.log(`Queueing ICE candidate from ${fromUserId} until remote description is ready`);
      const pendingCandidates = this.pendingIceCandidates.get(fromUserId) ?? [];
      pendingCandidates.push(candidate);
      this.pendingIceCandidates.set(fromUserId, pendingCandidates);
      return;
    }

    try {
      await peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
    } catch (error) {
      console.error(`[WebRTC] Error handling ICE candidate from ${fromUserId}:`, error);
    }
  }

  // Get peer connection for a user
  getPeerConnection(userId: string): RTCPeerConnection | undefined {
    return this.peerConnections.get(userId);
  }

  // Close peer connection
  closePeerConnection(userId: string): void {
    const peerConnection = this.peerConnections.get(userId);
    if (peerConnection) {
      this.log(`Closing peer connection for ${userId}`);
      peerConnection.close();
      this.peerConnections.delete(userId);
    }

    this.pendingIceCandidates.delete(userId);
    this.peerMeetingIds.delete(userId);
    this.pendingNegotiations.delete(userId);
    this.activeOfferCreations.delete(userId);
  }

  // Close all peer connections
  closeAllConnections(): void {
    this.peerConnections.forEach((pc, userId) => {
      pc.close();
    });
    this.peerConnections.clear();
    this.pendingIceCandidates.clear();
    this.peerMeetingIds.clear();
    this.pendingNegotiations.clear();
    this.activeOfferCreations.clear();
  }

  // Stop local stream
  stopLocalStream(): void {
    if (this.localStream) {
      this.localStream.getTracks().forEach(track => track.stop());
      this.localStream = undefined;
    }
  }

  // Toggle local video
  toggleVideo(enabled: boolean): void {
    if (this.localStream) {
      this.localStream.getVideoTracks().forEach(track => {
        track.enabled = enabled;
      });
    }
  }

  // Toggle local audio
  toggleAudio(enabled: boolean): void {
    if (this.localStream) {
      this.localStream.getAudioTracks().forEach(track => {
        track.enabled = enabled;
      });
    }
  }

  // Get local stream
  getLocalMediaStream(): MediaStream | undefined {
    return this.localStream;
  }

  getScreenMediaStream(): MediaStream | undefined {
    return this.screenStream;
  }

  // Replace video track with screen share
  async replaceVideoTrack(isScreenShare: boolean, meetingId: number): Promise<void> {
    const stream = isScreenShare ? this.screenStream : this.localStream;
    if (!stream) return;

    const videoTrack = stream.getVideoTracks()[0];
    if (!videoTrack) {
      return;
    }

    this.log(`Replacing outgoing video track`, {
      source: isScreenShare ? 'screen' : 'camera',
      trackId: videoTrack.id
    });

    const renegotiationTasks: Promise<unknown>[] = [];

    this.peerConnections.forEach((peerConnection, userId) => {
      const senders = peerConnection.getSenders();
      const videoSender = senders.find(sender => sender.track?.kind === 'video');

      if (videoSender) {
        renegotiationTasks.push(
          videoSender.replaceTrack(videoTrack).then(() => {
            this.log(`Replaced video track for ${userId}`, {
              source: isScreenShare ? 'screen' : 'camera',
              trackId: videoTrack.id
            });
            return this.queueNegotiation(userId, meetingId, `replace-${isScreenShare ? 'screen' : 'camera'}-track`);
          })
        );
      } else {
        this.log(`Adding video track for ${userId}`, {
          source: isScreenShare ? 'screen' : 'camera',
          trackId: videoTrack.id
        });
        peerConnection.addTrack(videoTrack, stream);
        renegotiationTasks.push(this.queueNegotiation(userId, meetingId, `add-${isScreenShare ? 'screen' : 'camera'}-track`));
      }
    });

    await Promise.all(renegotiationTasks);
  }

  private syncOutgoingTracks(peerConnection: RTCPeerConnection): void {
    if (!this.localStream) {
      return;
    }

    const outgoingVideoStream = this.getActiveVideoStream();
    const activeVideoTrack = outgoingVideoStream?.getVideoTracks()[0];
    const audioSenders = new Set(
      peerConnection
        .getSenders()
        .filter(sender => sender.track?.kind === 'audio')
        .map(sender => sender.track?.id)
    );

    this.localStream.getAudioTracks().forEach(track => {
      if (!audioSenders.has(track.id)) {
        peerConnection.addTrack(track, this.localStream!);
      }
    });

    if (!activeVideoTrack) {
      return;
    }

    const videoSender = peerConnection.getSenders().find(sender => sender.track?.kind === 'video');
    if (!videoSender) {
      peerConnection.addTrack(activeVideoTrack, outgoingVideoStream!);
      return;
    }

    if (videoSender.track?.id === activeVideoTrack.id) {
      return;
    }

    void videoSender.replaceTrack(activeVideoTrack).then(() => {
      this.log('Synced outgoing video track', {
        trackId: activeVideoTrack.id,
        source: this.screenStream?.active ? 'screen' : 'camera'
      });
    }).catch(error => {
      console.error('[WebRTC] Error syncing outgoing video track:', error);
    });
  }

  private async flushPendingIceCandidates(userId: string, peerConnection: RTCPeerConnection): Promise<void> {
    const pendingCandidates = this.pendingIceCandidates.get(userId);
    if (!pendingCandidates?.length) {
      return;
    }

    for (const candidate of pendingCandidates) {
      await peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
    }

    this.pendingIceCandidates.delete(userId);
  }

  private getActiveVideoStream(): MediaStream | undefined {
    if (this.screenStream?.active && this.screenStream.getVideoTracks().length > 0) {
      return this.screenStream;
    }

    return this.localStream;
  }

  private async queueNegotiation(userId: string, meetingId: number, reason: string): Promise<void> {
    this.pendingNegotiations.add(userId);
    await this.createOffer(userId, meetingId, reason);
  }

  private async flushQueuedNegotiation(
    userId: string,
    meetingId: number,
    peerConnection: RTCPeerConnection
  ): Promise<void> {
    if (meetingId <= 0 || !this.pendingNegotiations.has(userId)) {
      return;
    }

    if (peerConnection.signalingState !== 'stable' || this.activeOfferCreations.has(userId)) {
      return;
    }

    this.log(`Flushing queued renegotiation for ${userId}`);
    await this.createOffer(userId, meetingId, 'queued-renegotiation');
  }

  // Cleanup
  cleanup(): void {
    this.closeAllConnections();
    this.stopLocalStream();
    this.stopScreenShare();
  }
}
