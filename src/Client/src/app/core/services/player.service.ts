import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { CreatePlayerRequest } from '../dtos/create-player-request';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PlayerService {
  private playersBaseAddress: string;

  constructor(private http: HttpClient) {
    this.playersBaseAddress = `${environment.apiUrl}/players`;
  }

  register(createPlayerRequest: CreatePlayerRequest): Observable<void> {
    let res = this.http.post<void>(
      `${this.playersBaseAddress}`,
      createPlayerRequest
    );

    return res;
  }
}
