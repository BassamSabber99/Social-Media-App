export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
  displayName: string;
}

export interface AuthResponse {
  token: string;
  userId: string;
  userName: string;
  email: string;
  displayName: string;
}

export interface User {
  userId: string;
  userName: string;
  email: string;
  displayName: string;
}

