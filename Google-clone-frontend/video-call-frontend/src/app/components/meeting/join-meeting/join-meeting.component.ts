import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MeetingService } from '../../../services/meeting.service';

@Component({
  selector: 'app-join-meeting',
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
    MatProgressSpinnerModule
  ],
  templateUrl: './join-meeting.component.html',
  styleUrls: ['./join-meeting.component.css']
})
export class JoinMeetingComponent implements OnInit {
  joinForm: FormGroup;
  loading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private meetingService: MeetingService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.joinForm = this.fb.group({
      meetingCode: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(8)]]
    });
  }

  ngOnInit(): void {
    const meetingCode = this.route.snapshot.paramMap.get('meetingCode');
    if (!meetingCode) {
      return;
    }

    this.joinForm.patchValue({
      meetingCode: meetingCode.toUpperCase()
    });

    this.onSubmit();
  }

  onSubmit(): void {
    if (this.joinForm.valid) {
      this.loading = true;
      this.errorMessage = '';

      const meetingCode = this.joinForm.value.meetingCode.toUpperCase();

      // First, verify the meeting exists
      this.meetingService.getMeetingByCode(meetingCode).subscribe({
        next: (response) => {
          this.loading = false;
          if (response.success && response.data) {
            // Navigate to meeting room
            this.router.navigate(['/meeting', response.data.id]);
          } else {
            this.errorMessage = response.message || 'Meeting not found';
          }
        },
        error: (error) => {
          this.loading = false;
          this.errorMessage = error.error?.message || 'Invalid meeting code';
        }
      });
    }
  }

  formatMeetingCode(): void {
    const control = this.joinForm.get('meetingCode');
    if (control) {
      const value = control.value.replace(/[^A-Za-z0-9]/g, '').toUpperCase();
      control.setValue(value, { emitEvent: false });
    }
  }

  getErrorMessage(): string {
    const field = this.joinForm.get('meetingCode');
    
    if (field?.hasError('required')) {
      return 'Meeting code is required';
    }
    if (field?.hasError('minlength') || field?.hasError('maxlength')) {
      return 'Meeting code must be 8 characters';
    }
    
    return '';
  }
}
