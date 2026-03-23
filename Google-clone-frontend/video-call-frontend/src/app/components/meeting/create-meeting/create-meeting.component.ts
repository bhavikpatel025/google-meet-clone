import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MeetingService } from '../../../services/meeting.service';
import { CreateMeetingRequest } from '../../../models/meeting.models';

@Component({
  selector: 'app-create-meeting',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './create-meeting.component.html',
  styleUrl: './create-meeting.component.css'
})
export class CreateMeetingComponent {
  meetingForm: FormGroup;
  loading = false;
  errorMessage = '';
  successMessage = '';
  meetingCode = '';
  createdMeetingId?: number;

  constructor(
    private fb: FormBuilder,
    private meetingService: MeetingService,
    private router: Router
  ) {
    this.meetingForm = this.fb.group({
      title: ['', [Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]]
    });
  }

  onSubmit(): void {
    if (this.meetingForm.invalid) {
      this.meetingForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const formValue = this.meetingForm.getRawValue();
    const request: CreateMeetingRequest = {
      title: formValue.title?.trim() || undefined,
      description: formValue.description?.trim() || undefined
    };

    this.meetingService.createMeeting(request).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.success && response.data) {
          this.createdMeetingId = response.data.id;
          this.meetingCode = response.data.meetingCode;
          this.successMessage = response.message || 'Meeting created successfully';
          return;
        }

        this.errorMessage = response.message || 'Failed to create meeting';
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = error.error?.message || 'Failed to create meeting';
      }
    });
  }

  joinMeeting(): void {
    if (this.createdMeetingId) {
      this.router.navigate(['/meeting', this.createdMeetingId]);
    }
  }

  copyMeetingCode(): void {
    if (this.meetingCode) {
      navigator.clipboard.writeText(this.meetingCode);
    }
  }

  getErrorMessage(fieldName: string): string {
    const field = this.meetingForm.get(fieldName);
    if (!field) {
      return '';
    }

    if (field.hasError('required')) {
      return `${this.formatFieldName(fieldName)} is required`;
    }
    if (field.hasError('maxlength')) {
      return `${this.formatFieldName(fieldName)} is too long`;
    }

    return '';
  }

  private formatFieldName(fieldName: string): string {
    return fieldName
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, char => char.toUpperCase());
  }
}
