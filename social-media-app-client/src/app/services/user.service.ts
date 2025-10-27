import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environment';
import { Observable } from 'rxjs';

export interface UserDto {
  id: string;
  userName: string;
  email: string;
  displayName: string;
  bio: string;
  profileImageUrl?: string;
  createdAtUtc: string;
  followersCount: number;
  followingCount: number;
  isFollowedByRequester: boolean;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  searchUsers(query: string, skip: number = 0, take: number = 20): Observable<UserDto[]> {
    const url = `${this.baseUrl}/users/search?query=${encodeURIComponent(query)}&skip=${skip}&take=${take}`;
    return this.http.get<UserDto[]>(url);
  }

  followUser(userId: string): Observable<void> {
    const url = `${this.baseUrl}/users/${userId}/follow`;
    return this.http.post<void>(url, {});
  }

  unfollowUser(userId: string): Observable<void> {
    const url = `${this.baseUrl}/users/${userId}/follow`;
    return this.http.delete<void>(url);
  }
}

