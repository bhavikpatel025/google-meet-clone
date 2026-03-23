import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ChatMessage, SendMessageRequest } from '../models/chat.models';
import { ApiResponse } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  constructor(private http: HttpClient) {}

  sendMessage(meetingId: number, request: SendMessageRequest): Observable<ApiResponse<ChatMessage>> {
    return this.http.post<ApiResponse<ChatMessage>>(
      `${environment.apiUrl}/Chat/meeting/${meetingId}`,
      request
    );
  }

  getMessages(meetingId: number): Observable<ApiResponse<ChatMessage[]>> {
    return this.http.get<ApiResponse<ChatMessage[]>>(
      `${environment.apiUrl}/Chat/meeting/${meetingId}`
    );
  }
}