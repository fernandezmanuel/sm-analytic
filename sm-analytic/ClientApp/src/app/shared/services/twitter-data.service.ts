import { Injectable } from '@angular/core';
import { ApiService } from 'app/shared/services/api.service';
import { Subject } from 'rxjs/Subject';

@Injectable()
export class TwitterDataService {

  public tweets;
  public userData;
  public followers;
  public mentions;
  public hashtagCount;
  public searchedHashtags;
  public rank;
  public mentionList;
  public sentiment;
  public updated = new Subject<void>();
  public mentionCreatedBy;
  public counter: number = -1;
  constructor(private apiService: ApiService) { }

  ngOnInit() {}

    /*
   * Sends request to 'TwitterAuth' endpoint in our API
   * which returns a url that this function will redirect to
   * so that the user can authorize our app to use their credentials
   */
  twitterAuth() {
    this.apiService.get('TwitterAuth')
      .subscribe(
        val => window.location.href = val,
        error => console.log(error)
      );
  }

    /* 
   * After the user authorizes our app to use their Twitter account
   * this function will run and send a new request to our .NET API at
   * the 'ValidateTwitterAuth' endpoint, and returns the user's Twitter info
   * if they've successfully been authenticated
   */
  authorizeUser() {
    var requestBody = {};
    if (localStorage.getItem("authorization_id") == null) {
      var queryParams = window.location.search;

      if (queryParams) {

        var args = queryParams.split("&");

        args.forEach((arg) => {
          var argPair = arg.split("=");
          requestBody[argPair[0]] = argPair[1];

          localStorage.setItem(argPair[0].replace('?', ''), argPair[1]);
        });
      }
    }
    else {
      requestBody = {
        "authorization_id": localStorage.getItem("authorization_id"),
        "oauth_token": localStorage.getItem("oauth_token"),
        "oauth_verifier": localStorage.getItem("oauth_verifier")
      };
    }

      this.apiService.post('ValidateTwitterAuth', requestBody)
        .subscribe(
        val => {
          console.log(val);
          // taking array of json objects
          // and stores them into variables that will be accessed elsewhere in the
          // application
          this.userData = val[0].value;
          this.tweets = val[1].value;
          this.followers = val[2].value;
          this.mentions = val[3].value;
          this.hashtagCount = val[4].value;
          this.searchedHashtags = val[5].value;
          this.rank = val[6].value;
          this.mentionCreatedBy = val[8].value;
          this.mentionList = val[7].value;
          this.createSentimentObject();
          this.hashtagCount = val[3].value;
          this.searchedHashtags = val[4].value;
          this.mentions = val[5].value;
          this.updated.next();
          localStorage.setItem('userData', JSON.stringify(this.userData));
          localStorage.setItem('tweets', JSON.stringify(this.tweets));
        },
          error => {
            console.log(error)
          }
        );
    }

  createSentimentObject() {
    let mentions = Object.values(this.mentionList);
    var user = Object.values(this.mentionCreatedBy);
    var score = Object.values(this.rank);

    var newSentiment = [];
    var i = 0;

    for (let i = 0; i < Object.keys(this.mentionList).length; ++i) {
      newSentiment.push(JSON.stringify({
        "user": user[i],
        "mention": mentions[i],
        "score": score[i]
      }));
    }
    console.log(newSentiment);
 }

  clearSession() {
    this.userData = {};
    this.tweets = [];
    this.followers = [];
    window.location.reload();
  }

  hasData() {
    return this.userData;
  }

  hasNoData() {
    return !this.userData;
  }

  isAuthorize() {

  }
}
