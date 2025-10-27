import { Component, OnDestroy, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService, UserDto } from '../../../services/user.service';
import { ChatService } from '../../services/chat.service';
import { Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { SkeletonModule } from 'primeng/skeleton';
import { NavbarComponent } from '../../../shared/navbar/navbar.component';
import { LucideAngularModule, Search, UserPlus, UserMinus, Users, MessageCircle } from 'lucide-angular';

@Component({
  selector: 'app-user-discovery',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardModule,
    InputTextModule,
    ButtonModule,
    AvatarModule,
    SkeletonModule,
    NavbarComponent,
    LucideAngularModule
  ],
  templateUrl: './user-discovery.component.html',
  styleUrl: './user-discovery.component.scss'
})
export class UserDiscoveryComponent implements OnDestroy {
  readonly users = signal<UserDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  
  // Icons
  readonly Search = Search;
  readonly UserPlus = UserPlus;
  readonly UserMinus = UserMinus;
  readonly Users = Users;
  readonly MessageCircleIcon = MessageCircle;

  searchQuery = '';
  isLoadingMore = false;
  private readonly searchSubject = new Subject<string>();
  private readonly subscriptions: Subscription[] = [];
  private skip = 0;
  private readonly take = 20;
  private hasMore = true;
  private currentQuery = '';

  constructor(
    private readonly userService: UserService,
    private readonly chatService: ChatService,
    private readonly router: Router
  ) {
    this.setupSearch();
  }

  ngOnDestroy(): void {
    this.searchSubject.complete();
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  private setupSearch(): void {
    const sub = this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(query => {
        if (query.trim().length >= 2) {
          this.skip = 0;
          this.hasMore = true;
          this.currentQuery = query;
          this.performSearch(query, false);
        } else {
          this.users.set([]);
          this.currentQuery = '';
        }
      });

    this.subscriptions.push(sub);
  }

  onSearchChange(): void {
    this.searchSubject.next(this.searchQuery);
  }

  @HostListener('window:scroll', ['$event'])
  onScroll(): void {
    if (!this.currentQuery) return;
    
    const scrollPosition = window.pageYOffset + window.innerHeight;
    const pageHeight = document.documentElement.scrollHeight;
    
    // Trigger when 200px from bottom
    if (scrollPosition >= pageHeight - 200 && !this.isLoadingMore && this.hasMore && !this.loading()) {
      this.loadMore();
    }
  }

  private loadMore(): void {
    if (!this.hasMore || this.isLoadingMore) return;
    
    this.isLoadingMore = true;
    this.skip += this.take;
    this.performSearch(this.currentQuery, true);
  }

  private performSearch(query: string, append = false): void {
    this.loading.set(!append);
    this.error.set(null);

    const sub = this.userService.searchUsers(query, this.skip, this.take).subscribe({
      next: users => {
        if (users.length < this.take) {
          this.hasMore = false;
        }
        this.users.set(append ? [...this.users(), ...users] : users);
        this.loading.set(false);
        this.isLoadingMore = false;
      },
      error: err => {
        console.error('Search failed', err);
        this.error.set('Failed to search users');
        this.loading.set(false);
        this.isLoadingMore = false;
      }
    });

    this.subscriptions.push(sub);
  }

  toggleFollow(user: UserDto): void {
    const action = user.isFollowedByRequester
      ? this.userService.unfollowUser(user.id)
      : this.userService.followUser(user.id);

    // Optimistic update
    user.isFollowedByRequester = !user.isFollowedByRequester;
    user.followersCount += user.isFollowedByRequester ? 1 : -1;

    const sub = action.subscribe({
      error: err => {
        // Revert on error
        user.isFollowedByRequester = !user.isFollowedByRequester;
        user.followersCount += user.isFollowedByRequester ? 1 : -1;
        console.error('Follow/unfollow failed', err);
      }
    });

    this.subscriptions.push(sub);
  }

  getUserInitial(displayName: string): string {
    return displayName.charAt(0).toUpperCase();
  }

  messageUser(user: UserDto): void {
    const sub = this.chatService.getOrCreateChat(user.id).subscribe({
      next: () => {
        this.router.navigate(['/chat']);
      },
      error: err => {
        console.error('Failed to create chat', err);
      }
    });

    this.subscriptions.push(sub);
  }
}
