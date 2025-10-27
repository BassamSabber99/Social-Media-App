import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environment';
import { Observable, Subject } from 'rxjs';
import { map } from 'rxjs/operators';
import { FeedResponseDto, PostDto, CommentDto, CommentsResponseDto } from '../services/contracts';

@Injectable({ providedIn: 'root' })
export class FeedService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;
  private readonly feedUpdateSubject = new Subject<void>();

  readonly feedUpdates$ = this.feedUpdateSubject.asObservable();

  getFeed(skip: number, take: number): Observable<FeedResponseDto> {
    const url = `${this.baseUrl}/posts?skip=${skip}&take=${take}`;
    return this.http.get<PostDto[]>(url, { observe: 'response' }).pipe(
      map(response => ({
        items: response.body ?? [],
        totalCount: Number(response.headers.get('X-Total-Count') ?? (response.body?.length ?? 0))
      }))
    );
  }

  createPost(payload: { content: string; imageUrl?: string }): Observable<PostDto> {
    const url = `${this.baseUrl}/posts`;
    return this.http.post<PostDto>(url, payload).pipe(
      map(post => {
        this.feedUpdateSubject.next();
        return post;
      })
    );
  }

  likePost(postId: string): Observable<void> {
    const url = `${this.baseUrl}/posts/${postId}/like`;
    return this.http.post<void>(url, {});
  }

  unlikePost(postId: string): Observable<void> {
    const url = `${this.baseUrl}/posts/${postId}/like`;
    return this.http.delete<void>(url);
  }

  getComments(postId: string, skip: number, take: number): Observable<CommentsResponseDto> {
    const url = `${this.baseUrl}/posts/${postId}/comments?skip=${skip}&take=${take}`;
    return this.http.get<CommentDto[]>(url, { observe: 'response' }).pipe(
      map(response => ({
        items: response.body ?? [],
        totalCount: Number(response.headers.get('X-Total-Count') ?? (response.body?.length ?? 0))
      }))
    );
  }

  createComment(postId: string, content: string): Observable<CommentDto> {
    const url = `${this.baseUrl}/posts/${postId}/comments`;
    return this.http.post<CommentDto>(url, { content }).pipe(
      map(comment => {
        this.feedUpdateSubject.next();
        return comment;
      })
    );
  }

  deleteComment(postId: string, commentId: string): Observable<void> {
    const url = `${this.baseUrl}/posts/${postId}/comments/${commentId}`;
    return this.http.delete<void>(url);
  }
}

