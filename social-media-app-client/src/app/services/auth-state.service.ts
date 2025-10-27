import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { User } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthStateService {
  private loadingSubject = new BehaviorSubject<boolean>(false);
  public loading$ = this.loadingSubject.asObservable();

  private errorSubject = new BehaviorSubject<string | null>(null);
  public error$ = this.errorSubject.asObservable();

  setLoading(loading: boolean): void {
    this.loadingSubject.next(loading);
  }

  setError(error: string | null): void {
    this.errorSubject.next(error);
  }

  clearError(): void {
    this.errorSubject.next(null);
  }
}

