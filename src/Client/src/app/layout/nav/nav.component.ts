import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import {
  faToggleOn,
  faToggleOff,
  faSpinner,
  faIdCard,
  faCoins,
  faWeightHanging,
  faBoxes,
  faPencilAlt,
  faSort,
  faSortUp,
  faSortDown,
  faPlus,
  faTrashAlt,
  faHome,
  IconDefinition,
  faBolt,
  faSignIn,
  faSignOut,
  faUser,
} from '@fortawesome/free-solid-svg-icons';
import { faGithub } from '@fortawesome/free-brands-svg-icons';
import { AuthenticationService } from '@app/core/services';
import { LoginResponse } from '@app/core/dtos/login-response';

interface NavItem {
  icon: IconDefinition;
  title: string;
  link: string;
}

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.scss'],
})
export class NavComponent implements OnInit {
  public repoUrl = 'https://github.com/mehdihadeli';
  loginResponse?: LoginResponse | null;

  fa = {
    toggleOn: faToggleOn,
    home: faHome,
    toggleOff: faToggleOff,
    spinner: faSpinner,
    idCard: faIdCard,
    coins: faCoins,
    weightHanging: faWeightHanging,
    boxes: faBoxes,
    pencil: faPencilAlt,
    sort: faSort,
    sortUp: faSortUp,
    sortDown: faSortDown,
    plus: faPlus,
    trash: faTrashAlt,
    github: faGithub,
    bolt: faBolt,
    signin: faSignIn,
    signup: faUser,
    signout: faSignOut,
    user: faUser,
  };

  navItems: NavItem[] = [];
  logoName: string = 'LeaderBoard';

  constructor(private authenticationService: AuthenticationService) {
    this.authenticationService.loginResponse.subscribe(
      (x) => (this.loginResponse = x)
    );
  }

  ngOnInit() {
    this.navItems = [
      { link: '/home', title: 'Home', icon: this.fa.home },
      {
        link: 'players/player-score',
        title: 'Player Score',
        icon: this.fa.pencil,
      },
    ];
  }

  logout() {
    this.authenticationService.logout().subscribe();
  }
}
