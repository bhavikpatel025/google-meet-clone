import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MeetingService } from '../../../services/meeting.service';

interface InvitePeopleDialogData {
  meetingId: number;
  meetingCode: string;
  meetingTitle: string;
  hostName: string;
}

@Component({
  selector: 'app-invite-people-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './invite-people-dialog.component.html',
  styleUrls: ['./invite-people-dialog.component.css']
})
export class InvitePeopleDialogComponent {
  inviteForm;

  loading = false;
  errorMessage = '';
  successMessage = '';
  invitedEmails: string[] = [];
  skippedEmails: string[] = [];

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: InvitePeopleDialogData,
    private dialogRef: MatDialogRef<InvitePeopleDialogComponent>,
    private fb: FormBuilder,
    private meetingService: MeetingService
  ) {
    this.inviteForm = this.fb.group({
      emails: ['', [Validators.required]]
    });
  }

  get meetingLink(): string {
    return `${window.location.origin}/join/${this.data.meetingCode}`;
  }

  get parsedEmails(): string[] {
    const value = this.inviteForm.value.emails ?? '';

    return Array.from(new Set(
      value
        .split(/[\s,;\n\r]+/)
        .map(email => email.trim().toLowerCase())
        .filter(Boolean)
    ));
  }

  copyMeetingLink(): void {
    void navigator.clipboard.writeText(this.meetingLink);
    this.successMessage = 'Meeting link copied';
  }

  sendInvites(): void {
    const emails = this.parsedEmails;
    if (emails.length === 0) {
      this.inviteForm.get('emails')?.markAsTouched();
      this.errorMessage = 'Enter at least one email address';
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.meetingService.inviteParticipants({
      meetingId: this.data.meetingId,
      emails,
      hostName: this.data.hostName
    }).subscribe({
      next: response => {
        this.loading = false;
        if (!response.success || !response.data) {
          this.errorMessage = response.message || 'Failed to send invitations';
          return;
        }

        this.invitedEmails = response.data.invitedEmails ?? [];
        this.skippedEmails = response.data.skippedEmails ?? [];
        this.successMessage = `Invitation${response.data.sentCount === 1 ? '' : 's'} sent`;
      },
      error: error => {
        this.loading = false;
        this.errorMessage = error.error?.message || 'Failed to send invitations';
      }
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}
