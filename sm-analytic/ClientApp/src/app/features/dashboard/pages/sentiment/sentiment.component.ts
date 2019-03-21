import { Component, OnInit } from '@angular/core';
import { EngagementService } from 'app/shared/services/engagement.service';
import { TwitterDataService } from 'app/shared/services/twitter-data.service';
import { Subscription } from 'rxjs/Subscription';

@Component({
  selector: 'app-sentiment',
  templateUrl: './sentiment.component.html',
  styleUrls: ['./sentiment.component.scss']
})

export class SentimentComponent implements OnInit {

  mentions: any;
  ranking: any;
  private twitterDataUpdateRef: Subscription = null;

  constructor(
    private engagementService: EngagementService,
    private twitterDataService: TwitterDataService) { }



  ngOnInit() {



  }


  drawSentimentAnalysis() {
    this.mentions = this.twitterDataService.mentions;
    this.drawOverallSentiment;
    this.drawLatestAnalyzedComments
  }

  drawOverallSentiment() {



  }

  drawLatestAnalyzedComments() {


  }

}
