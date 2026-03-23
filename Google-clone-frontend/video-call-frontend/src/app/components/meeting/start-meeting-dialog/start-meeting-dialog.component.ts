import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MeetingService } from '../../../services/meeting.service';

@Component({
  selector: 'app-start-meeting-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './start-meeting-dialog.component.html',
  styleUrl: './start-meeting-dialog.component.css'
})
export class StartMeetingDialogComponent {
  meetingForm: FormGroup;
  loading = false;
  errorMessage = '';
  meetingCode = '';
  copySuccess = false;
  mode: 'options' | 'ready' = 'options';

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<StartMeetingDialogComponent>,
    private meetingService: MeetingService,
    private router: Router
  ) {
    this.meetingForm = this.fb.group({
      title: ['']
    });
  }

  close(): void {
    this.dialogRef.close();
  }

  createForLater(): void {
    this.createMeeting(false);
  }

  startInstantMeeting(): void {
    this.createMeeting(true);
  }

  async copyCode(): Promise<void> {
    if (!this.meetingCode) {
      return;
    }

    try {
      await navigator.clipboard.writeText(this.meetingCode);
      this.copySuccess = true;
    } catch {
      this.errorMessage = 'Unable to copy code automatically';
    }
  }

  private createMeeting(navigateToMeeting: boolean): void {
    if (this.loading) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.copySuccess = false;

    const title = this.meetingForm.get('title')?.value?.trim() || 'Instant meeting';

    this.meetingService.createMeeting({ title }).subscribe({
      next: (response) => {
        this.loading = false;

        if (!response.success || !response.data) {
          this.errorMessage = response.message || 'Failed to create meeting';
          return;
        }

        if (navigateToMeeting) {
          this.dialogRef.close();
          this.router.navigate(['/meeting', response.data.id]);
          return;
        }

        this.mode = 'ready';
        this.meetingCode = response.data.meetingCode;
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = error.error?.message || 'Failed to create meeting';
      }
    });
  }
}
