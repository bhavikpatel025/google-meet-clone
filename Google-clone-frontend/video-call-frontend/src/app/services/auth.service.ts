import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, User, ApiResponse } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject: BehaviorSubject<User | null>;
  public currentUser: Observable<User | null>;

  constructor(private http: HttpClient) {
    const storedUser = localStorage.getItem('currentUser');
    this.currentUserSubject = new BehaviorSubject<User | null>(
      storedUser ? JSON.parse(storedUser) : null
    );
    this.currentUser = this.currentUserSubject.asObservable();
  }

  public get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  public get token(): string | null {
    return localStorage.getItem('token');
  }

  public get userId(): string {
    return this.currentUserValue?.userId || this.getUserIdFromToken() || '';
  }

  register(request: RegisterRequest): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${environment.apiUrl}/Auth/register`, request)
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            this.saveAuthData(response.data);
          }
        })
      );
  }

  login(request: LoginRequest): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>(`${environment.apiUrl}/Auth/login`, request)
      .pipe(
        tap(response => {
          if (response.success && response.data) {
            this.saveAuthData(response.data);
          }
        })
      );
  }

  logout(): void {
    localStorage.removeItem('currentUser');
    localStorage.removeItem('token');
    this.currentUserSubject.next(null);
  }

  private saveAuthData(data: AuthResponse): void {
    const user: User = {
      userId: data.userId,
      email: data.email,
      firstName: data.firstName,
      lastName: data.lastName
    };
    
    localStorage.setItem('currentUser', JSON.stringify(user));
    localStorage.setItem('token', data.token);
    this.currentUserSubject.next(user);
  }

  isAuthenticated(): boolean {
    return !!this.token;
  }

  private getUserIdFromToken(): string | null {
    const token = this.token;
    if (!token) {
      return null;
    }

    try {
      const payload = token.split('.')[1];
      if (!payload) {
        return null;
      }

      const normalizedPayload = payload.replace(/-/g, '+').replace(/_/g, '/');
      const paddedPayload = normalizedPayload.padEnd(Math.ceil(normalizedPayload.length / 4) * 4, '=');
      const decodedPayload = JSON.parse(atob(paddedPayload)) as Record<string, string>;

      return decodedPayload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? null;
    } catch {
      return null;
    }
  }
}
