export interface PostDto {
  id: string;
  authorId: string;
  authorUserName: string;
  authorDisplayName: string;
  content: string;
  imageUrl?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  commentCount: number;
  likeCount: number;
  isLikedByRequester: boolean;
  isAuthorFollowedByRequester: boolean;
}

export interface FeedResponseDto {
  items: PostDto[];
  totalCount: number;
}

export interface CommentDto {
  id: string;
  postId: string;
  authorId: string;
  authorUserName: string;
  authorDisplayName: string;
  content: string;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface CommentsResponseDto {
  items: CommentDto[];
  totalCount: number;
}

