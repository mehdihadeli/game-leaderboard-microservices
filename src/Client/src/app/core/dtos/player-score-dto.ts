export interface PlayerScoreDto {
  playerId: string;
  score: number;
  leaderBoardName: string;
  rank?: number | null;
  firstName: string;
  lastName: string;
  country: string;
}

export interface PlayerScoreWithNeighborsDto {
  previous?: PlayerScoreDto | null;
  currentPlayerScore: PlayerScoreDto;
  next?: PlayerScoreDto | null;
}
