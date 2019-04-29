import { Component, OnInit } from '@angular/core';
import { EngagementService } from 'app/shared/services/engagement.service';
import { TwitterDataService } from 'app/shared/services/twitter-data.service';
import { Subscription } from 'rxjs/Subscription';
import { Console } from '@angular/core/src/console';

@Component({
  selector: 'app-sentiment',
  templateUrl: './sentiment.component.html',
  styleUrls: ['./sentiment.component.scss']
})

export class SentimentComponent implements OnInit {
  ovrSentiment: number;
  mentions: any;
  ranking: any;
  newSentiment: any;
  private twitterDataUpdateRef: Subscription = null;

  constructor(
    private engagementService: EngagementService,
    private twitterDataService: TwitterDataService
  ) { }


  ngOnInit() {
    this.mentions = this.twitterDataService.mentions;
    this.newSentiment = this.twitterDataService.newSentiment;
    this.ovrSentiment = Math.trunc(Number(this.twitterDataService.overallSentiment));
    
    console.log(this.ovrSentiment);
    // console.log(this.newSentiment);
  }


  drawSentimentAnalysis() {

    this.drawOverallSentiment;
    this.drawLatestAnalyzedComments
  }

  drawOverallSentiment() {



  }

  drawLatestAnalyzedComments() {


  }

}
