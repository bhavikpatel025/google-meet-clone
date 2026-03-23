import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MeetingService } from '../../../services/meeting.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-my-meetings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatMenuModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './my-meetings.component.html',
  styleUrls: ['./my-meetings.component.css']
})
export class MyMeetingsComponent implements OnInit, OnDestroy {
  joinCode = '';
  createLoading = false;
  joinLoading = false;
  errorMessage = '';
  currentUserName = '';
  currentDateTimeLabel = '';
  private clockInterval?: ReturnType<typeof setInterval>;

  constructor(
    private meetingService: MeetingService,
    private authService: AuthService,
    private router: Router
  ) {
    const currentUser = this.authService.currentUserValue;
    this.currentUserName = currentUser
      ? `${currentUser.firstName} ${currentUser.lastName}`.trim()
      : 'Guest';
  }

  ngOnInit(): void {
    this.updateCurrentDateTimeLabel();
    this.clockInterval = setInterval(() => this.updateCurrentDateTimeLabel(), 60_000);
  }

  ngOnDestroy(): void {
    if (this.clockInterval) {
      clearInterval(this.clockInterval);
      this.clockInterval = undefined;
    }
  }

  createMeeting(): void {
    if (this.createLoading) {
      return;
    }

    this.errorMessage = '';
    this.createLoading = true;

    this.meetingService.createMeeting({ title: 'Instant meeting' }).subscribe({
      next: (response) => {
        this.createLoading = false;
        if (response.success && response.data) {
          this.router.navigate(['/meeting', response.data.id]);
          return;
        }

        this.errorMessage = response.message || 'Failed to create meeting';
      },
      error: (error) => {
        this.createLoading = false;
        this.errorMessage = error.error?.message || 'Failed to create meeting';
      }
    });
  }

  joinMeeting(): void {
    const meetingCode = this.joinCode.trim().toUpperCase();
    if (!meetingCode || this.joinLoading) {
      return;
    }

    this.errorMessage = '';
    this.joinLoading = true;

    this.meetingService.getMeetingByCode(meetingCode).subscribe({
      next: (response) => {
        this.joinLoading = false;
        if (response.success && response.data) {
          this.router.navigate(['/meeting', response.data.id]);
          return;
        }

        this.errorMessage = response.message || 'Meeting not found';
      },
      error: (error) => {
        this.joinLoading = false;
        this.errorMessage = error.error?.message || 'Invalid meeting code or link';
      }
    });
  }

  updateJoinCode(value: string): void {
    this.joinCode = value.replace(/[^A-Za-z0-9]/g, '').toUpperCase().slice(0, 8);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  private updateCurrentDateTimeLabel(): void {
    const now = new Date();
    const time = now.toLocaleTimeString([], {
      hour: 'numeric',
      minute: '2-digit'
    });
    const date = now.toLocaleDateString([], {
      weekday: 'short',
      month: 'short',
      day: 'numeric'
    });

    this.currentDateTimeLabel = `${time} • ${date}`;
  }
}
