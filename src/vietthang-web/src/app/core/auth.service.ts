import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, UserInfo } from './models';

const TOKEN_KEY = 'vt_token';
const USER_KEY = 'vt_user';

/** Xác thực JWT: lưu token ở localStorage, expose signal user để header/guard theo dõi. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  readonly token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  readonly user = signal<UserInfo | null>(readUser());
  readonly isLoggedIn = computed(() => !!this.token());

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/login', { email, password }).pipe(tap(r => this.store(r)));
  }

  register(data: { fullName: string; email: string; phoneNumber: string; password: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/register', data).pipe(tap(r => this.store(r)));
  }

  logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.token.set(null);
    this.user.set(null);
  }

  /** Cập nhật tên hiển thị sau khi sửa hồ sơ. */
  patchUser(patch: Partial<UserInfo>) {
    const u = this.user();
    if (!u) return;
    const next = { ...u, ...patch };
    localStorage.setItem(USER_KEY, JSON.stringify(next));
    this.user.set(next);
  }

  private store(r: AuthResponse) {
    localStorage.setItem(TOKEN_KEY, r.token);
    localStorage.setItem(USER_KEY, JSON.stringify(r.user));
    this.token.set(r.token);
    this.user.set(r.user);
  }
}

function readUser(): UserInfo | null {
  try { const raw = localStorage.getItem(USER_KEY); return raw ? JSON.parse(raw) as UserInfo : null; }
  catch { return null; }
}
