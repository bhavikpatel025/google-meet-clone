import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { 
  CreateMeetingRequest, 
  MeetingResponse, 
  JoinMeetingRequest,
  Participant,
  UpdateMediaStateRequest,
  ApiResponse 
} from '../models/meeting.models';

@Injectable({
  providedIn: 'root'
})
export class MeetingService {
  constructor(private http: HttpClient) {}

  createMeeting(request: CreateMeetingRequest): Observable<ApiResponse<MeetingResponse>> {
    return this.http.post<ApiResponse<MeetingResponse>>(`${environment.apiUrl}/Meeting`, request);
  }

  getMeetingByCode(meetingCode: string): Observable<ApiResponse<MeetingResponse>> {
    return this.http.get<ApiResponse<MeetingResponse>>(`${environment.apiUrl}/Meeting/code/${meetingCode}`);
  }

  getMeetingById(id: number): Observable<ApiResponse<MeetingResponse>> {
    return this.http.get<ApiResponse<MeetingResponse>>(`${environment.apiUrl}/Meeting/${id}`);
  }

  getMyMeetings(): Observable<ApiResponse<MeetingResponse[]>> {
    return this.http.get<ApiResponse<MeetingResponse[]>>(`${environment.apiUrl}/Meeting/my-meetings`);
  }

  joinMeeting(request: JoinMeetingRequest): Observable<ApiResponse<MeetingResponse>> {
    return this.http.post<ApiResponse<MeetingResponse>>(`${environment.apiUrl}/Meeting/join`, request);
  }

  leaveMeeting(meetingId: number): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${environment.apiUrl}/Meeting/${meetingId}/leave`, {});
  }

  endMeeting(meetingId: number): Observable<ApiResponse<boolean>> {
    return this.http.post<ApiResponse<boolean>>(`${environment.apiUrl}/Meeting/${meetingId}/end`, {});
  }

  getParticipants(meetingId: number): Observable<ApiResponse<Participant[]>> {
    return this.http.get<ApiResponse<Participant[]>>(`${environment.apiUrl}/Meeting/${meetingId}/participants`);
  }

  updateMediaState(meetingId: number, request: UpdateMediaStateRequest): Observable<ApiResponse<boolean>> {
    return this.http.put<ApiResponse<boolean>>(`${environment.apiUrl}/Meeting/${meetingId}/media-state`, request);
  }
}