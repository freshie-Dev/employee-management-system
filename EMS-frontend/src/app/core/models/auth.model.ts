export interface LoginRequest { username: string; password: string; }
export interface AuthSession {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  userId: number;
  username: string;
  roles: string[];
}
