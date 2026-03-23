import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-profile-settings-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './profile-settings-dialog.component.html',
  styleUrl: './profile-settings-dialog.component.css'
})
export class ProfileSettingsDialogComponent {
  loading = false;
  errorMessage = '';
  successMessage = '';

  constructor(private authService: AuthService) {}

  get currentUserName(): string {
    const currentUser = this.authService.currentUserValue;
    return `${currentUser?.firstName ?? ''} ${currentUser?.lastName ?? ''}`.trim() || 'Guest';
  }

  get profileImageUrl(): string | null {
    return this.authService.resolveAssetUrl(this.authService.currentUserValue?.profilePictureUrl ?? null);
  }

  get initials(): string {
    return this.currentUserName.charAt(0).toUpperCase() || 'U';
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';

    if (!file) {
      return;
    }

    const allowedTypes = ['image/jpeg', 'image/png'];
    if (!allowedTypes.includes(file.type)) {
      this.errorMessage = 'Only JPG, JPEG, and PNG images are allowed';
      this.successMessage = '';
      return;
    }

    if (file.size > 2 * 1024 * 1024) {
      this.errorMessage = 'Profile image must be 2MB or smaller';
      this.successMessage = '';
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.uploadProfileImage(file).subscribe({
      next: response => {
        this.loading = false;
        if (response.success) {
          this.successMessage = 'Profile picture updated';
          return;
        }

        this.errorMessage = response.message || 'Failed to upload profile picture';
      },
      error: error => {
        this.loading = false;
        this.errorMessage = error.error?.message || 'Failed to upload profile picture';
      }
    });
  }

  removeProfileImage(): void {
    if (!this.authService.currentUserValue?.profilePictureUrl || this.loading) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.removeProfileImage().subscribe({
      next: response => {
        this.loading = false;
        if (response.success) {
          this.successMessage = 'Profile picture removed';
          return;
        }

        this.errorMessage = response.message || 'Failed to remove profile picture';
      },
      error: error => {
        this.loading = false;
        this.errorMessage = error.error?.message || 'Failed to remove profile picture';
      }
    });
  }
}
