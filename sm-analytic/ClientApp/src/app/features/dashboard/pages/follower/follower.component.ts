import { Component, OnInit } from '@angular/core';
import { EngagementService } from 'app/shared/services/engagement.service';
import { FollowersService } from 'app/shared/services/followers.service';
import { TwitterDataService } from 'app/shared/services/twitter-data.service';
import { Subscription } from 'rxjs/Subscription';

/*
 * Follower page component
 * Stores objects that are passsed to all chart
 * components on its page
 */
@Component({
  selector: 'app-follower',
  templateUrl: './follower.component.html',
  styleUrls: ['./follower.component.scss']
})
export class FollowerComponent implements OnInit {

  // data for each component
  engagementByHourData: any;
  engagementByDayData: any;
  engagementTotal: any;
  followerJoinedAt: any;

  // Subscriber object for listening to updates
  // to twitter data obtained from backend
  twitterDataUpdateRef: Subscription = null;

  // init services
  constructor(
    private engagementService: EngagementService,
    private twitterDataService: TwitterDataService,
    private followersService: FollowersService
  ) {}

  ngOnInit() {

    // this exists so that each chart data object
    // has non-null values, prevents charts from throwing erros
    // every data object should be initialized with this
    var chartObject = {
      title: '',
      subTitle: '',
      chartLabels: {},
      chartData: [],
      chartType: ''
    };

    // init each chart data object
    this.engagementByHourData = Object.create(chartObject);
    this.engagementByDayData = Object.create(chartObject);
    this.engagementTotal = Object.create(chartObject);
    this.followerJoinedAt = Object.create(chartObject);

    // loads actual twitter data into charts objects
    this.drawCharts();

    // listening for update event
    this.twitterDataUpdateRef = this.twitterDataService.updated.subscribe(() => {
      this.drawCharts();
    });

  }

  drawCharts() {

    // get twitter data from twitter-data.service.ts
    const tweets = this.twitterDataService.tweets;
    const followers = this.twitterDataService.followers;

    // do transformations on base data and add it to object
    this.engagementByHourData = this.engagementService.engagementByHourData(tweets);
    this.engagementByDayData = this.engagementService.engagementByDayData(tweets);
    this.engagementTotal = this.engagementService.engagementTotalData(tweets);
    this.followerJoinedAt = this.followersService.followerJoinedAtData(followers);

  }

}
