import { Component, OnInit } from '@angular/core';
import { SignalRService } from './core/services/signalr.service';
import { LoginResponse } from './core/dtos/login-response';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  loginResponse?: LoginResponse | null;

  ngOnInit(): void {}
}
