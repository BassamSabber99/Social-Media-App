import { Component, OnDestroy, OnInit, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FeedService } from '../../services/feed.service';
import { Subscription } from 'rxjs';
import { PostDto, CommentDto } from '../../services/contracts';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TextareaModule } from 'primeng/textarea';
import { AvatarModule } from 'primeng/avatar';
import { DividerModule } from 'primeng/divider';
import { SkeletonModule } from 'primeng/skeleton';
import { NavbarComponent } from '../../../shared/navbar/navbar.component';
import { LucideAngularModule, Heart, MessageCircle, Send, Trash2, ChevronDown, ChevronUp, UserPlus, UserMinus } from 'lucide-angular';
import { UserService } from '../../../services/user.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-feed-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    TextareaModule,
    AvatarModule,
    DividerModule,
    SkeletonModule,
    NavbarComponent,
    LucideAngularModule
  ],
  templateUrl: './feed-page.component.html',
  styleUrl: './feed-page.component.scss'
})
export class FeedPageComponent implements OnInit, OnDestroy {
  readonly posts = signal<PostDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly canLoadMore = computed(() => this.posts().length < this.totalCount);
  readonly skeletons = Array(3).fill(0);
  isLoadingMore = false;

  // Icons
  readonly Heart = Heart;
  readonly MessageCircle = MessageCircle;
  readonly Send = Send;
  readonly Trash2 = Trash2;
  readonly ChevronDown = ChevronDown;
  readonly ChevronUp = ChevronUp;
  readonly UserPlus = UserPlus;
  readonly UserMinus = UserMinus;

  draft = '';
  commentDrafts: { [postId: string]: string } = {};
  expandedComments: { [postId: string]: boolean } = {};
  postComments: { [postId: string]: CommentDto[] } = {};
  loadingComments: { [postId: string]: boolean } = {};

  private readonly subscriptions: Subscription[] = [];
  private skip = 0;
  private readonly take = 10;
  private totalCount = Number.MAX_SAFE_INTEGER;

  constructor(
    private readonly feedService: FeedService,
    private readonly userService: UserService,
    private readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadFeed();
    this.subscriptions.push(
      this.feedService.feedUpdates$.subscribe(() => {
        this.reloadFeed();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }

  submitPost(): void {
    if (!this.draft.trim()) {
      return;
    }

    this.loading.set(true);
    const content = this.draft;
    this.draft = '';

    const sub = this.feedService.createPost({ content, imageUrl: undefined }).subscribe({
      next: () => {
        this.loading.set(false);
        this.reloadFeed();
      },
      error: (err) => {
        console.error('Failed to create post', err);
        this.error.set('Failed to create post. Please try again.');
        this.loading.set(false);
        this.draft = content; // Restore draft on error
      }
    });

    this.subscriptions.push(sub);
  }

  toggleLike(post: PostDto): void {
    const isLiked = post.isLikedByRequester;
    const action = isLiked 
      ? this.feedService.unlikePost(post.id) 
      : this.feedService.likePost(post.id);

    // Optimistically update UI
    post.isLikedByRequester = !isLiked;
    post.likeCount += isLiked ? -1 : 1;

    const sub = action.subscribe({
      error: (err) => {
        // Revert on error
        post.isLikedByRequester = isLiked;
        post.likeCount += isLiked ? 1 : -1;
        console.error('Failed to toggle like', err);
      }
    });

    this.subscriptions.push(sub);
  }

  reloadFeed(): void {
    this.skip = 0;
    this.posts.set([]);
    this.fetchFeed();
  }

  @HostListener('window:scroll', ['$event'])
  onScroll(): void {
    const scrollPosition = window.pageYOffset + window.innerHeight;
    const pageHeight = document.documentElement.scrollHeight;
    
    // Trigger when 200px from bottom
    if (scrollPosition >= pageHeight - 200 && !this.isLoadingMore && this.canLoadMore() && !this.loading()) {
      this.loadMore();
    }
  }

  loadMore(): void {
    if (!this.canLoadMore() || this.isLoadingMore) {
      return;
    }

    this.isLoadingMore = true;
    this.skip += this.take;
    this.fetchFeed(true);
  }

  private loadFeed(): void {
    this.skip = 0;
    this.fetchFeed();
  }

  toggleComments(post: PostDto): void {
    this.expandedComments[post.id] = !this.expandedComments[post.id];
    
    if (this.expandedComments[post.id] && !this.postComments[post.id]) {
      this.loadComments(post.id);
    }
  }

  loadComments(postId: string): void {
    this.loadingComments[postId] = true;
    const sub = this.feedService.getComments(postId, 0, 50).subscribe({
      next: response => {
        this.postComments[postId] = response.items;
        this.loadingComments[postId] = false;
      },
      error: err => {
        console.error('Failed to load comments', err);
        this.loadingComments[postId] = false;
      }
    });
    this.subscriptions.push(sub);
  }

  submitComment(post: PostDto): void {
    const content = this.commentDrafts[post.id];
    if (!content?.trim()) return;

    const sub = this.feedService.createComment(post.id, content).subscribe({
      next: comment => {
        if (!this.postComments[post.id]) {
          this.postComments[post.id] = [];
        }
        this.postComments[post.id].unshift(comment);
        post.commentCount++;
        this.commentDrafts[post.id] = '';
      },
      error: err => {
        console.error('Failed to create comment', err);
      }
    });
    this.subscriptions.push(sub);
  }

  deleteComment(postId: string, commentId: string): void {
    const sub = this.feedService.deleteComment(postId, commentId).subscribe({
      next: () => {
        this.postComments[postId] = this.postComments[postId].filter(c => c.id !== commentId);
        const post = this.posts().find(p => p.id === postId);
        if (post) post.commentCount--;
      },
      error: err => {
        console.error('Failed to delete comment', err);
      }
    });
    this.subscriptions.push(sub);
  }

  toggleFollowAuthor(post: PostDto): void {
    const action = post.isAuthorFollowedByRequester
      ? this.userService.unfollowUser(post.authorId)
      : this.userService.followUser(post.authorId);

    // Optimistic update
    post.isAuthorFollowedByRequester = !post.isAuthorFollowedByRequester;

    const sub = action.subscribe({
      error: err => {
        // Revert on error
        post.isAuthorFollowedByRequester = !post.isAuthorFollowedByRequester;
        console.error('Failed to follow/unfollow', err);
      }
    });
    this.subscriptions.push(sub);
  }

  private fetchFeed(append = false): void {
    this.loading.set(true);
    const sub = this.feedService
      .getFeed(this.skip, this.take)
      .subscribe({
        next: response => {
          this.totalCount = response.totalCount;
          this.posts.set(append ? [...this.posts(), ...response.items] : response.items);
          this.loading.set(false);
          this.error.set(null);
          this.isLoadingMore = false;
        },
        error: err => {
          console.error('Failed to load feed', err);
          this.error.set('Unable to load feed. Please try again later.');
          this.loading.set(false);
          this.isLoadingMore = false;
        }
      });

    this.subscriptions.push(sub);
  }
  GetUserId(): string {
    return this.authService.getUserId() ?? '';
  }
}
