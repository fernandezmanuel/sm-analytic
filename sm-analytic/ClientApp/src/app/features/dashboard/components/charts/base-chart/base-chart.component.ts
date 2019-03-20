import { Component, Input, SimpleChanges, OnInit, OnChanges } from '@angular/core';
import { TwitterDataService } from 'app/shared/services/twitter-data.service';
import { Subscription } from 'rxjs/Subscription';
import { Observable } from 'rxjs/Rx';

@Component({
  selector: 'app-base-chart',
  templateUrl: './base-chart.component.html'
})
export class BaseChartComponent implements OnChanges, OnInit {

  @Input() title: string = '';
  @Input() subTitle: string = '';
  @Input() chartLabels: Array<String> = [];
  @Input() chartData: Array<Object> = [];
  @Input() chartLegend: boolean = true;
  @Input() chartType: string = '';
  @Input() chartOptions: Object = {};
  private twitterDataUpdateRef: Subscription = null;

  hasData: boolean = false;

  constructor(private twitterDataService: TwitterDataService) {

    this.chartOptions = {
      scaleShowVerticalLines: false,
      responsive: true
    };

  }

  ngOnInit() {
    this.hasData = this.chartDataHasData();
  }

  ngOnChanges(changes: SimpleChanges) {
    this.ngOnInit();
  }

  chartDataHasData() {

    console.log(this.chartData);

    if (!this.chartData) return false;

    var hasData = true;

    function elementHasData(ele) {

      var hasData = true;

      if (ele.data.length == 0) {
        hasData = false;
      } else {
        hasData = ele.data.every((x) => { return x[0] == 0 });
      }
      
      return hasData;

    }

    if (this.chartData.length == 0) {
      hasData = false;
    } else if (this.chartData.every(elementHasData)) {
      hasData = false;
    }

    console.log(hasData);

    return hasData;

  }

}

