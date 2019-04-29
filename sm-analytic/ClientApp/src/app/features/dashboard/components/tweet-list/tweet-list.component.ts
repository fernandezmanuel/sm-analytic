import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-tweet-list',
  templateUrl: './tweet-list.component.html',
  styleUrls: ['./tweet-list.component.scss']
})
export class TweetListComponent implements OnInit {

  @Input() title: string = '';
  @Input() subTitle: string = '';
  @Input() tweets: any;
  @Input() header: any;

  constructor() { }

  ngOnInit() {

    console.log(this.tweets);

    this.header = {
      text: "Tweet",
      score: "# of Likes/Rank"
    }
  }

}
