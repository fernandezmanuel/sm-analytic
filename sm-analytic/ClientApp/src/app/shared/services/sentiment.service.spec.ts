import { TestBed, inject } from '@angular/core/testing';

import { SentimentService } from './sentiment.service';

describe('SentimentService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SentimentService]
    });
  });

  it('should be created', inject([SentimentService], (service: SentimentService) => {
    expect(service).toBeTruthy();
  }));
});
