export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  profilePictureUrl?: string | null;
  token: string;
  expiration: Date;
}

export interface User {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  profilePictureUrl?: string | null;
}

export interface ProfileImageResponse {
  profileImageUrl?: string | null;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}
