import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-join-meeting-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule
  ],
  templateUrl: './join-meeting-dialog.component.html',
  styleUrl: './join-meeting-dialog.component.css'
})
export class JoinMeetingDialogComponent {
  joinForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<JoinMeetingDialogComponent>
  ) {
    this.joinForm = this.fb.group({
      meetingCode: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(8)]]
    });
  }

  updateCode(value: string): void {
    this.joinForm.patchValue({
      meetingCode: value.replace(/[^A-Za-z0-9]/g, '').toUpperCase().slice(0, 8)
    }, { emitEvent: false });
  }

  cancel(): void {
    this.dialogRef.close();
  }

  submit(): void {
    if (this.joinForm.invalid) {
      this.joinForm.markAllAsTouched();
      return;
    }

    this.dialogRef.close({
      meetingCode: this.joinForm.get('meetingCode')?.value?.trim().toUpperCase()
    });
  }
}
