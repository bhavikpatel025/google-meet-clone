import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { MeetingService } from '../../../services/meeting.service';
import { StartMeetingDialogComponent } from '../start-meeting-dialog/start-meeting-dialog.component';
import { JoinMeetingDialogComponent } from '../join-meeting-dialog/join-meeting-dialog.component';
import { ProfileSettingsDialogComponent } from '../../shared/profile-settings-dialog/profile-settings-dialog.component';

@Component({
  selector: 'app-my-meetings',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatMenuModule,
    MatDialogModule
  ],
  templateUrl: './my-meetings.component.html',
  styleUrls: ['./my-meetings.component.css']
})
export class MyMeetingsComponent implements OnInit, OnDestroy {
  joinLoading = false;
  errorMessage = '';
  currentUserName = '';
  currentDateTimeLabel = '';
  private clockInterval?: ReturnType<typeof setInterval>;

  constructor(
    private authService: AuthService,
    private router: Router,
    private dialog: MatDialog,
    private meetingService: MeetingService
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

  openStartMeetingModal(): void {
    this.dialog.open(StartMeetingDialogComponent, {
      width: 'min(100vw - 32px, 520px)',
      maxWidth: '520px',
      panelClass: 'meeting-action-dialog-panel',
      autoFocus: false
    });
  }

  async openJoinMeetingModal(): Promise<void> {
    if (this.joinLoading) {
      return;
    }

    const dialogRef = this.dialog.open(JoinMeetingDialogComponent, {
      width: 'min(100vw - 32px, 520px)',
      maxWidth: '520px',
      panelClass: 'meeting-action-dialog-panel',
      autoFocus: false
    });

    const result = await firstValueFrom(dialogRef.afterClosed());
    if (!result?.meetingCode) {
      return;
    }

    this.joinMeeting(result.meetingCode);
  }

  openProfileSettings(): void {
    this.dialog.open(ProfileSettingsDialogComponent, {
      width: 'min(100vw - 32px, 460px)',
      maxWidth: '460px',
      panelClass: 'meeting-action-dialog-panel',
      autoFocus: false
    });
  }

  joinMeeting(meetingCodeInput: string): void {
    const meetingCode = meetingCodeInput.trim().toUpperCase();
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

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  get currentUserProfileImageUrl(): string | null {
    return this.authService.resolveAssetUrl(this.authService.currentUserValue?.profilePictureUrl ?? null);
  }

  get currentUserInitial(): string {
    return this.currentUserName.charAt(0).toUpperCase() || 'U';
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
